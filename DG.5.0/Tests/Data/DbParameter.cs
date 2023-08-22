using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.OleDb;
using System.Diagnostics;

namespace Tests.Data
{
    [TestClass]
    public class DbParameter
    {
        [TestMethod]
        public void TestMethod1()
        {
            var connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Persist Security Info=False;Data Source=E:\\Apps\\archive\\Northwind\\nwind.mdb;";
            var sql = "SELECT * FROM Cities";
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand(sql, conn);
                var rdr = cmd.ExecuteReader();
                var cnt = 0;
                while (rdr.Read())
                {
                    Debug.Print($"Rec: {cnt++}, City: {rdr["City"]}");
                }
            }
        }

        [TestMethod]
        public void UnnamedParameter_Mdb()
        {
            var connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Persist Security Info=False;Data Source=E:\\Apps\\archive\\Northwind\\nwind.mdb;";
            var sql = "SELECT * FROM Cities where city like @p1 and country like @p1";
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand(sql, conn);
                var p1 = new OleDbParameter();
                p1.Value = "%a%";
                p1.ParameterName = "@p2";
                var p2 = new OleDbParameter();
                p2.Value = "%u%";
                p2.ParameterName = "@p1";

                cmd.Parameters.Add(p1);
                // cmd.Parameters.Add(p2);
                var rdr = cmd.ExecuteReader();
                var cnt = 0;
                while (rdr.Read())
                {
                    Debug.Print($"Rec: {cnt++}, City: {rdr["City"]}, Country: {rdr["Country"]}");
                }
            }
        }
        [TestMethod]

        public void UnnamedParameter_Accdb()
        {
            var connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Persist Security Info=False;Data Source=E:\\Apps\\archive\\Northwind\\nwind.accdb;";
            var sql = "SELECT * FROM Customers where City like @p1 and country like @p2";
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                var cmd = new OleDbCommand(sql, conn);
                var p1 = new OleDbParameter();
                p1.Value = "%a%";
                p1.ParameterName = "p2";
                var p2 = new OleDbParameter();
                p2.Value = "%u%";
                p2.ParameterName = "p1";

                cmd.Parameters.Add(p1);
                cmd.Parameters.Add(p2);
                var rdr = cmd.ExecuteReader();
                var cnt = 0;
                while (rdr.Read())
                {
                    Debug.Print($"Rec: {cnt++}, City: {rdr["City"]}, Country: {rdr["Country"]}");
                }
            }
        }
    }
}
