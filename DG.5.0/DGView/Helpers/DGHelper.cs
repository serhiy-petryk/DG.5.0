using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DGCore.Common;
using DGCore.Helpers;
using DGCore.PD;
using DGCore.Utils;
using DGView.ViewModels;
using WpfSpLib.Helpers;

namespace DGView.Helpers
{
    public static class DGHelper
    {
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

        public static DGColumnHelper[] GetColumnHelpers(DataGridColumn[] columns, PropertyDescriptorCollection properties, List<PropertyDescriptor> selectedProperties)
        {
            var columnHelpers = new List<DGColumnHelper>();
            selectedProperties?.Clear();
            foreach (var column in columns.OrderBy(c=>c.DisplayIndex))
            {
                if (!string.IsNullOrEmpty(column.SortMemberPath))
                {
                    columnHelpers.Add(new DGColumnHelper(properties[column.SortMemberPath], column.DisplayIndex));
                    selectedProperties?.Add(properties[column.SortMemberPath]);
                }
                else if (column.HeaderStringFormat == Constants.GroupItemCountColumnName)
                {
                    var p = new PropertyDescriptorForGroupItemCount((string) Application.Current.Resources["Loc:DGV.GroupItemCountColumnHeader"]);
                    columnHelpers.Add(new DGColumnHelper(p, column.DisplayIndex));
                    selectedProperties?.Add(p);
                }
                else if (column.HeaderStringFormat.StartsWith(Constants.GroupColumnNamePrefix)) { }
                else
                    throw new Exception("Trap!!!");
            }

            return columnHelpers.ToArray();
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

        public static void GenerateColumns(DGViewModel viewModel)
        {
            foreach (PropertyDescriptor pd in viewModel.Properties)
            {
                var propertyType = Types.GetNotNullableType(pd.PropertyType);
                var gridFormat = GetGridFormat((IMemberDescriptor)pd, viewModel.Formats);
                var oldColumn = viewModel.DGControl.Columns.FirstOrDefault(c => string.Equals(c.SortMemberPath, pd.Name, StringComparison.Ordinal));

                DataGridColumn column;
                if (propertyType == typeof(bool))
                    column = new DataGridCheckBoxColumn { ElementStyle = viewModel.DGControl.Resources["DataGridCheckBoxColumnElementStyle"] as Style };
                else if (propertyType == typeof(byte[]) && string.Equals(gridFormat, "image", StringComparison.OrdinalIgnoreCase))
                {
                    var template = TemplateGenerator.CreateDataTemplate(() =>
                    {
                        var result = new Image { Margin = new Thickness(1) };
                        result.SetBinding(Image.SourceProperty, pd.Name);
                        return result;
                    });
                    column = new DataGridTemplateColumn { CellTemplate = template, SortMemberPath = pd.Name };
                }
                else column = new DataGridTextColumn();

                column.Header = pd.DisplayName;
                if (column is DataGridBoundColumn boundColumn)
                {
                    var binding = new Binding(pd.Name);
                    if (pd.IsReadOnly)
                        binding.Mode = BindingMode.OneWay;

                    if (propertyType == typeof(byte[]) && !string.Equals(gridFormat, "image", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(gridFormat, "hex", StringComparison.OrdinalIgnoreCase))
                            binding.Converter = ByteArrayToHexStringConverter.Instance;
                        else
                            binding.Converter = null;
                    }
                    else if (!string.IsNullOrEmpty(gridFormat))
                        binding.StringFormat = gridFormat;
                    else if (Types.GetNotNullableType(pd.PropertyType) == typeof(DateTime)) // set smart format for DateTime
                        binding.Converter = DGDateTimeConverter.Instance;
                    //else if (pd.Name == "Picture")
                    //  binding.Converter = BytesToHexStringConverter.Instance;
                    boundColumn.Binding = binding;

                    if (pd.Name == "ACCOUNT") binding.StringFormat = "Account: {0}";
                }

                if (oldColumn == null)
                {
                    // ??? Sort support for BindingList=> doesn't work column.SortMemberPath = prefixes.Count == 0 ? t.Name : string.Join(".", prefixes) + "." + t.Name;
                    viewModel.DGControl.Columns.Add(column);
                    column.CanUserSort = typeof(IComparable).IsAssignableFrom(propertyType);
                    column.Visibility = pd.Name.Contains(Constants.MDelimiter)
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                    AddToolTipToGridColumn(column, pd);
                }
                else
                {
                    if (oldColumn.GetType() != column.GetType())
                    {
                        viewModel.DGControl.Columns.Insert(oldColumn.DisplayIndex, column);
                        viewModel.DGControl.Columns.Remove(oldColumn);
                        AddToolTipToGridColumn(column, pd);
                    }
                    else if (oldColumn is DataGridBoundColumn bc1 && column is DataGridBoundColumn bc2)
                    {
                        var b1 = (Binding)bc1.Binding;
                        var b2 = (Binding)bc2.Binding;
                        if (!string.Equals(b1.StringFormat, b2.StringFormat) || !Equals(b1.Converter, b2.Converter))
                            bc1.Binding = bc2.Binding;

                        if (bc1.Width.IsAuto)
                        {
                            bc1.Width = bc1.ActualWidth;
                            bc1.Width = DataGridLength.Auto;
                        }
                    }
                }
            }

            // Set IsFrozen to false (by default all columns after 'viewModel.DGControl.Columns.Add(column)' are frozen)
            if (viewModel.DGControl.Columns.Count > 0)
            {
                viewModel.DGControl.FrozenColumnCount = 1;
                viewModel.DGControl.FrozenColumnCount = 0;
            }

            viewModel.SetSetting(viewModel.GetSettings());
        }

        public static string GetGridFormat(IMemberDescriptor md, Dictionary<string, string> formats)
        {
            if (formats.ContainsKey(((PropertyDescriptor)md).Name))
                return formats[((PropertyDescriptor)md).Name] ?? md.DisplayFormat;
            return md.DisplayFormat;
        }

        public static void AddToolTipToGridColumn(DataGridColumn column, PropertyDescriptor pd)
        {
            if (!string.IsNullOrEmpty(pd.Description))
            {
                var columnHeaderStyle = Application.Current.Resources["MonochromeDGColumnHeaderStyle"] as Style;
                columnHeaderStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty, pd.Description));
                column.HeaderStyle = columnHeaderStyle;
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