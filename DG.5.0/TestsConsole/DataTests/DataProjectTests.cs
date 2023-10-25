namespace TestsConsole.DataTests
{
    public static class DataProjectTests
    {
        public static void SchemaTableTest()
        {
            var connString = "SqlClient;Data Source=localhost;Initial Catalog=dbOneSAP_DW;Integrated Security=True;Connection Timeout=300;Encrypt=false";

            /*using (var conn = Data.DB.DbHelper.Connection_Get(connString))
            // using (var cmd = Data.DB.Helper.Command_Get(conn, "SELECT * from dbQ2023.dbo.SymbolsPolygon where symbol=@symbol", null))
            // using (var cmd = Data.DB.DbHelper.Command_Get(conn, "select b.altacc altacc1, * from dbOneSAP_DW..mast_coa a left join dbOneSAP_DW..mast_coa_alt b on a.altacc = b.altacc;", null))
            using (var cmd = Data.DB.DbHelper.Command_Get(conn, "CUBE_GLDOCLINE_WITH_BALANCE", null))
            {
                var schema = Data.DB.DbHelper.GetSchemaTable(cmd);
                var aa = Data.DB.DbSchemaTable.GetSchemaTable(cmd);

                // var sourceCmd = Data.DB.DbHelper.Command_Get(conn, "select * from v_gldocline", null);
                // aa.ApplySqlForColumnAttributes(sourceCmd);

                Data.DB.DbHelper.Connection_Open(conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {

                    }
                }
            }*/

        }
    }
}
