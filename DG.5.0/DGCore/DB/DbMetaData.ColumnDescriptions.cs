using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace DGCore.DB
{

  public partial class DbMetaData
  {
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

    private static Dictionary<string, string> SqlClient_GetColumnDescription(DbConnection conn, string tableName)
    {
      SqlConnection conn1 = (SqlConnection)conn;
      //INFORMATION_SCHEMA list: http://technet.microsoft.com/en-us/library/ms186778(v=sql.90).aspx
      string sql;
      if (conn.ServerVersion.StartsWith("08."))
      {// Sql server 2000
        sql = "SELECT i_s.TABLE_NAME, i_s.COLUMN_NAME, s.value FROM INFORMATION_SCHEMA.COLUMNS i_s " +
              "INNER JOIN sysproperties s ON s.id = OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME) AND s.smallid = i_s.ORDINAL_POSITION AND s.name = 'MS_Description' " +
              "WHERE (i_s.TABLE_NAME=@table_name or (i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME)=@table_name) and OBJECTPROPERTY(OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME), 'IsMsShipped')=0";
      }
      else
      {// Sql server2005(version='09'), sql server 2014 (version='12')
        /*sql = "SELECT i_s.TABLE_NAME, i_s.COLUMN_NAME, s.value FROM INFORMATION_SCHEMA.COLUMNS i_s " +
          "INNER JOIN sys.extended_properties s ON s.major_id = OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME) AND s.minor_id = i_s.ORDINAL_POSITION AND s.name = 'MS_Description' " +
          "WHERE (i_s.TABLE_NAME=@table_name or (i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME)=@table_name) and OBJECTPROPERTY(OBJECT_ID(i_s.TABLE_SCHEMA+'.'+i_s.TABLE_NAME), 'IsMsShipped')=0";*/
        sql = "select st.name [Table_name], sc.name [Column_name], sep.value [Value] from sys.tables st " +
              "inner join sys.columns sc on st.object_id = sc.object_id " +
              "inner join sys.schemas ss on st.schema_id=ss.schema_id " +
              "left join sys.extended_properties sep on st.object_id = sep.major_id " +
              "and sc.column_id = sep.minor_id and sep.name = 'MS_Description' " +
              "where st.name = @table_name or ss.name+'.'+st.name = @table_name";
      }
      using (DataTable dt = new DataTable())
      {
        DbUtils.Fill(conn.GetType().Namespace + ";" + conn.ConnectionString, sql, new[] { tableName }, new[] { "@table_name" }, dt);
        Dictionary<string, string> dict = new Dictionary<string, string>();
        foreach (DataRow dr in dt.Rows)
          dict.Add(dr.ItemArray[1].ToString().ToUpper(), dr.ItemArray[2].ToString());
        return dict;
      }
    }

    private static Dictionary<string, string> MySqlClient_GetColumnDescription(DbConnection conn, string tableName)
    {
      //SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'exam' and ifnull(column_comment,'')<>''
      var sql =
        "SELECT table_name, column_name, column_comment as value FROM INFORMATION_SCHEMA.COLUMNS a " +
        "WHERE (TABLE_NAME = @table_name or concat_ws('.', a.TABLE_SCHEMA, a.TABLE_NAME) = @table_name) and ifnull(column_comment, '') <> ''";
      using (DataTable dt = new DataTable())
      {
        DbUtils.Fill(conn.GetType().Namespace + ";" + conn.ConnectionString, sql, new[] { tableName }, new[] { "@table_name" }, dt);
        Dictionary<string, string> dict = new Dictionary<string, string>();
        foreach (DataRow dr in dt.Rows)
          dict.Add(dr.ItemArray[1].ToString().ToUpper(), dr.ItemArray[2].ToString());
        return dict;
      }
    }
  }
}
