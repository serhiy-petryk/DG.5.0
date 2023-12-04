using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Tests.Data
{
    [SupportedOSPlatform("windows")]
    [TestClass]
    public class MdbMetaData
    {
        private static void ShowDataTable(DataTable table, int length)
        {
            foreach (DataColumn col in table.Columns)
            {
                Debug.Print("{0,-" + length + "}", col.ColumnName);
            }
            Debug.Print(null);

            foreach (DataRow row in table.Rows)
            {
                foreach (DataColumn col in table.Columns)
                {
                    if (col.DataType.Equals(typeof(DateTime)))
                        Debug.Print("{0,-" + length + ":d}", row[col]);
                    else if (col.DataType.Equals(typeof(Decimal)))
                        Debug.Print("{0,-" + length + ":C}", row[col]);
                    else
                        Debug.Print("{0,-" + length + "}", row[col]);
                }
                Debug.Print(null);
            }
        }

        [TestMethod]
        public void GetTableDescription()
        {
            var connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Persist Security Info=False;Data Source=E:\\Apps\\archive\\Northwind\\nwind.mdb;";
            var sql = "SELECT * FROM Cities where city like @p1 and country like @p1";
            using (var conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                // Get the Meta Data for Supported Schema Collections
                DataTable metaDataTable = conn.GetSchema("MetaDataCollections");

                Console.WriteLine("Meta Data for Supported Schema Collections:");
                ShowDataTable(metaDataTable, 25);
                Console.WriteLine();
            }
        }
    }
}
