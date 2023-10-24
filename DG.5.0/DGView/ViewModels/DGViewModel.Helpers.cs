using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DGCore.Common;
using DGCore.Helpers;
using DGCore.PD;

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
                    var p = new PropertyDescriptorForGroupItemCount((string)Application.Current.Resources["Loc:DGV.GroupItemCountColumnHeader"]);
                    columnHelpers.Add(new DGColumnHelper(p, column.DisplayIndex, "N0"));
                    selectedProperties?.Add(p);
                }
                else if (column.HeaderStringFormat.StartsWith(Constants.GroupColumnNamePrefix)) { }
                else
                    throw new Exception("Trap!!!");
            }

            return columnHelpers.ToArray();
        }


    }
}
