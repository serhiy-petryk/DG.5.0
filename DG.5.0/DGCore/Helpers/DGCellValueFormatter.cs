using System;
using System.ComponentModel;
using System.Globalization;
using DGCore.PD;

namespace DGCore.Helpers
{
    public class DGCellValueFormatter
    {
        private static readonly TypeConverter _dateTimeConverter = new DateTimeConverter();

        //===========================
        public Func<object, object> ValueForPrinterGetter { get; }
        public Func<object, object> ValueForClipboardGetter { get; }
        public Func<object, string> StringForFindTextGetter;

        public readonly bool IsValid;
        public readonly Type PropertyType;

        private readonly PropertyDescriptor _pd;
        private string _format;

        public DGCellValueFormatter(PropertyDescriptor propertyDescriptor)
        {
            IsValid = propertyDescriptor != null;
            if (!IsValid)
            {
                ValueForPrinterGetter = item => null;
                ValueForClipboardGetter = item => null;
                StringForFindTextGetter = item => null;
                return;
            }

            _format = ((IMemberDescriptor)propertyDescriptor).Format;
            _pd = propertyDescriptor;
            PropertyType = Utils.Types.GetNotNullableType(_pd.PropertyType);
            var converter = _pd.Converter;

            if (PropertyType == typeof(string))
            {
                ValueForPrinterGetter = item => _pd.GetValue(item);
                ValueForClipboardGetter = ValueForPrinterGetter;
                StringForFindTextGetter = item => (string)_pd.GetValue(item);
            }
            else if (PropertyType == typeof(byte[]))
            {
                ValueForPrinterGetter = item => _pd.GetValue(item);
                ValueForClipboardGetter = ValueForPrinterGetter;
                StringForFindTextGetter = item => null;
            }
            else if (PropertyType == typeof(bool))
            {
                ValueForPrinterGetter = item => _pd.GetValue(item);
                ValueForClipboardGetter = item => _pd.GetValue(item)?.ToString();
                StringForFindTextGetter = item => null;
            }
            else if (PropertyType == typeof(DateTime) && string.IsNullOrEmpty(_format))
            {
                ValueForPrinterGetter = item => _dateTimeConverter.ConvertTo(null, CultureInfo.CurrentCulture, _pd.GetValue(item), typeof(string));
                ValueForClipboardGetter = ValueForPrinterGetter;
                StringForFindTextGetter = item => (string)_dateTimeConverter.ConvertTo(null, CultureInfo.CurrentCulture, _pd.GetValue(item), typeof(string));
            }
            else if (typeof(IFormattable).IsAssignableFrom(PropertyType))
            {
                ValueForPrinterGetter = item => ((IFormattable)_pd.GetValue(item))?.ToString(_format, CultureInfo.CurrentCulture);
                ValueForClipboardGetter = item => _pd.GetValue(item)?.ToString();
                StringForFindTextGetter = item => ((IFormattable)_pd.GetValue(item))?.ToString(_format, CultureInfo.CurrentCulture);
            }
            else if (converter != null)
            {
                ValueForPrinterGetter = item => _pd.GetValue(item)?.ToString();
                ValueForClipboardGetter = ValueForPrinterGetter;
                StringForFindTextGetter = item => _pd.GetValue(item)?.ToString();
            }
            else
                throw new Exception($"Trap!!! DGCellValueFormatter.GetValueForPrint. Data type: {PropertyType}");
        }
    }
}
