using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;

namespace DGCore.DB
{
  public static class DbDynamicType
  {
    public static void Test()
    {
      string conn = @"SqlClient;Connect Timeout=1800;initial catalog=dbSAP_DW;Pooling=false;Data Source=z2t3uaprvla14;Integrated Security=SSPI;Encrypt=false";
      string sql = "select * from gldocline";
      using (DbCommand cmd = DbUtils.Command_Get(conn, sql))
      {
        string[] pk = { "DOCKEY", "LINENO" };
        //        List<string> pk = new List<string>(new string[] {  "LINENO" });
        Type t = null;
        //        Type t = xxGetDynamicType(cmd, pk, null);
        object o = Activator.CreateInstance(t);
        object o1 = Activator.CreateInstance(t);
        PropertyDescriptorCollection pdc = PD.MemberDescriptorUtils.GetTypeMembers(t);
        PropertyDescriptor pd1 = pdc["DOCKEY"];
        PropertyDescriptor pd2 = pdc["LINENO"];
        pd1.SetValue(o, (long)1);
        pd2.SetValue(o, (Int16)(-1123));
        int i1 = o.GetHashCode();
        int i2 = ((long)1).GetHashCode();
        int i3 = ((Int16)(-1123)).GetHashCode();
        int i4 = i2 ^ i3;
        string s1 = o.ToString();
        bool b1 = o.Equals(o1);
        int k1 = ((IComparable)o).CompareTo(o1);
        pd1.SetValue(o1, (long)1);
        bool b2 = o.Equals(o1);
        int k2 = ((IComparable)o).CompareTo(o1);
        pd2.SetValue(o1, (Int16)(-1123));
        bool b3 = o.Equals(o1);
        int k3 = ((IComparable)o).CompareTo(o1);
      }
    }

    //=======================
    private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

    public static Type GetDynamicType(DbCmd cmd, string[] primaryKey,
      Dictionary<string, AttributeCollection> columnAttributes) =>
      GetDynamicType(cmd, primaryKey, columnAttributes, false, out var dummy);

    public static Type GetDynamicType(DbCmd cmd, string[] primaryKey, Dictionary<string, AttributeCollection> columnAttributes, bool isMasterSql, out string masterPrimaryKeyName)
    {
      masterPrimaryKeyName = null;
      DbSchemaTable schemaTable = cmd.GetSchemaTable();
      if (isMasterSql)
      {
        // Primary key is the first column of MasterSql
        foreach (DbSchemaColumn column in schemaTable._columns.Values)
        {
          masterPrimaryKeyName = column.SqlName;
          primaryKey = new string[] { masterPrimaryKeyName };
          break;
        }
      }

      lock (_typeCache)
      {
        string key = GetKey(cmd, primaryKey);
        Type dynType;
        _typeCache.TryGetValue(key, out dynType);
        if (dynType != null) return dynType;

        List<string> propertyNames = new List<string>();
        List<Type> propertyTypes = new List<Type>();

        // Create field definition list
        Dictionary<string, Attribute[]> customAttributes = new Dictionary<string, Attribute[]>();
        foreach (DbSchemaColumn c in schemaTable._columns.Values)
        {
          if (c.DisplayName != null && c.DisplayName.StartsWith("--")) continue;

          AttributeCollection ac = null;
          if (columnAttributes != null) columnAttributes.TryGetValue(c.SqlName, out ac);
          List<Attribute> attrs = (ac == null ? new List<Attribute>() : new List<Attribute>(System.Linq.Enumerable.Cast<Attribute>(ac)));
          Common.BO_LookupTableAttribute attrLookup = null;
          foreach (Attribute a in attrs)
            if (a is Common.BO_LookupTableAttribute)
            {
              attrLookup = (Common.BO_LookupTableAttribute)a;
              break;
            }

          propertyNames.Add(c.SqlName);
          if (attrLookup != null)
          {
            var lookupConnectionString = attrLookup._connection ?? cmd._connectionString;
            using (DbCmd masterCmd = new DbCmd(lookupConnectionString , attrLookup._sql))
            {
              var masterSqlPrimaryKeyName = attrLookup._keyMember?.ToUpper();
              Type masterType = null;
              masterType = string.IsNullOrEmpty(masterSqlPrimaryKeyName)
                ? GetDynamicType(masterCmd, null, null, true, out masterSqlPrimaryKeyName)
                : GetDynamicType(masterCmd, new string[] {masterSqlPrimaryKeyName}, null);
              propertyTypes.Add(masterType);
              LookupTableHelper.InitLookupTableTypeConverter(masterType, new Common.BO_LookupTableAttribute(lookupConnectionString, masterCmd._sql, masterSqlPrimaryKeyName));
              //              attrs.Remove(attrLookup);
              //            PD.LookupTableHelper.InitLookupTableTypeConverter(masterType, attrLookup);
            }
          }
          else if (!string.IsNullOrEmpty(c.DbMasterSql))
          {
            using (DbCmd masterCmd = new DbCmd(cmd._connectionString, c.DbMasterSql))
            {
              Type masterType = GetDynamicType(masterCmd, null, null, true, out var masterSqlPrimaryKeyName);
              propertyTypes.Add(masterType);
              LookupTableHelper.InitLookupTableTypeConverter(masterType, new Common.BO_LookupTableAttribute(cmd._connectionString, masterCmd._sql, masterSqlPrimaryKeyName));
              //            attrs.Add(new BO_LookupTableAttribute(DbUtils.Connection_ConvertToString(cmd.Connection), masterCmd.CommandText, masterSqlPrimaryKeyName));
            }
          }
          else if (c.DataType == typeof(byte[]) && (c.SqlName.StartsWith("ICON") || c.SqlName.EndsWith("UID")))
          {
            propertyTypes.Add(typeof(string));
            attrs.Add(new Common.BO_DbColumnAttribute(null, "hex", null));
          }
          else if (c.DataType == typeof(byte[]) && c.Size == 16)
          {
            //BO_DbColumnAttribute
            propertyTypes.Add(typeof(string));
            // attrs.Add(new BO_DbColumnAttribute(null, "bytestoguid", null));
            attrs.Add(new Common.BO_DbColumnAttribute(null, "hex", null));
          }
          else
          {
            propertyTypes.Add(c.IsNullable ? Utils.Types.GetNullableType(c.DataType) : c.DataType);
          }
          // Add custom attributes
          //BO_DbColumnAttribute Attribute
          /*if ((ac == null || ac[typeof(BO_DbColumnAttribute)] == null) && c.DataType == typeof(Decimal) && c.DecimalPlaces > 0) {
            attrs.Add(new BO_DbColumnAttribute(null, "N" + c.DecimalPlaces.ToString(), System.Drawing.ContentAlignment.MiddleRight, null));
          }*/

          if ((ac == null || ac[typeof(Common.BO_DbColumnAttribute)] == null ||
               String.IsNullOrEmpty(((Common.BO_DbColumnAttribute)ac[typeof(Common.BO_DbColumnAttribute)])._format)) &&
              !String.IsNullOrEmpty(c.DisplayFormat))
            attrs.Add(new Common.BO_DbColumnAttribute(null, c.DisplayFormat, null));

          //DisplayName Attribute
          if ((ac == null || ac[typeof(DisplayNameAttribute)] == null ||
               String.IsNullOrEmpty(((DisplayNameAttribute)ac[typeof(DisplayNameAttribute)]).DisplayName)) &&
              c.DisplayName != c.SqlName && !string.IsNullOrEmpty(c.DisplayName))
            attrs.Add(new DisplayNameAttribute(c.DisplayName));

          //Description Attribute
          if (!string.IsNullOrEmpty(c.Description))
            attrs.Add(new DescriptionAttribute(c.Description));

          if (attrs.Count > 0)
            customAttributes.Add(c.SqlName, attrs.ToArray());
        }
        dynType = PD.DynamicType.GetDynamicType_PROPERTIES(propertyNames, propertyTypes, customAttributes, primaryKey);
        _typeCache.Add(key, dynType);
        return dynType;
      }
    }

    private static string GetKey(DbCmd dbCmd, string[] primaryKeys) =>
      dbCmd.Command_Key + (primaryKeys == null || primaryKeys.Length == 0 ? "" : ";" + string.Join("#", primaryKeys));
  }
}
