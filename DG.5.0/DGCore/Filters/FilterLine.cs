using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DGCore.Filters
{
    public class FilterLine_Database : FilterLineBase
    {
        public FilterLine_Database(DB.DbSchemaColumn dbColumn, string itemDisplayName, string itemDescription)
        {
            PropertyType = dbColumn.DataType;
            Id = dbColumn.SqlName;
            DisplayName = (string.IsNullOrEmpty(itemDisplayName) ? dbColumn.DisplayName ?? dbColumn.SqlName : itemDisplayName);
            Description = (string.IsNullOrEmpty(itemDescription) ? dbColumn.Description : itemDescription);
            PropertyCanBeNull = dbColumn.IsNullable;
        }
    }

    //=================================
    public class FilterLine_Item : FilterLineBase
    {
        private readonly Delegate _nativeGetter;

        public FilterLine_Item(PropertyDescriptor pd)
        {
            PropertyType = pd.PropertyType;
            Id = pd.Name;
            DisplayName = pd.DisplayName;
            Description = pd.Description;
            PropertyCanBeNull = pd.PropertyType.IsClass || Utils.Types.IsNullableType(pd.PropertyType);
            ComponentType = pd.ComponentType;
            Converter = pd.Converter;
            IgnoreCase = pd.PropertyType == typeof(string) ? (bool?) false : null;
            _nativeGetter = ((PD.IMemberDescriptor) pd).NativeGetter;
        }

        public Type ComponentType { get; }
        public TypeConverter Converter { get; }
        
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
        }

        public Type PropertyType { get; protected set; }
        public string Id { get; protected set; }
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
                        ss1.Add("окрім((" + string.Join(") або (", ss2.ToArray()) + "))");
                    }
                    else
                    {
                        ss1.Add("(" + string.Join(") або (", ss2.ToArray()) + ")");
                    }
                }
                if (ss1.Count == 1) return string.Join(" і ", ss1.ToArray());
                else if (ss1.Count > 1) return "{" + string.Join("} і {", ss1.ToArray()) + "}";
                else return null;
            }
        }
        public string FilterTextOrDescription => StringPresentation ?? Description;
        public FilterLineSubitemCollection Items { get; }
        public bool HasFilter => Items.Any(a => a.IsValid);
        public bool Not {get; set;}

        private bool? _ignoreCase;
        public bool? IgnoreCase
        {
            get => this._ignoreCase;
            set => _ignoreCase = PropertyType == typeof(string) ? (bool?) (value ?? false) : null;
        }
        
        //=====  Service items ===
        public bool CanBeNull => Items.Any(item => item.IsValid && item.FilterOperand == Common.Enums.FilterOperand.CanBeNull);

        public string RowsString
        {
            get
            {
                int rows = this.ValidLineNumbers;
                if (rows == 0) return null;
                else return rows.ToString();
            }
        }

        public int ValidLineNumbers => Items.Count(a => a.IsValid);

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
