using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfSpLib.Helpers;

namespace WpfSpLib.Common
{
    public static class Tips
    {
        public const double SCREEN_TOLERANCE = 0.001;
        public static bool AreEqual(double d1, double d2) => Math.Abs(d1 - d2) < SCREEN_TOLERANCE;

        public static DateTime MaxDateTime(DateTime date1, DateTime date2) => date1 > date2 ? date1 : date2;

        public static void Beep() => SystemSounds.Beep.Play();

        // ===================================
        public static bool IsTextTrimmed(TextBlock textBlock)
        {
            // see also https://stackoverflow.com/questions/1041820/how-can-i-determine-if-my-textblock-text-is-being-trimmed
            if (textBlock.TextWrapping != TextWrapping.NoWrap) return false;
            
            textBlock.Measure(new Size(double.PositiveInfinity, height: double.PositiveInfinity));
            return (textBlock.ActualWidth + textBlock.Margin.Left + textBlock.Margin.Right) < textBlock.DesiredSize.Width ||
                   (textBlock.ActualHeight + textBlock.Margin.Top + textBlock.Margin.Bottom) < textBlock.DesiredSize.Height;
        }

        #region =============  Colors  =============
        public static Brush GetActualBackgroundBrush(DependencyObject d)
        {
            // valid only for SolidColorBrush
            foreach (var c in d.GetVisualParents().Where(a1 => a1 is Control || a1 is Panel))
            {
                var brush = c is Control ? ((Control)c).Background : ((Panel)c).Background;
                if (brush is SolidColorBrush)
                {
                    var color = ((SolidColorBrush)brush).Color;
                    if (color != Colors.Transparent)
                        return brush;
                }
                else if (brush != null)
                    return brush;
            }
            return null;
        }
        public static Brush GetActualForegroundBrush(DependencyObject d)
        {
            // valid only for SolidColorBrush
            foreach (var o in d.GetVisualParents().Where(a1 => a1 is Control))
            {
                var c = (Control)o;
                var brush = c.Foreground;
                if (brush is SolidColorBrush)
                {
                    if (((SolidColorBrush)brush).Color != Colors.Transparent)
                        return brush;
                }
                else if (brush != null)
                    return brush;
            }
            return null;
        }

        public static Color GetActualBackgroundColor(DependencyObject d)
        {
            var color = GetColorFromBrush(GetActualBackgroundBrush(d));
            return color;
        }
        public static Color GetActualForegroundColor(DependencyObject d)
        {
            var color = GetColorFromBrush(GetActualForegroundBrush(d));
            return color;
        }
        public static Color GetColorFromBrush(Brush brush)
        {
            if (brush is SolidColorBrush)
                return ((SolidColorBrush)brush).Color;
            if (brush is LinearGradientBrush)
            {
                var gcs = ((LinearGradientBrush)brush).GradientStops;
                var color = gcs[gcs.Count / 2].Color;
                return color;
            }
            return Colors.Transparent;
        }
        #endregion

        #region =============  Type Utilities ==============
        public static Type GetNotNullableType(Type type) => IsNullableType(type) ? Nullable.GetUnderlyingType(type) : type;
        public static bool IsNullableType(Type type) => type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static object GetDefaultOfType(Type type)
        {
            if (type == typeof(DateTime))
                return DateTime.Today;

            var currentType = MethodBase.GetCurrentMethod().DeclaringType;
            var method = currentType.GetMethod("GetDefaultGeneric", BindingFlags.Static | BindingFlags.NonPublic);
            return method.MakeGenericMethod(type).Invoke(null, null);
        }
        private static T GetDefaultGeneric<T>() => default(T);

        //==================
        public static Type TryGetType(string typeName)
        {
            Type t = Type.GetType(typeName, false, false);
            if (t != null) return t;
            int i = typeName.IndexOf(",", StringComparison.Ordinal);
            string name = typeName;
            string assemblyName = null;
            if (i >= 0)
            {
                name = typeName.Substring(0, i).Trim();
                assemblyName = typeName.Substring(i + 1).Trim();
            }

            List<string> ss = new List<string>();
            Assembly[] aLoaded = Thread.GetDomain().GetAssemblies();
            // Searching from last loaded (A few copies of one assembly can be in Design Mode)
            for (int i1 = (aLoaded.Length - 1); i1 >= 0; i1--)
            {
                Assembly a = aLoaded[i1];
                string assemblyKey = a.GetName().Name;
                if (!ss.Contains(assemblyKey))
                {
                    t = TryGetTypeFromAssembly(name, assemblyName, a);
                    if (t != null) return t;
                    //          ss.Add(assemblyKey);
                }
            }
            /*      foreach (Assembly a in aLoaded) {
                    string assemblyKey  =a.GetName().Name;
                    if (!ss.Contains(assemblyKey)) {
                      t = TryGetTypeFromAssembly(name, assemblyName, a);
                      if (t != null) return t;
                      ss.Add(assemblyKey);
                    }
                  }*/
            return null;
        }

        private static Type TryGetTypeFromAssembly(string userTypeName, string userAssemblyName, Assembly assembly)
        {
            if (String.IsNullOrEmpty(userTypeName)) return null;
            string assName = assembly.GetName().Name;
            if (String.IsNullOrEmpty(userAssemblyName) || assName == userAssemblyName || assName.StartsWith(userAssemblyName + ","))
            {
                Type type = assembly.GetType(userTypeName, false, false);
                if (type != null) return type;
            }
            return null;
        }
        #endregion
    }
}
