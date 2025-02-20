using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DGCore.Common;
using DGCore.DGVList;
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

        public (bool, bool, bool, bool) GetStatusOfSortButtons()
        {
            var activeCell = DataGrid.SelectedCells.Count == 1 ? DataGrid.SelectedCells[0] : (DataGridCellInfo?)null;
            var rowValue = activeCell?.Item;
            var rowGroupLevel = rowValue is IDGVList_GroupItem groupItem ? groupItem.Level : -1;
            if (activeCell == null || rowGroupLevel == 0 || !activeCell.Value.Column.CanUserSort || string.IsNullOrEmpty(activeCell.Value.Column.SortMemberPath))
                return (false, false, false, false);

            var propertyName = activeCell.Value.Column.SortMemberPath;
            var columnGroupLevel = ViewModel.Data.Groups.FindIndex(g => string.Equals(g.PropertyDescriptor.Name,
                propertyName, StringComparison.OrdinalIgnoreCase));
            var columnGroupPropertyLevel = ViewModel.Data.Groups.FindIndex(g =>
                propertyName.StartsWith(g.PropertyDescriptor.Name + Constants.MDelimiter,
                    StringComparison.OrdinalIgnoreCase));

            if (rowGroupLevel == -1) // detail lines
            {
                if (columnGroupLevel != -1 || columnGroupPropertyLevel != -1)
                    return (false, false, false, true);
                else
                {
                    var sortDescription = ViewModel.Data.Sorts.FirstOrDefault(s =>
                        string.Equals(s.PropertyDescriptor.Name, propertyName, StringComparison.OrdinalIgnoreCase));
                    if (sortDescription == null)
                        return (true, true, false, true);
                    else
                    {
                        var asc = sortDescription.SortDirection == ListSortDirection.Ascending;
                        return (!asc, asc, true, true);
                    }
                }
            }

            var totalsIndex = ViewModel.Data.LiveTotalLines.FindIndex(l =>
                string.Equals(l.PropertyDescriptor.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (totalsIndex != -1) // column with statistical function
            {
                var sortDescription = ViewModel.Data.SortsOfGroups[rowGroupLevel - 1].FirstOrDefault(s =>
                    string.Equals(s.PropertyDescriptor.Name, propertyName, StringComparison.OrdinalIgnoreCase));
                if (sortDescription == null)
                    return (true, true, false, false); // ToDo! filter on total values
                else
                {
                    var asc = sortDescription.SortDirection == ListSortDirection.Ascending;
                    return (!asc, asc, true, false); // ToDo! filter on total values 
                }
            }

            if (columnGroupLevel == -1 && columnGroupPropertyLevel == -1) // detail columns in group row => everything is disabled
                return (false, false, false, false);

            var maxColumnGroupLevel = columnGroupLevel > columnGroupPropertyLevel
                ? columnGroupLevel
                : columnGroupPropertyLevel;

            if (maxColumnGroupLevel + 1 != rowGroupLevel) // groups of row and column are different
                return (false, false, false, maxColumnGroupLevel < rowGroupLevel);

            if (columnGroupLevel != -1) // Group id column
            {
                var asc = ViewModel.Data.Groups[columnGroupLevel].SortDirection == ListSortDirection.Ascending;
                return (!asc, asc, false, true);
            }

            { //  GroupProperty column (for example the name of group id)
                var sortDescription = ViewModel.Data.SortsOfGroups[rowGroupLevel - 1].FirstOrDefault(s =>
                    string.Equals(s.PropertyDescriptor.Name, propertyName, StringComparison.OrdinalIgnoreCase));
                if (sortDescription == null)
                    return (true, true, false, true);
                else
                {
                    var asc = sortDescription.SortDirection == ListSortDirection.Ascending;
                    return (!asc, asc, true, true);
                }
            }

            throw new Exception("Trap!!!");
        }

        private void OnDataGridSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var statuses = GetStatusOfSortButtons();
            if (BtnSortAsc.IsEnabled != statuses.Item1) BtnSortAsc.SetCurrentValueSmart(Button.IsEnabledProperty, statuses.Item1);
            if (BtnSortDesc.IsEnabled != statuses.Item2) BtnSortDesc.SetCurrentValueSmart(Button.IsEnabledProperty, statuses.Item2);
            if (BtnRemoveSort.IsEnabled != statuses.Item3) BtnRemoveSort.SetCurrentValueSmart(Button.IsEnabledProperty, statuses.Item3);
            if (BtnFilterOnValue.IsEnabled != statuses.Item4) BtnFilterOnValue.SetCurrentValueSmart(Button.IsEnabledProperty, statuses.Item4);

            ViewModel.UpdateColumnSortGlyphs();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var mwiChild = (MwiChild) Parent;
            mwiChild.InputBindings.Clear();
            mwiChild.GotFocus -= MwiChild_GotFocus;
            mwiChild.BeforeClose -= MwiChildOnBeforeClose;
            mwiChild.BeforeClose += MwiChildOnBeforeClose;

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

            ((IDGVList)ViewModel.Data).ResetBindings(); // for Detach/Atach window event
        }

        private void MwiChildOnBeforeClose(object sender, EventArgs e)
        {
            // Minimized memory leak
            var btn = this.GetVisualChildren().OfType<ToggleButton>().First();
            btn.IsChecked = true;
            var contextMenu = btn.Resources.Values.OfType<ContextMenu>().First();
            contextMenu.Width = 0;
            contextMenu.Height = 0;
            contextMenu.IsOpen = false;

            Window.GetWindow(this).Focus(); // Need because error after close detached window
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

            if (Keyboard.FocusedElement is MwiChild || Keyboard.FocusedElement is MwiStartup)
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
            if (Parent is MwiChild mwiChild2)
                mwiChild2.BeforeClose -= MwiChildOnBeforeClose;

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
