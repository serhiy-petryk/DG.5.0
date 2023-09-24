namespace TestsConsole.DataTests
{
    public static class DataProjectTests
    {
        public static void SchemaTableTest()
        {
            var connString = "SqlClient;Data Source=localhost;Initial Catalog=dbQ2023;Integrated Security=True;Connection Timeout=300;Encrypt=false";
            using (var conn = Data.DB.Helper.Connection_Get(connString))
            using (var cmd = Data.DB.Helper.Command_Get(conn, "SELECT * from dbQ2023.dbo.SymbolsPolygon where symbol=@symbol", null))
            {
                var schema = Data.DB.Helper.GetSchemaTable(cmd);
                Data.DB.Helper.Connection_Open(conn);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {

                    }
                }
            }

        }
    }
}
