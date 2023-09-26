using System;
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
