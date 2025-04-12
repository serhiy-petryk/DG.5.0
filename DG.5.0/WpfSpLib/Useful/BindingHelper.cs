using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace WpfSpLib.Helpers
{
    public static class BindingHelper_Useful
    {
        private static Dictionary<Type, List<FieldInfo>> _fiOfDpCache = new Dictionary<Type, List<FieldInfo>>();
        public static void UpdateAllBindings(this DependencyObject target)
            // based on 'H.B.' comment in https://stackoverflow.com/questions/5023025/is-there-a-way-to-get-all-bindingexpression-objects-for-a-window
        {
            foreach (var child in (new[] { target })/*.Union(target.GetVisualChildren())*/)
            {
                var fiOfDp = GetFieldInfosOfDp(child.GetType());
                fiOfDp.ForEach(dp => BindingOperations.GetBindingExpression(child, dp.GetValue(child) as DependencyProperty)?.UpdateTarget());
            }
        }
        private static List<FieldInfo> GetFieldInfosOfDp(Type type)
        {
            if (!_fiOfDpCache.ContainsKey(type))
            {
                var propertiesDp = new List<FieldInfo>();
                var currentType = type;
                while (currentType != typeof(object))
                {
                    propertiesDp.AddRange(currentType.GetFields().Where(x => x.FieldType == typeof(DependencyProperty)));
                    currentType = currentType.BaseType;
                }
                _fiOfDpCache[type] = propertiesDp;
            }

            return _fiOfDpCache[type];
        }
    }
}
