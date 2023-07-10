using System;
using System.Collections.Generic;

namespace WpfSpLib.Common
{
    public class DataTypeMetadata
    {
        public enum DataType { String, Bool, Byte, Short, Integer, Long, Double, Decimal, Date, DateTime, Time }
        public enum DataTemplate { TextBox, CheckBox, NumericBox, DatePicker, DateTimePicker, TimePicker }

        public static readonly Dictionary<DataType, DataTypeMetadata> MetadataList =
            new Dictionary<DataType, DataTypeMetadata>
            {
                {DataType.String, new DataTypeMetadata(typeof(string), DataTemplate.TextBox)},
                {DataType.Bool, new DataTypeMetadata(typeof(bool), DataTemplate.CheckBox)},
                {DataType.Date, new DataTypeMetadata(typeof(DateTime), DataTemplate.DatePicker, DateTime.MinValue.Date, DateTime.MaxValue.Date)},
                {DataType.DateTime, new DataTypeMetadata (typeof(DateTime), DataTemplate.DateTimePicker, DateTime.MinValue, DateTime.MaxValue)},
                {DataType.Time, new DataTypeMetadata (typeof(TimeSpan), DataTemplate.TimePicker,  TimeSpan.Zero, TimeSpan.MaxValue)},
                {DataType.Byte, new DataTypeMetadata (typeof(byte), DataTemplate.NumericBox, byte.MinValue, byte.MaxValue, 0)},
                {DataType.Short, new DataTypeMetadata (typeof(short), DataTemplate.NumericBox, short.MinValue, short.MaxValue, 0)},
                {DataType.Integer, new DataTypeMetadata (typeof(int), DataTemplate.NumericBox, int.MinValue, int.MaxValue, 0)},
                {DataType.Long, new DataTypeMetadata (typeof(long), DataTemplate.NumericBox, long.MinValue, long.MaxValue, 0)},
                {DataType.Double, new DataTypeMetadata( typeof(double), DataTemplate.NumericBox, decimal.MinValue, decimal.MaxValue)},
                {DataType.Decimal, new DataTypeMetadata( typeof(decimal), DataTemplate.NumericBox, decimal.MinValue, decimal.MaxValue)}
            };

        public readonly Type Type;
        public readonly DataTemplate Template;
        public int? DecimalPlaces { get; }
        public readonly object MinValue;
        public readonly object MaxValue;

        public DataTypeMetadata(Type type, DataTemplate template, object minValue = null, object maxValue = null, int? decimalPlaces = null)
        {
            Type = type;
            Template = template;
            DecimalPlaces = decimalPlaces;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}
