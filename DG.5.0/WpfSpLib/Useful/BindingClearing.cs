using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WpfSpLib.Useful
{
    public static class BindingClearing
    {
        private static Dictionary<Type, List<DependencyProperty>> _propertiesCacheObj = new Dictionary<Type, List<DependencyProperty>>();
        private static Dictionary<Type, List<DependencyProperty>> _propertiesCacheType = new Dictionary<Type, List<DependencyProperty>>();
        private static int _lastCountOfDefaultProviders;
        private static FieldInfo _fiDefaultProvider = typeof(TypeDescriptor).GetField("_defaultProviders", BindingFlags.NonPublic | BindingFlags.Static);

        public static void CheckNewProperties()
        {
            var newCount = (_fiDefaultProvider.GetValue(null) as Hashtable).Count;
            if (newCount != _lastCountOfDefaultProviders)
            {
                _lastCountOfDefaultProviders = newCount;
                _propertiesCacheObj.Clear();
                _propertiesCacheType.Clear();
            }
        }

        public static void MyClearAllBindings(this DependencyObject target, bool strongClearing)
        {
            var children = target is Visual || target is Visual3D ? target.GetVChildren().ToArray() : new[] {target};

            foreach (var child in children)
            {
                ClearAllBindingsOfObject(child, strongClearing);
                // child.ClearAllBindings();
            }
        }

        public static Dictionary<DependencyObject, List<Binding>> GetAllBindings(this DependencyObject target, bool strongClearing)
        {
            var bindings = new Dictionary<DependencyObject, List<Binding>>();
            foreach (var child in target.GetVChildren().ToArray())
            {
                var aa1 = GetAllBindingsOfObject(child, strongClearing);
                if (aa1.Count > 0)
                    bindings.Add(child, aa1);
            }

            var list = bindings.SelectMany(a => a.Value).ToArray();
            // Debug.Print($"GetAllBindings. Element: {bindings.Count}. Count: {list.Length}");
            return bindings;
        }

        //=============
        public static void ClearAllBindingsOfObject(this DependencyObject target, bool strongClearing)
        {
            var hardMethod = true;
            var dps = GetProperties(target, strongClearing);
            dps.ForEach(dp =>
            {
                var binding = BindingOperations.GetBinding(target, dp);
                if (binding != null)
                {
                    BindingOperations.ClearBinding(target, dp);
                    var binding1 = BindingOperations.GetBinding(target, dp);
                    // var x1 = GetDefaultValue(dp.PropertyType);
                    if (binding1 != null)
                    {
                        if (!hardMethod)
                            target.ClearValue(dp);
                        else
                            target.SetValue(dp, GetDefaultValue(dp.PropertyType));
                        // Debug.Print($"ClearBinding: {_clearCount++}, {child}, {(child is FrameworkElement ? ((FrameworkElement)child).Name : null)}, {dp1.Name}, {a1.Path.Path}, {child.GetValue(dp1)}");
                    }

                }
            });

            object GetDefaultValue(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        private static List<Binding> GetAllBindingsOfObject(DependencyObject target, bool strongClearing)
        {
            var bindins = new List<Binding>();
            var dps = GetProperties(target, strongClearing);
            dps.ForEach(dp =>
            {
                var binding = BindingOperations.GetBinding(target, dp);
                if (binding != null)
                    bindins.Add(binding);
            });

            return bindins;
        }


        //===========================
        private static Attribute[] _attrs = { new PropertyFilterAttribute(PropertyFilterOptions.All) };
        private static List<DependencyProperty> GetProperties(DependencyObject obj, bool strongClearing)
        {
            var type = obj.GetType();
            var cache = strongClearing ? _propertiesCacheObj : _propertiesCacheType;
            if (!cache.ContainsKey(type))
            {
                var properties = new List<DependencyProperty>();
                var pdc = strongClearing
                    ? TypeDescriptor.GetProperties(obj, _attrs)
                    : TypeDescriptor.GetProperties(type, _attrs);
                foreach (PropertyDescriptor pd in pdc)
                {
                    var dpd = DependencyPropertyDescriptor.FromProperty(pd);
                    if (dpd != null)
                        properties.Add(dpd.DependencyProperty);
                }

                cache[type] = properties;
            }

            return cache[type];
        }

        private static IEnumerable<DependencyObject> GetVChildren(this DependencyObject current)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
            {
                var child = VisualTreeHelper.GetChild(current, i);

                foreach (var childOfChild in GetVChildren(child))
                    yield return childOfChild;
            }
            yield return current;
        }

        /* Statistics :
         Memory usage: 4,077,252
Memory usage: 10,251,156
GetAllBindings. Element: 266. Count: 417
Time: 608

Memory usage: 4,084,124
Memory usage: 10,267,224
GetAllBindings. Element: 247. Count: 357
Time: 130
Memory usage: 12,947,488

Memory usage: 19,869,468

===========  MwiStartup test ============
Time: 708, 33, 33. Count: 417, 374	(element)
Time: 705, 48, 25. Count: 417, 1	(element + hard)
Time: 660, 474, 30. Count: 417, 62	(MS)
Time: 154, 28, 9. Count: 357, 1		(type + hard)

---------  MwiContainer test  --------
Time: 366, 426, 245. Count: 4410, 20
Time: 371, 5043, 285. Count: 4410, 720
Time: 125, 256, 89. Count: 3710, 20

======================================
======================================
TYPE:
Time: 144, 27, 8, 544. Count: 357, 1, 52
Time: 110, 245, 90, 316. Count: 3710, 20, 600
ELEMENT:
Time: 692, 69, 10, 25. Count: 417, 1, 1
Time: 337, 452, 89, 247. Count: 4410, 20, 20
MS:
Time: 723, 493, 11, 29. Count: 417, 2, 62
Time: 371, 5261, 121, 320. Count: 4410, 20, 720

================================
================================
MS:
Test0: 10,503,284
Test1: 10,576,084
Test2: 10,700,856
Test3: 10,720,380
Test4: 10,738,092
Memory usage: 10,732,864
New WeakRefs: 3

Type:
Test0: 10,507,508
Test1: 10,581,376
Test2: 10,704,720
Test3: 10,724,428
Test4: 10,741,964
Memory usage: 10,729,348
New WeakRefs: 3

Object:
Weak Data: 21
Test0: 17,283,756
Test1: 17,321,920
Test2: 17,433,976
Test3: 17,452,696
Test4: 17,471,208
Memory usage: 17,464,868
New WeakRefs: 3

No clear bindings:
Test0: 10,490,448
Test1: 10,567,028
Test2: 10,691,424
Test3: 10,710,648
Test4: 10,729,348
         */
    }
}
