using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace WpfSpLib.Common
{
    public class GetParentOfTypeConverter : IValueConverter
    {
        public static GetParentOfTypeConverter Instance = new GetParentOfTypeConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DependencyObject @do && parameter is Type type)
            {
                var parent = @do.GetVisualParents().FirstOrDefault(o => type.IsInstanceOfType(o));
                return parent;
            }
            throw new ArgumentException($"GetParentOfTypeConverter error! Argument 'value' must be 'DependencyObject type' and argument 'parameter' must be 'Type' type");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringToGeometryConverter : IValueConverter
    {
        public static StringToGeometryConverter Instance = new StringToGeometryConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Geometry.Parse((string)value);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class DummyConverter : DependencyObject, IValueConverter
    {
        public static DummyConverter Instance = new DummyConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class FocusVisualConverter : DependencyObject, IValueConverter
    {
        public static FocusVisualConverter CornerRadius = new FocusVisualConverter();
        public static FocusVisualConverter Brush = new FocusVisualConverter { _brush = true };
        public static FocusVisualConverter Margin = new FocusVisualConverter { _margin = true };
        private bool _margin;
        private bool _brush;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AdornerControl adorner && adorner.AdornedElement is FrameworkElement element)
            {
                // Focus brush
                if (_brush)
                {
                    var color = Tips.GetActualBackgroundColor(element);
                    var focusBrush = (Brush)ColorHslBrush.Instance.Convert(color, typeof(Brush), parameter, null);
                    return focusBrush;
                }

                // Margin
                var focusThickness = ((Control)adorner.Child).BorderThickness;
                if (_margin)
                    return new Thickness(-focusThickness.Left, -focusThickness.Top, -focusThickness.Right, -focusThickness.Bottom);

                // CornerRadius
                var radius = ControlHelper.GetCornerRadius(element);
                if (targetType == typeof(CornerRadius))
                {
                    if (radius.HasValue)
                    {
                        var newRadius = new CornerRadius(
                            radius.Value.TopLeft + Math.Max(focusThickness.Left, focusThickness.Top) / 2,
                            radius.Value.TopRight + Math.Max(focusThickness.Top, focusThickness.Right) / 2,
                            radius.Value.BottomRight + Math.Max(focusThickness.Right, focusThickness.Bottom) / 2,
                            radius.Value.BottomLeft + Math.Max(focusThickness.Bottom, focusThickness.Left) / 2);
                        return newRadius;

                    }
                    return new CornerRadius();
                }
                // target type = typeof(double)
                var averageThicknessWidth = (focusThickness.Left + focusThickness.Top + focusThickness.Right + focusThickness.Bottom) / 4;
                if (radius.HasValue)
                    return averageThicknessWidth + (radius.Value.TopLeft + radius.Value.TopRight + radius.Value.BottomRight + radius.Value.BottomLeft) / 4;

                return averageThicknessWidth;
            }

            if (targetType == typeof(CornerRadius))
                return new CornerRadius();
            if (targetType == typeof(Brush))
                return Brushes.Transparent;
            return 0.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ChangeTypeConverter : DependencyObject, IValueConverter
    {
        public static ChangeTypeConverter Instance = new ChangeTypeConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Tips.GetDefaultOfType(targetType);
            
            var type = Tips.GetNotNullableType(targetType);
            if (value.GetType() == type)
                return value;

            if (value is IConvertible && type == typeof(string))
                return ((IConvertible)value).ToString(culture);
            if (value is IFormattable && type == typeof(string))
                return ((IFormattable)value).ToString(null, culture);

            try
            {
                return System.Convert.ChangeType(value, type, culture);
            }
            catch { }

            throw new NotImplementedException($"Type converter for {targetType.Name} isn't implemented");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
    }

    // is using in ToggleGridVisibilityButton (DG3.1 project)
    public class GridLinesVisibilityConverter : IValueConverter
    {
        public static GridLinesVisibilityConverter Instance = new GridLinesVisibilityConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Equals(value, DataGridGridLinesVisibility.None);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Equals(value, false) ? DataGridGridLinesVisibility.All : DataGridGridLinesVisibility.None;
    }

    public class VisibilityConverter : IValueConverter
    {
        public static VisibilityConverter Instance = new VisibilityConverter();
        public static VisibilityConverter InverseInstance = new VisibilityConverter {_inverse = true};
        private bool _inverse;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _inverse ^ (value == null || Equals(value, false) || Equals(value, 0) || Equals(value, ""))
                ? (Equals(parameter, "Hide") ? Visibility.Hidden : Visibility.Collapsed)
                : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /// <summary>
    /// Math converter. Support single and multiple input values (IValueConverter, IMultiValueConverter)
    /// Support polish notation (https://en.wikipedia.org/wiki/Polish_notation). Math expressions are stored in ConverterParameter.
    /// Supported operators: '+,-,*,/,%' for double, '!' for boolean. Supported types of return value: double, bool, Thickness, GridLength.
    /// Usage example (single input value):
    ///             Height="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=ActualWidth, Converter={x:Static common:MathConverter.Instance}, ConverterParameter=12%8+}">
    /// Usage example (multiple input values) (OutputValue = (ActualWidth + ActualHeight)/2 * 10 /100 + 8 => 10% of average of ActualWidth and ActualHeight, and plus 8):
    ///             <Thumb.Width>
    ///                 <MultiBinding Converter="{x:Static common:MathConverter.Instance}" ConverterParameter="+2/10%8+">
    ///                     <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="ActualWidth" />
    ///                     <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="ActualHeight" />
    ///                 </MultiBinding>
    ///             </Thumb.Width>
    /// </summary>
    public class MathConverter : IValueConverter, IMultiValueConverter
    {
        /// <summary>
        /// Math converter. Support single and multiple input values (IValueConverter, IMultiValueConverter)
        /// Support polish notation (https://en.wikipedia.org/wiki/Polish_notation). Math expressions are stored in ConverterParameter.
        /// Supported operators: '+,-,*,/,%' for double, '!' for boolean. Supported types of return value: double, bool, Thickness, GridLength, Visibility.
        /// Usage example (single input value):
        ///             Height="{Binding RelativeSource={RelativeSource TemplatedParent}, Path=ActualWidth, Converter={x:Static common:MathConverter.Instance}, ConverterParameter=12%8+}">
        /// Usage example (multiple input values) (OutputValue = (ActualWidth + ActualHeight)/2 * 10 /100 + 8 => 10% of average of ActualWidth and ActualHeight, and plus 8):
        ///             <Thumb.Width>
        ///                 <MultiBinding Converter="{x:Static common:MathConverter.Instance}" ConverterParameter="+2/10%8+">
        ///                     <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="ActualWidth" />
        ///                     <Binding RelativeSource="{RelativeSource TemplatedParent}" Path="ActualHeight" />
        ///                 </MultiBinding>
        ///             </Thumb.Width>
        /// </summary>
        public static MathConverter Instance = new MathConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(new[] {value}, targetType, parameter, culture);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var operand = "";
            var operands = new List<object>(values);

            var s = (parameter as string ?? "").Trim();
            s = s.Replace("Max", "↑", StringComparison.InvariantCultureIgnoreCase);
            s = s.Replace("Min", "↓", StringComparison.InvariantCultureIgnoreCase);

            foreach (var c in s)
            {
                if (c == '.' || char.IsDigit(c))
                {
                    operand += c;
                    continue;
                }

                if (!string.IsNullOrEmpty(operand))
                    operands.Add(operand);
                operand = "";

                if (c != ' ') Calculate(c);
            }

            if (operands.Count != 1)
                throw new Exception($"MathConverter error. Invalid number of operands at the end of calculation. Should be 1, but is {operands.Count}");

            if (targetType == typeof(Thickness))
                return new Thickness(GetDouble(operands[0]));
            if (targetType == typeof(GridLength))
                return new GridLength(GetDouble(operands[0]));
            if (targetType == typeof(double))
                return GetDouble(operands[0]);
            if (targetType == typeof(bool))
                return GetBool(operands[0]);
            if (targetType == typeof(Visibility))
                return GetVisibility(operands[0]);
            throw new Exception($"MathConverter error. Unsupported target type of return value: {targetType.Name}");

            void Calculate(char _operator)
            {
                if (_operator == '+' && operands.Count >= 2)
                    operands[operands.Count - 2] = GetDouble(operands[operands.Count - 2]) + GetDouble(operands[operands.Count - 1]);
                else if (_operator == '-' && operands.Count >= 2)
                    operands[operands.Count - 2] = GetDouble(operands[operands.Count - 2]) - GetDouble(operands[operands.Count - 1]);
                else if (_operator == '*' && operands.Count >= 2)
                    operands[operands.Count - 2] = GetDouble(operands[operands.Count - 2]) * GetDouble(operands[operands.Count - 1]);
                else if (_operator == '/' && operands.Count >= 2)
                    operands[operands.Count - 2] = GetDouble(operands[operands.Count - 2]) / GetDouble(operands[operands.Count - 1]);
                else if (_operator == '%' && operands.Count >= 2)
                    operands[operands.Count - 2] = GetDouble(operands[operands.Count - 2]) * (GetDouble(operands[operands.Count - 1]) / 100.0);
                else if (_operator == '↑' && operands.Count >= 2)
                    operands[operands.Count - 2] = Math.Max(GetDouble(operands[operands.Count - 2]), GetDouble(operands[operands.Count - 1]));
                else if (_operator == '↓' && operands.Count >= 2)
                    operands[operands.Count - 2] = Math.Min(GetDouble(operands[operands.Count - 2]), GetDouble(operands[operands.Count - 1]));
                else if (_operator == '!' && operands.Count >= 1)
                    operands[operands.Count - 1] = !GetBool(operands[operands.Count - 1]);
                else
                {
                    if (operands.Count < 1 && _operator == '!')
                        throw new Exception($"MathConverter error. There are no operand for '{_operator}' operator");
                    if (operands.Count < 2)
                        throw new Exception($"MathConverter error. Not enough operands '{_operator}' operator. Should be 2, but are {operands.Count}");
                    throw new Exception($"MathConverter error. Undefined operator: {_operator}");
                }

                if (_operator != '!')
                    operands.RemoveAt(operands.Count - 1);
            }

            double GetDouble(object o)
            {
                if (o is double d) return d;
                if (o is string s1) return double.Parse(s1, LocalizationHelper.InvariantCulture);
                try
                {
                    var o1 = System.Convert.ToDouble(o);
                    return o1;
                }
                catch (Exception ex)
                {
                    throw new Exception($"MathConverter error. Can't convert {o} to double data type");
                }
            }

            bool GetBool(object o)
            {
                if (o == null) return false;
                if (o is bool b) return b;
                if (o is string s1) return !string.IsNullOrEmpty(s1);
                try
                {
                    var n = System.Convert.ToInt64(o);
                    return n != 0;
                }
                catch (Exception ex)
                {
                    throw new Exception($"MathConverter error. Can't convert {o} to boolean data type");
                }
            }

            Visibility GetVisibility(object o) => GetBool(o) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class LeftRightButtonConverter : IValueConverter
    {
        public static LeftRightButtonConverter LeftUpPolygonPoints = new LeftRightButtonConverter { _isLeftUpPolygonPoints = true };
        public static LeftRightButtonConverter RightDownPolygonPoints = new LeftRightButtonConverter();
        public static LeftRightButtonConverter BorderWidth = new LeftRightButtonConverter{_isBorderWidth = true};

        private bool _isLeftUpPolygonPoints = false;
        private bool _isBorderWidth = false;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value is FrameworkElement fe)
            {
                double.TryParse(fe.Tag as string, out var temp);
                if (_isBorderWidth)
                    return temp;

                var border = temp / 2;
                var height = fe.ActualHeight - border;
                var width = fe.ActualWidth - border;

                if (_isLeftUpPolygonPoints)
                    return new PointCollection(new[] { new Point(border, border), new Point(width, border), new Point(border, height) });
                
                return new PointCollection(new[] { new Point(border, height), new Point(width, border), new Point(width, height) });
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
