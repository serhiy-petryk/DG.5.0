using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using DGCore.Common;
using DGCore.Menu;
using DGCore.UserSettings;
using DGView.Controls.Printing;
using DGView.ViewModels;
using WpfSpLib.Common;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Views
{
    /// <summary>
    /// Interaction logic for MwiLeftPanelView.xaml
    /// </summary>
    public partial class MwiLeftPanelView : UserControl, INotifyPropertyChanged
    {
        private MwiContainer Host => MwiContainer.GetMwiContainer(this);

        public DGCore.Misc.DataDefinition DataDefinition { get; private set; }
        public bool IsCbDataSettingEnabled => CbDataSettingName.ItemsSource is IList list && list.Count > 0;
        public string ErrorText { get; private set; }

        private Visibility _filterPanelVisibility = Visibility.Collapsed;
        public Visibility FilterPanelVisibility
        {
            get => _filterPanelVisibility;
            set
            {
                _filterPanelVisibility = value;
                OnPropertiesChanged(nameof(FilterPanelVisibility));
            }
        }

        public MwiLeftPanelView()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void LeftPanelView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                RootMenu rootMenu;
                try
                {
                    // Load menu items
                    rootMenu = new RootMenu();
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    if (ex is LoadJsonConfigException loadException)
                    {
                        sb.AppendLine($@"Помилка у файлі конфігурації: {loadException.FileName}");
                        if (loadException.LineNumber.HasValue)
                            sb.AppendLine($"Рядок файлу: {loadException.LineNumber}. Позиція: {loadException.Position}.");
                        sb.AppendLine(null);
                        sb.AppendLine($@"Текст помилки:");
                        sb.AppendLine(loadException.Message);
                    }
                    else
                        sb.AppendLine(ex.Message);

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        new DialogBox(DialogBox.DialogBoxKind.Error)
                        {
                            Host = Host,
                            Caption = "Помилка",
                            Message = sb.ToString()
                        }.ShowDialog();
                        Application.Current.Shutdown();
                    }), DispatcherPriority.ApplicationIdle);
                    return;
                }

                MenuTreeView.ItemsSource = rootMenu.Items;
                // Set application header
                if (!string.IsNullOrEmpty(rootMenu.ApplicationTitle) && Window.GetWindow(this) is MwiStartup app)
                    app.Title = rootMenu.ApplicationTitle;

                var mwiContainer = this.GetVisualParents().OfType<MwiContainer>().FirstOrDefault();
                var resizeThumb = mwiContainer?.GetVisualChildren().OfType<FrameworkElement>().FirstOrDefault(a => a.Name == "LeftPanelDragThumb");
                if (resizeThumb != null)
                {
                    var b = new Binding { Path = new PropertyPath("Background"), Source = this, Converter = ColorHslBrush.Instance, ConverterParameter = "+5%" };
                    resizeThumb.SetBinding(BackgroundProperty, b);
                    resizeThumb.Opacity = 1.0;
                }
            }
        }

        private void TreeViewItem_OnExpanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (item.Items.Count > 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // BringIntoView for TreeViewItem
                    var scrollViewer = item.GetVisualParents().OfType<ScrollViewer>().FirstOrDefault();
                    if (scrollViewer != null)
                    {
                        var upPoint = item.TransformToVisual(scrollViewer).Transform(new Point(0, 0)).Y + scrollViewer.VerticalOffset;
                        var downPoint = upPoint + item.ActualHeight;
                        if (scrollViewer.ActualHeight + scrollViewer.VerticalOffset < downPoint)
                        {
                            var newOffset = Math.Min(upPoint, downPoint - scrollViewer.ActualHeight);
                            scrollViewer.BeginAnimationAsync(ScrollViewerAnimator.VerticalOffsetProperty, scrollViewer.VerticalOffset, newOffset);
                            //var sb = new Storyboard();
                            //sb.Children.Add(WpfSpLib.Helpers.AnimationHelper.GetScrollViewerVerticalOffsetAnimation(scrollViewer, scrollViewer.VerticalOffset, newOffset));
                            //sb.Begin();
                        }
                    }
                }), DispatcherPriority.ContextIdle);
                e.Handled = true;
            }
        }

        private TreeViewItem _lastSelectedItem;
        private async void TreeViewItem_OnItemSelected(object sender, RoutedEventArgs e)
        {
            var tvi = e.OriginalSource as TreeViewItem;
            if (_lastSelectedItem == tvi)
                return;

            var ok = await ActivateMenuOption(tvi);
            FilterPanelVisibility = ok ? Visibility.Visible : Visibility.Collapsed;
            RefreshUI();
            e.Handled = true;
            if (!tvi.IsSelected)
            {
                _lastSelectedItem = tvi;
                tvi.Focus();
                await tvi.Dispatcher.BeginInvoke(new Action(() => _lastSelectedItem = null), DispatcherPriority.Background);
            }
        }

        private void TreeViewItem_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (!(item.DataContext is MenuOption)) // Submenu
                item.IsExpanded = !item.IsExpanded;
            e.Handled = true;
            RefreshUI();
        }

        private void MenuOption_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                e.Handled = true;
                ActionProcedure();
            }
        }

        private async Task<bool> ActivateMenuOption(TreeViewItem tvi)
        {
            var menuOption = tvi?.DataContext as MenuOption;
            if (menuOption == null)
                return false;

            // Reset error decoration of menu item
            var menuItems = MenuTreeView.GetVisualChildren().OfType<TextBlock>().Where(item => item.DataContext == menuOption);
            foreach (var item in menuItems.Where(i => i.TextDecorations.Contains(TextDecorations.Strikethrough[0])))
                item.TextDecorations = new TextDecorationCollection();

            var icons = MenuTreeView.GetVisualChildren().OfType<FrameworkElement>().Where(item => item.DataContext == menuOption && item.Name.EndsWith("Icon")).ToArray();
            icons[0].Visibility = Visibility.Collapsed;
            icons[1].Visibility = Visibility.Visible;
            FilterPanelVisibility = Visibility.Collapsed;

            var settingKeys= new List<string>();
            DGCore.Filters.FilterList whereFilter = null;
            Exception exception = null;

            await Task.Factory.StartNew(() => {
                try
                {
                    DataDefinition = menuOption.GetDataDefiniton();
                    if (DataDefinition != null)
                    {
                        var userSettingProperties = new FakeUserSettingProperties
                        {
                            SettingKind = DGViewModel.UserSettingsKind,
                            SettingKey = DataDefinition.SettingID
                        };

                        if (!string.IsNullOrEmpty(DataDefinition?.SettingID))
                            settingKeys = UserSettingsUtils.GetKeysFromDb(userSettingProperties);
                        if ((DataDefinition?.DbParameters?._parameters.Count ?? 0) == 0)
                            whereFilter = DataDefinition.WhereFilter;
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            icons[0].Visibility = Visibility.Visible;
            icons[1].Visibility = Visibility.Collapsed;

            if (exception != null)
            {
                foreach (var item in menuItems)
                    item.TextDecorations = TextDecorations.Strikethrough;
                new DialogBox(DialogBox.DialogBoxKind.Error)
                {
                    Host = Host, Caption = "Помилка", Message = exception.Message,
                    Buttons = new[] { "OK" }, Details = exception.ToString()
                }.ShowDialog();
                return false;
            }

            CbDataSettingName.SelectedIndex = -1;
            CbDataSettingName.ItemsSource = settingKeys;
            if (settingKeys.Count > 0)
                CbDataSettingName.Width = settingKeys.Max(k => ControlHelper.MeasureStringForDisplay(k, CbDataSettingName).Width) + 10.0;

            if ((DataDefinition?.DbParameters?._parameters.Count ?? 0) == 0)
            {
                DbProcedureParameterArea.Visibility = Visibility.Collapsed;
                FilterArea.Visibility = whereFilter == null ? Visibility.Collapsed : Visibility.Visible;
                ErrorText = null;
                if (whereFilter != null)
                    DbFilterView.Bind(DataDefinition.WhereFilter, DataDefinition.SettingID, ActionProcedure, null);
            }
            else
            {
                DbProcedureParameterArea.Visibility = Visibility.Visible;
                FilterArea.Visibility = Visibility.Collapsed;
                ErrorText = DataDefinition.DbParameters.GetError();
                // ToDo: Bind ParameterView & parameter list: this.pg.SelectedObject = parameters;
            }
            return true;
        }

        private void ActionProcedure()
        {
            var mo = MenuTreeView.SelectedItem as MenuOption;
            var dataDefinition = mo?.GetDataDefiniton();
            if (dataDefinition == null)
                return;

            var parameters = dataDefinition.DbParameters;
            var startUpParameters = parameters == null || parameters._parameters.Count == 0
                ? dataDefinition.WhereFilter.StringPresentation
                : dataDefinition.DbParameters.GetStringPresentation();

            var dgView = DGViewModel.CreateDataGrid(Host, mo.Label);
            dgView.ViewModel.Bind(dataDefinition.GetDataSource(dgView.ViewModel), dataDefinition.SettingID, startUpParameters, (string) CbDataSettingName.SelectedValue, null);

            Host.HideLeftPanel();
        }

        #region ============  INotifyPropertyChanged  ============
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void RefreshUI()
        {
            OnPropertiesChanged(nameof(DataDefinition), nameof(IsCbDataSettingEnabled), nameof(ErrorText));
        }
        #endregion

        private void LoadDataFromProcedure_OnClick(object sender, RoutedEventArgs e)
        {
            ActionProcedure();
        }

        private void OnTreeViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = (Grid)sender;
            var topGrid = (Grid)grid.Parent;
            var leftPanel = this.GetVisualParents().OfType<FrameworkElement>().FirstOrDefault(a => a.Name == "LeftPanelContainer");
            leftPanel?.SetCurrentValueSmart(MinWidthProperty, grid.ActualWidth + topGrid.ColumnDefinitions[2].MinWidth + 14);
        }

        private void OnMemoryUsedClick(object sender, RoutedEventArgs e) => MessageBox.Show($"Memory: {DGCore.Utils.Tips.MemoryUsedInBytes:N0} байт");

        private void OnPrintClick(object sender, RoutedEventArgs e)
        {
            new PrintPreviewWindow(new PrintContentGeneratorSample()) {Owner = Window.GetWindow(this)}.ShowDialog();
        }

        private void OnDependentObjectClick(object sender, RoutedEventArgs e)
        {
            var s = DGCore.Misc.DependentObjectManager.GetStringPresentation();
            new DialogBox(DialogBox.DialogBoxKind.Info)
            {
                Host = Host,
                Caption = "Dependent Object Manager",
                Message = s
            }.ShowDialog();
        }
    }
}
