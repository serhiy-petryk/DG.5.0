using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

namespace DGCore.DB
{

    public static partial class DbMetaData
    {
        //To Do: 
        //1. To extend providers, use custom section in app.config(like www.bltoolkit.net -> Data -> DataAdapters)
        // Bad: There are a lot of warnings after solution building
        // (how to do: see http://www.codeproject.com/KB/files/CustomConfigSection.aspx, "Custom Configuration Sections for Lazy Coders" by John Whitmire):
        // Code steps:
        // - read from app.config the StringCollection of string presentation of provider types (format: "FullTypeName, Assembly")
        // - AddNewData procedure must be call for each type of provider types in static DbMetaData() init procedure of this class

        // _cache data by long and short namespace, for keys example : "System.Data.OracleClient", "OracleClient" ...

        #region ==========  Init section  ============
        private static readonly DbMetaDataBase[] MetaDataList = {new DbMetaData_SqlClient(), new DbMetaData_OleDb(), new DbMetaData_MySqlClient()};
        private static readonly Dictionary<string, DbMetaDataBase> CacheMetaData = new Dictionary<string, DbMetaDataBase>();

        static DbMetaData()
        {
            foreach (var m in MetaDataList)
                using (var conn = m.GetConnection(null))
                {
                    var key = conn.GetType().Namespace.ToUpper();
                    if (!CacheMetaData.ContainsKey(key))
                    {
                        CacheMetaData.Add(key, m);
                        var ss = key.Split('.');
                        if (ss[ss.Length - 1] != key)
                            CacheMetaData.Add(ss[ss.Length - 1], m);
                    }
                }
        }
        #endregion

        #region =============  Static section  =============
        public static DbConnection GetConnection(string shortOrLongNamespace, string connectionString) => GetMetaDataObject(shortOrLongNamespace).GetConnection(connectionString);
        public static string QuotedColumnName(string dbProviderNamespace, string unquotedColumnName) => GetMetaDataObject(dbProviderNamespace).QuotedColumnName(unquotedColumnName);
        public static string QuotedTableName(string dbProviderNamespace, string unquotedTableName) => GetMetaDataObject(dbProviderNamespace).QuotedTableName(unquotedTableName);
        public static string QuotedParameterName(string dbProviderNamespace, string unquotedParameterName) => GetMetaDataObject(dbProviderNamespace).QuotedParameterName(unquotedParameterName);
        public static string ParameterNamePattern(string dbProviderNamespace) => GetMetaDataObject(dbProviderNamespace).ParameterNamePattern();
        public static Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableName) => GetMetaDataObject(conn.GetType().Namespace).GetColumnDescriptions(conn, tableName);

        private static DbMetaDataBase GetMetaDataObject(string key) => CacheMetaData[key.ToUpper()];
        #endregion

        #region ===========  DbMetaDataBase class  ============
        private abstract class DbMetaDataBase
        {
            public abstract DbConnection GetConnection(string connectionString); //to get connection by short or long namespace (commonly you need use the DataFactory)
            // Quoted(Column/Table)Name/ParameterNamePattern may depend on Provider/version which can obtain from Connection object (for Odbc/OleDb)
            public abstract string QuotedColumnName(string unquotedColumnName);//DbCommandBuilder.QuoteIdentifier//Suffix/Prefix does not work correctly for OleDb
            public abstract string QuotedTableName(string unquotedTableName);//DbCommandBuilder.QuoteIdentifier/Suffix/Prefix does not work correctly for OleDb/Oracle
            public abstract string QuotedParameterName(string unquotedParameterName);//DbCommandBuilder.QuoteIdentifier/Suffix/Prefix does not work correctly for OleDb/Oracle
            public abstract string ParameterNamePattern();// look at conn.GetSchema(DbMetaDataCollectionNames.DataSourceInformation), column "ParameterNamePattern"
            public abstract Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableBName);
        }
        #endregion

        #region ===========  OleDb DbMetaData class  ============
        sealed class DbMetaData_OleDb : DbMetaDataBase
        {
            public override DbConnection GetConnection(string connectionString) => new System.Data.OleDb.OleDbConnection(connectionString);
            public override string QuotedColumnName(string unquotedColumnName) => "[" + unquotedColumnName + "]";
            public override string QuotedTableName(string unquotedTableName) => "[" + unquotedTableName + "]";
            public override string QuotedParameterName(string unquotedParameterName) => "@" + unquotedParameterName;
            public override string ParameterNamePattern() => @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
            public override Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableBName) => null;
        }
        #endregion

        #region ===========  SqlClient DbMetaData class  ============
        sealed class DbMetaData_SqlClient : DbMetaDataBase
        {
            public override DbConnection GetConnection(string connectionString) => new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            public override string QuotedColumnName(string unquotedColumnName) => "[" + unquotedColumnName + "]";
            public override string QuotedTableName(string unquotedTableName) => "[" + unquotedTableName.Replace("..", ".DBO.").Replace(".", "].[") + "]";
            public override string QuotedParameterName(string unquotedParameterName) => "@" + unquotedParameterName;
            public override string ParameterNamePattern() => @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
            public override Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableName) => SqlClient_GetColumnDescription(conn, tableName);
        }
        #endregion

        #region ===========  MySqlClient DbMetaData class  ============
        sealed class DbMetaData_MySqlClient : DbMetaDataBase
        {
            public override DbConnection GetConnection(string connectionString) => new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            public override string QuotedColumnName(string unquotedColumnName) => "`" + unquotedColumnName + "`";
            public override string QuotedTableName(string unquotedTableName) => "`" + unquotedTableName.Replace(".", "`.`") + "`";
            public override string QuotedParameterName(string unquotedParameterName) => "@" + unquotedParameterName;
            public override string ParameterNamePattern() => @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
            public override Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableName) => MySqlClient_GetColumnDescription(conn, tableName);
        }
        #endregion

        //==================  OracleClient  ===================================
        /*      sealed class DbMetaData_OracleClient : DbMetaDataBase {
                public override string Namespace {
                  get { return "System.Data.OracleClient"; }
                }
                public override DbConnection GetConnection(string connectionString) {
                  return new System.Data.OracleClient.OracleConnection(connectionString);
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
