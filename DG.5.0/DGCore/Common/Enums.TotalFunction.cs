using System;
using System.ComponentModel;
using System.Linq;

namespace DGCore.Common
{
  public partial class Enums
  {
    [TypeConverter(typeof(TotalFunctionTypeConverter))]
    public enum TotalFunction { None, Sum, Average, Minimum, Maximum, Count, First, Last };
    public static TotalFunction[] TotalFunctionsWithoutNull => Enum.GetValues<TotalFunction>().Where(o => o != TotalFunction.None).ToArray();
    
    private class TotalFunctionTypeConverter : TypeConverter
    {
      //==========   Static Section  ===============
      private static string[] TotalFunctionDisplayName = {"", "Сума", "Середнє", "Мінімум", "Максимум", "Кількість", "Перший", "Останній"};
      private static TotalFunction TotalFunction_GetFunctionByName(string displayName) => (TotalFunction)Array.IndexOf(TotalFunctionDisplayName, displayName);

      //==========================
      public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || sourceType == typeof(DBNull);

      public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

      public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
      {
        if (value == null || value.GetType() != typeof(string)) return TotalFunction.None;
        return TotalFunction_GetFunctionByName((string)value);
      }
      public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
      {
        if (value == null || destinationType != typeof(string)) return null;
        return TotalFunctionDisplayName[Convert.ToInt32(value)];
      }
    }

  }
}
