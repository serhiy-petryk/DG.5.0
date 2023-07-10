using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DGCore.DB
{
  public static class LookupTableHelper
  {
    static Dictionary<string, TypeConverter> _cached = new Dictionary<string, TypeConverter>();

    //    static object dummy = PD.DynTypeSerializator.dummy;
    /*public static TypeConverter GetTypeConverter(Type componentType, BO_LookupTableAttribute attr) {
      using (DbConnection conn = DbUtils.Connection_Get(attr._connection)) {
        PropertyDescriptor x = PD.MemberDescriptorUtils.GetMember(componentType, attr._keyMember, true);
        Type propType = Utils.Types.GetNotNullableType(x.PropertyType);
        string key = componentType.Name + ";" + propType.Name + DbUtils.Connection_GetKey(conn) + ";" + attr._sql.Trim().ToUpper() + attr._keyMember.Trim().ToUpper();
        TypeConverter tc = null;
        if (!_cached.TryGetValue(key, out tc)) {
          Type lookupType = typeof(PD.LookupTableTypeConverter<,>).MakeGenericType(componentType, propType);
          tc= (TypeConverter)Activator.CreateInstance(lookupType, new object[] { attr });
          _cached.Add(key, tc);
        }
        return tc;
      }
    }*/

    public static void InitLookupTableTypeConverter(Type componentType, Common.BO_LookupTableAttribute attr)
    {
      TypeConverter tc = TypeDescriptor.GetConverter(componentType);
      if (tc.GetType() == typeof(TypeConverter))
      {// No converter
        PropertyDescriptor x = PD.MemberDescriptorUtils.GetMember(componentType, attr._keyMember, true);
        Type tcType = typeof(LookupTableTypeConverter<,>).MakeGenericType(x.ComponentType, Utils.Types.GetNotNullableType(x.PropertyType));
        // add converter
        TypeDescriptor.AddAttributes(componentType, new TypeConverterAttribute(tcType));
        // set inti value for converter
        FieldInfo fi = tcType.GetField("publicAttribute", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        fi.SetValue(null, attr);
        // Activate initial converter value
        tc = TypeDescriptor.GetConverter(componentType);// Init converter: run constructor
        if (!(tc is Common.ILookupTableTypeConverter))
          throw new Exception("Can not get LookupTableTypeConverter for " + componentType.Name);
      }
    }
  }
  
  public class LookupTableTypeConverter<TType, TKeyMemberType> : TypeConverter, IComponent, Common.ILookupTableTypeConverter
  {
    public static Common.BO_LookupTableAttribute publicAttribute;

    public string SqlKey => _dbCmd?.Command_Key;
    private readonly DbCmd _dbCmd;
    //    readonly string _conn;
    //  readonly string _sql;
    //    readonly string _keyMember;
    private readonly PD.MemberDescriptor<TType> _pdKeyMember;
    private Dictionary<TKeyMemberType, TType> _data = null;
    private bool _isDataReady;

    public LookupTableTypeConverter() : this(publicAttribute)
    {
      /*      this._conn = publicAttribute._connection;
            this._sql = publicAttribute._sql;
            this._pdKeyMember = (PD.MemberDescriptor<TType>)MemberDescriptorUtils.GetMember(typeof(TType), publicAttribute._keyMember, false);
            if (_pdKeyMember == null) this._pdKeyMember = (PD.MemberDescriptor<TType>)MemberDescriptorUtils.GetMember(typeof(TType), publicAttribute._keyMember, true);
            if (_pdKeyMember == null) throw new Exception("MasterDataTypeConverter. Can not find '" + publicAttribute._keyMember +
              "' property for key member of type " + typeof(TType).Name);

            publicAttribute = null;*/
    }
    public LookupTableTypeConverter(Common.BO_LookupTableAttribute attr)
    {
      this._dbCmd = new DbCmd(attr._connection, attr._sql);
      //      this._conn = attr._connection;
      //    this._sql = attr._sql;
      this._pdKeyMember = (PD.MemberDescriptor<TType>)PD.MemberDescriptorUtils.GetMember(typeof(TType), attr._keyMember, false);
      if (_pdKeyMember == null) this._pdKeyMember = (PD.MemberDescriptor<TType>)PD.MemberDescriptorUtils.GetMember(typeof(TType), attr._keyMember, true);
      if (_pdKeyMember == null) throw new Exception("MasterDataTypeConverter. Can not find '" + attr._keyMember +
        "' property for key member of type " + typeof(TType).Name);

      publicAttribute = null;
    }

    public void LoadData(IComponent consumer)
    {
      Misc.DependentObjectManager.Bind(this, consumer);
      if (!_isDataReady) LoadData();
    }

    public object GetKeyByItemValue(object item)
    {
      if (item == null || item == DBNull.Value) return null;
      return _pdKeyMember.GetValue(item);
    }

    public object GetItemByKeyValue(object keyValue)
    {
      if (keyValue == null || keyValue == DBNull.Value) return null;
      if (keyValue is string && string.IsNullOrEmpty((string)keyValue)) return null;
      TKeyMemberType key = (keyValue is TKeyMemberType ? (TKeyMemberType)keyValue : (TKeyMemberType)Convert.ChangeType(keyValue, typeof(TKeyMemberType)));
      if (!_isDataReady) LoadData();
      TType o;
      if (!_data.TryGetValue(key, out o))
      {// Create new item if it dosenot exist and set key field
        o = Activator.CreateInstance<TType>();
        _pdKeyMember.SetValue(o, key);
      }
      return o;
    }

    /* public IEnumerable<object> GetKeyMembers(IEnumerable items) {
       return Enumerable.Select<TType, object>(Enumerable.Cast<TType>(items), delegate(TType item) { 
         return (item==null ? null : _pdKeyMember.GetValue(item)); 
       });
     }*/

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
      return (sourceType == typeof(string)) || (sourceType == typeof(TType)) || sourceType == typeof(TKeyMemberType) ||
        sourceType == Utils.Types.GetNullableType(typeof(TKeyMemberType)) || base.CanConvertFrom(context, sourceType);
    }
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
      return destinationType == typeof(string) || destinationType == typeof(TType) || destinationType == typeof(TKeyMemberType) ||
        destinationType == Utils.Types.GetNullableType(typeof(TKeyMemberType)) || base.CanConvertFrom(context, destinationType);
    }
    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    {
      if (value == null || value == DBNull.Value) return null;
      if (value is string)
      {
        value = (TKeyMemberType)Convert.ChangeType(value, typeof(TKeyMemberType));
      }
      if (value is TKeyMemberType)
      {
        if (!_isDataReady) LoadData();
        TType o;
        if (_data.TryGetValue((TKeyMemberType)value, out o)) return o;
        TType newItem = Activator.CreateInstance<TType>();
        this._pdKeyMember.SetValue(newItem, value);
        return newItem;
      }
      if (value is TType) return value;
      return base.ConvertFrom(context, culture, value);
    }
    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    {
      if (value == null || value == DBNull.Value) return null;
      if (destinationType == typeof(TKeyMemberType))
      {
        return _pdKeyMember.GetValue(value);
      }
      if (destinationType == typeof(string))
        return _pdKeyMember.GetValue(value).ToString();
      //        return _pdKeyMember.GetValue(Convert.ChangeType(value, typeof(TKeyMemberType)));
      if (destinationType == typeof(TType)) return value;
      return base.ConvertTo(context, culture, value, destinationType);
    }
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

    public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
      if (!_isDataReady) LoadData();
      return new StandardValuesCollection(_data.Keys);
    }
    void LoadData()
    {
      lock (this)
      {
        if (_isDataReady) return;
        _data = new Dictionary<TKeyMemberType, TType>();
        _dbCmd.Fill<TKeyMemberType, TType>(_data, (Func<TType, TKeyMemberType>) this._pdKeyMember.NativeGetter, null);
        _isDataReady = true;
      }
    }

    #region IComponent && IDisposable Members

    public event EventHandler Disposed;

    ISite _site;
    public ISite Site
    {
      get { return this._site; }
      set { this._site = value; }
    }

    public void Dispose()
    {
      if (Disposed != null) Disposed.Invoke(this, new EventArgs());
      this._data = null;
      _isDataReady = false;
    }

    #endregion
    public override string ToString()
    {
      return "MasterDataConverter(" + typeof(TType).Name + ", " + typeof(TKeyMemberType).Name + "): " + this._dbCmd._sql;
    }
  }

}
