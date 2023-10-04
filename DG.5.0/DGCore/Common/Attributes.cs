using System;

namespace DGCore.Common
{
    #region ===========  BO_LookupTableAttribute  ===============
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    public class BO_LookupTableAttribute : Attribute
    {
        public readonly string Connection;
        public readonly string Sql;
        public readonly string KeyMember;

        //  public BO_LookupTableAttribute(string connection, string sql, string keyMember, string displayMember, string columnMembers) {
        // Display member сложно использовать, потому что:
        // 1. В функциях ConvertTo/From нужно различать, что имеется ввиду DisplayMemeber or KeyMember   
        // 2. Нужно иметь 2 словаря данных (для DisplayMember и KeyMember)
        // ColumnMembers нецелесообразно использовать, для этого можно указать список полей в sql запросе
        public BO_LookupTableAttribute(string connection, string sql, string keyMember)
        {
            this.Connection = connection; this.Sql = sql; this.KeyMember = keyMember;
        }
    }
    #endregion

    #region ===========  BO_DisplayFormatAttribute  ===============
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class BO_DisplayFormatAttribute : Attribute
    {
        public readonly string DisplayFormat;
        public BO_DisplayFormatAttribute(string format) => DisplayFormat = format;
    }
    #endregion
}
