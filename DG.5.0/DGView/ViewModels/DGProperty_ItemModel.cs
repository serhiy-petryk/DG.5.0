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
            get => Column.Format;
            set
            {
                Column.Format = value;
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

        private readonly Type _propertyType;

        public DGProperty_ItemModel(DGEditSettingsView host, Column column, DGV settings, IMemberDescriptor descriptor)
        {
            _host = host;
            Column = column;
            Name = ((PropertyDescriptor)descriptor).DisplayName;
            Format = descriptor.Format;
            
            var item = settings.Groups.FirstOrDefault(o => o.Id == Id);
            if (item != null)
                GroupDirection = item.SortDirection;

            IsFrozen = settings.FrozenColumns.Contains(Id);
            Description = ((PropertyDescriptor)descriptor).Description;
            _propertyType = ((PropertyDescriptor)descriptor).PropertyType;
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
