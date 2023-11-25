using System;
using System.ComponentModel;
using System.Linq;
using DGCore.Common;
using DGCore.PD;
using DGCore.UserSettings;
using DGView.Views;

namespace DGView.ViewModels
{
    public class DGProperty_ItemModel: INotifyPropertyChanged
    {
        private readonly DGEditSettingsView _host;

        public Column Column;
        public string Id => Column.Id;
        public string Name { get; }
        public string Description { get; }
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
        public bool IsTotalSupport => DGCore.Utils.Types.IsNumericType(_propertyType);
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

        public bool IsSortingSupport => typeof(IComparable).IsAssignableFrom(DGCore.Utils.Types.GetNotNullableType(_propertyType));
        // public FilterLine_Item FilterLine { get; }

        private readonly Type _propertyType;

        public DGProperty_ItemModel(DGEditSettingsView host, Column column, DGV settings, PropertyDescriptor descriptor)
        {
            _host = host;
            Column = column;
            Name = descriptor.DisplayName;
            Format = column.Format_Actual;
            
            var item = settings.Groups.FirstOrDefault(o => o.Id == Id);
            if (item != null)
                GroupDirection = item.SortDirection;

            IsFrozen = settings.FrozenColumns.Contains(Id);
            Description = descriptor.Description;
            _propertyType = descriptor.PropertyType;
          /*  FilterLine = settings.WhereFilter.FirstOrDefault(f => string.Equals(f.Name,
                ((PropertyDescriptor) descriptor).Name, StringComparison.InvariantCultureIgnoreCase));*/
        }

        public override string ToString() => Name;

        #region ===========  INotifyPropertyChanged  ==============
        public event PropertyChangedEventHandler PropertyChanged;
        internal void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }
}
