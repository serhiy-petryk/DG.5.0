using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using DGCore.Utils;

namespace DGCore.Common
{
  public partial class Enums
  {
    [TypeConverter(typeof(FilterOperandTypeConverter))]
    public enum FilterOperand
    {
      None = 0,
      Contains = 1,
      NotContains = 2,
      Equal = 3,
      NotEqual = 4,
      Between = 5,
      NotBetween = 6,
      Less = 7,
      NotLess = 8,
      Greater = 9,
      NotGreater = 10,
      StartsWith = 11,
      NotStartsWith = 12,
      EndsWith = 13,
      NotEndsWith = 14,
      CanBeNull = 15
    }

    //=================  SubClass OperandTypeConverter  ================
    public class FilterOperandTypeConverter : TypeConverter
    {
      public static FilterOperand[] GetPossibleOperands(Type propertyType, bool propertyCanBeNull)
      {
        Type notNullableValueType = Types.GetNotNullableType(propertyType);
        List<FilterOperand> oo = new List<FilterOperand>();
        switch (notNullableValueType.Name)
        {
          case "String":
            // All values for strings + canBeNull if necessary (for database filter)
            oo.AddRange((IEnumerable<FilterOperand>)Enum.GetValues(typeof(FilterOperand)));
            if (!propertyCanBeNull) oo.Remove(FilterOperand.CanBeNull); // Remove canBeNull
            break;
          case "Boolean":
            // We need only 'equal' operand for for boolean type
            oo.AddRange(new[] { FilterOperand.None, FilterOperand.Equal });
            break;
          default:
            oo.Add(FilterOperand.None);
            if (notNullableValueType.GetInterface("IEquatable`1") != null ||
                notNullableValueType.GetMethod("Equals",
                  BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding, null,
                  new Type[] { typeof(object) }, null) != null ||
                notNullableValueType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public) != null)
            {
              oo.Add(FilterOperand.Equal);
              oo.Add(FilterOperand.NotEqual);
            }

            if (notNullableValueType.GetInterface("IComparable`1") != null ||
                notNullableValueType.GetInterface("IComparable") != null || (
                  notNullableValueType.GetMethod("op_LessThan", BindingFlags.Static | BindingFlags.Public) != null &&
                  notNullableValueType.GetMethod("op_GreaterThan", BindingFlags.Static | BindingFlags.Public) != null))
            {
              oo.AddRange(new[]
              {
                FilterOperand.Between, FilterOperand.NotBetween, FilterOperand.Less, FilterOperand.NotLess, FilterOperand.Greater, FilterOperand.NotGreater
              });
            }

            break;
        }

        if (propertyCanBeNull && !oo.Contains(FilterOperand.CanBeNull)) oo.Add(FilterOperand.CanBeNull);
        return oo.ToArray();
      }

      public static int GetParameterQuantity(FilterOperand operand)
      {
        switch (operand)
        {
          case FilterOperand.None:
          case FilterOperand.CanBeNull:
            return 0;
          case FilterOperand.Between:
          case FilterOperand.NotBetween: return 2;
          default: return 1;
        }
      }

      private static string[] operandDisplayName = {
        "", "містить", "не містить", "=", "<>", "між", "не між", "<", ">=", ">", "<=",
        "починається з", "не починається з", "закінчується на", "не закінчується на", "є пустим"
      };
      private static FilterOperand Operand_GetOperandByName(string displayName) => (FilterOperand)Array.IndexOf<string>(operandDisplayName, displayName);

      //================================

      public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || sourceType == typeof(DBNull);

      public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

      public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture,
        object value)
      {
        if (value == null || value.GetType() != typeof(string)) return FilterOperand.None;
        return Operand_GetOperandByName((string)value);
      }

      public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture,
        object value, Type destinationType)
      {
        if (value == null || destinationType != typeof(string)) return null;
        return operandDisplayName[Convert.ToInt32(value)];
      }
    }

  }
}
