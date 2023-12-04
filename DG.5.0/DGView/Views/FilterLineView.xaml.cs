// ToDo:
// +1. Clone values
// 2. Copy & paste for grid
// - (difficult to remove event handlers after disposed and/or get access to mwiChild in FilterLineView) 3. MwiChild + Disposable => close
// 4. Button => monochrome or flat style

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DGCore.Filters;
using DGView.Helpers;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Views
{
    /// <summary>
    /// Interaction logic for FilterLineView.xaml
    /// </summary>
    public partial class FilterLineView : UserControl
    {
        #region ========  Static section =============
        internal static void OnFilterEditPreviewMouseDown(DataGridCell cell)
        {
            // var cell = (DataGridCell)sender;
            var filterLine = cell.DataContext as DGCore.Filters.FilterLineBase;
            if (!(bool)CanConvertStringTo.Instance.Convert(filterLine.PropertyType, null, null, null))
                return;

            var view = new FilterLineView(filterLine);
            var container = cell.GetVisualParents().OfType<MwiContainer>().FirstOrDefault();
            var geometry = (Geometry)Application.Current.Resources["FilterGeometry"];
            var transforms = WpfSpLib.Helpers.ControlHelper.GetActualLayoutTransforms(container);
            var height = Math.Max(200, Window.GetWindow(cell).ActualHeight * 2 / 3 / transforms.Value.M22);
            Helpers.Misc.OpenMwiDialog(container, view, "Filter Setup", geometry, (child, adorner) =>
            {
                child.Height = height;
                child.Theme = container?.ActualTheme;
                child.ThemeColor = container?.ActualThemeColor;
            });
        }
        #endregion

        public FilterLineBase FilterLine { get; }
        public FilterLineSubitemCollection Clone_FilterLines { get; }
        public bool Clone_Not { get; set; }

        private MwiChild ParentWindow => this.GetVisualParents().OfType<MwiChild>().FirstOrDefault();

        public FilterLineView(FilterLineBase filterLine)
        {
            InitializeComponent();
            DataContext = this;
            FilterLine = filterLine;
            Clone_FilterLines = (FilterLineSubitemCollection)filterLine.Items.Clone();
            Clone_Not = FilterLine.Not;
        }

        #region ==========  Event handlers  ==========
        private void DataGrid_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // To prevent error: ''DeferRefresh' is not allowed during an AddNew or EditItem transaction.'
            ((DataGrid)sender).CommitEdit(DataGridEditingUnit.Row, true);
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            FilterLine.Items.Clear();
            foreach (var item in Clone_FilterLines.Where(a => a.IsValid))
            // foreach (var item in Clone_FilterLines)
                FilterLine.Items.Add(item);
            FilterLine.Not = Clone_Not;
            CloseButton_OnClick(sender, e);
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            Clone_FilterLines.Clear();
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            FilterLine.OnPropertiesChanged(nameof(FilterLineBase.FilterTextOrDescription), nameof(FilterLineBase.HasFilter));
            ParentWindow?.CmdClose.Execute(null);
        }
        #endregion

        private void DbPropertiesDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (e.AddedCells.Count == 1)
            {
                var grid = (DataGrid)sender;
                grid.Dispatcher.BeginInvoke(new Action((() => grid.BeginEdit())));
            }

            /*var blankLines = e.RemovedCells.Where(c =>
                c.Item is FilterLineSubitem si && Equals(si.FilterOperand, DGCore.Common.Enums.FilterOperand.None) &&
                Equals(si.Value1, null) && Equals(si.Value2, null)).ToArray();
            for (var k = 0; k < blankLines.Length; k++)
                Clone_FilterLines.Remove((FilterLineSubitem) blankLines[k].Item);*/
        }
    }
}
