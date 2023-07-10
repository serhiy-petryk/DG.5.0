using System;
using System.Collections;
using System.ComponentModel;

namespace DGCore.Sql {

  //======================  Static section  ===============================
  public partial class DbDataSource : DataSourceBase {
    //======================   Static section  =========================
    public static DbDataSource GetDataSource(DB.DbCmd cmd, Filters.DbWhereFilter whereFilter, Type itemType, string primaryKeyMemberName, IComponent consumer) {

      if (itemType == null)
        throw new Exception("ItemType parameter can not be null in DbDataSource.GetDataSource procedure");

      var dsNew = new DbDataSource(cmd, whereFilter, itemType, primaryKeyMemberName);
      Misc.DependentObjectManager.Bind(dsNew, consumer);
      return dsNew;
    }

    //======================   Object section  =========================
    DB.DbCmd _cmdData;// With filters
    DB.DbCmd _cmd;
    PropertyDescriptor _pdPrimaryKey;
    IDbDataSourceExtension _extension;
    private bool _partiallyLoaded;// User canceled the data loading
    private bool _isDataReady;

    public override int RecordCount => _extension.RecordCount;

    public override bool DataLoadingCancelFlag
    {
      set
      {
        if (_extension != null) // not disposed
          _extension.DataLoadingCancelFlag = value;
      }
    }

    public override bool IsPartiallyLoaded => _partiallyLoaded;
    public override bool IsDataReady => _isDataReady;

    DbDataSource(DB.DbCmd cmd, Filters.DbWhereFilter whereFilter, Type itemType, string primaryKeyMemberName) {
      this._cmd= (DB.DbCmd) cmd.Clone();

      if (whereFilter == null || String.IsNullOrEmpty(whereFilter._whereExpression)) {// Procedure or sql without parameters
        this._cmdData = this._cmd;
      }
      else {// Sql with parameters
        this._cmdData = new DB.DbCmd(this._cmd._connectionString, "SELECT * from (" + this._cmd._sql + ") x WHERE " + whereFilter._whereExpression, 
          this._cmd._paramValues, this._cmd._paramNames);
        this._cmdData.Parameters_Add(whereFilter._parameterValues, whereFilter._parameterNames);
      }

      base._itemType = itemType;
      if (!String.IsNullOrEmpty(primaryKeyMemberName)) {
        this._pdPrimaryKey = PD.MemberDescriptorUtils.GetMember(itemType, primaryKeyMemberName, false);
        if (this._pdPrimaryKey == null) this._pdPrimaryKey = PD.MemberDescriptorUtils.GetMember(itemType, primaryKeyMemberName, true);
        if (this._pdPrimaryKey == null) throw new Exception("DbDataSource. Can not find '" + primaryKeyMemberName + "' property for key member of type " + itemType.Name);
        throw new Exception("Dos not supported yet");
      }
      this._extension = (IDbDataSourceExtension)Activator.CreateInstance(typeof(DbDataSourceExtension<>).MakeGenericType(this._itemType), this);
    }

    public override ICollection GetData(bool requeryFlag) => _extension.GetData(requeryFlag);

    public override void Dispose() {
      DataLoadingCancelFlag = true;
      if (_cmd != null)
      {
        _cmd.Dispose();
        _cmd = null;
      }
      if (_cmdData != null)
      {
        _cmdData.Dispose();
        _cmdData = null;
      }
      if (this._extension != null)
      {
        Utils.Events.RemoveAllEventSubscriptions(this._extension);
        this._extension = null;
      }
      this.Site = null;
    }

    public override string ToString() {
      return "DBDataSource: " + _cmdData?._dbCmd?.CommandText;
    }
  }

}
