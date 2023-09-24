using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Data.Helpers
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

        private static readonly Dictionary<string, Dictionary<string, string>> CacheColumnDescriptions =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        static DbMetaData()
        {
            foreach (var m in MetaDataList)
                using (var conn = m.GetConnection(null))
                {
                    var key = conn.GetType().Namespace;
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
        public static string QuotedParameterName(string dbProviderNamespace, string unquotedParameterName) => GetMetaDataObject(dbProviderNamespace).QuotedParameterName(unquotedParameterName);
        public static string ParameterNamePattern(string dbProviderNamespace) => GetMetaDataObject(dbProviderNamespace).ParameterNamePattern();
        public static Dictionary<string, string> GetColumnDescriptions(DbConnection conn, string tableName) => GetMetaDataObject(conn.GetType().Namespace).ColumnDescriptions(conn, tableName);

        private static DbMetaDataBase GetMetaDataObject(string key) => CacheMetaData[key];
        #endregion

        #region ===========  DbMetaDataBase class  ============
        private abstract class DbMetaDataBase
        {
            public abstract DbConnection GetConnection(string connectionString); //to get connection by short or long namespace (commonly you need use the DataFactory)
            // Quoted(Column/Table)Name/ParameterNamePattern may depend on Provider/version which can obtain from Connection object (for Odbc/OleDb)
            public abstract string QuotedColumnName(string unquotedColumnName);//DbCommandBuilder.QuoteIdentifier//Suffix/Prefix does not work correctly for OleDb
            public abstract string QuotedParameterName(string unquotedParameterName);//DbCommandBuilder.QuoteIdentifier/Suffix/Prefix does not work correctly for OleDb/Oracle
            public abstract string ParameterNamePattern();// look at conn.GetSchema(DbMetaDataCollectionNames.DataSourceInformation), column "ParameterNamePattern"
            public abstract Dictionary<string, string> ColumnDescriptions(DbConnection conn, string tableBName);
        }
        #endregion

        #region ===========  OleDb DbMetaData class  ============
        sealed class DbMetaData_OleDb : DbMetaDataBase
        {
            public override DbConnection GetConnection(string connectionString) => new System.Data.OleDb.OleDbConnection(connectionString);
            public override string QuotedColumnName(string unquotedColumnName) => "[" + unquotedColumnName + "]";
            public override string QuotedParameterName(string unquotedParameterName) => "@" + unquotedParameterName;
            public override string ParameterNamePattern() => @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
            public override Dictionary<string, string> ColumnDescriptions(DbConnection conn, string tableBName) => null;
        }
        #endregion

        #region ===========  SqlClient DbMetaData class  ============
        sealed class DbMetaData_SqlClient : DbMetaDataBase
        {
            public override DbConnection GetConnection(string connectionString) => new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            public override string QuotedColumnName(string unquotedColumnName) => "[" + unquotedColumnName + "]";
            public override string QuotedParameterName(string unquotedParameterName) => "@" + unquotedParameterName;
            public override string ParameterNamePattern() => @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
            public override Dictionary<string, string> ColumnDescriptions(DbConnection conn, string tableName)
            {
                var key = Db.Connection_GetKey(conn) + "#" + tableName;
                if (!CacheColumnDescriptions.ContainsKey(key))
                {
                    // SQL SERVER INFORMATION_SCHEMA list: http://technet.microsoft.com/en-us/library/ms186778(v=sql.90).aspx
                    string sql;
                    if (conn.ServerVersion.StartsWith("08."))
                    {
                        // Sql server 2000
                        sql = "SELECT i_s.TABLE_NAME, i_s.COLUMN_NAME, s.value FROM INFORMATION_SCHEMA.COLUMNS i_s " +
                              "INNER JOIN sysproperties s ON s.id = OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME) AND s.smallid = i_s.ORDINAL_POSITION AND s.name = 'MS_Description' " +
                              "WHERE (i_s.TABLE_NAME=@table_name or (i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME)=@table_name) and OBJECTPROPERTY(OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME), 'IsMsShipped')=0";
                    }
                    else
                    {
                        // Sql server2005(version='09'), sql server 2014 (version='12')
                        //sql = "SELECT i_s.TABLE_NAME, i_s.COLUMN_NAME, s.value FROM INFORMATION_SCHEMA.COLUMNS i_s " +
                        //  "INNER JOIN sys.extended_properties s ON s.major_id = OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME) AND s.minor_id = i_s.ORDINAL_POSITION AND s.name = 'MS_Description' " +
                        //  "WHERE (i_s.TABLE_NAME=@table_name or (i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME)=@table_name) and OBJECTPROPERTY(OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME), 'IsMsShipped')=0";
                        /*sql = "select st.name [Table_name], sc.name [Column_name], sep.value [Value] from sys.tables st " +
                              "inner join sys.columns sc on st.object_id = sc.object_id " +
                              "inner join sys.schemas ss on st.schema_id=ss.schema_id " +
                              "left join sys.extended_properties sep on st.object_id = sep.major_id " +
                              "and sc.column_id = sep.minor_id and sep.name = 'MS_Description' " +
                              "where st.name = @table_name or ss.name+'.'+st.name = @table_name";*/
                        var names = tableName.Split('.');
                        var dbName = names.Length == 3 ? names[0] + "." : null;
                        var ownerName = names.Length > 1 && !string.IsNullOrEmpty(names[names.Length - 2])
                            ? names[names.Length - 2]
                            : "dbo";
                        tableName = names[names.Length - 1];
                        sql =
                            $"select st.name [Table_name], sc.name [Column_name], sep.value [Value] from {dbName}sys.tables st " +
                            $"inner join {dbName}sys.columns sc on st.object_id = sc.object_id " +
                            $"inner join {dbName}sys.schemas ss on st.schema_id=ss.schema_id " +
                            $"left join {dbName}sys.extended_properties sep on st.object_id = sep.major_id " +
                            $"and sc.column_id = sep.minor_id and sep.name = 'MS_Description' " +
                            $"where st.name = @table_name and ss.name = '{ownerName}'";
                    }
                    CacheColumnDescriptions.Add(key, GetColumnDescriptionsBySql(conn, sql, tableName));
                }

                return CacheColumnDescriptions[key];
            }
        }
        #endregion

        #region ===========  MySqlClient DbMetaData class  ============
        sealed class DbMetaData_MySqlClient : DbMetaDataBase
        {
            public override DbConnection GetConnection(string connectionString) => new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            public override string QuotedColumnName(string unquotedColumnName) => "`" + unquotedColumnName + "`";
            public override string QuotedParameterName(string unquotedParameterName) => "@" + unquotedParameterName;
            public override string ParameterNamePattern() => @"@[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
            public override Dictionary<string, string> ColumnDescriptions(DbConnection conn, string tableName)
            {
                var key = Db.Connection_GetKey(conn) + "#" + tableName;
                if (!CacheColumnDescriptions.ContainsKey(key))
                {
                    var sql = "SELECT table_name, column_name, column_comment as value FROM INFORMATION_SCHEMA.COLUMNS a " +
                        "WHERE (TABLE_NAME = @table_name or concat_ws('.', a.TABLE_SCHEMA, a.TABLE_NAME) = @table_name) and ifnull(column_comment, '') <> ''";
                    CacheColumnDescriptions.Add(key, GetColumnDescriptionsBySql(conn, sql, tableName));
                }
                return CacheColumnDescriptions[key];
            }
        }
        #endregion

        #region ==========  Private methods  ==============
        // !!! COLUMNS DESCRIPTIONS 
        // taken from http://databases.aspfaq.com/schema-tutorials/schema-how-do-i-show-the-description-property-of-a-column.html
        // =============  Sql Server 2000:
        //Создание: exec sp_addextendedproperty N'MS_Description', N'Точность в бухгалтерии', N'user', N'dbo', N'table', N'Branches', N'column', N'scale_buh'
        /*  1.
        select sysobjects.name as name_table,
        syscolumns.name as name_column, 
        systypes.name as name_type,
        syscolumns.length ,
        syscolumns.xprec,
        syscolumns.xscale,
        sysproperties.value as description 
        from
        sysobjects inner join syscolumns on syscolumns.id=sysobjects.id
        inner join sysproperties on sysproperties.smallid=syscolumns.colid and sysproperties.id=sysobjects.id
        inner join systypes on syscolumns.xtype=systypes.xusertype
        where sysobjects.xtype='U' and sysproperties.name='MS_Description' and sysobjects.name='nbu_rates'
        order by sysobjects.name,syscolumns.name

    2. SELECT 
            [Table Name] = i_s.TABLE_NAME, 
            [Column Name] = i_s.COLUMN_NAME, 
            [Description] = s.value 
        FROM 
            INFORMATION_SCHEMA.COLUMNS i_s 
        INNER JOIN 
            sysproperties s 
        ON 
            s.id = OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME) 
            AND s.smallid = i_s.ORDINAL_POSITION 
            AND s.name = 'MS_Description' 
        WHERE 
            OBJECTPROPERTY(OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME), 'IsMsShipped')=0 
        ORDER BY 
            i_s.TABLE_NAME, i_s.ORDINAL_POSITION*/

        //===============  Sql server 2005:
        /*    SELECT  
            [Table Name] = OBJECT_NAME(c.object_id), 
            [Column Name] = c.name, 
            [Description] = ex.value  
        FROM  
            sys.columns c  
        LEFT OUTER JOIN  
            sys.extended_properties ex  
        ON  
            ex.major_id = c.object_id 
            AND ex.minor_id = c.column_id  
            AND ex.name = 'MS_Description'  
        WHERE  
            OBJECTPROPERTY(c.object_id, 'IsMsShipped')=0  
            -- AND OBJECT_NAME(c.object_id) = 'your_table' 
        ORDER  
            BY OBJECT_NAME(c.object_id), c.column_id*/

        // =============  MS Access 
        //1.      OleDbConnection conn = (OleDbConnection)DbUtils.Connection_Get(cs);
        //      conn.Open();
        //      DataTable dt = conn.GetSchema("Columns");// column description is 27-th item in DataRow.ItemArray
        //2.      <% 
        //  on error resume next 
        //Set Catalog = CreateObject("ADOX.Catalog") 
        //    Catalog.ActiveConnection = "Provider=Microsoft.Jet.OLEDB.4.0;" & _ 
        //      "Data Source=<path>\<file>.mdb" 

        //dsc = Catalog.Tables("table_name").Columns("column_name").Properties("Description").Value 
        //
        // if err.number <> 0 then 
        //   Response.Write "&lt;" & err.description & "&gt;" 
        //    else 
        //      Response.Write "Description = " & dsc 
        //end if 
        //    Set Catalog = nothing 
        //%>  

        private static Dictionary<string, string> GetColumnDescriptionsBySql(DbConnection conn, string sql, string tableName)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = Db.Command_Get(conn, sql, new Dictionary<string, object>{{"@table_name", tableName}}))
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                {
                    var value = reader["Value"].ToString();
                    if (!string.IsNullOrEmpty(value))
                        dict.Add(((string) reader["Column_Name"]), value);
                }

            return dict;
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
