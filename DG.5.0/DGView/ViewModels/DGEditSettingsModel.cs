using System;
using System.ComponentModel;
using System.Linq;
using DGCore.Common;
using DGCore.Filters;
using DGCore.UserSettings;
using DGView.Views;

namespace DGView.ViewModels
{
    public class DGEditSettingsModel: FilterLine_Item //  FilterLineBase : IDataErrorInfo, INotifyPropertyChanged
    {
        private readonly DGEditSettingsView _host;

        public Column Column;
        public string Format
        {
            get => Column.Format_Actual;
            set
            {
                Column.Format_UserDefined = value;
                OnPropertiesChanged(nameof(Format));
            }
        }

        public Enums.TotalFunction? TotalFunction { get; set; }
        public bool IsTotalSupport => DGCore.Utils.Types.IsNumericType(PropertyType);
        public bool IsHidden
        {
            get => Column.IsHidden;
            set
            {
                Column.IsHidden = value;
                OnPropertiesChanged(nameof(IsHidden));
            }
        }

        private bool _isFrozen;
        public bool IsFrozen
        {
            get => _isFrozen;
            set
            {
                _isFrozen = value;
                _host.ReorderFrozenItems();
                OnPropertiesChanged(nameof(IsFrozen));
            }
        }

        private ListSortDirection? _groupDirection;
        public ListSortDirection? GroupDirection
        {
            get => _groupDirection;
            set
            {
                _groupDirection = value;
                if (_groupDirection.HasValue && !IsFrozen)
                    IsFrozen = true;
                else if (!_groupDirection.HasValue && IsFrozen)
                    IsFrozen = false;
                _host.GroupChanged(this);
                OnPropertiesChanged(nameof(GroupDirection));
            }
        }

        public bool IsSortingSupport => typeof(IComparable).IsAssignableFrom(DGCore.Utils.Types.GetNotNullableType(PropertyType));

        public DGEditSettingsModel(DGEditSettingsView host, Column column, DGV settings, PropertyDescriptor descriptor): base(descriptor)
        {
            _host = host;
            Column = column;
            Format = column.Format_Actual;
            if (settings.Groups.FirstOrDefault(o => o.Id == Id) is Sorting sorting)
                GroupDirection = sorting.SortDirection;
            IsFrozen = settings.FrozenColumns.Contains(Id);
            var filter = settings.WhereFilter.FirstOrDefault(f => string.Equals(f.Name, Id, StringComparison.OrdinalIgnoreCase));
            if (filter != null)
            {
                IgnoreCase = filter.IgnoreCase;
                Not = filter.Not;
                foreach (var o in filter.Lines)
                {
                    Items.Add(new FilterLineSubitem { Owner = this, FilterOperand = o.Operand, Value1 = o.Value1, Value2 = o.Value2 });
                }
            }
        }

        public override string ToString() => DisplayName;
    }
}
