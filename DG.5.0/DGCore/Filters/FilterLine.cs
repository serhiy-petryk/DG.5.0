using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DGCore.Filters
{

  public class FilterLine_Database : FilterLineBase
  {

    public readonly DB.DbSchemaColumn _dbColumn;
    readonly string _displayName;
    readonly string _description;

    public FilterLine_Database(DB.DbSchemaColumn dbColumn, string itemDisplayName, string itemDescription)
    {
      this._dbColumn = dbColumn;
      this._displayName = (String.IsNullOrEmpty(itemDisplayName) ? dbColumn.DisplayName ?? dbColumn.SqlName : itemDisplayName);
      this._description = (String.IsNullOrEmpty(itemDescription) ? dbColumn.Description : itemDescription);
      this._items = new FilterLineSubitemCollection(this);
      this._frmItems = new FilterLineSubitemCollection(this);
    }

    [Browsable(false)]
    public override Type PropertyType
    {
      get { return this._dbColumn.DataType; }
    }
    [Browsable(false)]
    public override string UniqueID
    {
      get { return this._dbColumn.SqlName; }
    }
    [Browsable(false)]
    public override string DisplayName
    {
      get { return this._displayName ?? this._dbColumn.DisplayName; }
    }
    [Browsable(false)]
    public override string Description
    {
      get { return this._description ?? this._dbColumn.Description; }
    }
    [Browsable(false)]
    public override bool PropertyCanBeNull
    {
      get { return this._dbColumn.IsNullable; }
    }
    [Browsable(false)]
    public override bool IgnoreCaseSupport
    {
      get { return false; }
    }
    /*[Browsable(false)]
    public override object GetNullValue() {
      return null;
    }*/
  }

  //=================================
  public class FilterLine_Item : FilterLineBase
  {

    public PropertyDescriptor _pd; // of type MemberDescriptor<T>

    public FilterLine_Item(PropertyDescriptor pd)
    {
      this._pd = pd;
      this._items = new FilterLineSubitemCollection(this);
      this._frmItems = new FilterLineSubitemCollection(this);
      if (this._pd.PropertyType == typeof(string)) this._ignoreCase = false;
      else this._ignoreCase = null;
    }

    [Browsable(false)]
    public override Type PropertyType
    {
      get { return this._pd.PropertyType; }
    }
    [Browsable(false)]
    public override string UniqueID
    {
      get { return this._pd.Name; }
    }
    [Browsable(false)]
    public override string DisplayName
    {
      get { return this._pd.DisplayName; }
    }
    [Browsable(false)]
    public override string Description
    {
      get { return this._pd.Description; }
    }
    [Browsable(false)]
    public override bool PropertyCanBeNull
    {
      get
      {
        bool canBeNull = _pd.PropertyType.IsClass || Utils.Types.IsNullableType(_pd.PropertyType);
        if (!canBeNull && _pd is PD.IMemberDescriptor) canBeNull = ((PD.IMemberDescriptor)_pd).DbNullValue != null;
        return canBeNull;
      }
    }
    [Browsable(false)]
    public override bool IgnoreCaseSupport
    {
      get { return true; }
    }

    [Browsable(false)]
    public Type ComponentType
    {
      get { return this._pd.ComponentType; }
    }

    public Delegate GetWherePredicate()
    {
      Type propertyType = this.PropertyType;

      Type typePredicateItem = typeof(PredicateItem<>).MakeGenericType(Utils.Types.GetNotNullableType(this.PropertyType));
      Type typeListPredicateItems = typeof(List<>).MakeGenericType(typePredicateItem);

      MethodInfo miGetDelegat = null;
      if (propertyType.IsClass)
      {
        MethodInfo miGetDelegatGeneric = typePredicateItem.GetMethod("GetWhereDelegate_Class", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        miGetDelegat = miGetDelegatGeneric.MakeGenericMethod(this.ComponentType);
      }
      else if (Utils.Types.IsNullableType(propertyType))
      {
        MethodInfo miGetDelegatGeneric = typePredicateItem.GetMethod("GetWhereDelegate_Nullable", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        miGetDelegat = miGetDelegatGeneric.MakeGenericMethod(this.ComponentType, Utils.Types.GetNotNullableType(this.PropertyType));
      }
      else
      {
        MethodInfo miGetDelegatGeneric = typePredicateItem.GetMethod("GetWhereDelegate_ValueType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        miGetDelegat = miGetDelegatGeneric.MakeGenericMethod(this.ComponentType, this.PropertyType);
      }

      IList items = (IList)Activator.CreateInstance(typeListPredicateItems);
      foreach (FilterLineSubitem item in this.Items)
      {
        if (item.IsValid && item.FilterOperand != Common.Enums.FilterOperand.CanBeNull)
        {
          items.Add(Activator.CreateInstance(typePredicateItem, new object[] { item.FilterOperand, this.IgnoreCase, item.Value1, item.Value2 }));
        }
      }
      if (propertyType.IsClass)
      {
        return (Delegate)miGetDelegat.Invoke(null, new object[] { ((PD.IMemberDescriptor)this._pd).NativeGetter, items, this.CanBeNull, this.Not });
      }
      else if (Utils.Types.IsNullableType(propertyType))
      {
        return (Delegate)miGetDelegat.Invoke(null, new object[] { ((PD.IMemberDescriptor)this._pd).NativeGetter, items, this.CanBeNull, this.Not });
      }
      else
      {
        return (Delegate)miGetDelegat.Invoke(null, new object[] { ((PD.IMemberDescriptor)this._pd).NativeGetter, items,
            this.CanBeNull, this.Not, ((PD.IMemberDescriptor)this._pd).DbNullValue });
      }

    }
    /*[Browsable(false)]
    public override object GetNullValue() {
      if (this._pd is PD.IMemberDescriptor) return ((PD.IMemberDescriptor)_pd).DbNullValue;
      else return null;
    }*/

  }

  //===============================
  public abstract class FilterLineBase : IDataErrorInfo
  {

    protected FilterLineSubitemCollection _items;
    protected FilterLineSubitemCollection _frmItems;//для редактирования в форме
    protected bool _not = false;
    protected bool? _ignoreCase;

    [Browsable(false)]
    public abstract Type PropertyType { get; }
    [Browsable(false)]
    public abstract string UniqueID { get; }
    public abstract string DisplayName { get; }
    public string StringPresentation
    {
      get
      {
        List<string> ss1 = new List<string>();
        List<string> ss2 = new List<string>();
        foreach (FilterLineSubitem item in this.Items)
        {
          if (item.IsValid)
          {
            string s = item.GetShortStringPresentation();
            if (s != null) ss2.Add(s);
          }
        }
        if (ss2.Count == 1)
        {
          if (this.Not)
          {
            ss1.Add("окрім(" + ss2[0] + ")");
          }
          else
          {
            ss1.Add(ss2[0]);
          }
        }
        else if (ss2.Count > 1)
        {
          if (this.Not)
          {
            ss1.Add("окрім((" + String.Join(") або (", ss2.ToArray()) + "))");
          }
          else
          {
            ss1.Add("(" + String.Join(") або (", ss2.ToArray()) + ")");
          }
        }
        if (ss1.Count == 1) return String.Join(" і ", ss1.ToArray());
        else if (ss1.Count > 1) return "{" + String.Join("} і {", ss1.ToArray()) + "}";
        else return null;
      }
    }
    public abstract string Description { get; }
    [Browsable(false)]
    public abstract bool PropertyCanBeNull { get; }
    [Browsable(false)]
    public abstract bool IgnoreCaseSupport { get; }
    /*      [Browsable(false)]
          public abstract object GetNullValue();*/

    public FilterLineSubitemCollection Items
    {
      get { return this._items; }
    }
    public FilterLineSubitemCollection FrmItems
    {
      get { return this._frmItems; }
    }
    [DefaultValue(false)]
    public bool Not
    {
      get { return this._not; }
      set { this._not = value; }
    }
    [DefaultValue(null)]
    public bool? IgnoreCase
    {
      get { return this._ignoreCase; }
      set
      {
        if (this.PropertyType == typeof(string))
        {
          this._ignoreCase = value ?? false;
        }
        else
        {
          this._ignoreCase = null;
        }
      }
    }
    //=====  Service items ===
    [Browsable(false)]
    public bool CanBeNull
    {
      get
      {
        foreach (FilterLineSubitem item in this._items)
        {
          if (item.IsValid && item.FilterOperand == Common.Enums.FilterOperand.CanBeNull) return true;
        }
        return false;
      }
    }
    public string RowsString
    {
      get
      {
        int rows = this.ValidLineNumbers;
        if (rows == 0) return null;
        else return rows.ToString();
      }
    }

    [Browsable(false)]
    public bool IsNotEmpty
    {
      get
      {
        foreach (FilterLineSubitem item in this._items)
        {
          if (item.IsValid) return true;
        }
        return false;
      }
    }
    [Browsable(false)]
    public int ValidLineNumbers
    {
      get
      {
        int rows = 0;
        foreach (FilterLineSubitem e in this._items)
        {
          if (e.IsValid) rows++;
        }
        return rows;
      }
    }

    [Browsable(false)]
    public IEnumerable PossibleOperands => Common.Enums.FilterOperandTypeConverter.GetPossibleOperands(PropertyType, PropertyCanBeNull);

    #region IDataErrorInfo Members

    [Browsable(false)]
    public string Error => null;

    public string this[string columnName]
    {
      get
      {
        if (columnName == "RowsString")
        {
          int errors = 0;
          foreach (FilterLineSubitem item in this._items)
          {
            if (item.IsError) errors++;
          }
          if (errors != 0) return errors.ToString() + " помилкових рядків";
        }
        return null;
      }
    }

    #endregion

    public override string ToString() => StringPresentation;
  }

}
