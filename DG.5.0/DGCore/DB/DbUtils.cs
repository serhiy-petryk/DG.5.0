using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.IO;
using System.Text.RegularExpressions;

namespace DGCore.DB {
  public static partial class DbUtils {

    // format: key: connID; value: (DataAdapterKey; connectionString) or (filename from MDB, ..)
    const string _defaultParameterPattern = @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";

    //  =======  Connection 
    public static DbConnection Connection_Get(string myConnectionString) {
      // myConnectionString format: filename or "short/long provider namespace;connection string"
      // Example: @"Oledb;Provider=Microsoft.Jet.OLEDB.4.0;Data Source=T:\Data\DBQ\mdb.day\testDB.mdb"
      //         @"system.data.Oledb;Provider=Microsoft.Jet.OLEDB.4.0;Data Source=T:\Data\DBQ\mdb.day\testDB.mdb"
      //         @"T:\Data\DBQ\mdb.day\testDB.mdb"

      // Try to get connectionString from standard connection repository (_standardConnections)

      string sTmp;
      DbCmd._standardConnections.TryGetValue(myConnectionString, out sTmp);
      if (!String.IsNullOrEmpty(sTmp)) {
        myConnectionString = sTmp;
      }
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
      if (k <1) throw new Exception("Invalid connection string. ConnectionString must be exist in StandardConnectionDictionary or to be in format  \"<short/long provider namespace>;<connection string>\"");
      string provider = myConnectionString.Substring(0, k).Trim();// Provider name
      if (provider != "SqlClient") {
      }
      string connString = myConnectionString.Substring(k + 1).Trim();
      try {
        return DbMetaData.GetConnection(provider, connString);
      }
      catch {
        throw new Exception("Error while creating of connection string. Provider: " + provider+"; connectionString: " + connString);
      }
    }
    public static string Connection_GetKey(DbConnection conn) {
      return conn.GetType().FullName + ";" + conn.Database + ";" + conn.DataSource;
    }
    public static void Connection_Open(DbConnection connection) {
      if ((ConnectionState.Open & connection.State) == ConnectionState.Closed) connection.Open();
    }

    //  =======  Command
    public static CommandType Command_GetType(string commandText) {
      //CommandType.TableDirect does not supported by SqlServer
      return (commandText.IndexOf(' ') == -1 ? CommandType.StoredProcedure : CommandType.Text);
    }
    public static DbCommand Command_Get(DbConnection conn, string sql) {
      return Command_Get(conn, sql, null, null);
    }
    public static DbCommand Command_Get(string connectionString, string sql) {
      return Command_Get(Connection_Get(connectionString), sql, null, null);
    }
    public static DbCommand Command_Get(DbConnection conn, string sql, IEnumerable paramValues, IEnumerable<string> paramNames) {
      DbCommand cmd = conn.CreateCommand();
      cmd.CommandText = sql;
      cmd.CommandType = DbUtils.Command_GetType(sql);
      if (paramValues != null) Command_SetParameters(cmd, paramValues, paramNames);
      return cmd;
    }
    public static void Command_SetParameters(DbCommand cmd, IEnumerable paramValues, IEnumerable<string> paramNames) {
      cmd.Parameters.Clear();
      Command_AddParameters(cmd, paramValues, paramNames);
    }
    public static void Command_AddParameters(DbCommand cmd, IEnumerable paramValues, IEnumerable<string> paramNames) {
      if (paramNames == null) {
        foreach (object parValue in paramValues) {
          DbParameter par = cmd.CreateParameter();
//          AdjustParameter(par, parValue);
          cmd.Parameters.Add(par);
        }
      }
      else {
        IEnumerator<string> enames= paramNames.GetEnumerator();
        enames.MoveNext();
        foreach(object paramValue in paramValues) {
          DbParameter par = cmd.CreateParameter();
          string s = enames.Current;
          par.ParameterName = enames.Current;
          par.Value = paramValue;
  //        AdjustParameter(par, paramValue);
          cmd.Parameters.Add(par);
          enames.MoveNext();
        }
      }
      AdjustParameters(cmd);
    }
    // =============  Fill =============
    public static void Fill(string connectionString, string sql, IEnumerable paramValues, IEnumerable<string> paramNames, DataTable dt) {
      DbConnection conn = Connection_Get(connectionString);
      DbCommand cmd = Command_Get(conn, sql, paramValues, paramNames);
      AdjustParameters(cmd);
      using (DbDataAdapter da = DbMetaData.GetDataAdapter(cmd.GetType().Namespace))
      {
        da.SelectCommand = cmd;
        Connection_Open(cmd.Connection);
        cmd.Prepare();
        da.Fill(dt);
      }
    }

    //===================== Private section (GetDataAdapter/Schema/DbProviderFactory) ==============
    internal static DataTable GetSchemaTable(DbCommand cmd) {
      // SqlClient needs to fill cmd with parameters; OracleClient&OleDb - does not need  
      Connection_Open(cmd.Connection);
      List<string> parameterNames = cmd.Connection is CSV.TestCsvConnection
        ? new List<string>()
        : GetParameterNamesFromSqlText(cmd.GetType().Namespace, cmd.CommandText);
      int flagParameterInfo = 0;
      bool error = false;
      while (flagParameterInfo < 2) {
        try {
          if (flagParameterInfo == 1) {
            DBNull[] paramValues = new DBNull[parameterNames.Count];
            for (int i = 0; i < paramValues.Length; i++) paramValues[i] = DBNull.Value;
            Command_SetParameters(cmd, paramValues, parameterNames);
          }
          AdjustParameters(cmd);
          using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly)) {
            DataTable dt = reader.GetSchemaTable();
            reader.Close();
            if (dt == null) {
              try {
                using (DbDataReader reader1 = cmd.ExecuteReader()) {
                  reader1.Read();
                  reader1.Close();
                }
              }
              catch (Exception ex) {
                error = true;
                throw new Exception("Invalid SQL statement: " + cmd.CommandText + Environment.NewLine + ex.Message);
              }
              error = true;
              throw new Exception("Invalid SQL statement: " + cmd.CommandText);
            }
            dt.ExtendedProperties.Add("ParameterNames", parameterNames);
            return dt;
          }
        }
        catch (Exception ex) {
          if (error) throw; // return earlier error
          if (flagParameterInfo > 0) throw new Exception("Can not get Schema for SQL statement: " + cmd.CommandText + Environment.NewLine + ex.Message);
          flagParameterInfo++;
        }
      }
      return null;
    }

    private static List<string> GetParameterNamesFromSqlText(string dbProviderNamespace, string sql)
    {
      List<string> parameterNames = new List<string>();
      List<string> parameterNamesInUpper = new List<string>();
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
    
    public static void AdjustParameters(DbCommand cmd) {
      foreach (DbParameter par in cmd.Parameters) {
        if (par.Value == null || par.Value == DBNull.Value) {
          par.Value = DBNull.Value;
//          par.DbType = DbType.String;
//          par.DbType = DbType.DateTime;
          if (par.DbType == DbType.String) par.Size = 1;
          return;
        }
        if (par.Value is DateTime && ((DateTime)par.Value) == new DateTime(0)) {
          par.Value = DBNull.Value;
          return;
        }
        if ((par.Value is double) && (double.IsNaN((double)par.Value))) {
          par.Value = DBNull.Value;
          par.DbType = DbType.Double;
          return;
        }
        if ((par.Value is float) && (float.IsNaN((float)par.Value))) {
          par.Value = DBNull.Value;
          par.DbType = DbType.Single;
          return;
        }
//        par.Value = value;
        par.DbType = par.DbType;//нужно явно указать тип параметра
        if (par.DbType == DbType.String) {
          if (par.Value is string) {
            par.Size = Math.Max(1, ((string)par.Value).Length);
          }
        }
      }
    }
  }
}
