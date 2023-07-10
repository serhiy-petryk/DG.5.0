using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DGCore.Sql {

  [TypeConverter(typeof(SqlParameterConverter))]
  public class ParameterCollection {

    public List<Parameter> _parameters;
    Func<ParameterCollection, string> _getErrorFunction;
    Action<ParameterCollection, string, object, object> _afterValueChangedProcedure; // this + parameterName + oldValue + newValue

    public ParameterCollection(Func<ParameterCollection, string> getErrorFunction,
      Action<ParameterCollection, string, object, object> afterValueChangedProcedure, params Parameter[] parameters) {
//   afterValueChangedProcedure( this, parameterName, oldValue, newValue)

      this._parameters = new List<Parameter>(parameters);
      this._getErrorFunction = getErrorFunction;
      this._afterValueChangedProcedure = afterValueChangedProcedure;
    }

    public ParameterCollection(Parameter[] parameters) {
      this._parameters = new List<Parameter>(parameters);
      this._getErrorFunction = null;
      this._afterValueChangedProcedure = null;
    }

    public string GetError() {
      if (this._getErrorFunction == null) return null;
      return this._getErrorFunction(this);
    }
    public void AfterValueChanged(string parameterName, object oldValue, object newValue) {
      if (this._afterValueChangedProcedure != null) this._afterValueChangedProcedure(this, parameterName, oldValue, newValue);
    }

    public Parameter this[string key] {
      get {
        foreach (Parameter p in this._parameters) if (p._sqlName == key) return p;
        return null; 
      }
    }
    public string[] GetParameterNames() {
      List<string> oo = new List<string>();
      foreach (Parameter p in this._parameters) oo.Add(p._sqlName);
      return oo.ToArray();
    }
    public object[] GetParameterValues() {
      List<object> oo = new List<object>();
      foreach (Parameter p in this._parameters) oo.Add(p._value);
      return oo.ToArray();
    }
    public string GetStringPresentation() {
      List<string> ss = new List<string>();
      foreach (Parameter p in this._parameters) ss.Add(p.GetStringPresentation());
      return String.Join(";  ", ss.ToArray());
    }
    public string GetParameterValuesKey() {
      StringBuilder sb = new StringBuilder();
      foreach (Parameter p in this._parameters) sb.Append((p._value ?? "").ToString()+";");
      return sb.ToString();
    }

    public override string ToString() => GetStringPresentation();
  }

  //==============================================================
  /// <summary>
  /// Параметры пользовательского интерфейса, которые используются в вызове процедуры/запроса к данным
  /// </summary>
  public class Parameter{// : ParameterStandardValuesConverter.IParameterStandardValuesConverterSupport {

    static Parameter() {
//      TypeDescriptor.AddAttributes(typeof(Parameter[]), new Attribute[] { new TypeConverterAttribute(typeof(SqlParameterConverter)) });
    }

    public string _sqlName;
    public string _displayName;
    public string _description;
    public Type _valueType;
    public object _value;
    //public ICollection _list;
    public TypeConverter _typeConverter = null;
    public string _listIDMember;

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="sqlName">Код параметра при использовании его в вызове процедуры/запроса к данным</param>
    /// <param name="displayName">Подсказка пользователю</param>
    /// <param name="description">Описание параметра, которое будет видно пользователю при вводе данных</param>
    /// <param name="valueType">Тип данных</param>
    /// <param name="defaultValue">Значение по умолчанию</param>
    public Parameter(string sqlName, string displayName, string description, Type valueType, object defaultValue) {//, ICollection listValues) {
      this._sqlName = sqlName; this._displayName = displayName??sqlName ;
      this._description = description;
      this._valueType = valueType;
      this._value = defaultValue;
      //this._list = listValues;
    }
    public Parameter(string sqlName, string displayName, string description, Type valueType, object defaultValue, ICollection listValues, bool isExclusive)
      :
    this(sqlName, displayName, description, valueType, defaultValue) {
      if (listValues != null) this._typeConverter = new ParameterStandardValuesConverter(listValues, isExclusive);
    }
    public Parameter(string sqlName, string displayName, string description, Type valueType, object defaultValue, string connectionString, string sql, bool isExclusive)
      :
    this(sqlName, displayName, description, valueType, defaultValue) {
      this._typeConverter = new ParameterStandardValuesConverter(connectionString, sql, isExclusive);
    }

    public string GetStringPresentation() => this._displayName + ": " + Utils.Tips.ConvertTo(this._value, typeof(string), null);
    public override string ToString() => GetStringPresentation();

/*    #region IStandardValuesConverterSupport Members

    public ICollection GetStandardValues(string propertyName) {
      return _list;
    }
    public bool GetStandardValuesSupported() {
      return _list != null && !(_list is IDictionary) ;
    }

    #endregion*/
  }

  //=========================================================
  public class ParameterStandardValuesConverter : TypeConverter {

    static Dictionary<string, StandardValuesCollection> _cache = new Dictionary<string, StandardValuesCollection>();

    bool _activated = false;
    StandardValuesCollection _list;
    string _connectionString; 
    string _sql;
    bool _isExclusive=true;
    //string _valueType;
    // Constructors
    public ParameterStandardValuesConverter(ICollection list, bool isExclusive) {
      this._list = new StandardValuesCollection(list);
      _isExclusive = isExclusive;
      //this._valueType = valueType;
      _activated = true;
    }
    public ParameterStandardValuesConverter(string connectionString, string sql, bool isExclusive) {
      this._connectionString = connectionString; this._sql = sql; //true.this._valueType = valueType;
      _isExclusive = isExclusive;
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
      return true;
    }
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
      return true;
    }

    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
      if (value==null || value.GetType()==context.PropertyDescriptor.PropertyType) return value;
      try
      {
        return Utils.Tips.ConvertTo(value, context.PropertyDescriptor.PropertyType, null);
      }
      catch {}
      return base.ConvertFrom(context, culture, value);
    }
    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
      return base.ConvertTo(context, culture, value, destinationType);
    }
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
//      return (context != null && context.Instance is IParameterStandardValuesConverterSupport && ((IParameterStandardValuesConverterSupport)context).GetStandardValuesSupported());
      return (_list!=null || _connectionString!=null);
    }
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
      return _isExclusive;
    }
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
      if (!_activated) {
        string key = _connectionString.ToUpper() + ";" + _sql.ToUpper().Replace(" ", "");
        if (!_cache.ContainsKey(key)) {
          using (DB.DbCmd x = new DB.DbCmd(this._connectionString, this._sql))
          {
            List<object> data = new List<object>();
            x.Fill_ToValueList(data);
            _list = new StandardValuesCollection(data);
            _cache.Add(key, _list);
          }
        }
        else _list = _cache[key];
        _activated = true;
      }
      return _list;
    }
  }

  //=========================================
  class ParameterPropertyDescriptor : PropertyDescriptor {

    Parameter _sqlParameter;
    object _defaultValue;

    public ParameterPropertyDescriptor(Parameter sqlParameter):base(sqlParameter._sqlName, null) {
      this._sqlParameter = sqlParameter;
      this._defaultValue = sqlParameter._value;
    }

    public override string DisplayName {
      get { return _sqlParameter._displayName; }
    }
    public override string Name {
      get { return this._sqlParameter._sqlName; }
    }
    public override string Description {
      get { return this._sqlParameter._description; }
    }
    public override TypeConverter Converter {
      get {
//        return (_sqlParameter._list == null ? base.Converter : new ParameterStandardValuesConverter(_sqlParameter._list));
        return (_sqlParameter._typeConverter?? base.Converter);
      }
    }

    public override bool CanResetValue(object component) {
      return true;
    }

    public override Type ComponentType {
      get { return typeof(Parameter[]); }
    }

    public override object GetValue(object component) {
      return this._sqlParameter._value;
    }

    public override bool IsReadOnly {
      get { return false; }
    }

    public override Type PropertyType {
      get { return this._sqlParameter._valueType; }
    }

    public override void ResetValue(object component) {
      this._sqlParameter._value=_defaultValue;
    }

    public override void SetValue(object component, object value) {
      this._sqlParameter._value=value;
    }

    public override bool ShouldSerializeValue(object component) {
      return false;
    }
  }

  //==============================
  public class SqlParameterConverter : TypeConverter {
    public SqlParameterConverter() {}
    public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) {
      if (value is ParameterCollection) value = ((ParameterCollection)value)._parameters;
      if (value is IEnumerable<Parameter>) {
        List<PropertyDescriptor> pdList = new List<PropertyDescriptor>();
        foreach (Parameter p in (IEnumerable<Parameter>)value) {
          pdList.Add(new ParameterPropertyDescriptor(p));
        }
        return new PropertyDescriptorCollection(pdList.ToArray());
      }
      return null;
    }
    public override bool GetPropertiesSupported(ITypeDescriptorContext context) {
      return true;
    }

  }

}
