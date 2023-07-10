using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DGCore.Filters {

  public class FilterList : List<FilterLineBase>, UserSettings.IUserSettingSupport<List<UserSettings.Filter>>
  {
    internal string _dbProviderNamespace;

    // Constructor for Item Filter
    public FilterList(PropertyDescriptorCollection pdc) =>
      AddRange(pdc.Cast<PropertyDescriptor>()
        .Where(pd => pd.IsBrowsable && Utils.Types.GetNotNullableType(pd.PropertyType).GetInterface("System.IComparable") != null)
        .Select(pd => new FilterLine_Item(pd)));

    // Constructor for Database Filter: itemType needs for DisplayName/Descriptions only
    public FilterList(DB.DbCmd cmd, Type itemType, Dictionary<string, AttributeCollection> columnAttributes) {
      this._dbProviderNamespace = cmd._dbCmd.GetType().Namespace;
      if (itemType == null) {// do not init new dynamic type while user do not open DGV
        DB.DbSchemaTable tbl = cmd.GetSchemaTable();
        foreach (DB.DbSchemaColumn col in tbl._columns.Values) {
          if (col.DisplayName != null && col.DisplayName.StartsWith("--")) continue;
          AttributeCollection attrs = null;
          string displayName = null;
          string description = null;
          if (columnAttributes != null && columnAttributes.TryGetValue(col.SqlName, out attrs)) {
            DescriptionAttribute a1 = (DescriptionAttribute)attrs[typeof(DescriptionAttribute)];
            if (a1 != null) description = a1.Description;
            DisplayNameAttribute a2 = (DisplayNameAttribute)attrs[typeof(DisplayNameAttribute)];
            if (a2 != null) displayName = a2.DisplayName;
          }
          Add(new FilterLine_Database(col, displayName, description));
        }
      }
      else {
        DB.DbColumnMapElement[] map = DB.DbColumnMapElement.GetDefaultColumnMap(cmd, itemType);
        foreach (DB.DbColumnMapElement e in map) {
          if (e.DbColumn.DisplayName != null && e.DbColumn.DisplayName.StartsWith("--")) continue;
          AttributeCollection attrs = null;
          string displayName = (e.MemberDescriptor == null ? null : e.MemberDescriptor.DisplayName);
          string description = (e.MemberDescriptor == null ? null : e.MemberDescriptor.Description);
          if (columnAttributes != null && columnAttributes.TryGetValue(e.DbColumn.SqlName, out attrs)) {
            DescriptionAttribute a1 = (DescriptionAttribute)attrs[typeof(DescriptionAttribute)];
            if (a1 != null && !String.IsNullOrEmpty(a1.Description)) description = a1.Description;
            DisplayNameAttribute a2 = (DisplayNameAttribute)attrs[typeof(DisplayNameAttribute)];
            if (a2 != null && !String.IsNullOrEmpty(a2.DisplayName)) displayName = a2.DisplayName;
          }
          if (!(!String.IsNullOrEmpty(displayName) && displayName.StartsWith("--"))) {
            this.Add(new FilterLine_Database(e.DbColumn, displayName, description));
          }
        }
      }
    }

    public void ClearFilter() {
      foreach (FilterLineBase item in this) {
        if (item.Not) item.Not = false;
        item.Items.Clear();
      }
    }

    public bool IgnoreCaseSupport => this._dbProviderNamespace == null;

    public bool IsEmpty => this.All(line => !line.Items.Any(item => item.IsValid));

    public string StringPresentation
    {
        get
        {
            var ss1 = new List<string>();
            foreach (var line in this)
            {
                var ss2 = new List<string>();
                foreach (var item in line.Items)
                {
                    if (item.IsValid)
                    {
                        var s = item.GetStringPresentation();
                        if (s != null) ss2.Add(s);
                    }
                }
                if (ss2.Count == 1)
                {
                    if (line.Not)
                        //            ss1.Add(" або окрім(" + ss2[0] + ")");
                        ss1.Add("окрім(" + ss2[0] + ")");
                    else
                        //            ss1.Add(" або " + ss2[0]);
                        ss1.Add(ss2[0]);
                }
                else if (ss2.Count > 1)
                {
                    if (line.Not)
                        ss1.Add("окрім((" + string.Join(") або (", ss2.ToArray()) + "))");
                    else
                        ss1.Add("(" + string.Join(") або (", ss2.ToArray()) + ")");
                }
            }
            if (ss1.Count == 1) return string.Join(" і ", ss1.ToArray());
            if (ss1.Count > 1) return "{" + string.Join("} і {", ss1.ToArray()) + "}";
            return null;
        }
    }

    public Delegate[] GetWherePredicates() => this.Where(item => item.ValidLineNumbers > 0).OrderBy(item => item.ValidLineNumbers).Select(item=>((FilterLine_Item)item).GetWherePredicate()).ToArray();

    public Delegate SetFilterByValue(string propertyName, object value) {
      foreach (FilterLineBase item in this) {
        if (item.UniqueID == propertyName) {
          item.Items.Clear();
          item.IgnoreCase = false;
          FilterLineSubitem lineItem = new FilterLineSubitem();
          item.Items.Add(lineItem);
          if (value == null) {
            lineItem.FilterOperand = Common.Enums.FilterOperand.CanBeNull;
          }
          else {
            lineItem.FilterOperand = Common.Enums.FilterOperand.Equal;
            lineItem.Value1 = value;
          }
          return ((FilterLine_Item)lineItem.Owner).GetWherePredicate();
        }
      }
      return null;
    }

    //============================
    public string SettingKind { get; }

    public string SettingKey { get; }
    List<UserSettings.Filter> UserSettings.IUserSettingSupport<List<UserSettings.Filter>>.GetSettings()
    {
      var oo = new List<UserSettings.Filter>();
      foreach (var line in this)
      {
        if (line.IsNotEmpty)
        {
          var oLine = new UserSettings.Filter();
          oo.Add(oLine);
          oLine.Name = line.UniqueID;
          oLine.Not = line.Not;
          oLine.IgnoreCase = line.IgnoreCase;
          foreach (var item in line.Items)
            if (item.IsValid)
            {
              var tc = TypeDescriptor.GetConverter(line.PropertyType) as Common.ILookupTableTypeConverter;
              if (tc != null)
                oLine.Lines.Add(new UserSettings.FilterLine()
                {
                  Operand = item.FilterOperand,
                  Value1 = tc.GetKeyByItemValue(item.Value1),
                  Value2 = tc.GetKeyByItemValue(item.Value2)
                });
              else
                oLine.Lines.Add(new UserSettings.FilterLine()
                {
                  Operand = item.FilterOperand,
                  Value1 = item.Value1,
                  Value2 = item.Value2
                });
            }

        }
      }
      return oo;
    }

    List<UserSettings.Filter> UserSettings.IUserSettingSupport<List<UserSettings.Filter>>.GetBlankSetting() => new List<UserSettings.Filter>();

    void UserSettings.IUserSettingSupport<List<UserSettings.Filter>>.SetSetting(List<UserSettings.Filter> settings)
    {
      //throw new NotImplementedException();
      if (settings == null) return;
      // Clear FilterObject
      foreach (var line in this)
      {
        line.Items.Clear();
        line.Not = false;
      }
      // Fill filterObject
      foreach (var o in settings)
      {
        var name = o.Name;// Get saved property name
        foreach (var line in this)
        {
          if (line.UniqueID == name)
          {// Saved property name exists in current FilterObject == ApplyInfo
            line.Not = o.Not;
            line.IgnoreCase = o.IgnoreCase;
            // Restore FilterLine items 
            foreach (var o1 in o.Lines)
            {
              var item = new FilterLineSubitem();
              line.Items.Add(item);
              item.FilterOperand = o1.Operand;
              var tc = TypeDescriptor.GetConverter(line.PropertyType) as Common.ILookupTableTypeConverter;
              if (tc != null)
              {// Deserialize object of dynamic type
                item.Value1 = tc.GetItemByKeyValue(o1.Value1);
                item.Value2 = tc.GetItemByKeyValue(o1.Value2);
              }
              else
              {
                item.Value1 = o1.Value1;
                item.Value2 = o1.Value2;
              }
            }
            break;
          }
        }
      }
    }

    public override string ToString() => StringPresentation;
  }
}
