using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WpfSpLib.Common.ColorSpaces;
using WpfSpLib.Helpers;

namespace WpfSpLib.Common
{
    internal static class ColorConverterHelper
    {
        internal static double ConvertValue(double value, object parameter, bool? isUp = null, double multiplier = 100.0)
        {
            /* Process parameter of ColorHslBrush/ColorLabBrush/ColorGrayScaleBrush
             value is double in range [0-1.0]
             format of parameter: [+ or -]NN[%] where NN is double between 0-100
             split is double in range [0-1.0] or null

            1. split = null ( no split):
             NN[%] return = NN / 100
             +NN  return = value + NN / 100
             -NN    return = value - NN / 100
             +NN% return = value + (1 - value) * NN/100
             -NN%:  return = value - value * NN/100

            2. split has value:
             NN[%] return = NN / 100

             if (value < split)
             +NN  return = value + NN / 100
             -NN    return = value - NN / 100
             +NN% return = value + (1 - value) * NN/100
             -NN%:  return = value - value * NN/100

             if (value >= split)
             +NN  return = value - NN / 100
             -NN    return = value + NN / 100
             +NN% return = value - value * NN/100
             -NN%:  return = value + (1 - value) * NN/100

             return: double in [0-1.0]  or null(if bad parameter)
             */

            if (parameter is string sParameter)
            {
                var isEqual = true;
                var isPlus = true;
                var isPercent = false;

                if (sParameter.StartsWith("+"))
                {
                    isEqual = false;
                    sParameter = sParameter.Substring(1);
                }
                else if (sParameter.StartsWith("-"))
                {
                    isEqual = false;
                    isPlus = false;
                    sParameter = sParameter.Substring(1);
                }

                if (sParameter.EndsWith("%"))
                {
                    isPercent = true;
                    sParameter = sParameter.Substring(0, sParameter.Length - 1);
                }

                double temp;
                if (double.TryParse(sParameter, NumberStyles.Any, LocalizationHelper.InvariantCulture, out temp))
                {
                    double result;
                    var dParameter = temp / multiplier;

                    if (isEqual)
                        return dParameter;

                    if (isUp.HasValue)
                    {
                        var isValueLess = isUp.Value;
                        if ((isPercent && isPlus && isValueLess) || isPercent && !isPlus && !isValueLess)
                            result = value + (1.0 - value) * dParameter;
                        else if (isPercent)
                            result = value - value * dParameter;
                        else if (isPlus && isValueLess || !isPlus && !isValueLess)
                            result = value + dParameter;
                        else
                            result = value - dParameter;
                    }
                    else
                    {
                        if (isPercent && isPlus)
                            result = value + (1.0 - value) * dParameter;
                        else if (isPercent)
                            result = value - value * dParameter;
                        else if (isPlus)
                            result = value + dParameter;
                        else
                            result = value - dParameter;
                    }

                    result = Math.Round(result, 9);
                    if (result > 1.0)
                        result -= 1.0;
                    else if (result < 0.0)
                        result += 1.0;
                    return result;
                }
            }
            return value;
        }

        /// <summary>
        /// Return HSL which is modified according to the parameter
        /// </summary>
        /// <param name="hsl"></param>
        /// <param name="parameter">See parameter format in comment of internal ColorConverterHelper.ConvertValue method</param>
        /// <param name="noSplit"></param>
        /// <returns></returns>
        internal static HSL ModifyHsl(HSL hsl, string parameter, bool noSplit)
        {
            foreach (var p in parameter.Split('/'))
            {
                var isDarkColor = ColorUtils.IsDarkColor(hsl.RGB.Color);
                var pp = p.Split(':');
                var newS = hsl.S;
                var newH = hsl.H;
                if (pp.Length > 2)
                    newH = ConvertValue(hsl.H, pp[pp.Length - 3], !noSplit ? hsl.H < 0.5 : (bool?) null, 360);
                if (pp.Length > 1)
                    newS = ConvertValue(hsl.S, pp[pp.Length - 2], !noSplit ? hsl.S < 0.5 : (bool?)null);
                var newL = ConvertValue(hsl.L, pp[pp.Length - 1], !noSplit ? isDarkColor : (bool?) null);
                hsl = new HSL(newH, newS, newL);
            }

            return hsl;
        }

    }

    public class ColorHslBrush : IValueConverter
    {
        public static ColorHslBrush Instance = new ColorHslBrush();
        public static ColorHslBrush NoSplit = new ColorHslBrush { _noSplit = true };

        private bool _noSplit = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter">Set of transformations HSL divided by '/'. Transformation syntax: 'H:S:L' or 'S:L' or 'L' where H/S/L mean changes of HSL components. See format of changes in comments of ColorConverterHelper.ConvertValue method</param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
                throw new Exception("Trap for string, .., etc");

            HSL hsl = null;
            Color? oldColor = null;
            if (value is HSL)
                hsl = (HSL)value;
            else if (value is Brush brush)
                oldColor = Tips.GetColorFromBrush(brush);
            else if (value is Color color)
                oldColor = color;
            else if (value is DependencyObject d)
                oldColor = Tips.GetActualBackgroundColor(d);

            if (oldColor.HasValue)
                hsl = new HSL(new RGB(oldColor.Value));

            if (!(parameter is string p1 && string.IsNullOrEmpty(p1)))
            {
                if (hsl == null)
                    hsl = new HSL();

                if (parameter is string p)
                    hsl = ColorConverterHelper.ModifyHsl(hsl, p, _noSplit);

                if (Tips.GetNotNullableType(targetType) == typeof(Color))
                    return hsl.RGB.GetColor(oldColor?.A / 255.0 ?? 1.0);
                return new SolidColorBrush(hsl.RGB.GetColor(oldColor?.A / 255.0 ?? 1.0));
            }

            if (Tips.GetNotNullableType(targetType) == typeof(Color))
                return Colors.Transparent;
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ColorLabBrush : IValueConverter
    {
        public static ColorLabBrush Instance = new ColorLabBrush();
        public static ColorLabBrush NoSplit = new ColorLabBrush { _noSplit = true };

        private bool _noSplit = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
                throw new Exception("Trap for string, .., etc");

            LAB lab = null;
            if (value is Brush brush)
                lab = new LAB(new RGB(Tips.GetColorFromBrush(brush)));
            else if (value is DependencyObject d)
                lab = new LAB(new RGB(Tips.GetActualBackgroundColor(d)));

            if (lab != null)
            {
                var color = lab.RGB.Color;
                var isDarkColor = ColorUtils.IsDarkColor(color);
                var newL = ColorConverterHelper.ConvertValue(lab.L / 100.0, parameter, !_noSplit ? isDarkColor : (bool?)null);
                if (Tips.GetNotNullableType(targetType) == typeof(Color))
                    return new LAB(newL * 100.0, lab.A, lab.B).RGB.Color;
                return new SolidColorBrush(new LAB(newL * 100.0, lab.A, lab.B).RGB.Color);

            }

            if (Tips.GetNotNullableType(targetType) == typeof(Color))
                return Colors.Transparent;
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ColorGrayScaleBrush : IValueConverter
    {
        public static ColorGrayScaleBrush Instance = new ColorGrayScaleBrush();
        public static ColorGrayScaleBrush NoSplit = new ColorGrayScaleBrush { _noSplit = true };

        private bool _noSplit = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color? color = null;
            if (value is Brush brush)
                color = Tips.GetColorFromBrush(brush);
            else if (value is DependencyObject d)
                color = Tips.GetActualBackgroundColor(d);

            if (color.HasValue)
            {
                var oldGrayLevel = ColorUtils.GetGrayLevel(new RGB(color.Value)) / 255.0;
                var isDarkColor = ColorUtils.IsDarkColor(color.Value);
                var newGrayLevel = ColorConverterHelper.ConvertValue(oldGrayLevel, parameter, !_noSplit ? isDarkColor : (bool?)null);
                var newGrayValue = System.Convert.ToByte(newGrayLevel * 255.0);
                return new SolidColorBrush(Color.FromRgb(newGrayValue, newGrayValue, newGrayValue));
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class GradientMonochromeBrush : IValueConverter
    {
        public static GradientMonochromeBrush Instance = new GradientMonochromeBrush();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter">GradientStops divided by '/'. Every element is in format: \lightness(double),offset(double)\</param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string)
            {
                HSL hsl = null;
                if (value is HSL)
                    hsl = (HSL) value;
                else if (value is Brush brush)
                    hsl = new HSL(new RGB(Tips.GetColorFromBrush(brush)));

                if (hsl != null)
                {
                    var gradientStops = new GradientStopCollection();
                    foreach (var p in parameter.ToString().Split('/'))
                    {
                        var pp = p.Split(',');
                        if (pp.Length == 2)
                        {
                            var newHsl = ColorConverterHelper.ModifyHsl(hsl, pp[0], false);
                            var offset = double.Parse(pp[1].Trim(), LocalizationHelper.InvariantCulture);
                            gradientStops.Add(new GradientStop(newHsl.RGB.Color, offset));
                        }
                    }

                    return new LinearGradientBrush(gradientStops, new Point(0, 0), new Point(0, 1));
                }
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
