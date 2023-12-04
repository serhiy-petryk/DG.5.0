using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DGCore.Common;
using DGCore.Filters;
using DGCore.UserSettings;
using DGView.Helpers;
using DGView.ViewModels;
using WpfSpLib.Common;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Views
{
    /// <summary>
    /// Interaction logic for DGEditSettingsView.xaml
    /// </summary>
    public partial class DGEditSettingsView : UserControl, INotifyPropertyChanged
    {
        public FilterList PropertiesData { get; }
        public PropertyGroupItem GroupItem { get; } = new PropertyGroupItem(null);
        public DGV Settings { get; }
        public bool IsClearFilterButtonEnabled => !string.IsNullOrEmpty(PropertiesData.StringPresentation);
        public string FilterText => PropertiesData.StringPresentation;

        private readonly DGViewModel _viewModel;

        #region =======  Quick Filter  =========
        private string _quickFilterText;
        public string QuickFilterText
        {
            get => _quickFilterText;
            set
            {
                if (!Equals(_quickFilterText, value))
                {
                    _quickFilterText = value;
                    SetFilter();
                }
            }
        }
        private void SetFilter()
        {
            var view = CollectionViewSource.GetDefaultView(PropertiesData);
            view.Filter += Filter;
            // DataGrid.SelectedItem = DataGrid.Items.OfType<object>().FirstOrDefault();
        }
        private bool Filter(object obj) => Helpers.Misc.SetFilter(((DGEditSettingsModel) obj).DisplayName, QuickFilterText);
        #endregion

        public DGEditSettingsView(DGViewModel dgViewModel)
        {
            InitializeComponent();
            DataContext = this;
            _viewModel = dgViewModel;
            Settings = ((IUserSettingSupport<DGV>)_viewModel).GetSettings();
            PropertiesData = new DGCore.Filters.FilterList(Settings.AllColumns.Select(o => new DGEditSettingsModel(this, o, Settings, _viewModel.Data.Properties[o.Id])));

            CmdApply = new RelayCommand(cmdApply);
            CmdClearFilter = new RelayCommand(cmdClearFilter);

            GroupItem.Children.Clear();
            for (var i1 = 0; i1 < Settings.Groups.Count; i1++)
            {
                var groupItem = Settings.Groups[i1];
                var item = (DGEditSettingsModel)PropertiesData.FirstOrDefault(o => o.Id == groupItem.Id);
                var group = GroupItem.AddNewItem(item, groupItem.SortDirection);
                for (var i2 = 0; i2 < Settings.SortsOfGroup[i1].Count; i2++)
                {
                    var sortItem = Settings.SortsOfGroup[i1][i2];
                    item = (DGEditSettingsModel)PropertiesData.FirstOrDefault(o => o.Id == sortItem.Id);
                    group.AddNewItem(item, sortItem.SortDirection);
                }
            }

            var detailsGroup = GroupItem.AddNewItem(null, ListSortDirection.Ascending);
            for (var i = 0; i < Settings.Sorts.Count; i++)
            {
                var sortItem = Settings.Sorts[i];
                var item = (DGEditSettingsModel)PropertiesData.FirstOrDefault(o => o.Id == sortItem.Id);
                detailsGroup.AddNewItem(item, sortItem.SortDirection);
            }

            foreach (var item in Settings.TotalLines.Where(o => o.TotalFunction != Enums.TotalFunction.None))
            {
                var pitem = (DGEditSettingsModel)PropertiesData.FirstOrDefault(o => o.Id == item.Id);
                pitem.TotalFunction = item.TotalFunction;
            }
        }

        #region =======  Drag/Drop event handlers ========
        private void PropertyList_OnPreviewMouseMove(object sender, MouseEventArgs e) => DragDropHelper.DragSource_OnPreviewMouseMove(sender, e);
        private void GroupTreeView_OnPreviewMouseMove(object sender, MouseEventArgs e) => DragDropHelper.DragSource_OnPreviewMouseMove(sender, e, null, GroupTreeView_AllowDrag);
        private void PropertyList_OnPreviewGiveFeedback(object sender, GiveFeedbackEventArgs e) => DragDropHelper.DragSource_OnPreviewGiveFeedback(sender, e);
        private void PropertyList_OnPreviewDragOver(object sender, DragEventArgs e) => DragDropHelper.DropTarget_OnPreviewDragOver(sender, e);
        private void GroupTreeView_OnPreviewDragOver(object sender, DragEventArgs e) =>
            DragDropHelper.DropTarget_OnPreviewDragOver(sender, e, new[] { "TreeView", "DataGrid" }, GroupTreeView_AfterDefiningIndex);
        private void PropertyList_OnPreviewDragEnter(object sender, DragEventArgs e) => DragDropHelper.DropTarget_OnPreviewDragOver(sender, e);
        private void GroupTreeView_OnPreviewDragEnter(object sender, DragEventArgs e) =>
            DragDropHelper.DropTarget_OnPreviewDragOver(sender, e, new[] { "TreeView", "DataGrid" }, GroupTreeView_AfterDefiningIndex);
        private void PropertyList_OnPreviewDragLeave(object sender, DragEventArgs e) => DragDropHelper.DropTarget_OnPreviewDragLeave(sender, e);
        private async void PropertyList_OnPreviewDrop(object sender, DragEventArgs e)
        {
            await DragDropHelper.DropTarget_OnPreviewDrop(sender, e);
            ReorderFrozenItems();
        }

        private async void GroupTreeView_OnPreviewDrop(object sender, DragEventArgs e)
        {
            await DragDropHelper.DropTarget_OnPreviewDrop(sender, e, new[] {"TreeView", "DataGrid"}, ConverterForTreeView);
            foreach (var child in PropertyGroupItem.GetAllChildren(GroupItem))
                child.RefreshUI();
        }

        #endregion

        #region ==========  Helper methods  ============
        private bool GroupTreeView_AllowDrag(object[] arg) => arg.Length == 1 && arg[0] is PropertyGroupItem groupItem && groupItem.CanSort;

        private static void GroupTreeView_AfterDefiningIndex(object[] dragData, ItemsControl control, DragEventArgs e)
        {
            if (dragData.Length == 0 || dragData.OfType<DGEditSettingsModel>().Any(o => !o.IsSortingSupport))
            {
                DragDropHelper.Drag_Info.DragDropEffect = DragDropEffects.None;
                return;
            }

            var hoveredElement = DragDropHelper.Drag_Info.GetHoveredItem(control);
            var hoveredItem = hoveredElement?.DataContext as PropertyGroupItem;
            var internalDragItem = dragData.Length == 1 && dragData[0] is PropertyGroupItem ? dragData[0] as PropertyGroupItem : null;
            if (hoveredItem == null || internalDragItem != null && hoveredItem.Parent != internalDragItem.Parent)
            {
                DragDropHelper.Drag_Info.DragDropEffect = DragDropEffects.None;
                return;
            }

            if (internalDragItem != null || dragData.OfType<DGEditSettingsModel>().Count() == dragData.Length)
            {
                if (hoveredItem.Type == PropertyGroupItem.ItemType.Label)
                {
                    if (hoveredItem.Parent.Children.Count > 1)
                    {
                        DragDropHelper.Drag_Info.InsertIndex++;
                        DragDropHelper.Drag_Info.IsBottomOrRightEdge = false;
                    }
                    else
                        DragDropHelper.Drag_Info.IsBottomOrRightEdge = true;
                }
                else if (hoveredItem.Type == PropertyGroupItem.ItemType.Group || hoveredItem.Type == PropertyGroupItem.ItemType.Sorting)
                {
                    if (DragDropHelper.Drag_Info.IsBottomOrRightEdge && DragDropHelper.Drag_Info.InsertIndex < hoveredItem.Parent.Children.Count)
                    {
                        DragDropHelper.Drag_Info.InsertIndex = DragDropHelper.Drag_Info.InsertIndex + hoveredItem.Children.Count + 1;
                        DragDropHelper.Drag_Info.IsBottomOrRightEdge = false;
                    }
                }
                else if (hoveredItem.Type == PropertyGroupItem.ItemType.Details && hoveredItem.Children.Count > 0)
                    DragDropHelper.Drag_Info.IsBottomOrRightEdge = false;
            }
            else
                DragDropHelper.Drag_Info.DragDropEffect = DragDropEffects.None;
        }
        private void ConverterForTreeView(object[] data)
        {
            var hoveredElement = DragDropHelper.Drag_Info.GetHoveredItem(GroupTreeView);
            var targetItem = hoveredElement?.DataContext as PropertyGroupItem;
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] is DGEditSettingsModel propertyItem)
                    data[i] = new PropertyGroupItem(targetItem.Parent, propertyItem);
            }
        }

        #endregion

        private void PropertyList_OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            var rowHeaderText = (e.Row.GetIndex() + 1).ToString("N0", LocalizationHelper.CurrentCulture);
            if (!Equals(e.Row.Header, rowHeaderText)) e.Row.Header = rowHeaderText;
        }

        // ==================
        internal async void GroupChanged (DGEditSettingsModel item)
        {
            GroupTreeView.ItemContainerGenerator.StatusChanged += ItemsControlHelper.OnItemContainerGeneratorStatusChanged;
            var groupItem = GroupItem.Children.FirstOrDefault(o => o.Item == item);
            if (item.GroupDirection.HasValue && groupItem == null)
            {
                var propertyItem = GroupItem.AddNewItem(item, item.GroupDirection.Value);
                if (GroupTreeView.ItemContainerGenerator.ContainerFromItem(propertyItem) is TreeViewItem treeViewItem)
                {
                    VisualHelper.DoEvents(DispatcherPriority.Render);
                    var tasks = AnimationHelper.GetHeightContentAnimations(treeViewItem, true);
                    await Task.WhenAll(tasks);
                    treeViewItem.Height = double.NaN;
                }
            }
            else if (!item.GroupDirection.HasValue && groupItem != null)
            {
                if (GroupTreeView.ItemContainerGenerator.ContainerFromItem(groupItem) is TreeViewItem treeViewItem)
                {
                    var tasks = AnimationHelper.GetHeightContentAnimations(treeViewItem, false);
                    await Task.WhenAll(tasks);
                    treeViewItem.Height = double.NaN;
                }
                GroupItem.Children.Remove(groupItem);
            }
            else if (item.GroupDirection.HasValue && groupItem != null && item.GroupDirection.Value != groupItem.SortDirection)
                groupItem.SortDirection = item.GroupDirection.Value;

            GroupTreeView.ItemContainerGenerator.StatusChanged -= ItemsControlHelper.OnItemContainerGeneratorStatusChanged;

            foreach (var child in PropertyGroupItem.GetAllChildren(GroupItem))
                child.RefreshUI();
        }

        internal async void ReorderFrozenItems()
        {
            if (PropertiesData == null) return;
            PropertyList.CommitEdit();

            var frozenItems = PropertiesData.Where(o => ((DGEditSettingsModel)o).IsFrozen).ToArray();
            if (frozenItems.Length > 0)
                await PropertyList.AddOrReorderItems(frozenItems, 0);

            foreach (DataGridRow item in PropertyList.GetItemsHost().Children)
                PropertyList_OnLoadingRow(PropertyList, new DataGridRowEventArgs(item));
        }

        #region  ===========  Commands  =============
        public RelayCommand CmdApply { get; }
        public RelayCommand CmdClearFilter { get; }
        private void cmdApply(object p)
        {
            PropertyList.CommitEdit();
            
            Settings.AllColumns = PropertiesData.OrderBy(o => ((DGEditSettingsModel)o).IsHidden).ThenByDescending(o => ((DGEditSettingsModel)o).IsFrozen).Select(o => ((DGEditSettingsModel)o).Column).ToList();

            var details = GroupItem.Children.First(o => o.Type == PropertyGroupItem.ItemType.Details);
            Settings.Sorts = details.Children.Where(o => o.Type == PropertyGroupItem.ItemType.Sorting)
                .Select(o => new Sorting {Id = o.Item.Id, SortDirection = o.SortDirection}).ToList();

            Settings.Groups = new List<Sorting>();
            Settings.SortsOfGroup = new List<List<Sorting>>();
            foreach (var groupItem in GroupItem.Children.Where(o => o.Type == PropertyGroupItem.ItemType.Group))
            {
                Settings.Groups.Add(new Sorting { Id = groupItem.Item.Id, SortDirection = groupItem.SortDirection });
                Settings.SortsOfGroup.Add(groupItem.Children.Where(o => o.Type == PropertyGroupItem.ItemType.Sorting)
                    .Select(o => new Sorting {Id = o.Item.Id, SortDirection = o.SortDirection}).ToList());
            }

            Settings.TotalLines = new List<TotalLine>();
            foreach (DGEditSettingsModel item in PropertiesData.Where(o => ((DGEditSettingsModel)o).TotalFunction.HasValue && ((DGEditSettingsModel)o).TotalFunction.Value != Enums.TotalFunction.None))
            {
                var total = new TotalLine {Id = item.Id, TotalFunction = item.TotalFunction.Value};
                Settings.TotalLines.Add(total);
            }

            Settings.WhereFilter.Clear();
            foreach (var o in PropertiesData.Where(a => a.HasFilter))
            {
                var filter = new Filter { Name = o.Id, IgnoreCase = o.IgnoreCase, Not = o.Not, Lines = new List<FilterLine>() };
                foreach (var line in o.Items)
                    filter.Lines.Add(new FilterLine { Operand = line.FilterOperand, Value1 = line.Value1, Value2 = line.Value2 });
                Settings.WhereFilter.Add(filter);
            }

            _viewModel.ApplySetting(Settings);
            _viewModel.GenerateColumns();
        }

        private void cmdClearFilter(object p)
        {
            PropertyList.CommitEdit();
            PropertiesData.ClearFilter();
            RefreshUI();
        }
        #endregion

        private void PropertyList_OnCurrentCellChanged(object sender, EventArgs e)
        {
            var grid = (DataGrid) sender;
            if (Equals(grid.CurrentCell.Column?.SortMemberPath, "DataContext.Format"))
            {
                grid.Dispatcher.BeginInvoke(new Action((() => grid.BeginEdit())));
            }
        }

        private void OnFilterEditPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var cell = (DataGridCell)sender;
            var filterLine = cell.DataContext as DGCore.Filters.FilterLineBase;
            if (!(bool)CanConvertStringTo.Instance.Convert(filterLine.PropertyType, null, null, null))
                return;

            var view = new FilterLineView(filterLine);
            var container = this.GetVisualParents().OfType<MwiContainer>().FirstOrDefault();
            var geometry = (Geometry)Application.Current.Resources["FilterGeometry"];
            var transforms = WpfSpLib.Helpers.ControlHelper.GetActualLayoutTransforms(container);
            var height = Math.Max(200, Window.GetWindow(this).ActualHeight * 2 / 3 / transforms.Value.M22);
            Helpers.Misc.OpenMwiDialog(container, view, "Filter Setup", geometry, (child, adorner) =>
            {
                child.Height = height;
                child.Theme = container?.ActualTheme;
                child.ThemeColor = container?.ActualThemeColor;
            });
            RefreshUI();
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
            OnPropertiesChanged(nameof(FilterText), nameof(IsClearFilterButtonEnabled));
            foreach (var o in PropertiesData)
                o.OnPropertiesChanged(nameof(FilterLineBase.FilterTextOrDescription), nameof(FilterLineBase.HasFilter), nameof(FilterLineBase.Error));
        }
        #endregion

    }
}

