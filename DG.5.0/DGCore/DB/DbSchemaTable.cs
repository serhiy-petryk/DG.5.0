using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace DGCore.DB
{
    public class DbSchemaTable
    {
        // ================   Static section  =====================
        static Dictionary<string, DbSchemaTable> _schemaTables = new Dictionary<string, DbSchemaTable>();// key=connString+tblName; value=dbTable

        public static DbSchemaTable GetSchemaTable(DbCommand cmd, string connectionKey)
        {
            string key = GetDictionaryKey(cmd, connectionKey);

            lock (_schemaTables)
            {
                if (!_schemaTables.ContainsKey(key))
                    return new DbSchemaTable(cmd, connectionKey);
                return _schemaTables[key];
            }
        }
        static string GetDictionaryKey(DbCommand cmd, string connectionKey)
        {
            if (string.IsNullOrEmpty(connectionKey))
                return (DbUtils.Connection_GetKey(cmd.Connection) + "#" + cmd.CommandText).ToUpper();
            return (connectionKey + "#" + cmd.CommandText).ToUpper();
        }

        //=======================================================
        public readonly string _baseTableName = null;
        public Dictionary<string, DbSchemaColumn> _columns = new Dictionary<string, DbSchemaColumn>();

        private List<DbSchemaColumn> _primaryKey = new List<DbSchemaColumn>();// technical field
                                                                              //    string _columnsKey = null;

        private DbSchemaTable(DbCommand cmd, string connectionKey)
        {// must be command with parameters (for SqlClient)
            Dictionary<string, DbSchemaColumnProperty> customColumnProperties = DbSchemaColumnProperty.GetProperties(GetDictionaryKey(cmd, connectionKey));
            List<string> tableNames = new List<string>();
            using (DataTable dt = DbUtils.GetSchemaTable(cmd))
            {
                var colCnt = 0;
                var isColumnHiddenExist = dt.Columns.Contains("IsHidden");// SqlServer supported or Jet.TableDirect; Jet.Sql and Oracle do not support
                var isColumnBaseCatalogNameExist = dt.Columns.Contains("BaseCatalogName");
                var isColumnBaseSchemaNameExist = dt.Columns.Contains("BaseSchemaName");
                short position = -1;
                foreach (DataRow dr in dt.Rows)
                {
                    position++;
                    bool isHidden = isColumnHiddenExist && (dr["IsHidden"] == DBNull.Value ? false : (bool)dr["IsHidden"]);
                    if (!isHidden)
                    {
                        string columnName = dr["ColumnName"].ToString().ToUpper();
                        // Get the basetable name
                        string baseTableName = String.Join(".",
                          new string[]
                          {
                (isColumnBaseCatalogNameExist ? dr["BaseCatalogName"].ToString().ToUpper() : ""),
                (isColumnBaseSchemaNameExist ? dr["BaseSchemaName"].ToString().ToUpper() : ""),
                dr["BaseTableName"].ToString().ToUpper()
                          });
                        if (baseTableName.StartsWith(".")) baseTableName = baseTableName.Remove(0, 1);
                        if (baseTableName.StartsWith(".")) baseTableName = baseTableName.Remove(0, 1);
                        //            StringBuilder sbTableName = new StringBuilder(baseCatalogName);
                        if (!string.IsNullOrEmpty(baseTableName) && !tableNames.Contains(baseTableName) &&
                            !(isColumnBaseSchemaNameExist && dr["BaseSchemaName"].ToString().ToUpper() == "SYS"))
                            tableNames.Add(baseTableName);

                        string baseColumnName = dr["BaseColumnName"].ToString().ToUpper();
                        // Int16 position = Convert.ToInt16(dr["ColumnOrdinal"]); // "ColumnOrdinal" starts with 0 for SqlServer, or 1 for MySql
                        Type type = (Type)dr["DataType"];
                        int size;
                        byte dp = 0;
                        if (type == typeof(string) || type == typeof(byte[]))
                        {
                            size = Convert.ToInt32(dr["ColumnSize"]);
                        }
                        else
                        {
                            size = Convert.ToInt32(dr["NumericPrecision"]);
                            dp = Convert.ToByte(dr["NumericScale"]);
                        }
                        bool isNullable = (bool)dr["AllowDBNull"];
                        bool isPrimaryKey = (bool)dr["IsKey"];
                        DbSchemaColumn column = new DbSchemaColumn(columnName, position, size, dp, type, isNullable, baseTableName, baseColumnName);
                        if (customColumnProperties != null)
                        {
                            DbSchemaColumnProperty customProperty;
                            customColumnProperties.TryGetValue(columnName, out customProperty);
                            column._customProperty = customProperty;
                        }
                        this._columns.Add(column.SqlName, column);
                    }
                    colCnt++;
                }
            }

            _schemaTables.Add(GetDictionaryKey(cmd, connectionKey), this);

            foreach (var tableName in tableNames)
            {
                var columnDescriptions = DbMetaData.GetColumnDescriptions(cmd.Connection, tableName);
                if (columnDescriptions != null)
                {
                    foreach (var cd in columnDescriptions)
                    {
                        var name = tableName;
                        foreach (var column in _columns.Values.Where(c =>
                          string.Equals(c.BaseTableName, name, StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(c.SqlName, cd.Key, StringComparison.OrdinalIgnoreCase)))
                        {
                            var ss = cd.Value.Split('^');
                            column._dbDisplayName = ss[0].Trim();
                            if (ss.Length >= 2) column._dbDescription = ss[1].Trim();
                            if (ss.Length >= 3) column._dbMasterSql = ss[2].Trim();
                        }
                    }
                }
            }
        }
    }
}
