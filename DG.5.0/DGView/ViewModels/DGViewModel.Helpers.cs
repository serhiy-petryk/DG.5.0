using System;
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
using DGView.Helpers;
using WpfSpLib.Helpers;

namespace DGView.ViewModels
{
    public partial class DGViewModel
    {
        public DGColumnHelper[] GetColumnHelpers(DataGridColumn[] columns, List<PropertyDescriptor> selectedProperties)
        {
            var columnHelpers = new List<DGColumnHelper>();
            selectedProperties?.Clear();
            foreach (var column in columns.OrderBy(c => c.DisplayIndex))
            {
                if (!string.IsNullOrEmpty(column.SortMemberPath))
                {
                    var dgColumn = _columns.FirstOrDefault(c => string.Equals(c.Id, column.SortMemberPath, StringComparison.OrdinalIgnoreCase));
                    columnHelpers.Add(new DGColumnHelper(Properties[column.SortMemberPath], column.DisplayIndex, dgColumn?.Format_Actual));
                    selectedProperties?.Add(Properties[column.SortMemberPath]);
                }
                else if (column.HeaderStringFormat == Constants.GroupItemCountColumnName)
                {
                    var p = PropertyDescriptorForGroupItemCount.PD_ForGroupItemCount;
                    columnHelpers.Add(new DGColumnHelper(p, column.DisplayIndex, ((IMemberDescriptor)p).DisplayFormat));
                    selectedProperties?.Add(p);
                }
                else if (column.HeaderStringFormat.StartsWith(Constants.GroupColumnNamePrefix)) { }
                else
                    throw new Exception("Trap!!!");
            }

            return columnHelpers.ToArray();
        }

        public void GenerateColumns()
        {
            foreach (PropertyDescriptor pd in Properties)
            {
                var propertyType = Types.GetNotNullableType(pd.PropertyType);
                var gridFormat = GetColumnActualFormat(pd);
                var oldColumn = DGControl.Columns.FirstOrDefault(c => string.Equals(c.SortMemberPath, pd.Name, StringComparison.Ordinal));

                DataGridColumn column;
                if (propertyType == typeof(bool))
                    column = new DataGridCheckBoxColumn { ElementStyle = DGControl.Resources["DataGridCheckBoxColumnElementStyle"] as Style };
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
                            binding.Converter = Helpers.ByteArrayToHexStringConverter.Instance;
                        else
                            binding.Converter = null;
                    }
                    else if (!string.IsNullOrEmpty(gridFormat))
                        binding.StringFormat = gridFormat;
                    else if (Types.GetNotNullableType(pd.PropertyType) == typeof(DateTime)) // set smart format for DateTime
                        binding.Converter = DGDateTimeConverter.Instance;

                    boundColumn.Binding = binding;
                }

                if (oldColumn == null)
                {
                    // ??? Sort support for BindingList=> doesn't work column.SortMemberPath = prefixes.Count == 0 ? t.Name : string.Join(".", prefixes) + "." + t.Name;
                    DGControl.Columns.Add(column);
                    column.CanUserSort = typeof(IComparable).IsAssignableFrom(propertyType);
                    column.Visibility = pd.Name.Contains(Constants.MDelimiter) ? Visibility.Collapsed : Visibility.Visible;
                    AddToolTipToGridColumn(column, pd);
                }
                else
                {
                    if (oldColumn.GetType() != column.GetType())
                    {
                        column.Visibility = oldColumn.Visibility;
                        DGControl.Columns.Insert(oldColumn.DisplayIndex, column);
                        DGControl.Columns.Remove(oldColumn);
                        AddToolTipToGridColumn(column, pd);
                    }
                    else if (oldColumn is DataGridBoundColumn bcOld && column is DataGridBoundColumn bcNew)
                    {
                        var bOld = (Binding)bcOld.Binding;
                        var bNew = (Binding)bcNew.Binding;
                        if (!string.Equals(bOld.StringFormat, bNew.StringFormat) || !Equals(bOld.Converter, bNew.Converter))
                        {
                            bcOld.Binding = bcNew.Binding;
                            if (bcOld.Width.IsAuto) // Refresh auto width
                            {
                                bcOld.Width = bcOld.ActualWidth;
                                bcOld.Width = DataGridLength.Auto;
                            }
                        }
                    }
                }
            }

            // Set IsFrozen to false (by default all columns after 'DGControl.Columns.Add(column)' are frozen)
            if (DGControl.Columns.Count > 0)
            {
                DGControl.FrozenColumnCount = 1;
                DGControl.FrozenColumnCount = 0;
            }

            //==========================
            void AddToolTipToGridColumn(DataGridColumn column, PropertyDescriptor pd)
            {
                if (!string.IsNullOrEmpty(pd.Description))
                {
                    var columnHeaderStyle = Application.Current.Resources["MonochromeDGColumnHeaderStyle"] as Style;
                    columnHeaderStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty, pd.Description));
                    column.HeaderStyle = columnHeaderStyle;
                }
            }
        }

        internal string GetColumnActualFormat(PropertyDescriptor pd)
        {
            var column = _columns.FirstOrDefault(c => string.Equals(c.Id, pd.Name, StringComparison.OrdinalIgnoreCase));
            return column == null ? ((IMemberDescriptor)pd).DisplayFormat : column.Format_Actual;
        }
    }
}
