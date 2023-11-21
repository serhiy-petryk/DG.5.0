using System.Collections.Generic;
using System.Text.Json;
using DGCore.UserSettings;
using Microsoft.Data.SqlClient;

namespace DGView.Usefull
{
    public static class ClearDbSettings
    {
        public static void Run()
        {
            return;
            var ids = new List<(string, string)>();

            var connectionString = @"initial catalog=dbUserSettings;Pooling=false;Data Source=localhost;Integrated Security=SSPI;Connection Timeout=30;Encrypt=false";
            var sql = "SELECT * FROM _UserSettings where data like '%\"width\":%' and kind='DGV_Setting'";
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(sql, conn);
                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    ids.Add(((string)rdr["Key"], (string)rdr["ID"]));
                    //  Debug.Print($"Rec: {cnt++}, City: {rdr["City"]}");
                }
            }

            foreach (var id in ids)
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT data from _UserSettings where kind='DGV_setting' and [key]=@key and id=@id", conn);
                    cmd.Parameters.AddRange(new[]
                    {
                        new SqlParameter("@key", id.Item1),new SqlParameter("@id", id.Item2)
                    });


                    var cmdUpdate = new SqlCommand("UPDATE _UserSettings SET data=@data WHERE [kind]='DGV_Setting' and [key]=@key and [id]=@id", conn);
                    cmdUpdate.Parameters.AddRange(new[]
                    {
                        new SqlParameter("@key", id.Item1),new SqlParameter("@id", id.Item2)
                    });

                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var o1 = JsonSerializer.Deserialize<DGV>(dr.GetString(0), DGCore.Utils.Json.DefaultJsonOptions);

                            foreach (var c in o1.AllColumns)
                                c.Width = null;

                            var data = JsonSerializer.Serialize(o1, DGCore.Utils.Json.DefaultJsonOptions);
                            cmdUpdate.Parameters.Add(new SqlParameter("data", data));
                        }
                    }
                    cmdUpdate.ExecuteNonQuery();
                }
            }
        }

    }
}

