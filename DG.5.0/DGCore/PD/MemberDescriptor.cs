using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using DGCore.Common;

namespace DGCore.PD
{

    public enum MemberKind { Field = 0, Property = 1, Method = 2 };
    public delegate object GetHandler(object source);
    public delegate void SetHandler(object source, object value);
    public delegate object ConstructorHandler();

    public interface IMemberDescriptor
    {
        MemberKind MemberKind { get; }
        object DbNullValue { get; }
        MemberInfo ReflectedMemberInfo { get; }
        Delegate NativeGetter { get; }
        string Format { get; }
        // System.Drawing.ContentAlignment? Alignment { get; }
        Enums.Alignment? Alignment { get; }
    }

    //======================================
    public class MemberDescriptor<T> : PropertyDescriptor, IMemberDescriptor
    {
        public readonly string _path;
        public readonly MemberElement _member;
        private readonly string[] _members;

        string _dbColumnName;
        // System.Drawing.ContentAlignment? _alignment;
        object _dbNullValue;
        TypeConverter _thisConverter;

        //    TypeConverter _objectConverter;// Converter from DataBase simple type to object type

        public MemberDescriptor(string path) : base(path, new Attribute[0])
        {
            _path = path;
            _members = path.Split('.');
            List<string> ss = new List<string>(path.Split('.'));
            _member = new MemberElement(null, typeof(T), ss);
            base.AttributeArray = _member.Attributes;

            var a = (Common.BO_DbColumnAttribute)Attributes[typeof(Common.BO_DbColumnAttribute)];
            if (a != null)
            {
                _dbColumnName = a._dbColumnName;
                Format = a._format;
                //        _alignment = a._alignment;
                _dbNullValue = a._dbNullValue;
                _dbNullValue = Utils.Types.CastDbNullValue(_dbNullValue, PropertyType, ComponentType.Name + "." + PropertyType.Name);
                if (_dbNullValue != null)
                {
                    TypeConverter tc = base.Converter;
                    _thisConverter = PD.ConverterWithNonStandardDefaultValue.GetConverter(tc, _dbNullValue);
                }
                else if (string.Equals(Format, "hex", StringComparison.OrdinalIgnoreCase))
                    _thisConverter = new ByteArrayToHexStringConverter();
                else if (string.Equals(Format, "bytestoguid", StringComparison.OrdinalIgnoreCase))
                    _thisConverter = new ByteArrayToGuidStringConverter();
            }

            /* not need!!! if (_thisConverter == null && PropertyType == typeof(string) && (path.EndsWith("UID", StringComparison.OrdinalIgnoreCase) || path.StartsWith("ICON", StringComparison.CurrentCultureIgnoreCase)))
              _thisConverter = new ByteArrayToHexStringConverter();*/

            /*test: if (Name.StartsWith("ICON"))
            {
              _thisConverter = new PD.ByteArrayToHexStringConverter();
            }*/

            /*BO_LookupTableAttribute aLookup = (BO_LookupTableAttribute)Attributes[typeof(BO_LookupTableAttribute)];
            if (aLookup != null) {
              if (PropertyType.IsClass && PropertyType != typeof(string)) {
                _thisConverter = PD.LookupTableHelper.GetTypeConverter(PropertyType, aLookup);
              }
              else {
                throw new Exception(path + " Property of " + ComponentType +" type. BO_LookupTableAttribute attribute applables only for not string object properties");
              }
            }*/
        }

        public MemberKind MemberKind => _member._memberKind;
        public string Format { get; } // for DGV
        public Enums.Alignment? Alignment { get; }
        public object DbNullValue => _dbNullValue;
        public MemberInfo ReflectedMemberInfo => _member._memberKind == MemberKind.Property ? _member._memberInfo.ReflectedType.GetProperty(Name) : _member._memberInfo;
        public Delegate NativeGetter => _member._nativeGetter;

        // ===================    OrderBy Section ============================
        public IEnumerable GroupBy(IEnumerable<T> source)
        {
            MethodInfo mi = MemberDescriptorUtils.GenericGroupByMi.MakeGenericMethod(new Type[] { typeof(T), _member._nativeGetter.Method.ReturnType });
            return (IEnumerable)mi.Invoke(null, new object[] { source, _member._nativeGetter });
        }
        //========================  Public section  ==============================
        /*    public int MemberLevel {
              get { return _token._memberLevel; }
            }*/

        // ====================  Override section   =======================
        /*public override void AddValueChanged(object component, EventHandler handler) {
          base.AddValueChanged(component, handler);
        }
        public override void RemoveValueChanged(object component, EventHandler handler) {
          base.RemoveValueChanged(component, handler);
        }*/
        public override bool SupportsChangeEvents
        {
            get { return false; }
        }

        public override TypeConverter Converter
        {
            get
            {
                object o = _thisConverter ?? base.Converter;
                return _thisConverter ?? base.Converter;
            }
        }
        public sealed override string Name
        {
            get { return string.Join(Constants.MDelimiter, _members); }
        }
        public override Type ComponentType
        {
            get { return _member._instanceType; }
        }
        public override Type PropertyType
        {
            get
            {
                return _member._lastNullableReturnType;
                //        return _token._lastReturnType; 
            }
        }
        public override object GetValue(object component)
        {
            if (Utils.Tips.IsDesignMode)
              return Activator.CreateInstance(_member._lastReturnType);
            
            if (component is Common.IGetValue)
              return ((Common.IGetValue) component).GetValue(Name);
            
            return _member._getter(component);
            //      return _getter_CD(component);
        }

        public override void SetValue(object component, object value)
        {
            _member._setter(component, value == DBNull.Value ? _dbNullValue : value);

            /*      object oldValue = GetValue(component);
                  object newValue = (value == DBNull.Value ? _dbNullValue : value);
                  _member._setter(component, newValue);*/
            /*      if (_token._fi != null) {
                    _token._fi.SetValue(component, newValue);
                  }
                  if (_token._pi != null) {
                    _token._pi.SetValue(component, newValue, null);
                  }*/
            /*      OnValueChanged x = OnValueChangedHandler;
                  if (x != null) {
                    x(this, component, newValue, oldValue);
                  }*/
        }

        public override bool IsBrowsable
        {
            get
            {
                if (_member._getter == null) return false;
                return base.IsBrowsable;
            }
        }

        public override bool IsReadOnly
        {
            get { return _member._setter == null; }
        }

        private object DefaultValue
        {
            get
            {
                DefaultValueAttribute attribute = (DefaultValueAttribute)Attributes[typeof(DefaultValueAttribute)];
                if (attribute != null) return attribute.Value;
                else return null;
            }
        }

        public override string ToString()
        {
            return typeof(T).Name + "." + Name;
        }

        public override bool CanResetValue(object component)
        {
            // Taken from System.ComponentModel.TypeConverter+SimplePropertyDescriptor
            DefaultValueAttribute attribute = (DefaultValueAttribute)Attributes[typeof(DefaultValueAttribute)];
            if (attribute == null)
            {
                return false;
            }
            return attribute.Value.Equals(GetValue(component));
        }
        public override void ResetValue(object component)
        {
            // Taken from System.ComponentModel.TypeConverter+SimplePropertyDescriptor
            DefaultValueAttribute attribute = (DefaultValueAttribute)Attributes[typeof(DefaultValueAttribute)];
            if (attribute != null)
            {
                SetValue(component, attribute.Value);
            }
        }
        public override bool ShouldSerializeValue(object component)
        {
            //      return false;
            return true;
        }


    }
}
