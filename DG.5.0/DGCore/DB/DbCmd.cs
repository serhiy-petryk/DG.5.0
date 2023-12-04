using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;

namespace DGCore.DB
{
  [SupportedOSPlatform("windows")]
  public class DbCmd : IComponent, IDisposable, ICloneable
  {
    public enum DbCmdKind { Query, Procedure, File }

    private static int idCnt = 0;

    public int ID = idCnt++;
    public readonly string _connectionString;
    public readonly string _sql;
    readonly DbConnection _dbConn;
    public readonly DbCommand _dbCmd;
    public readonly Dictionary<string, object> _parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public DbCmdKind _cmdKind => _connectionString.StartsWith("File;", StringComparison.OrdinalIgnoreCase)
      ? DbCmdKind.File
      : (_sql.IndexOf(' ') == -1 ? DbCmdKind.Procedure : DbCmdKind.Query);

    public DbCmd(string connectionString, string sql) : this(connectionString, sql, null)
    {
    }

    public DbCmd(string connectionString, string sql, IDictionary<string, object> parameters)
    {
      if (File.Exists(connectionString)) connectionString = "File;" + connectionString;
      this._connectionString = connectionString;
      this._sql = sql.Trim();
      this._dbConn = DbUtils.Connection_Get(connectionString);
      this._dbCmd = this._dbConn.CreateCommand();
      this._dbCmd.CommandText = this._sql;
      this._dbCmd.CommandType = _cmdKind == DbCmdKind.Procedure ? CommandType.StoredProcedure : CommandType.Text;
      if (_dbConn.ConnectionTimeout == 0 || (_dbCmd.CommandTimeout != 0 && _dbCmd.CommandTimeout < _dbConn.ConnectionTimeout))
        _dbCmd.CommandTimeout = _dbConn.ConnectionTimeout;
      this.Parameters_Add(parameters, false);
    }

    public string Command_Key => DbUtils.Command_GetKey(_dbCmd);

    public void Parameters_Add(IDictionary<string, object> parameters, bool clear)
    {
      if (clear) _parameters.Clear();
      
      if (parameters != null)
        foreach (var kvp in parameters) _parameters.Add(kvp.Key, kvp.Value);

      Parameters_Update();
    }

    void Parameters_Update()
    {
      this._dbCmd.Parameters.Clear();
      foreach (var kvp in _parameters)
      {
        var par = _dbCmd.CreateParameter();
        par.ParameterName = kvp.Key;
        par.Value = kvp.Value;
        _dbCmd.Parameters.Add(par);
      }
      DbUtils.AdjustParameters(this._dbCmd);
    }

    public DbSchemaTable GetSchemaTable() => DbSchemaTable.GetSchemaTable(_dbCmd);

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
    public void Fill<KeyType, ItemType>(IDictionary data, Delegate keyFunction, DbColumnMapElement[] columnMap)
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

    public object Clone() => new DbCmd(this._connectionString, this._sql, this._parameters);

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
