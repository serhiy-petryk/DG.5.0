using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DGCore.DB {

  // Class DbSchemaColumnProperty  ==================================================
  [TypeConverter(typeof(DbSchemaColumnPropertyTypeConverter))]
  // Can not edit string:    [TypeConverter(typeof(ExpandableObjectConverter))]
  public class DbSchemaColumnProperty {
    static Dictionary<string, Dictionary<string,DbSchemaColumnProperty>> _columnProperties = new Dictionary<string, Dictionary<string, DbSchemaColumnProperty>>();

    static DbSchemaColumnProperty() {
      // Init TypeConvertor for Dictionary<string, DbSchemaColumnProperty> (need for PropertyGrid)
      TypeDescriptor.AddAttributes(typeof(Dictionary<string, DbSchemaColumnProperty>), new TypeConverterAttribute(typeof(PD.Converter_Dictionary<string, DbSchemaColumnProperty>)));
    }

    //======================    Static section  ===============================
    // Public methods (read, write)
    public static Dictionary<string,DbSchemaColumnProperty> GetProperties(string key) {
      Dictionary<string,DbSchemaColumnProperty> o;
      _columnProperties.TryGetValue(key, out o);
      return o;
    }
    // ===========================   Object  ==========================
    string _displayName;
    string _description;
    string _masterSql;
    string _dispayFormat;

    public DbSchemaColumnProperty() {
    }
    public DbSchemaColumnProperty(string[] ss) {
      this._displayName = ss[0].Trim();
      if (ss.Length > 1) this._description = ss[1].Trim();
      if (ss.Length > 2) this._dispayFormat = ss[2].Trim();
      if (ss.Length > 3) this._masterSql = ss[3].Trim();
    }
    [RefreshProperties(RefreshProperties.Repaint)]
    public string DisplayName {
      get { return this._displayName; }
      set { this._displayName = value; }
    }
    [RefreshProperties(RefreshProperties.Repaint)]
    public string Desription {
      get { return this._description; }
      set { this._description = value; }
    }
    [RefreshProperties(RefreshProperties.Repaint)]
    public string DisplayFormat {
      get { return this._dispayFormat; }
      set { this._dispayFormat = value; }
    }
    [RefreshProperties(RefreshProperties.Repaint)]
    public string MasterSql {
      get { return this._masterSql; }
      set { this._masterSql = value; }
    }
    //===============
    public override string ToString() {
      return _displayName + "^" + this._description + "^" + this._dispayFormat + "^" + this._masterSql;
    }

  }

  //Class DbSchemaColumnPropertyTypeConverter  =============
  class DbSchemaColumnPropertyTypeConverter : ExpandableObjectConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
      if (sourceType == typeof(string)) return true;
      return base.CanConvertFrom(context, sourceType);
    }
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
      if (destinationType == typeof(string)) return true;
      return base.CanConvertTo(context, destinationType);
    }
    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
      if (value is string) {
        return new DbSchemaColumnProperty(((string)value).Split('^'));
      }
      return base.ConvertFrom(context, culture, value);
    }
    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
      return base.ConvertTo(context, culture, value, destinationType);
    }
  }

}
