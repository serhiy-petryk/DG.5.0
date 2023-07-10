using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DGCore.DGVList;

namespace DGView.Helpers
{
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

    public class DGDateTimeConverter : IValueConverter
    {
        public static DGDateTimeConverter Instance = new DGDateTimeConverter();
        private static readonly TypeConverter _converter = TypeDescriptor.GetConverter(typeof(DateTime));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => _converter.ConvertTo(null, culture, value, typeof(string));
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

    public class TestConverter : IValueConverter
    {
        public static TestConverter Instance = new TestConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class OpacityForDataGridRowHeaderConverter : IValueConverter
    {
        public static OpacityForDataGridRowHeaderConverter Instance = new OpacityForDataGridRowHeaderConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value.GetType().Name == "NamedObject" => new row ({{NewItemPlaceholder}}) in DataGrid
            return value == null || Equals(value.GetType().Name, "NamedObject") ? 0.0 : 1.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
