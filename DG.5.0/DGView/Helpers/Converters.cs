﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DGCore.DGVList;

namespace DGView.Helpers
{
    public class CanConvertStringTo : IValueConverter
    {
        public static CanConvertStringTo Instance = new CanConvertStringTo();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            DGCore.Utils.Tips.CanConvertStringTo((Type) value);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    /* Not need // bug 96. Format for column of Dynamic type object key value.
    public class DGDynamicObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DGCore.Helpers.DGCellValueFormatter.DynamicObjectConverter(value, targetType, _converter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

        private readonly DGCore.Common.ILookupTableTypeConverter _converter;
        public DGDynamicObjectConverter(object converter)
        {
            _converter = (DGCore.Common.ILookupTableTypeConverter)converter;
        }
    }*/

    public class IsNotNull : IValueConverter
    {
        public static IsNotNull Instance = new IsNotNull();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ListSortDirectionConverter : IValueConverter
    {
        // Converts ListSortDirection to bool and vice versa
        public static ListSortDirectionConverter Instance = new ListSortDirectionConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (Equals(value, ListSortDirection.Ascending)) return false;
            if (Equals(value, ListSortDirection.Descending)) return true;
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (Equals(value, false)) return ListSortDirection.Ascending;
            if (Equals(value, true)) return ListSortDirection.Descending;
            throw new NotImplementedException();
        }
    }

    public class ByteArrayToHexStringConverter : IValueConverter
    {
        public static ByteArrayToHexStringConverter Instance = new ByteArrayToHexStringConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            DGCore.Helpers.DGCellValueFormatter.ByteArrayToHexStringConverter(value, targetType, parameter, culture);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class DGDateTimeConverter : IValueConverter
    {
        public static DGDateTimeConverter Instance = new DGDateTimeConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            DGCore.Helpers.DGCellValueFormatter.DateTimeDefaultConverter.ConvertTo(null, culture, value,
                typeof(string));
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BackgroundOfSelectedMenuItemConverter : IMultiValueConverter
    {
        public static BackgroundOfSelectedMenuItemConverter Instance = new BackgroundOfSelectedMenuItemConverter();
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values == null || values.Length < 2 || !Equals(values[0], values[1]) ? null: Brushes.PaleGreen;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ComparisonConverter : IValueConverter
    {
        public static ComparisonConverter Instance = new ComparisonConverter();
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }

    public class IsGroupItemConverter : IValueConverter
    {
        public static IsGroupItemConverter Instance = new IsGroupItemConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is IDGVList_GroupItem;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
