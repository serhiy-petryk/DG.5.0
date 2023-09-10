using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Text.RegularExpressions;

namespace DGCore.DB
{
    public static partial class DbUtils
    {

        // format: key: connID; value: (DataAdapterKey; connectionString) or (filename from MDB, ..)
        const string _defaultParameterPattern = @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";

        #region =========  Connection  ==========
        public static string Connection_GetKey(DbConnection conn) =>
            conn.GetType().FullName + ";" + conn.Database + ";" + conn.DataSource;

        public static DbConnection Connection_Get(string myConnectionString)
        {
            // myConnectionString format: filename or "short/long provider namespace;connection string"
            // Example: @"Oledb;Provider=Microsoft.Jet.OLEDB.4.0;Data Source=T:\Data\DBQ\mdb.day\testDB.mdb"
            //         @"system.data.Oledb;Provider=Microsoft.Jet.OLEDB.4.0;Data Source=T:\Data\DBQ\mdb.day\testDB.mdb"
            //         @"T:\Data\DBQ\mdb.day\testDB.mdb"

            if (File.Exists(myConnectionString))
            {// file name
                string extension = Path.GetExtension(myConnectionString).ToLower();
                switch (extension)
                {
                    case ".mdb": return new OleDbConnection($@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={myConnectionString}");
                    case ".csv": return new CSV.TestCsvConnection(myConnectionString);
                }
                throw new Exception("Connection does not define for file type " + extension);
            }

            if (myConnectionString.StartsWith("File;", StringComparison.InvariantCultureIgnoreCase) && File.Exists(myConnectionString.Substring(5)))
            {// file (csv, json, ...)
                string fn = myConnectionString.Substring(5);
                string extension = Path.GetExtension(fn).ToLower();
                switch (extension)
                {
                    case ".mdb": return new OleDbConnection($@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={fn}");
                    case ".csv": return new CSV.TestCsvConnection(fn);
                }
                throw new Exception("Connection does not define for file type " + extension);
            }

            int k = myConnectionString.IndexOf(';');
            if (k < 1) throw new Exception("Invalid connection string. ConnectionString must be exist in StandardConnectionDictionary or to be in format  \"<short/long provider namespace>;<connection string>\"");
            string provider = myConnectionString.Substring(0, k).Trim();// Provider name
            if (provider != "SqlClient")
            {
            }
            string connString = myConnectionString.Substring(k + 1).Trim();
            try
            {
                return DbMetaData.GetConnection(provider, connString);
            }
            catch
            {
                throw new Exception("Error while creating of connection string. Provider: " + provider + "; connectionString: " + connString);
            }
        }

        public static void Connection_Open(DbConnection connection)
        {
            if ((ConnectionState.Open & connection.State) == ConnectionState.Closed) connection.Open();
        }
        #endregion

        #region ==========  Command  =============
        public static string Command_GetKey(DbCommand cmd) => Connection_GetKey(cmd.Connection) + ";" + cmd.CommandText;
        public static DbCommand Command_Get(DbConnection conn, string sql, Dictionary<string, object> parameters)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = sql.IndexOf(' ') == -1 ? CommandType.StoredProcedure : CommandType.Text;
            Command_SetParameters(cmd, parameters);
            return cmd;
        }
        public static void Command_SetParameters(DbCommand cmd, Dictionary<string, object> parameters)
        {
            cmd.Parameters.Clear();
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    var par = cmd.CreateParameter();
                    par.ParameterName = kvp.Key;
                    par.Value = kvp.Value;
                    cmd.Parameters.Add(par);
                }
                AdjustParameters(cmd);
            }
        }
        #endregion

        //===================== Private section (GetDataAdapter/Schema/DbProviderFactory) ==============
        internal static DataTable GetSchemaTable(DbCommand cmd)
        {
            // SqlClient needs to fill cmd with parameters; OracleClient&OleDb - does not need  
            Connection_Open(cmd.Connection);
            var flagParameterInfo = 0;
            var error = false;
            while (flagParameterInfo < 2)
            {
                try
                {
                    if (flagParameterInfo == 1)
                    {
                        var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        if (!(cmd.Connection is CSV.TestCsvConnection))
                        {
                            foreach (var p in GetParameterNamesFromSqlText(cmd.GetType().Namespace, cmd.CommandText))
                                parameters.Add(p, DBNull.Value);
                            Command_SetParameters(cmd, parameters);
                        }
                    }
                    using (var reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly))
                    {
                        var dt = reader.GetSchemaTable();
                        reader.Close();
                        if (dt == null)
                        {
                            try
                            {
                                using (var reader1 = cmd.ExecuteReader())
                                    reader1.Read();
                            }
                            catch (Exception ex)
                            {
                                error = true;
                                throw new Exception("Invalid SQL statement: " + cmd.CommandText + Environment.NewLine + ex.Message);
                            }
                            error = true;
                            throw new Exception("Invalid SQL statement: " + cmd.CommandText);
                        }
                        // dt.ExtendedProperties.Add("ParameterNames", parameterNames);
                        return dt;
                    }
                }
                catch (Exception ex)
                {
                    if (error) throw; // return earlier error
                    if (flagParameterInfo > 0) throw new Exception("Can not get Schema for SQL statement: " + cmd.CommandText + Environment.NewLine + ex.Message);
                    flagParameterInfo++;
                }
            }
            return null;
        }

        private static List<string> GetParameterNamesFromSqlText(string dbProviderNamespace, string sql)
        {
            var parameterNames = new List<string>();
            var parameterNamesInUpper = new List<string>();
            Regex r = new Regex(DbMetaData.ParameterNamePattern(dbProviderNamespace), RegexOptions.Singleline | RegexOptions.IgnoreCase);
            MatchCollection matches = r.Matches(sql);
            foreach (Match match in matches)
            {
                if (!parameterNamesInUpper.Contains(match.Value.ToUpper()))
                {
                    parameterNamesInUpper.Add(match.Value.ToUpper());
                    parameterNames.Add(match.Value);
                }
            }
            return parameterNames;
        }

        public static void AdjustParameters(DbCommand cmd)
        {
            foreach (DbParameter par in cmd.Parameters)
            {
                if (par.Value == null || par.Value == DBNull.Value)
                {
                    par.Value = DBNull.Value;
                    //          par.DbType = DbType.String;
                    //          par.DbType = DbType.DateTime;
                    if (par.DbType == DbType.String) par.Size = 1;
                    return;
                }
                if (par.Value is DateTime && ((DateTime)par.Value) == new DateTime(0))
                {
                    par.Value = DBNull.Value;
                    return;
                }
                if ((par.Value is double) && (double.IsNaN((double)par.Value)))
                {
                    par.Value = DBNull.Value;
                    par.DbType = DbType.Double;
                    return;
                }
                if ((par.Value is float) && (float.IsNaN((float)par.Value)))
                {
                    par.Value = DBNull.Value;
                    par.DbType = DbType.Single;
                    return;
                }
                //        par.Value = value;
                par.DbType = par.DbType;//нужно явно указать тип параметра
                if (par.DbType == DbType.String)
                {
                    if (par.Value is string)
                    {
                        par.Size = Math.Max(1, ((string)par.Value).Length);
                    }
                }
            }
        }
    }
}
