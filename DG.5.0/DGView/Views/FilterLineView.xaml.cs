﻿// ToDo:
// +1. Clone values
// 2. Copy & paste for grid
// - (difficult to remove event handlers after disposed and/or get access to mwiChild in FilterLineView) 3. MwiChild + Disposable => close
// 4. Button => monochrome or flat style

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DGCore.Filters;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Views
{
    /// <summary>
    /// Interaction logic for FilterLineView.xaml
    /// </summary>
    public partial class FilterLineView : UserControl
    {
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
                FilterLine.Items.Add(new FilterLineSubitem { Owner = item.Owner, FilterOperand = item.FilterOperand, Value1 = item.Value1, Value2 = item.Value2 });
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

            var blankLines = e.RemovedCells.Where(c =>
                c.Item is FilterLineSubitem si && Equals(si.FilterOperand, DGCore.Common.Enums.FilterOperand.None) &&
                Equals(si.Value1, null) && Equals(si.Value2, null)).ToArray();
            for (var k = 0; k < blankLines.Length; k++)
                Clone_FilterLines.Remove((FilterLineSubitem) blankLines[k].Item);
        }
    }
}
