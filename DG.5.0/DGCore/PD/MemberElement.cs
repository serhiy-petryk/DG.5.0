using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using DGCore.Common;

namespace DGCore.PD
{
  public partial class MemberElement
  {
    public readonly MemberKind _memberKind;
    public readonly MemberElement _parent;
    public readonly MemberElement _child;
    public readonly Type _instanceType;
    public readonly MemberInfo _memberInfo;// FieldInfo (for fields) or MethodInfo(for properties & methods)
    private readonly PropertyInfo _pi;
    public readonly string _error;

    public readonly Type _lastReturnType;
    public readonly Type _lastNullableReturnType;
    public readonly bool _canBeNull;

    public readonly GetHandler _getter;
    public readonly SetHandler _setter;
    public readonly Delegate _nativeGetter;
    bool _tokenIsBrowsable = true;
    string _tokenDisplayName;
    Attribute[] _attributes; // only for lowest level

    // Constructors
    public MemberElement(MemberElement owner, Type instanceType, List<string> memberNames)
    {
      this._parent = owner;
      this._instanceType = instanceType;
      Type notNullType = Utils.Types.GetNotNullableType(this._instanceType);
      string memberName = memberNames[0].Trim();
      this._memberInfo = notNullType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
      if (this._memberInfo == null)
      {// Not field
        this._pi = notNullType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
        if (this._pi == null)
        {// not property 
          if (memberName.EndsWith("()")) memberName = memberName.Substring(0, memberName.Length - 2);
          this._memberInfo = notNullType.GetMethod(memberName, BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
          this._memberKind = MemberKind.Method;
        }
        else
        {
          this._memberInfo = this._pi.GetGetMethod(false);
          this._memberKind = MemberKind.Property;
        }
      }
      else
      {
        this._memberKind = MemberKind.Field;
      }
      // Check
      if (this._memberInfo == null) this._error = "Can not find public '" + memberName + "' member for " + notNullType + " type";
      if (this._error == null && this.IsPrivate) this._error = "'" + memberName + "' member for " + notNullType + " type is not public";

      if (this._error == null)
      {// No error
        memberNames.RemoveAt(0);// Recursive create child
        // Attributes (set isBrowsabel && displayname
        object[] oo = (this._memberKind == MemberKind.Property ? this._pi.GetCustomAttributes(false) : this._memberInfo.GetCustomAttributes(false));
        List<Attribute> attrs = new List<Attribute>();
        _tokenDisplayName = memberName;
        foreach (object o in oo)
        {
          if (o is BrowsableAttribute) _tokenIsBrowsable = ((BrowsableAttribute)o).Browsable;
          else if (o is DisplayNameAttribute) _tokenDisplayName = ((DisplayNameAttribute)o).DisplayName;
          else if (o is Common.BO_DisplayNameAttribute) _tokenDisplayName = ((Common.BO_DisplayNameAttribute)o).DisplayName;
          else if (o is Common.BO_LookupTableAttribute)
          {
            //            LookupTableHelper.InitLookupTableTypeConverter( _tokenDisplayName = ((BO_DisplayNameAttribute)o).DisplayName;
            attrs.Add((Attribute)o);
          }
          else attrs.Add((Attribute)o);
        }
        // Create childs
        if (memberNames.Count > 0)
          this._child = new MemberElement(this, this.ReturnType, memberNames);

        if (this._parent == null)
        {
          // Set recursive properties
          MemberElement e = this;
          while (e != null)
          {
            _lastReturnType = e.ReturnType;
            if (!_canBeNull) _canBeNull = (e.ReturnType.IsClass || Utils.Types.IsNullableType(e.ReturnType)) && e._child != null;
            if (this._error == null && e._error != null)
            {
              this._error = e._error;
              break;
            }
            e = e._child;
          }
          if (this._error == null)
          {
            _lastNullableReturnType = this._canBeNull ? Utils.Types.GetNullableType(this._lastReturnType) : this._lastReturnType;
            _getter = EmitHelper.CreateGetHandler(this);
            try
            {
              if (this._pi != null) this._setter = EmitHelper.CreateSetHandler(this._pi);
              else if (this._memberInfo is FieldInfo) this._setter = EmitHelper.CreateSetHandler((FieldInfo)this._memberInfo);
            }
            catch (Exception ex) { }

            _nativeGetter = EmitHelper.CreateNativeGetHandler(this);
          }//if (this._error == null) {
        }//if (this._parent == null)

        if (memberName.Contains(Constants.MDelimiter))
        {
          var parts = memberName.Split(new[] {Constants.MDelimiter}, StringSplitOptions.None);
          var currentType = instanceType;
          var displayNames = new List<string>();
          var isBrowsable = this.IsBrowsable;
          string description = null;
          for (var i = 0; i < parts.Length; i++)
          {
            var pd = currentType.GetProperty(parts[i]);
            var attributes = pd.GetCustomAttributes().ToArray();
            displayNames.Add(attributes.OfType<DisplayNameAttribute>().FirstOrDefault()?.DisplayName ?? parts[i]);
            if (isBrowsable)
              isBrowsable = attributes.OfType<BrowsableAttribute>().FirstOrDefault()?.Browsable ?? true;
            description = pd.GetCustomAttributes().OfType<DescriptionAttribute>().FirstOrDefault()?.Description;
            currentType = pd.PropertyType;
          }
          attrs.Add(new DisplayNameAttribute(string.Join(Environment.NewLine, displayNames)));
          attrs.Add(new BrowsableAttribute(isBrowsable));
          if (!string.IsNullOrEmpty(description))
            attrs.Add(new DescriptionAttribute(description));
          this._attributes = attrs.ToArray();
        }
        else if (this._child == null)
        {
          attrs.Add(new BrowsableAttribute(this.IsBrowsable));
          attrs.Add(new DisplayNameAttribute(this.DisplayName));
          this._attributes = attrs.ToArray();
        }

        // Activate lookup table convertor
        Common.BO_LookupTableAttribute aa = (Common.BO_LookupTableAttribute)TypeDescriptor.GetAttributes(this.ReturnType)[typeof(Common.BO_LookupTableAttribute)];
        if (aa != null)
        {
          // ??? Don't know where it used
          throw new Exception("2019-12 Lovushka for LookupTableHelper.InitLookupTableTypeConverter");
          // LookupTableHelper.InitLookupTableTypeConverter(this.ReturnType, aa);
        }
      }//if (this._error == null)
    }

    /*    public object[] GetCustomAttributes() {
          if (this._memberKind == MemberKind.Property) {
            return this._pi.GetCustomAttributes(false);
          }
          else {
            return this._memberInfo.GetCustomAttributes(false);
          }
        }*/

    public Attribute[] Attributes => _child == null ? _attributes : _child.Attributes;

    string DisplayName => (_parent == null ? "" : _parent.DisplayName + "^") + _tokenDisplayName;

    bool IsBrowsable => _tokenIsBrowsable && (_parent == null ? true : _parent.IsBrowsable);

    bool IsPrivate => IsField ? ((FieldInfo)_memberInfo).IsPrivate : ((MethodInfo)_memberInfo).IsPrivate;

    // ===== Public properties
    public bool IsValid => _getter != null || _setter != null;

    public bool IsField => _memberInfo is FieldInfo;

    public Type ReturnType => IsField ? ((FieldInfo)_memberInfo).FieldType : ((MethodInfo)_memberInfo).ReturnType;

    /*    public bool CanBeNull {
          get {
            if (this._child == null) return false;
            return (this.ReturnType.IsClass || Utils.Types.IsNullableType(this.ReturnType) || this._child.CanBeNull);
          }
        }
        public Type LastReturnType {
          get {
            if (this._child == null) return this.ReturnType;
            else return this._child.LastReturnType;
          }
        }*/
    public override string ToString() => _memberInfo.Name + (_child == null ? "" : "." + _child.ToString());
  }
}

