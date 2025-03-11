using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace DGCore.DB
{
    [SupportedOSPlatform("windows")]
    public class DbColumnMapElement
    {
        //=============  Static section  ==================
        private static readonly Dictionary<string, DbColumnMapElement[]> _defaultMaps =
            new Dictionary<string, DbColumnMapElement[]>(StringComparer.OrdinalIgnoreCase);

        public static DbColumnMapElement[] GetDefaultColumnMap(DbCmd cmd, Type itemType)
        {
            var key = itemType.FullName + ";" + cmd.Command_Key;
            lock (_defaultMaps)
            {
                if (_defaultMaps.ContainsKey(key)) return _defaultMaps[key];

                var tbl = cmd.GetSchemaTable();
                var mi = typeof(DbColumnMapElement).GetMethod("PrepareDefaultColumnMap", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var mi1 = mi.MakeGenericMethod(itemType);
                var map = (DbColumnMapElement[])mi1.Invoke(null, new object[] { tbl.Columns.Values });
                _defaultMaps.Add(key, map);
                return map;
            }
        }

        private static DbColumnMapElement[] PrepareDefaultColumnMap<T>(IEnumerable<DbSchemaColumn> dbColumns)
        {
            var pdc = PD.MemberDescriptorUtils.GetTypeMembers(typeof(T));
            return dbColumns.Where(c => pdc.Find(c.SqlName, true) != null).Select(c => new DbColumnMapElement(c, pdc.Find(c.SqlName, true))).ToArray();
        }

        // ====================================
        public DbColumnMapElement(DbSchemaColumn column, PropertyDescriptor pd)
        {
            DbColumn = column;
            MemberDescriptor = pd;
        }

        public DbSchemaColumn DbColumn { get; }
        public PropertyDescriptor MemberDescriptor { get; }
        public bool IsField => ((PD.IMemberDescriptor)this.MemberDescriptor).MemberKind == PD.MemberKind.Field;
        public Type ItemDataType => MemberDescriptor?.PropertyType;
        public bool CanBeNull => // for datareader
          (this.ItemDataType.IsClass || Utils.Types.IsNullableType(this.ItemDataType)) && this.DbColumn.IsNullable;
    }
}
