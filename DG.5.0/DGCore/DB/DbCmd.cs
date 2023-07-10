using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace DGCore.DB
{

  public class DbCmd : IComponent, IDisposable, ICloneable
  {
    public enum DbCmdKind { Query, Procedure, File }

    public static Dictionary<string, string> _standardConnections = new Dictionary<string, string>();
    static int idCnt = 0;

    public int ID = idCnt++;
    public readonly string _connectionString;
    public readonly string _sql;
    readonly DbConnection _dbConn;
    public readonly DbCommand _dbCmd;
    public readonly List<object> _paramValues = new List<object>();
    public readonly List<string> _paramNames = new List<string>();

    public DbCmdKind _cmdKind => _connectionString.StartsWith("File;", StringComparison.InvariantCultureIgnoreCase)
      ? DbCmdKind.File
      : (_sql.IndexOf(' ') == -1 ? DbCmdKind.Procedure : DbCmdKind.Query);

    public DbCmd(string connectionString, string sql)
      : this(connectionString, sql, null, null)
    {
    }

    public DbCmd(string connectionString, string sql, IEnumerable paramValues, IEnumerable<string> paramNames)
    {
      this._connectionString = connectionString;
      this._sql = sql.Trim();
      this._dbConn = DbUtils.Connection_Get(connectionString);
      this._dbCmd = this._dbConn.CreateCommand();
      this._dbCmd.CommandText = this._sql;
      this._dbCmd.CommandType = (this._sql.IndexOf(' ') == -1 ? CommandType.StoredProcedure : CommandType.Text);
      if (_dbConn.ConnectionTimeout == 0 || (_dbCmd.CommandTimeout != 0 && _dbCmd.CommandTimeout < _dbConn.ConnectionTimeout))
        _dbCmd.CommandTimeout = _dbConn.ConnectionTimeout;
      this.Parameters_Add(paramValues, paramNames);
    }

    public string Connection_Key
    {
      get
      {
        if (_standardConnections.ContainsKey(this._connectionString)) return this._connectionString;
        return this._dbConn.GetType().FullName + ";" + this._dbConn.Database + ";" + this._dbConn.DataSource;
      }
    }

    public string Command_Key => this.Connection_Key + ";" + this._sql;

    public void Parameters_Add(IEnumerable paramValues, IEnumerable<string> paramNames)
    {
      if (paramValues != null) _paramValues.AddRange(System.Linq.Enumerable.Cast<object>(paramValues));
      if (paramNames != null) _paramNames.AddRange(paramNames);
      Parameters_Update();
    }

    public void Parameters_UpdateByNewValues(IEnumerable paramValues, IEnumerable<string> paramNames)
    {
      this._paramNames.Clear();
      this._paramValues.Clear();
      this.Parameters_Add(paramValues, paramNames);
    }

    void Parameters_Update()
    {
      if (this._paramNames.Count != 0 && this._paramNames.Count != this._paramValues.Count)
        throw new Exception("Numbers of parameter names and values are not equal. Connection: " + this._dbConn + ". Command: " + this._sql);

      this._dbCmd.Parameters.Clear();
      for (int i = 0; i < this._paramValues.Count; i++)
      {
        DbParameter par = _dbCmd.CreateParameter();
        if (this._paramNames.Count > 0) par.ParameterName = this._paramNames[i];
        par.Value = this._paramValues[i];
        _dbCmd.Parameters.Add(par);
      }
      DbUtils.AdjustParameters(this._dbCmd);
    }

    public DbSchemaTable GetSchemaTable() => DbSchemaTable.GetSchemaTable(_dbCmd, Connection_Key);

    public void Connection_Open()
    {
      while (_dbConn.State.HasFlag(ConnectionState.Connecting))
        Thread.Sleep(100);

      if (!_dbConn.State.HasFlag(ConnectionState.Open))
        _dbConn.Open();
    }

    public void Fill_ToValueList(IList data)
    {
      this.Connection_Open();
      using (DbDataReader reader = this._dbCmd.ExecuteReader())
      {
        while (reader.Read())
        {
          try
          {
            data.Add(reader.GetValue(0));
          }
          catch (Exception exception)
          {
            object[] values = new object[reader.FieldCount];
            reader.GetValues(values);
            throw;
          }
        }
      }
    }

    // fill dictionary - is simplest; user must create more complex fill separate
    public void Fill<KeyType, ItemType>(IDictionary data, Delegate keyFunction, IEnumerable<DbColumnMapElement> columnMap)
    {
      Func<ItemType, KeyType> keyFunc = (Func<ItemType, KeyType>)keyFunction;
      Func<DbDataReader, ItemType> func = DbUtils.Reader.GetDelegate_FromDataReaderToObject<ItemType>(this, columnMap);

      Connection_Open();
      using (DbDataReader reader = this._dbCmd.ExecuteReader())
      {
        while (reader.Read())
        {
          try
          {
            ItemType item = func(reader);
            data.Add(keyFunc(item), item);
          }
          catch (Exception exception)
          {
            object[] values = new object[reader.FieldCount];
            reader.GetValues(values);
            throw;
          }
        }
      }
    }

    #region Interface Members

    public void Dispose()
    {
      // UI.frmLog.Log.Add(DateTime.Now + " Dispose DbCmd " + this._sql);
      Disposed?.Invoke(this, new EventArgs());
      _dbConn?.Dispose();
      _dbCmd?.Dispose();
    }

    public object Clone()
    {
      return new DbCmd(this._connectionString, this._sql, this._paramValues, this._paramNames);
    }

    public event EventHandler Disposed;

    ISite _site;
    public ISite Site
    {
      get { return this._site; }
      set { this._site = value; }
    }

    #endregion

    public override string ToString() => $"dbCmd: {ID}; {Command_Key}";
  }
}
