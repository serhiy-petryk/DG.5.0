using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DGCore.Utils;
using WpfSpLib.Helpers;

namespace DGView.Helpers
{
    public static class DGHelper
    {
        public static void DataGrid_OnRowEditEnding(DataGrid dataGrid, DataGridRowEditEndingEventArgs e)
        {
            if (e.Cancel || e.EditAction != DataGridEditAction.Commit) return;

            if (e.Row.DataContext is DGCore.Common.IIsEmptySupport item && dataGrid.ItemsSource is IList itemList)
            {
                dataGrid.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (item.IsEmpty())
                    {
                        itemList.Remove(item);
                        dataGrid.UpdateAllBindings();
                        // dataGrid.Items.Refresh();
                    }
                }), DispatcherPriority.Loaded);
            }
        }

        public static DataGridCellInfo GetActiveCellInfo(DataGrid dg)
        {
            var cellInfo = dg.CurrentCell;
            if (!cellInfo.IsValid && dg.SelectedCells.Count > 0)
                // cellInfo = dg.SelectedCells[dg.SelectedCells.Count - 1];
                cellInfo = dg.SelectedCells[0];
            if (!cellInfo.IsValid && dg.Items.Count > 0)
            {
                var firstItem = dg.Items[0];
                var firstColumn = dg.Columns.Where(c => c.Visibility == Visibility.Visible).OrderBy(c => c.DisplayIndex).FirstOrDefault();
                if (firstColumn != null)
                    cellInfo = new DataGridCellInfo(firstItem, firstColumn);
            }
            return cellInfo;
        }

        public static DataGridCell GetDataGridCell(DataGridCellInfo cellInfo)
        {
            var cellContent = cellInfo.Column.GetCellContent(cellInfo.Item);
            if (cellContent != null && cellContent.Parent is DataGridCell cell)
                return cell;
            return null;
        }

        public static void GetSelectedArea(DataGrid dg, out IList items, out DataGridColumn[] columns)
        {
            var validColumns = dg.Columns.Where(c => c.Visibility == Visibility.Visible);
            if (dg.SelectedCells.Count < 2)
            {
                items = (IList)dg.ItemsSource;
                columns = validColumns.OrderBy(c => c.DisplayIndex).ToArray();
                return;
            }

            var rowItems = dg.ItemsSource.Cast<object>().Select((item, index) => new { index, item }).ToDictionary(x => x.item, x => x.index);
            var tempColumns = validColumns.ToDictionary(x => x, x => x.DisplayIndex);
            var selectedItems = new Dictionary<object, int>();
            var selectedColumns = new Dictionary<DataGridColumn, int>();

            foreach (var item in dg.SelectedItems)
            {
                selectedItems.Add(item, rowItems[item]);
                rowItems.Remove(item);
            }

            foreach (var cell in dg.SelectedCells.Where(c => c.IsValid && c.Column.Visibility == Visibility.Visible))
            {
                if (rowItems.Count > 0 && rowItems.ContainsKey(cell.Item))
                {
                    selectedItems.Add(cell.Item, rowItems[cell.Item]);
                    rowItems.Remove(cell.Item);
                }

                if (tempColumns.Count > 0 && tempColumns.ContainsKey(cell.Column))
                {
                    selectedColumns.Add(cell.Column, tempColumns[cell.Column]);
                    tempColumns.Remove(cell.Column);
                }

                if (rowItems.Count == 0 && tempColumns.Count == 0)
                    break;
            }

            items = selectedItems.OrderBy(o => o.Value).Select(o => o.Key).ToArray();
            columns = selectedColumns.OrderBy(o => o.Value).Select(o => o.Key).ToArray();
        }

        public static void SetColumnVisibility(DataGridColumn column, bool isVisible)
        {
            if (column.Visibility == Visibility.Visible && !isVisible)
                column.Visibility = Visibility.Collapsed;
            else if (column.Visibility != Visibility.Visible && isVisible)
                column.Visibility = Visibility.Visible;
        }

        public static TextAlignment? GetDefaultColumnAlignment(Type type)
        {
            type = Types.GetNotNullableType(type);
            if (type == null) return null;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return TextAlignment.Center;
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.DateTime:
                    return TextAlignment.Right;
                case TypeCode.String:
                    return TextAlignment.Left;
                case TypeCode.Object:
                    return TextAlignment.Left;

                default:
                    throw new Exception("Check DataGridHelper.GetDefaultColumnAlignment method");
            }
        }

        public static TextAlignment? GetColumnAlignment(DataGridColumn column) =>
            column is DataGridBoundColumn boundColumn
                ? boundColumn.ElementStyle.Setters.OfType<Setter>().Select(s => s.Value).OfType<TextAlignment?>()
                    .FirstOrDefault()
                : null;
        public static TextWrapping? GetColumnTextWrapping(DataGridColumn column) =>
            column is DataGridBoundColumn boundColumn
                ? boundColumn.ElementStyle.Setters.OfType<Setter>().Select(s => s.Value).OfType<TextWrapping?>()
                    .FirstOrDefault()
                : null;
    }
}