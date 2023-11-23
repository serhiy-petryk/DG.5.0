using System;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        MemberInfo ReflectedMemberInfo { get; }
        Delegate NativeGetter { get; }
        string DisplayFormat { get; }
    }

    //======================================
    public class MemberDescriptor<T> : PropertyDescriptor, IMemberDescriptor
    {
        private readonly MemberElement _member;

        private readonly string[] _members;

        public MemberDescriptor(string path) : base(path, new Attribute[0])
        {
            _members = path.Split('.');
            List<string> ss = new List<string>(path.Split('.'));
            _member = new MemberElement(null, typeof(T), ss);
            base.AttributeArray = _member.Attributes;

            var displayFormat = ((BO_DisplayFormatAttribute)_member.Attributes.FirstOrDefault(a => a is BO_DisplayFormatAttribute))?.DisplayFormat;
            if (!string.IsNullOrEmpty(displayFormat))
                DisplayFormat = displayFormat;
        }

        public MemberKind MemberKind => _member._memberKind;
        public bool IsValid => _member.IsValid;
        public string DisplayFormat { get; }
        public MemberInfo ReflectedMemberInfo => _member._memberKind == MemberKind.Property ? _member._memberInfo.ReflectedType.GetProperty(Name) : _member._memberInfo;
        public Delegate NativeGetter => _member._nativeGetter;

        // ===================    OrderBy Section ============================
        public IEnumerable GroupBy(IEnumerable<T> source)
        {
            MethodInfo mi = MemberDescriptorUtils.GenericGroupByMi.MakeGenericMethod(new Type[] { typeof(T), _member._nativeGetter.Method.ReturnType });
            return (IEnumerable)mi.Invoke(null, new object[] { source, _member._nativeGetter });
        }

        public override bool SupportsChangeEvents => false;

        public sealed override string Name => string.Join(Constants.MDelimiter, _members);

        public override Type ComponentType => _member._instanceType;

        public override Type PropertyType => _member._lastNullableReturnType;

        public override object GetValue(object component)
        {
            if (Utils.Tips.IsDesignMode)
              return Activator.CreateInstance(_member._lastReturnType);
            
            if (component is Common.IGetValue)
              return ((Common.IGetValue) component).GetValue(Name);
            
            return _member._getter(component);
        }

        public override void SetValue(object component, object value)
        {
            _member._setter(component, value == DBNull.Value ? null : value);
        }

        public override bool IsBrowsable => _member._getter != null && base.IsBrowsable;

        public override bool IsReadOnly => _member._setter == null;

        private object DefaultValue
        {
            get
            {
                DefaultValueAttribute attribute = (DefaultValueAttribute)Attributes[typeof(DefaultValueAttribute)];
                return attribute?.Value;
            }
        }

        public override string ToString() => typeof(T).Name + "." + Name;

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
        public override bool ShouldSerializeValue(object component) => true;
    }
}
