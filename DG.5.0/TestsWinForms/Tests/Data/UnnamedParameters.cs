using System.Data.OleDb;

namespace TestsWinForms.Tests.Data
{
    public static class UnnamedParameters
    {
        public static void Mdb()
        {
            var connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Persist Security Info=False;Data Source=E:\\Apps\\archive\\Northwind\\nwind.mdb;";
            var sql = "SELECT * FROM Cities";
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand(sql, conn);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {

                }
            }
        }

    }
}
