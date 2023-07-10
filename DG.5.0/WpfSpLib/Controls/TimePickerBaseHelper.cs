using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfSpLib.Controls.TimePickerHelper
{
    #region ===========  Subclasses  ===============
    //=================  SelectorRow  ================
    public class SelectorRow
    {
        public static SelectorRow[] HourRows = GetHourRows();
        public static SelectorRow[] NightHourRows = GetNightHourRows();
        public static SelectorRow[] DayHourRows = GetDayHourRows();
        public static SelectorRow[] MinuteRows = GetMinuteRows();
        private static SelectorRow[] GetHourRows()
        {
            var aa = new SelectorRow[8];
            for (var k = 0; k < aa.Length; k++)
                aa[k] = new SelectorRow(k, 3, false);
            return aa;
        }
        private static SelectorRow[] GetNightHourRows()
        {
            var aa = new SelectorRow[4];
            for (var k = 0; k < aa.Length; k++)
                aa[k] = new SelectorRow(k, 3, true);
            return aa;
        }
        private static SelectorRow[] GetDayHourRows()
        {
            var aa = new SelectorRow[4];
            for (var k = 0; k < aa.Length; k++)
                aa[k] = new SelectorRow(k + 4, 3, true);
            return aa;
        }
        private static SelectorRow[] GetMinuteRows()
        {
            var aa = new SelectorRow[12];
            for (var k = 0; k < aa.Length; k++)
                aa[k] = new SelectorRow(k, 5, false);
            return aa;
        }
        private static string GetCellText(int value, bool isAmPmHour)
        {
            if (isAmPmHour && value == 0)
                return "12";
            if (isAmPmHour && value > 12)
                return (value - 12).ToString();
            return value.ToString();
        }

        //======================
        public string Item0 { get; }
        public string Item1 { get; }
        public string Item2 { get; }
        public string Item3 { get; }
        public string Item4 { get; }
        public readonly int Offset;
        public readonly int BackgroundIndex;
        public SelectorRow(int rowNo, int factor, bool isAmPmHour)
        {
            BackgroundIndex = rowNo / (factor == 3 ? 2 : 3);
            Offset = rowNo * factor;
            Item0 = GetCellText(rowNo * factor, isAmPmHour);
            Item1 = GetCellText(rowNo * factor + 1, isAmPmHour);
            Item2 = GetCellText(rowNo * factor + 2, isAmPmHour);
            Item3 = GetCellText(rowNo * factor + 3, isAmPmHour);
            Item4 = GetCellText(rowNo * factor + 4, isAmPmHour);
        }
    }

    //=========  DataGridRowBackgroundConverter  ===========
    public class DataGridRowBackgroundConverter : DependencyObject, IValueConverter
    {
        private static Brush[] brushes = {
                new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
                new SolidColorBrush(Color.FromRgb(0xEF, 0xEF, 0xFF)),
                new SolidColorBrush(Color.FromRgb(0xEF, 0xFF, 0xEF)),
                new SolidColorBrush(Color.FromRgb(0xFF, 0xEF, 0xEF))
            };

        public static DataGridRowBackgroundConverter Instance = new DataGridRowBackgroundConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var selectorRow = ((DataGridRow)value).DataContext as SelectorRow;
            return brushes[selectorRow.BackgroundIndex];
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    #endregion

}
