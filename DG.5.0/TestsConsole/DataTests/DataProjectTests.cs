namespace TestsConsole.DataTests
{
    public static class DataProjectTests
    {
        public static void SchemaTableTest()
        {
            var connString = "SqlClient;Data Source=localhost;Initial Catalog=dbQ2023;Integrated Security=True;Connection Timeout=300;Encrypt=false";
            using (var conn = Data.Helpers.Db.Connection_Get(connString))
            using (var cmd = Data.Helpers.Db.Command_Get(conn, "SELECT * from SymbolsPolygon", null))
            {
                Data.Helpers.Db.Connection_Open(conn);
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
