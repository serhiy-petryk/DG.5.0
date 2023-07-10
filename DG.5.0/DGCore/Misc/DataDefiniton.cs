using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DGCore.Misc {
  public class DataDefinition : IDisposable {

    public readonly string _description;
    public readonly DB.DbCmd _dbCmd;
    Type _itemType;
    string _settingID;
    Filters.FilterList _whereFilter;// Lazy get
    Sql.ParameterCollection _dbParameters;
    Dictionary<string, AttributeCollection> _columnAttributes;// format: key-propertyName, value[0]-display name, value[1]-description

    /// <summary>
    /// Опции в меню пользователя
    /// </summary>
    /// <param name="description">Текст опции меню</param>
    /// <param name="connectionString">Код строки подключения к базе данных</param>
    /// <param name="sql">Текст запроса к базе данных</param>
    /// <param name="dbParameters">Список параметров для выполнения запроса к базе данных</param>
    /// <param name="itemType">Тип объектов данных</param>
    /// <param name="settingID">Код используемый для записи настроеек пользователя</param>
    /// <param name="columnAttributes">Атрибуты колонок для типа данных объектов</param>
    public DataDefinition(string description, string connectionString, string sql, Sql.ParameterCollection dbParameters,
      Type itemType, string settingID, Dictionary<string, AttributeCollection> columnAttributes) {

      if (dbParameters == null) {
        _dbCmd = new DB.DbCmd(connectionString, sql);
      }
      else {
        _dbCmd = new DB.DbCmd(connectionString, sql, dbParameters.GetParameterValues(), dbParameters.GetParameterNames());
      }
      AttributeCollection ac = new AttributeCollection();
      this._description = description;
      //      this._connectionString = connectionString; this._sql = sql;
      this._itemType = itemType; this._settingID = settingID; this._dbParameters = dbParameters;
      this._columnAttributes = columnAttributes;
    }

    public string SettingID {
      get { return this._settingID; }
    }

    public Sql.ParameterCollection DbParameters {
      get { return this._dbParameters; }
    }

    public Type ItemType {
      get {
        if (this._itemType == null) {
          this._itemType = DB.DbDynamicType.GetDynamicType(this._dbCmd, null, this._columnAttributes);
        }
        return this._itemType;
      }
    }

    public Filters.FilterList WhereFilter {
      get {// Lazy get
        if (this._dbCmd._cmdKind == DGCore.DB.DbCmd.DbCmdKind.Procedure) return null;
        if (this._whereFilter == null) {
          this._whereFilter = new Filters.FilterList(this._dbCmd, this._itemType, this._columnAttributes);
          /*          using (DbCommand cmd = GetDbCommandWithoutFilter()) {
          //            this._whereFilter = new FilterObject(cmd, this.ItemType);
                      this._whereFilter = new FilterObject(cmd, this._itemType, this._columnAttributes);
                      cmd.Connection.Dispose();
                    }*/
        }
        return this._whereFilter;
      }
    }

    public Sql.DbDataSource GetDataSource(IComponent consumer) {
      /*List<object> paramValues = new List<object>();
      List<string> paramNames = new List<string>();

      if (this._dbParameters!=null) {
        paramValues.AddRange(this._dbParameters.GetParameterValues());
        paramNames.AddRange(this._dbParameters.GetParameterNames());
      }*/

      if (_dbParameters != null)
        _dbCmd.Parameters_UpdateByNewValues(_dbParameters.GetParameterValues(), _dbParameters.GetParameterNames());
      Filters.DbWhereFilter whereFilter = WhereFilter == null ? null : new Filters.DbWhereFilter(WhereFilter);
      Sql.DbDataSource ds = Sql.DbDataSource.GetDataSource(_dbCmd, whereFilter, ItemType, null, consumer);
      return ds;
      //      return Sql.DbDataSource.GetDataSource(this._connectionString, this._sql, wo, this._dbParameters, this.ItemType, null, consumer);


      /*    string sql;
          if (wo == null || String.IsNullOrEmpty(wo._whereExpression)) {// Procedure or sql without parameters
            sql = this._sql;
          }
          else {// Sql with parameters
            sql = "SELECT * from (" + this._sql + ") x WHERE " + wo._whereExpression;
            paramValues.AddRange(wo._parameterValues);
            paramNames.AddRange(wo._parameterNames);
          }
          return Sql.DbDataSource.GetDataSource(this._connectionString, sql, wo, paramValues, paramNames, this.ItemType, null, consumer);*/
    }

    public override string ToString() {
      return this._description;
    }


    //===================   Private section  =================================
    /*    DbCommand GetDbCommandWithoutFilter() {
          List<object> paramValues = new List<object>();
          List<string> paramNames = new List<string>();
          if (this._dbParameters!=null) {
              paramValues.AddRange(this._dbParameters.GetParameterValues());
              paramNames.AddRange(this._dbParameters.GetParameterNames());
          }
          DbConnection conn = DB.DbUtils.Connection_Get(this._connectionString);
          return DB.DbUtils.Command_Get(conn, this._sql, paramValues, paramNames);
        }*/


    public void Dispose() {
      //      if (Disposed != null) Disposed.Invoke(this, new EventArgs());
      if (this._dbCmd != null) _dbCmd.Dispose();
    }
  }
}
