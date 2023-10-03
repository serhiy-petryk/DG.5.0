using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.DB
{
    public class DbCmd: IDisposable, ICloneable
    {
        public enum DbCmdKind { Query, Procedure, File }

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
            _connectionString = connectionString;
            _sql = sql.Trim();
            _dbConn = DbHelper.Connection_Get(connectionString);
            _dbCmd = this._dbConn.CreateCommand();
            _dbCmd.CommandText = this._sql;
            _dbCmd.CommandType = _cmdKind == DbCmdKind.Procedure ? CommandType.StoredProcedure : CommandType.Text;
            if (_dbConn.ConnectionTimeout == 0 || (_dbCmd.CommandTimeout != 0 && _dbCmd.CommandTimeout < _dbConn.ConnectionTimeout))
                _dbCmd.CommandTimeout = _dbConn.ConnectionTimeout;
            Parameters_Add(parameters, false);
        }

        public void Parameters_Add(IDictionary<string, object> parameters, bool clear)
        {
            if (clear) _parameters.Clear();

            if (parameters != null)
                foreach (var kvp in parameters)
                    _parameters.Add(kvp.Key, kvp.Value);

            Parameters_Update();
        }

        void Parameters_Update()
        {
            _dbCmd.Parameters.Clear();
            foreach (var kvp in _parameters)
            {
                var par = _dbCmd.CreateParameter();
                par.ParameterName = kvp.Key;
                par.Value = kvp.Value;
                _dbCmd.Parameters.Add(par);
            }
            DbHelper.AdjustParameters(_dbCmd);
        }

        public DbSchemaTable GetSchemaTable() => DbSchemaTable.GetSchemaTable(_dbCmd);

        // fill dictionary - is simplest; user must create more complex fill separate
        public void Fill<KeyType, ItemType>(IDictionary data, Delegate keyFunction, DbColumnMapElement[] columnMap)
        {
            Func<ItemType, KeyType> keyFunc = (Func<ItemType, KeyType>)keyFunction;
            Func<DbDataReader, ItemType> func = DbReaderHelper.GetDelegate_FromDataReaderToObject<ItemType>(this, columnMap);

            DbHelper.Connection_Open(_dbConn);
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

        public event EventHandler Disposed;
        public void Dispose()
        {
            // UI.frmLog.Log.Add(DateTime.Now + " Dispose DbCmd " + this._sql);
            Disposed?.Invoke(this, new EventArgs());
            _dbConn?.Dispose();
            _dbCmd?.Dispose();
        }

        public object Clone() => new DbCmd(_connectionString, _sql, _parameters);

        #endregion
    }
}
