using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace DGCore.DB
{
    public class DbSchemaTable
    {
        #region ================   Static section  =====================
        private static readonly Dictionary<string, DbSchemaTable> SchemaTables =
            new Dictionary<string, DbSchemaTable>(StringComparer.OrdinalIgnoreCase); // key=connString+sql; value=dbTable

        public static DbSchemaTable GetSchemaTable(DbCommand cmd)
        {
            var key = DbUtils.Command_GetKey(cmd);
            lock (SchemaTables)
            {
                if (!SchemaTables.ContainsKey(key))
                    SchemaTables.Add(key, new DbSchemaTable(cmd));

                return SchemaTables[key];
            }
        }
        #endregion

        #region ================   Instance section  =====================
        public Dictionary<string, DbSchemaColumn> Columns = new Dictionary<string, DbSchemaColumn>(StringComparer.OrdinalIgnoreCase);

        private DbSchemaTable(DbCommand cmd)
        {// must be command with parameters (for SqlClient)
            List<string> tableNames = new List<string>();
            using (DataTable dt = DbUtils.GetSchemaTable(cmd))
            {
                var isColumnHiddenExist = dt.Columns.Contains("IsHidden");// SqlServer supported or Jet.TableDirect; Jet.Sql and Oracle do not support
                var isColumnBaseCatalogNameExist = dt.Columns.Contains("BaseCatalogName");
                var isColumnBaseSchemaNameExist = dt.Columns.Contains("BaseSchemaName");
                short position = -1;
                foreach (DataRow dr in dt.Rows)
                {
                    position++;
                    var isHidden = isColumnHiddenExist && (dr["IsHidden"] != DBNull.Value && (bool)dr["IsHidden"]);
                    if (isHidden) continue;

                    var columnName = dr["ColumnName"].ToString();

                    // Get the basetable name
                    var baseTableName = String.Join(".",
                        (isColumnBaseCatalogNameExist ? dr["BaseCatalogName"].ToString() : ""),
                        (isColumnBaseSchemaNameExist ? dr["BaseSchemaName"].ToString() : ""),
                        dr["BaseTableName"].ToString());
                    if (baseTableName.StartsWith(".")) baseTableName = baseTableName.Remove(0, 1);
                    if (baseTableName.StartsWith(".")) baseTableName = baseTableName.Remove(0, 1);

                    if (!string.IsNullOrEmpty(baseTableName) && !tableNames.Contains(baseTableName) &&
                        !(isColumnBaseSchemaNameExist && dr["BaseSchemaName"].ToString().ToUpper() == "SYS"))
                        tableNames.Add(baseTableName);

                    var baseColumnName = dr["BaseColumnName"].ToString();
                    // Int16 position = Convert.ToInt16(dr["ColumnOrdinal"]); // "ColumnOrdinal" starts with 0 for SqlServer, or 1 for MySql
                    var type = (Type)dr["DataType"];
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
                    var isNullable = (bool)dr["AllowDBNull"];
                    var isPrimaryKey = (bool)dr["IsKey"];
                    var column = new DbSchemaColumn(columnName, position, size, dp, type, isNullable, baseTableName, baseColumnName);
                    Columns.Add(column.SqlName, column);
                }
            }

            foreach (var tableName in tableNames)
            {
                var columnDescriptions = DbMetaData.GetColumnDescriptions(cmd.Connection, tableName);
                if (columnDescriptions != null)
                {
                    foreach (var cd in columnDescriptions)
                    {
                        foreach (var column in Columns.Values.Where(c =>
                          string.Equals(c.BaseTableName, tableName, StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(c.BaseColumnName, cd.Key, StringComparison.OrdinalIgnoreCase)))
                        {
                            var ss = cd.Value.Split('^');
                            if (!string.IsNullOrEmpty(ss[0])) column.DisplayName = ss[0].Trim();
                            if (ss.Length >= 2 && !string.IsNullOrEmpty(ss[1])) column.Description = ss[1].Trim();
                            if (ss.Length >= 3 && !string.IsNullOrEmpty(ss[2])) column.DbMasterSql = ss[2].Trim();
                        }
                    }
                }
            }
        }
        #endregion
    }
}
