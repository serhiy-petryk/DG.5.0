using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DGCore.DB {

  public class DbColumnMapElement {

    //=============  Static section  ==================
    private static readonly Dictionary<string, DbColumnMapElement[]> _defaultMaps = new Dictionary<string, DbColumnMapElement[]>();

    public static DbColumnMapElement[] GetDefaultColumnMap(DbCmd cmd, Type itemType) {
      if (itemType == null) return GetColumnMapWithoutItemType(cmd);// need for Database filter
      string key = itemType.FullName + ";" + cmd.Command_Key;
      lock (_defaultMaps) {
        if (_defaultMaps.ContainsKey(key)) return _defaultMaps[key];

        DbSchemaTable tbl = cmd.GetSchemaTable();
        MethodInfo mi = typeof(DbColumnMapElement).GetMethod("PrepareDefaultColumnMap", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo mi1 = mi.MakeGenericMethod(itemType);
        DbColumnMapElement[] map = (DbColumnMapElement[])mi1.Invoke(null, new object[] { tbl._columns.Values });
        _defaultMaps.Add(key, map);
        return map;
      }
    }

    private static DbColumnMapElement[] GetColumnMapWithoutItemType(DbCmd cmd) {
      List<DbColumnMapElement> map = new List<DbColumnMapElement>();
      DbSchemaTable tbl = cmd.GetSchemaTable();
      foreach (DbSchemaColumn col in tbl._columns.Values) {
        map.Add(new DbColumnMapElement(col, null));
      }
      return map.ToArray();
    }

    private static DbColumnMapElement[] PrepareDefaultColumnMap<T>(IEnumerable<DbSchemaColumn> dbColumns) {
      // is used in DbColumnMapElement.GetDefaultColumnMap
      List<DbColumnMapElement> map = new List<DbColumnMapElement>();
      List<string> mappedProperties = new List<string>();
      PropertyDescriptorCollection pdc = PD.MemberDescriptorUtils.GetTypeMembers(typeof(T));
      foreach (DbSchemaColumn column in dbColumns) {
        bool flag = false;
        // Check by Properties
        foreach (PropertyDescriptor pd in pdc) {
          PD.MemberDescriptor<T> md = (PD.MemberDescriptor<T>)pd;
          if (md._member._memberKind == PD.MemberKind.Property && !md.IsReadOnly &&
            string.Equals(column.SqlName, md.Name, StringComparison.OrdinalIgnoreCase) && !mappedProperties.Contains(md.Name)) {
            map.Add(new DbColumnMapElement(column, pd));
            mappedProperties.Add(md.Name);
            flag = true;
            break;
          }
        }
        if (!flag) {
          // Check by Field
          foreach (PropertyDescriptor pd in pdc) {
            PD.MemberDescriptor<T> md = (PD.MemberDescriptor<T>)pd;
            if (md._member._memberKind == PD.MemberKind.Field && !md.IsReadOnly &&
              string.Equals(column.SqlName, md.Name, StringComparison.OrdinalIgnoreCase) && !mappedProperties.Contains(md.Name)) {
              map.Add(new DbColumnMapElement(column, pd));
              mappedProperties.Add(md.Name);
              flag = true;
              break;
            }
          }
        }
        if (!flag) {// Unlinked column
          map.Add(new DbColumnMapElement(column, null));
        }
      }
      return map.ToArray();
    }

    // ====================================
    public DbColumnMapElement(DbSchemaColumn column, PropertyDescriptor pd) {
      DbColumn = column;
      MemberDescriptor = pd;
    }

    public DbSchemaColumn DbColumn { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PropertyDescriptor MemberDescriptor { get; set; }

    [Browsable(false)]
    public bool IsValid => this.MemberDescriptor != null;

    [Browsable(false)]
    public bool IsField => ((PD.IMemberDescriptor)this.MemberDescriptor).MemberKind== PD.MemberKind.Field;

    public Type DbDataType => this.DbColumn.DataType;

    public Type ItemDataType => MemberDescriptor?.PropertyType;

    [Browsable(false)]
    public bool CanBeNull => // for datareader
      (this.ItemDataType.IsClass || Utils.Types.IsNullableType(this.ItemDataType) || 
       ((PD.IMemberDescriptor)this.MemberDescriptor).DbNullValue != null) && this.DbColumn.IsNullable;
  }

}
