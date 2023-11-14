using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace DGCore.Helpers
{
    public class DGCellValueFormatter
    {
        public static readonly TypeConverter DateTimeDefaultConverter = new DateTimeConverter();

        public static readonly Func<object, Type, object, CultureInfo, string> ByteArrayToHexStringConverter =
            (value, targetType, parameter, culture) =>
            {
                if (Equals(value, null)) return null;

                var bb = (byte[])value;
                var hex = new StringBuilder("0x", bb.Length * 2 + 2);
                foreach (var b in bb)
                    hex.AppendFormat("{0:X2}", b);
                return hex.ToString();
            };

    //===========================
        public Func<object, object> ValueForPrinterGetter { get; }
        public Func<object, object> ValueForClipboardGetter { get; }
        public Func<object, string> StringForFindTextGetter;

        public readonly bool IsValid;

        private readonly PropertyDescriptor _pd;

        public DGCellValueFormatter(PropertyDescriptor propertyDescriptor, string format)
        {
            IsValid = propertyDescriptor != null;
            if (!IsValid)
            {
                ValueForPrinterGetter = item => null;
                ValueForClipboardGetter = item => null;
                StringForFindTextGetter = item => null;
                return;
            }

            _pd = propertyDescriptor;
            var propertyType = Utils.Types.GetNotNullableType(_pd.PropertyType);
            if (propertyType == typeof(byte[]))
            {
                if (string.Equals(format, "image", StringComparison.OrdinalIgnoreCase))
                {
                    ValueForPrinterGetter = item => _pd.GetValue(item);
                    ValueForClipboardGetter = item => null;
                    StringForFindTextGetter = item => null;
                }
                else if (string.Equals(format, "hex", StringComparison.OrdinalIgnoreCase))
                {
                    ValueForPrinterGetter = item => ByteArrayToHexStringConverter;
                    ValueForClipboardGetter = item => ValueForPrinterGetter;
                    StringForFindTextGetter = item => ValueForPrinterGetter(item).ToString();
                }
                else
                {
                    ValueForPrinterGetter = item => _pd.GetValue(item)?.ToString();
                    ValueForClipboardGetter = item => ValueForPrinterGetter;
                    StringForFindTextGetter = item => ValueForPrinterGetter(item)?.ToString();
                }
                return;
            }
            else if (propertyType == typeof(DateTime) && string.IsNullOrEmpty(format))
            {
                ValueForPrinterGetter = item => DateTimeDefaultConverter.ConvertTo(null, CultureInfo.CurrentCulture, _pd.GetValue(item), typeof(string));
                ValueForClipboardGetter = ValueForPrinterGetter;
                StringForFindTextGetter = item => (string)DateTimeDefaultConverter.ConvertTo(null, CultureInfo.CurrentCulture, _pd.GetValue(item), typeof(string));
            }
            else if (propertyType == typeof(bool))
            {
                ValueForPrinterGetter = item => _pd.GetValue(item);
                ValueForClipboardGetter = item => _pd.GetValue(item)?.ToString();
                StringForFindTextGetter = item => null;
            }
            else if (typeof(IFormattable).IsAssignableFrom(propertyType))
            {
                ValueForPrinterGetter = item => ((IFormattable)_pd.GetValue(item))?.ToString(format, CultureInfo.CurrentCulture);
                ValueForClipboardGetter = item => _pd.GetValue(item)?.ToString();
                StringForFindTextGetter = item => ((IFormattable)_pd.GetValue(item))?.ToString(format, CultureInfo.CurrentCulture);
            }
            else
            {
                ValueForPrinterGetter = item =>
                {
                    var value = _pd.GetValue(item);
                    return value == null ? null : (string.IsNullOrEmpty(format) ? value : string.Format(null, format, value));
                };
                ValueForClipboardGetter = item => _pd.GetValue(item)?.ToString();
                StringForFindTextGetter = item =>
                {
                    var value = _pd.GetValue(item);
                    return value == null ? null : (string.IsNullOrEmpty(format) ? value.ToString() : string.Format(null, format, value));
                };

            }
        }
    }
}
