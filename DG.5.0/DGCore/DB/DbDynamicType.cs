using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using DGCore.Common;

namespace DGCore.DB
{
    [SupportedOSPlatform("windows")]
    public static class DbDynamicType
    {
        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public static Type GetDynamicType(DbCmd cmd, string[] primaryKey, Dictionary<string, AttributeCollection> columnAttributes) =>
          GetDynamicType(cmd, primaryKey, columnAttributes, false, out var dummy);

        public static Type GetDynamicType(DbCmd cmd, string[] primaryKey, Dictionary<string, AttributeCollection> columnAttributes, bool isMasterSql, out string masterPrimaryKeyName)
        {
            masterPrimaryKeyName = null;
            DbSchemaTable schemaTable = cmd.GetSchemaTable();
            if (isMasterSql)
            {
                // Primary key is the first column of MasterSql
                foreach (DbSchemaColumn column in schemaTable.Columns.Values)
                {
                    masterPrimaryKeyName = column.SqlName;
                    primaryKey = new string[] { masterPrimaryKeyName };
                    break;
                }
            }

            lock (_typeCache)
            {
                var key = GetKey(cmd, primaryKey);
                _typeCache.TryGetValue(key, out var dynType);
                if (dynType != null) return dynType;

                var propertyNames = new List<string>();
                var propertyTypes = new List<Type>();

                // Create field definition list
                Dictionary<string, Attribute[]> customAttributes = new Dictionary<string, Attribute[]>(StringComparer.OrdinalIgnoreCase);
                foreach (DbSchemaColumn c in schemaTable.Columns.Values)
                {
                    if (c.DisplayName != null && c.DisplayName.StartsWith("--")) continue;

                    propertyNames.Add(c.SqlName);

                    AttributeCollection ac = null;
                    columnAttributes?.TryGetValue(c.SqlName, out ac);
                    var attrs = ac == null ? new List<Attribute>() : new List<Attribute>(System.Linq.Enumerable.Cast<Attribute>(ac));

                    var attrLookup = (Common.BO_LookupTableAttribute)attrs.FirstOrDefault(a => a is BO_LookupTableAttribute);
                    if (attrLookup == null && !string.IsNullOrEmpty(c.DbMasterSql))
                        attrLookup = new BO_LookupTableAttribute(cmd._connectionString, c.DbMasterSql, null);
                    if (attrLookup != null)
                    {
                        var lookupConnectionString = attrLookup.Connection ?? cmd._connectionString;
                        using (DbCmd masterCmd = new DbCmd(lookupConnectionString, attrLookup.Sql))
                        {
                            var masterSqlPrimaryKeyName = attrLookup.KeyMember;
                            Type masterType = null;
                            masterType = string.IsNullOrEmpty(masterSqlPrimaryKeyName)
                              ? GetDynamicType(masterCmd, null, null, true, out masterSqlPrimaryKeyName)
                              : GetDynamicType(masterCmd, new string[] { masterSqlPrimaryKeyName }, null);
                            propertyTypes.Add(masterType);
                            LookupTableHelper.InitLookupTableTypeConverter(masterType, new Common.BO_LookupTableAttribute(lookupConnectionString, masterCmd._sql, masterSqlPrimaryKeyName));
                        }
                    }
                    else
                    {
                        propertyTypes.Add(c.IsNullable ? Utils.Types.GetNullableType(c.DataType) : c.DataType);
                    }

                    //DisplayName Attribute
                    var displayName = ((DisplayNameAttribute)attrs.FirstOrDefault(a => a is DisplayNameAttribute))?.DisplayName ?? c.DisplayName;
                    if (!string.IsNullOrEmpty(displayName))
                        attrs.Add(new DisplayNameAttribute(displayName));

                    //Description Attribute
                    var description = ((DescriptionAttribute)attrs.FirstOrDefault(a => a is DescriptionAttribute))?.Description ?? c.Description;
                    if (!string.IsNullOrEmpty(description))
                        attrs.Add(new DescriptionAttribute(description));

                    //DisplayFormat Attribute
                    var displayFormat = ((BO_DisplayFormatAttribute)attrs.FirstOrDefault(a => a is BO_DisplayFormatAttribute))?.DisplayFormat ?? c.DisplayFormat;
                    if (!string.IsNullOrEmpty(displayFormat))
                        attrs.Add(new BO_DisplayFormatAttribute(displayFormat));

                    if (attrs.Count > 0)
                        customAttributes.Add(c.SqlName, attrs.ToArray());
                }
                dynType = PD.DynamicType.GetDynamicType_PROPERTIES(propertyNames, propertyTypes, customAttributes, primaryKey);
                _typeCache.Add(key, dynType);
                return dynType;
            }
        }

        private static string GetKey(DbCmd dbCmd, string[] primaryKeys) =>
            dbCmd.Command_Key + (primaryKeys == null || primaryKeys.Length == 0
                ? ""
                : ";" + string.Join("#", primaryKeys));
    }
}
