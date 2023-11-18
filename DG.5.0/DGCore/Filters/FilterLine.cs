using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DGCore.Filters
{
    public class FilterLine_Database : FilterLineBase
    {
        public FilterLine_Database(DB.DbSchemaColumn dbColumn, string itemDisplayName, string itemDescription)
        {
            PropertyType = dbColumn.DataType;
            UniqueID = dbColumn.SqlName;
            DisplayName = (String.IsNullOrEmpty(itemDisplayName) ? dbColumn.DisplayName ?? dbColumn.SqlName : itemDisplayName);
            Description = (String.IsNullOrEmpty(itemDescription) ? dbColumn.Description : itemDescription);
            PropertyCanBeNull = dbColumn.IsNullable;
        }

        public override bool IgnoreCaseSupport => false;
    }

    //=================================
    public class FilterLine_Item : FilterLineBase
    {
        public FilterLine_Item(PropertyDescriptor pd)
        {
            PropertyType = pd.PropertyType;
            UniqueID = pd.Name;
            DisplayName = pd.DisplayName;
            Description = pd.Description;
            PropertyCanBeNull = pd.PropertyType.IsClass || Utils.Types.IsNullableType(pd.PropertyType);
            ComponentType = pd.ComponentType;
            Converter = pd.Converter;
            if (pd.PropertyType == typeof(string)) _ignoreCase = false;
            else this._ignoreCase = null;


            _nativeGetter = ((PD.IMemberDescriptor) pd).NativeGetter;
        }

        public override bool IgnoreCaseSupport => true;
        public Type ComponentType { get; }
        public TypeConverter Converter { get; }
        private readonly Delegate _nativeGetter;

        public Delegate GetWherePredicate()
        {
            Type typePredicateItem = typeof(PredicateItem<>).MakeGenericType(Utils.Types.GetNotNullableType(this.PropertyType));
            Type typeListPredicateItems = typeof(List<>).MakeGenericType(typePredicateItem);

            MethodInfo miGetDelegat = null;
            if (PropertyType.IsClass)
            {
                MethodInfo miGetDelegatGeneric = typePredicateItem.GetMethod("GetWhereDelegate_Class", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                miGetDelegat = miGetDelegatGeneric.MakeGenericMethod(this.ComponentType);
            }
            else if (Utils.Types.IsNullableType(PropertyType))
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

            return (Delegate)miGetDelegat.Invoke(null, new object[] { _nativeGetter, items, this.CanBeNull, this.Not });
        }
    }

    //===============================
    public abstract class FilterLineBase : IDataErrorInfo, INotifyPropertyChanged
    {
        public FilterLineBase()
        {
            Items = new FilterLineSubitemCollection(this);
            FrmItems = new FilterLineSubitemCollection(this);
        }

        protected bool _not = false;
        protected bool? _ignoreCase;

        public Type PropertyType { get; protected set; }
        public string UniqueID { get; protected set; }
        public string DisplayName { get; protected set; }
        public string Description { get; protected set; }
        public bool PropertyCanBeNull { get; protected set; }

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
        public string FilterTextOrDescription => StringPresentation ?? Description;
        public abstract bool IgnoreCaseSupport { get; }
        public FilterLineSubitemCollection Items { get; }
        public FilterLineSubitemCollection FrmItems { get; } //для редактирования в форме
        public bool Not
        {
            get { return this._not; }
            set { this._not = value; }
        }
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
        public bool CanBeNull
        {
            get
            {
                foreach (FilterLineSubitem item in this.Items)
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

        public bool IsNotEmpty
        {
            get
            {
                foreach (FilterLineSubitem item in this.Items)
                {
                    if (item.IsValid) return true;
                }
                return false;
            }
        }
        public int ValidLineNumbers
        {
            get
            {
                int rows = 0;
                foreach (FilterLineSubitem e in this.Items)
                {
                    if (e.IsValid) rows++;
                }
                return rows;
            }
        }

        public IEnumerable PossibleOperands => Common.Enums.FilterOperandTypeConverter.GetPossibleOperands(PropertyType, PropertyCanBeNull);

        #region IDataErrorInfo Members

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                if (columnName == "RowsString")
                {
                    int errors = 0;
                    foreach (FilterLineSubitem item in this.Items)
                    {
                        if (item.IsError) errors++;
                    }
                    if (errors != 0) return errors.ToString() + " помилкових рядків";
                }
                return null;
            }
        }

        #endregion

        #region =================  INotifyPropertyChanged  ==================
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public override string ToString() => StringPresentation;
    }

}
