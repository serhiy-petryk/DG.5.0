using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DGCore.PD {

  //Class Converter_Dictionary  =============
  // Using: TypeDescriptor.AddAttributes(typeof(Dictionary<string, DbSchemaColumnProperty>),
  //   new Attribute[] { new TypeConverterAttribute(typeof(PD.Converter_Dictionary<string, DbSchemaColumnProperty>)) });

  public class Converter_Dictionary<TKey, TValue> : TypeConverter {

    public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) {
      if (value is Dictionary<TKey,TValue>) {
        Dictionary<TKey, TValue > o = (Dictionary<TKey, TValue >)value;
        List<PropertyDescriptor> pp = new List<PropertyDescriptor>();
        foreach (KeyValuePair<TKey, TValue> kvp in o)
          pp.Add(new PropertyDescriptor_Dictionary(kvp.Key));
        return new PropertyDescriptorCollection(pp.ToArray());
      }
      return TypeDescriptor.GetProperties(value, attributes);
    }
    public override bool GetPropertiesSupported(ITypeDescriptorContext context) => true;

    //Subclass PropertyDescriptor_Dictionary =========
    class PropertyDescriptor_Dictionary : PropertyDescriptor {

      TKey _key;

      public PropertyDescriptor_Dictionary(TKey key)
        : base(key.ToString(), null) {
        this._key = key;
      }

      public override Type ComponentType => typeof(Dictionary<TKey, TValue>);
      public override Type PropertyType => typeof(TValue);
      public override bool IsReadOnly => false;
      public override bool CanResetValue(object component) => false;
      public override void ResetValue(object component) => throw new Exception("The method or operation is not implemented.");
      public override bool ShouldSerializeValue(object component) => false;


      public override object GetValue(object component) {
        if (component is Dictionary<TKey, TValue>) {
          return ((Dictionary<TKey, TValue>)component)[_key];
        }
        throw new Exception("The method or operation is not implemented.");
      }

      public override void SetValue(object component, object value) {
        if (component is Dictionary<TKey, TValue>) {
          ((Dictionary<TKey, TValue>)component)[_key] = (TValue)value;
          return;
        }
        throw new Exception("The method or operation is not implemented.");
      }
    }
  }


}
