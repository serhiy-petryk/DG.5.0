using System;
using System.ComponentModel;

namespace DGCore.DB {
  [TypeConverter(typeof(ExpandableObjectConverter))]
  public class DbSchemaColumn {
    public string _dbDisplayName;
    public string _dbDescription;
    public string _dbMasterSql;
//    public string _dbMasterSqlPrimaryKey;
    readonly string _sqlName;
    readonly short _position;
    readonly int _size;
    readonly byte _dp = 0;
    readonly Type _type;
    public readonly bool _isNullable;
    string _baseTableName;
    string _baseColumnName;
    public DbSchemaColumn _baseColumn = null;
    public DbSchemaColumnProperty _customProperty;

    public string SqlName { get { return this._sqlName; } }
//    public string SqlNativeName { get { return this._sqlName; } }
    public int Size { get { return this._size; } }
    public Int16 Position { get { return this._position; } }
    public byte DecimalPlaces { get { return this._dp; } }
    public Type DataType { get { return this._type; } }
    public bool IsNullable { get { return this._isNullable; } }
    public string BaseTableName { get { return this._baseTableName; } }
    public string BaseColumnName { get { return this._baseColumnName; } }

    //===============
    public DbSchemaColumn(string name, Int16 position, int size, byte dp, Type type, bool isNullable, string baseTableName, string baseColumnName) {

      this._sqlName = name; this._position = position; this._size = size; this._dp = dp;
      this._type = type; this._isNullable = isNullable;
      this._baseTableName = baseTableName; this._baseColumnName = baseColumnName;
    }

    //===============
    public override string ToString() {
      return "DbColumn: " + this.SqlName;
    }

    public string DisplayName {
      get {
        if (_customProperty != null && !String.IsNullOrEmpty(_customProperty.DisplayName)) return this._customProperty.DisplayName;
        if (!String.IsNullOrEmpty(this._dbDisplayName)) return this._dbDisplayName;
        if (this._baseColumn != null && !String.IsNullOrEmpty(this._baseColumn._dbDisplayName)) return this._baseColumn._dbDisplayName;
        return null;
//        return this._sqlName;
      }
    }

    public string Description {
      get {
        if (_customProperty != null && !String.IsNullOrEmpty(_customProperty.Desription)) return this._customProperty.Desription;
        if (!String.IsNullOrEmpty(this._dbDescription)) return this._dbDescription;
        if (this._baseColumn != null && !String.IsNullOrEmpty(this._baseColumn._dbDescription)) return this._baseColumn._dbDescription;
        return null;
      }
    }

    public string DisplayFormat {
      get {
        if (_customProperty != null && !String.IsNullOrEmpty(_customProperty.DisplayFormat)) return this._customProperty.DisplayFormat;
        if (this.DataType==typeof(Decimal) && this.DecimalPlaces > 0) return "N"+ this.DecimalPlaces.ToString();
        return null;
      }
    }

    public string DbMasterSql {
      get {
        if (_customProperty != null && !String.IsNullOrEmpty(_customProperty.MasterSql)) return this._customProperty.MasterSql;
        if (!String.IsNullOrEmpty(this._dbMasterSql)) return this._dbMasterSql;
        if (this._baseColumn != null && !String.IsNullOrEmpty(this._baseColumn._dbMasterSql)) return this._baseColumn._dbMasterSql;
        return null;
      }
    }

    public void ClearBaseTable() {
      this._baseColumnName = null;
      this._baseTableName = null;
      this._baseColumn = null;
    }

  }

}
