using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DGView.Helpers;
using DGView.ViewModels;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Views
{
    /// <summary>
    /// Interaction logic for DataGridView.xaml
    /// </summary>
    public partial class DataGridView : UserControl
    {
        private const bool IsVerticalScrollBarDeferred = false;
        public DGViewModel ViewModel => DataGrid.ViewModel;

        public DataGridView()
        {
            InitializeComponent();

            DataGrid.SelectedCellsChanged += OnDataGridSelectedCellsChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContext = ViewModel;
        }

        private void OnDataGridSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            ViewModel.OnPropertiesChanged(nameof(ViewModel.IsSetFilterOnValueOrSortingEnable), nameof(ViewModel.IsClearSortingEnable));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var mwiChild = (MwiChild) Parent;
            mwiChild.InputBindings.Clear();
            mwiChild.GotFocus -= MwiChild_GotFocus;

            var key = new KeyBinding(ViewModel.CmdSearch, Key.F, ModifierKeys.Control);
            mwiChild.InputBindings.Add(key);
            mwiChild.GotFocus += MwiChild_GotFocus;

            if (IsVerticalScrollBarDeferred)
            {
                UnwireScrollViewer();
                _scrollViewer = WpfSpLib.Helpers.VisualHelper.GetVisualChildren(DataGrid).OfType<ScrollViewer>()
                    .FirstOrDefault();
                WireScrollViewer();
            }
        }

        private void MwiChild_GotFocus(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.FocusedElement as FrameworkElement).GetVisualParents().OfType<DGFindTextView>().FirstOrDefault() != null)
                return;

            if (Keyboard.FocusedElement is DataGridCell || Keyboard.FocusedElement is TextBox)
            {
                var mwiChildHost = (Keyboard.FocusedElement as FrameworkElement).GetVisualParents().FirstOrDefault(o => o == this);
                if (mwiChildHost != null)
                    return;
                throw new Exception($"Trap!!! MwiChild_GotFocus is wrong");
            }

            if (Keyboard.FocusedElement is MwiChild)
            {
                var activeCell = DGHelper.GetActiveCellInfo(DataGrid);
                if (activeCell.IsValid)
                    DGHelper.GetDataGridCell(activeCell)?.Focus();
                return;
            }
            throw new Exception($"Trap2!!! MwiChild_GotFocus is wrong");
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnwireScrollViewer();
            if (this.IsElementDisposing())
            {
                DataGrid.SelectedCellsChanged -= OnDataGridSelectedCellsChanged;
                if (Parent is MwiChild mwiChild)
                {
                    mwiChild.InputBindings.Clear();
                    mwiChild.GotFocus -= MwiChild_GotFocus;
                }

                InputBindings.Clear();
                _scrollViewer = null;
                ViewModel.Dispose();
                DataGrid.Dispose();
            }
        }

        #region ==========  Event handlers of CommandBar  ==========
        private void OnGroupLevelContextMenuOpened(object sender, RoutedEventArgs e)
        {
            var cm = (ContextMenu) sender;
            cm.Items.Clear();

            var currentGroupLevel = ViewModel.Data.ExpandedGroupLevel;
            var showUpperLevels = ViewModel.Data.ShowGroupsOfUpperLevels;
            var cnt = 0;
            for (var i = 0; i < ViewModel.Data.Groups.Count; i++)
            {
                var item = new MenuItem {Header = (i + 1) + " рівень", Command=ViewModel.CmdSetGroupLevel, CommandParameter = i+1 };
                cm.Items.Add(item);
                if ((i == 0 && currentGroupLevel == 1) || (i + 1) == currentGroupLevel && showUpperLevels)
                    item.IsChecked = true;
                cnt++;
            }
            for (int i = 1; i < ViewModel.Data.Groups.Count; i++)
            {
                var item = new MenuItem { Header = (i + 1) + " рівень (не показувати рядки вищого рівня)", Command = ViewModel.CmdSetGroupLevel, CommandParameter = - (i + 1) };
                cm.Items.Add(item);
                if ((i + 1) == currentGroupLevel && !showUpperLevels)
                    item.IsChecked = true;
                cnt++;
            }

            var item2 = new MenuItem { Header = "Вся інформація", Command = ViewModel.CmdSetGroupLevel};
            cm.Items.Add(item2);
            if (currentGroupLevel == int.MaxValue && showUpperLevels)
                item2.IsChecked = true;
        }

        private void OnSetSettingsContextMenuOpened(object sender, RoutedEventArgs e)
        {
            var cm = (ContextMenu)sender;
            foreach (var mi in cm.GetVisualChildren().OfType<MenuItem>())
                mi.IsChecked = Equals(mi.Header, ViewModel.LastAppliedLayoutName);
        }
        private void OnRowViewModeContextMenuOpened(object sender, RoutedEventArgs e)
        {
            var cm = (ContextMenu)sender;
            foreach (var mi in cm.GetVisualChildren().OfType<MenuItem>())
            {
                var rowViewMode = (DGCore.Common.Enums.DGRowViewMode)Enum.Parse(typeof(DGCore.Common.Enums.DGRowViewMode), (string)mi.CommandParameter);
                mi.IsChecked = Equals(rowViewMode, ViewModel.RowViewMode);
            }
        }
        #endregion

        #region ========  ScrollViewer  =========
        private ScrollViewer _scrollViewer;
        private void WireScrollViewer()
        {
            if (_scrollViewer != null)
            {
                foreach (var bar in _scrollViewer.GetVisualChildren().OfType<ScrollBar>())
                    bar.PreviewMouseLeftButtonDown += OnScrollBarPreviewMouseLeftButtonDown;
            }
        }
        private void UnwireScrollViewer()
        {
            if (_scrollViewer != null)
            {
                foreach (var bar in _scrollViewer.GetVisualChildren().OfType<ScrollBar>())
                    bar.PreviewMouseLeftButtonDown -= OnScrollBarPreviewMouseLeftButtonDown;
                _scrollViewer = null;
            }
        }

        private void OnScrollBarPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var bar = (ScrollBar)sender;
            var sv = (ScrollViewer)bar.TemplatedParent;
            var isDeferredScrollingEnabled = bar.Orientation == Orientation.Vertical;
            if (sv.IsDeferredScrollingEnabled != isDeferredScrollingEnabled)
                sv.IsDeferredScrollingEnabled = isDeferredScrollingEnabled;
        }
        #endregion

        private void OnStopLoadingClick(object sender, RoutedEventArgs e) => ViewModel.Data.UnderlyingData.DataLoadingCancelFlag = true;
    }
}
