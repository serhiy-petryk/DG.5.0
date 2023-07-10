using System;
using System.ComponentModel;
using DGCore.PD;

namespace DGCore.Helpers
{
    public class DGColumnHelper
    {
        public string Name { get; }
        public string DisplayName { get; }
        public int ColumnDisplayIndex { get; }
        public string Format { get; }
        public object GetValue(object component) => _getter(component);
        public Type NotNullableValueType { get; }

        private Func<object, object> _getter;
        // For common columns
        public DGColumnHelper(PropertyDescriptor pd, int columnDisplayIndex)
        {
            Name = pd.Name;
            DisplayName = pd.DisplayName;
            ColumnDisplayIndex = columnDisplayIndex;
            NotNullableValueType = Utils.Types.GetNotNullableType(pd.PropertyType);
            Format = ((IMemberDescriptor)pd).Format;
            _getter = o => pd.GetValue(o);
        }
    }
}
