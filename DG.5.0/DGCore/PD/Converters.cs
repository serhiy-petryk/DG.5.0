using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace DGCore.PD {
  public class ByteArrayToHexStringConverter : TypeConverter
  {
    public static void Test()
    {
      var c = new ByteArrayToHexStringConverter();
      var bb = new byte[] { 0, 1, 2, 255 };
      var s = c.ConvertFrom(bb);
      var z = c.ConvertTo("0x55", typeof(byte[]));
    }
    //-----------------------
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(byte[]);
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(byte[]);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
      if (value == null || value == DBNull.Value)
        return null;

      if (!(value is byte[])) return base.ConvertFrom(context, culture, value);

      var bb = (byte[])value;
      var hex = new StringBuilder("0x", bb.Length * 2 + 2);
      foreach (var b in bb)
        hex.AppendFormat("{0:X2}", b);
      return hex.ToString();
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      if (value == null || value == DBNull.Value)
        return (byte[])null;

      if (destinationType != typeof(byte[]) || !(value is string))
        return base.ConvertTo(context, culture, value, destinationType);

      var hex = (string)value;
      if (!hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        throw new Exception("String have to start with '0x' prefix");
      if ((hex.Length % 2) != 0)
        throw new Exception("Number of characters in string must be even");

      var cnt = 1;
      foreach (var c in hex.Substring(2))
      {
        if (!IsHexSymbol(c))
          throw new Exception($"{cnt}-th character of hex string ('{c}') is not hexadecimal symbol");
        cnt++;
      }

      var numberChars = hex.Length - 2;
      var bytes = new byte[numberChars / 2];
      for (var i = 0; i < numberChars; i += 2)
        bytes[i / 2] = Convert.ToByte(hex.Substring(i + 2, 2), 16);

      return bytes;
    }

    private static bool IsHexSymbol(char c) => ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
  }

  //==============  ByteArrayToGuidStringConverter  ================
  public class ByteArrayToGuidStringConverter : TypeConverter
  {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(byte[]);
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(byte[]);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
      if (value == null || value == DBNull.Value)
        return null;

      if (!(value is byte[])) return base.ConvertFrom(context, culture, value);

      return (new Guid((byte[])value)).ToString();
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      if (value == null || value == DBNull.Value)
        return (byte[])null;

      if (destinationType != typeof(byte[]) || !(value is Guid))
        return base.ConvertTo(context, culture, value, destinationType);

      return Guid.Parse(value.ToString()).ToByteArray();
    }
  }


  /*//======================  DummyEditor   ========================
  public class DummyEditor : UITypeEditor {
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
      return UITypeEditorEditStyle.None;
    }
  }*/

}
