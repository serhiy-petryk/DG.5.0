using System;

namespace DGCore.DB {

    public class DbSchemaColumn
    {
        public string SqlName { get; }
        public int Size { get; }
        public Int16 Position { get; }
        public byte DecimalPlaces { get; }
        public Type DataType { get; }
        public bool IsNullable { get; }
        public string BaseTableName { get; }
        public string BaseColumnName { get; }

        public string DisplayName { get; internal set; }
        public string Description { get; internal set; }
        public string DbMasterSql { get; internal set; }
        public string DisplayFormat { get; internal set; }
        //===============
        public DbSchemaColumn(string name, Int16 position, int size, byte decimalPlaces, Type dataType, bool isNullable, string baseTableName, string baseColumnName)
        {
            this.SqlName = name; this.Position = position; this.Size = size; this.DecimalPlaces = decimalPlaces;
            this.DataType = dataType; this.IsNullable = isNullable;
            this.BaseTableName = baseTableName; this.BaseColumnName = baseColumnName;

            if (dataType == typeof(Decimal) && decimalPlaces > 0)
                DisplayFormat = $"N{decimalPlaces}";
        }

        //===============
        public override string ToString() => $"DbColumn: {SqlName}";
    }
}
