using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

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
              return new DbSchemaTable(cmd, connectionKey, false);
          return _schemaTables[key];
      }
    }
    static DbSchemaTable GetSchemaTableForDataTable(DbCommand cmd, string connectionKey)
    {
      // do not need to lock _schemaTable == call inside existing lock
      var tableCmd = DbUtils.Command_Get(cmd.Connection, "SELECT * from " + DbMetaData.QuotedTableName(cmd.GetType().Namespace, cmd.CommandText.ToUpper()));
      string key = GetDictionaryKey(tableCmd, connectionKey);
      if (!_schemaTables.ContainsKey(key))
        return new DbSchemaTable(cmd, connectionKey, true);
      return _schemaTables[key];
    }

    static string GetDictionaryKey(DbCommand cmd, string connectionKey)
    {
      if (string.IsNullOrEmpty(connectionKey))
        return (DbUtils.Connection_GetKey(cmd.Connection) + "#" + cmd.CommandText).ToUpper();
      return (connectionKey + "#" + cmd.CommandText).ToUpper();
    }

    //=======================================================
    public readonly string _baseTableName = null;
    public List<string> _parameterNames = new List<string>();
    public Dictionary<string, DbSchemaColumn> _columns = new Dictionary<string, DbSchemaColumn>();
    public Dictionary<string, DbSchemaTable> _updatableTables = new Dictionary<string, DbSchemaTable>();

    private List<DbSchemaColumn> _primaryKey = new List<DbSchemaColumn>();// technical field
    //    string _columnsKey = null;

    private DbSchemaTable(DbCommand cmd, string connectionKey, bool isTable)
    {// must be command with parameters (for SqlClient)
      Dictionary<string, DbSchemaColumnProperty> customColumnProperties = DbSchemaColumnProperty.GetProperties(GetDictionaryKey(cmd, connectionKey));
      Dictionary<string, string> columnDescriptions = null;
      if (isTable)
      {
        this._baseTableName = cmd.CommandText.ToUpper();
        cmd = DbUtils.Command_Get(cmd.Connection, "SELECT * from " + DbMetaData.QuotedTableName(cmd.GetType().Namespace, this._baseTableName));
        columnDescriptions = DbMetaData.GetColumnDescriptions(cmd.Connection, this._baseTableName);
      }

      List<string> tableNames = new List<string>();
      using (DataTable dt = DbUtils.GetSchemaTable(cmd))
      {
        this._parameterNames = (List<string>)dt.ExtendedProperties["ParameterNames"];
        var colCnt = 0;
        var isColumnHiddenExist = dt.Columns.Contains("IsHidden");// SqlServer supported or Jet.TableDirect; Jet.Sql and Oracle do not support
        var isColumnReadOnlyExist = dt.Columns.Contains("IsReadOnly");// Oracle does not support
        var isColumnAutoIncrementExist = dt.Columns.Contains("IsAutoIncrement");// Oracle does not support
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
            string baseTableName = String.Join(".", new string[] {(isColumnBaseCatalogNameExist? dr["BaseCatalogName"].ToString().ToUpper(): ""),
            (isColumnBaseSchemaNameExist? dr["BaseSchemaName"].ToString().ToUpper(): ""),
            dr["BaseTableName"].ToString().ToUpper()});
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
            bool isAutoIncrement = isColumnAutoIncrementExist && (bool)dr["IsAutoIncrement"];
            bool isReadOnly = isColumnReadOnlyExist && (bool)dr["IsReadOnly"];
            bool isNullable = (bool)dr["AllowDBNull"];
            bool isPrimaryKey = (bool)dr["IsKey"];
            DbSchemaColumn column = new DbSchemaColumn(columnName, position, size, dp, type, isReadOnly, isNullable, isAutoIncrement, isPrimaryKey, baseTableName, baseColumnName);
            if (customColumnProperties != null)
            {
              DbSchemaColumnProperty customProperty;
              customColumnProperties.TryGetValue(columnName, out customProperty);
              column._customProperty = customProperty;
            }
            if (columnDescriptions != null && columnDescriptions.ContainsKey(column.SqlName))
            {
              //              string s = columnDescriptions[column.SqlName];
              string[] ss = columnDescriptions[column.SqlName].Split('^');
              column._dbDisplayName = ss[0].Trim();
              column._dbDescription = (ss.Length < 2 ? null : ss[1].Trim());
              if (ss.Length >= 3) column._dbMasterSql = ss[2].Trim();
              /*              int k1 = s.IndexOf(";");
                            if (k1 < 0) column._dbDisplayName = s;
                            else {
                              column._dbDisplayName = s.Substring(0, k1).Trim();
                              column._dbDescription = s.Substring(k1 + 1).Trim();
                            }*/
            }
            this._columns.Add(column.SqlName, column);
            if (isPrimaryKey) this._primaryKey.Add(column);
          }
          colCnt++;
        }
      }

      _schemaTables.Add(GetDictionaryKey(cmd, connectionKey), this);

      if (isTable || cmd.CommandType == CommandType.TableDirect)
      {
        //        DbSchemaColumn.OleDb_AdjustColumns(conn, this._baseTableName, this._columns.Values);
      }
      else
      {
        for (int i = 0; i < tableNames.Count; i++)
        {
          string s = tableNames[i];
          try
          {
            DbSchemaTable x = GetSchemaTableForDataTable(DbUtils.Command_Get(cmd.Connection, s), connectionKey);
            // Updatable table has to have all primary key columns in column list of sql statement
            if (x.IsUpdatable)
            {
              bool updatable = true;
              foreach (DbSchemaColumn col in x._primaryKey)
              {
                bool flag = false;
                foreach (DbSchemaColumn thisCol in this._columns.Values)
                {
                  if (thisCol.BaseTableName == col.BaseTableName && thisCol.BaseColumnName == col.BaseColumnName)
                  {
                    flag = true;
                    break;
                  }
                }
                if (!flag)
                {
                  updatable = false;
                  break;
                }
              }
              if (updatable) this._updatableTables.Add(s, x);
            }

            foreach (DbSchemaColumn col in this._columns.Values)
            {
              if (col.BaseTableName == s)
              {
                col._baseColumn = x._columns[col.BaseColumnName];
              }
            }
          }
          catch (Exception ex)
          {
            foreach (DbSchemaColumn col in this._columns.Values)
            {
              if (col.BaseTableName == s) col.ClearBaseTable();
            }
            tableNames.Remove(s);
            i--;
          }
        }
      }
    }

    //Format of primaryKeyInfo(Dictionary<DbSchemaTable, DbSchemaColumn[]>): 
    //      key(DbSchemaTable)=base database table; 
    //      value(DbSchemaColumn[])=list of primary key columns
    public Dictionary<DbSchemaTable, DbSchemaColumn[]> GetPrimaryKeysInfo()
    {
      Dictionary<DbSchemaTable, DbSchemaColumn[]> info = new Dictionary<DbSchemaTable, DbSchemaColumn[]>();
      foreach (DbSchemaTable tbl in this._updatableTables.Values)
      {
        List<DbSchemaColumn> thisTableColumns = new List<DbSchemaColumn>();
        bool flag = false;
        foreach (DbSchemaColumn c in tbl._primaryKey)
        {
          foreach (DbSchemaColumn c1 in this._columns.Values)
          {
            if (c1.BaseTableName == c.BaseTableName && c1.BaseColumnName == c.BaseColumnName)
            {
              thisTableColumns.Add(c1);
              flag = true;
              break;
            }
          }
        }
        if (!flag) throw new Exception("Something wrong!");
        info.Add(tbl, thisTableColumns.ToArray());
      }
      return info;
    }

    public bool IsUpdatable => _primaryKey.Count != 0;
  }
}
