using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DGCore.Menu
{
    public class MenuOption : SqlObject
    {
        private static int idCnt = 0;
        public int Id { get; } = idCnt++;
        public int? ParentId { get; set; }
        public string Label { get; set; }
        public string layoutId { get; set; }
        public bool IsSubmenu => string.IsNullOrEmpty(Sql);
        public string GetLayoutId() => (layoutId ?? Label.Replace(" ", "")).ToLower();

        public override string ToString() => Label;

        private Misc.DataDefinition _dataDefinition;

        public Misc.DataDefinition GetDataDefiniton()
        {
            if (IsSubmenu || _dataDefinition != null)
                return _dataDefinition;

            var attributes = GetAttributesFromDbMetaData(CS, SqlForColumnAttributes);
            var objectAttrs =
              (Columns?.ToDictionary(kvp1 => kvp1.Key, kvp2 => kvp2.Value.Attributes ?? new List<Attribute>(),
                StringComparer.OrdinalIgnoreCase)) ??
              new Dictionary<string, List<Attribute>>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in attributes)
            {
                if (objectAttrs.ContainsKey(kvp.Key))
                {
                    var aa1 = objectAttrs[kvp.Key];
                    var aa2 = kvp.Value;
                    // Merge 2 array values into one and remove duplicates 
                    var aa = aa1.Concat(aa2.Where(p1 => !aa1.Any(p2 => p2.GetType() == p1.GetType()))).ToList();
                    objectAttrs[kvp.Key] = aa;
                }
                else
                    objectAttrs.Add(kvp.Key, kvp.Value);
            }
            var aaa = objectAttrs.ToDictionary(kvp => kvp.Key, xx => new AttributeCollection(xx.Value.ToArray()));

            // Sql parameters
            var pp = Parameters == null
              ? null
              : new Sql.ParameterCollection(Parameters.Where(x => x.Value.Parameter != null)
                .Select(x => x.Value.Parameter).ToArray());

            _dataDefinition = new Misc.DataDefinition(Label, oCS.GetConnectionString(), Sql, pp, oItemType, GetLayoutId(), aaa);
            return _dataDefinition;

        }

        private static Dictionary<string, List<Attribute>> GetAttributesFromDbMetaData(string connectionString, string sql)
        {
            var data = new Dictionary<string, List<Attribute>>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(sql))
                return data;

            using (var cmd = new DB.DbCmd(connectionString, sql))
            {
                var tbl = cmd.GetSchemaTable();
                foreach (var kvp in tbl._columns)
                {
                    var attrs = new List<Attribute>();
                    if (!string.IsNullOrEmpty(kvp.Value.DisplayName))
                    {
                        if (kvp.Value.DisplayName.StartsWith("--")) continue;
                        attrs.Add(new DisplayNameAttribute(kvp.Value.DisplayName));
                    }

                    if (!string.IsNullOrEmpty(kvp.Value.Description))
                        attrs.Add(new DescriptionAttribute(kvp.Value.Description));

                    if (!string.IsNullOrEmpty(kvp.Value.DbMasterSql))
                    {
                        using (var masterCmd = new DB.DbCmd(connectionString, kvp.Value.DbMasterSql))
                        {
                            string masterSqlPrimaryKeyName;
                            DB.DbDynamicType.GetDynamicType(masterCmd, null, null, true, out masterSqlPrimaryKeyName);
                            var x1 = new Common.BO_LookupTableAttribute(connectionString, kvp.Value.DbMasterSql, masterSqlPrimaryKeyName);
                            attrs.Add(x1);
                        }
                    }
                    if (attrs.Count > 0)
                        data.Add(kvp.Key, attrs);
                }
            }
            return data;
        }

    }
}
