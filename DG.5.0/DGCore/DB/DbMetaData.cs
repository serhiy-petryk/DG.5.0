using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace DGCore.DB {

  public static partial class DbMetaData {
    //To Do: 
    //1. To extend providers, use custom section in app.config(like www.bltoolkit.net -> Data -> DataAdapters)
    // Bad: There are a lot of warnings after solution building
    // (how to do: see http://www.codeproject.com/KB/files/CustomConfigSection.aspx, "Custom Configuration Sections for Lazy Coders" by John Whitmire):
    // Code steps:
    // - read from app.config the StringCollection of string presentation of provider types (format: "FullTypeName, Assembly")
    // - AddNewData procedure must be call for each type of provider types in static DbMetaData() init procedure of this class

    // _cache data by long and short namespace, for keys example : "System.Data.OracleClient", "OracleClient" ...
    static Dictionary<string, DbMetaDataBase> _cacheMetaData = new Dictionary<string, DbMetaDataBase>();

    public static void AddNewData(DbMetaDataBase data) {
      string key = data.Namespace.ToUpper();
      if (!_cacheMetaData.ContainsKey(key)) {
        _cacheMetaData.Add(data.Namespace.ToUpper(), data);
        string[] ss = key.Split('.');
        if (ss[ss.Length-1]!=key) {
          _cacheMetaData.Add(ss[ss.Length - 1], data);
        }
      }
    }
    public static DbConnection GetConnection(string shortOrLongNamespace, string connectionString) {
      return GetMetaDataObject(shortOrLongNamespace).GetConnection(connectionString);
    }
    public static DbDataAdapter GetDataAdapter(string dbProviderNamespace) {
      return GetMetaDataObject(dbProviderNamespace).GetDataAdapter();
    }
    public static string QuotedColumnName(string dbProviderNamespace, string unquotedColumnName) {
      return GetMetaDataObject(dbProviderNamespace).QuotedColumnName(unquotedColumnName);
    }
    public static string QuotedTableName(string dbProviderNamespace, string unquotedTableName) {
      return GetMetaDataObject(dbProviderNamespace).QuotedTableName(unquotedTableName);
    }
    public static string QuotedParameterName(string dbProviderNamespace, string unquotedParameterName) {
      return GetMetaDataObject(dbProviderNamespace).QuotedParameterName(unquotedParameterName);
    }
    public static string ParameterNamePattern(string dbProviderNamespace) {
      return GetMetaDataObject(dbProviderNamespace).ParameterNamePattern();
    }
    public static Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableName) {
      return GetMetaDataObject(conn.GetType().Namespace).GetColumnDescriptions(conn, tableName);
    }

    // ================   Private section  =====================
    static DbMetaData() {// Init == fill _cache by subclasses of DbMetaDataBase
      Type t = typeof(DbMetaData);
      Type[] tt = t.GetNestedTypes(BindingFlags.NonPublic| BindingFlags.Public);
      Type baseType =typeof(DbMetaDataBase);
      foreach (Type t1 in tt) {
        if (t1.IsSubclassOf(baseType) && !t1.IsAbstract) {
          ConstructorInfo ci = t1.GetConstructor(new Type[0]);
          if (ci != null) {
            DbMetaDataBase x = (DbMetaDataBase)ci.Invoke(null);
            AddNewData(x);
          }
        }
      }
    }
//    static DbMetaDataBase GetMetaDataObject(DbConnection conn) {
  //    return _cacheMetaData[conn.GetType().Namespace.ToUpper()];
    //}
    static DbMetaDataBase GetMetaDataObject(string key) {
      return _cacheMetaData[key.ToUpper()];
    }

    //=========================================
    public abstract class DbMetaDataBase {
      public abstract string Namespace { get;}
      public abstract DbConnection GetConnection(string connectionString); //to get connection by short or long namespace (commonly you need use the DataFactory)
      public abstract DbDataAdapter GetDataAdapter(); //Commonly to get DataAdapter you need found DataFactory (by namespace name)
      //Quoted(Column/Table)Name/ParameterNamePattern may depend on Provider/version which can obtain fron Connection object (for Odbc/OleDb)
      public abstract string QuotedColumnName(string unquotedColumnName);//DbCommandBuilder.QuoteIdentifier//Suffix/Prefix does not work correctly for OleDb
      public abstract string QuotedTableName(string unquotedTableName);//DbCommandBuilder.QuoteIdentifier/Suffix/Prefix does not work correctly for OleDb/Oracle
      public abstract string QuotedParameterName(string unquotedParameterName);//DbCommandBuilder.QuoteIdentifier/Suffix/Prefix does not work correctly for OleDb/Oracle
      public abstract string ParameterNamePattern();// look at conn.GetSchema(DbMetaDataCollectionNames.DataSourceInformation), column "ParameterNamePattern"
      public abstract Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableBName);
    }

    //===================  OleDb  ===============================
    sealed class DbMetaData_OleDb : DbMetaDataBase {
      public override string Namespace {
        get { return "System.Data.OleDb"; }
      }
      public override DbConnection GetConnection(string connectionString) {
        return new System.Data.OleDb.OleDbConnection(connectionString);
      }
      public override DbDataAdapter GetDataAdapter() {
        return new System.Data.OleDb.OleDbDataAdapter();
      }
      public override string QuotedColumnName(string unquotedColumnName) {
        return "[" + unquotedColumnName + "]";
      }
      public override string QuotedTableName(string unquotedTableName) {
        return "[" + unquotedTableName + "]";
      }
      public override string QuotedParameterName(string unquotedParameterName) {
        return "@" + unquotedParameterName;
      }
      public override string ParameterNamePattern() {
        return @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
      }
      public override Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableBName) {
        return null;
      }
    }

    //==================  SqlClient  ===================================
    sealed class DbMetaData_SqlClient : DbMetaDataBase
    {
      public override string Namespace
      {
        get { return "Microsoft.Data.SqlClient"; }
      }
      public override DbConnection GetConnection(string connectionString)
      {
        return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
      }
      public override DbDataAdapter GetDataAdapter()
      {
        return new Microsoft.Data.SqlClient.SqlDataAdapter();
      }
      public override string QuotedColumnName(string unquotedColumnName)
      {
        return "[" + unquotedColumnName + "]";
      }
      public override string QuotedTableName(string unquotedTableName)
      {
        return "[" + unquotedTableName.Replace("..", ".DBO.").Replace(".", "].[") + "]";
      }
      public override string QuotedParameterName(string unquotedParameterName)
      {
        return "@" + unquotedParameterName;
      }
      public override string ParameterNamePattern()
      {
        return @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
      }
      public override Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableName)
      {
        return SqlClient_GetColumnDescription(conn, tableName);
      }
    }

    //==================  MySqlClient  ===================================
    sealed class DbMetaData_MySqlClient : DbMetaDataBase
    {
      public override string Namespace
      {
        get { return "MySql.Data.MySqlClient"; }
      }
      public override DbConnection GetConnection(string connectionString)
      {
        return new MySql.Data.MySqlClient.MySqlConnection(connectionString);
      }
      public override DbDataAdapter GetDataAdapter()
      {
        return new MySql.Data.MySqlClient.MySqlDataAdapter();
      }
      public override string QuotedColumnName(string unquotedColumnName)
      {
        return "`" + unquotedColumnName + "`";
      }
      public override string QuotedTableName(string unquotedTableName)
      {
        return "`" + unquotedTableName.Replace(".", "`.`") + "`";
      }
      public override string QuotedParameterName(string unquotedParameterName)
      {
        return "@" + unquotedParameterName;
      }
      public override string ParameterNamePattern()
      {
        return @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
      }
      public override Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableName)
      {
        return MySqlClient_GetColumnDescription(conn, tableName);
      }
    }

    //==================  OracleClient  ===================================
    /*      sealed class DbMetaData_OracleClient : DbMetaDataBase {
            public override string Namespace {
              get { return "System.Data.OracleClient"; }
            }
            public override DbConnection GetConnection(string connectionString) {
              return new System.Data.OracleClient.OracleConnection(connectionString);
            }
            public override DbDataAdapter GetDataAdapter() {
              return new System.Data.OracleClient.OracleDataAdapter();
            }
            public override string QuotedColumnName(string unquotedColumnName) {
              return "\"" + unquotedColumnName + "\"";
            }
            public override string QuotedTableName(string unquotedTableName) {
              return unquotedTableName;
            }
            public override string QuotedParameterName(string unquotedParameterName) {
              return ":" + unquotedParameterName;
            }
            public override string ParameterNamePattern() {
              return @":([\\p{Lo}\\p{Lu}\\p{Ll}\\p{Lm}__#$][\\p{Lo}\\p{Lu}\\p{Ll}\\p{Lm}\\p{Nd}__#$]*)";
            }
            public override Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableBName) {
              return null;
            }
          }*/

  }
}
