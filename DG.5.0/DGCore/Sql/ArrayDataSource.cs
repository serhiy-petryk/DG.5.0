using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DGCore.Sql {

  public class ArrayDataSource : DataSourceBase {

    public static ArrayDataSource GetDataSource(ICollection labels, ICollection values,IComponent consumer) {
      // Key value pairs: ReportingServices.ReportParameter.AvailableValues
      return null;
    }

    public static ArrayDataSource GetDataSource(ICollection data, string primaryKeyMemberName, IComponent consumer) {
      ArrayDataSource[] dss = Misc.DependentObjectManager.GetProducerList<ArrayDataSource>();
      foreach (ArrayDataSource ads in dss) {
        if (ads._data == data && ads._primaryKeyMemberName == primaryKeyMemberName) {
          Misc.DependentObjectManager.Bind(ads, consumer);
          return ads;
        }
      }
      ArrayDataSource dsNew = new ArrayDataSource(data, primaryKeyMemberName);
      Misc.DependentObjectManager.Bind(dsNew, consumer);
      return dsNew;
    }

    //=============================================
    public override int RecordCount => _data?.Count ?? 0;
    public override bool IsPartiallyLoaded => false;
    public override bool IsDataReady => true;

    public override bool DataLoadingCancelFlag { set {} }

    ICollection _data;
    string _primaryKeyMemberName;
    PropertyDescriptor _pdPrimaryKey;

    ArrayDataSource(ICollection data, string primaryKeyMemberName) {
      this._primaryKeyMemberName = primaryKeyMemberName;
      // var a1  = ListBindingHelper.GetListItemType(data);
      base._itemType = Utils.Types.GetItemType(data);
      if (!string.IsNullOrEmpty(primaryKeyMemberName)) {
        _pdPrimaryKey = PD.MemberDescriptorUtils.GetMember(base._itemType, primaryKeyMemberName, false);
        if (_pdPrimaryKey == null) throw new Exception("Can not find '" + primaryKeyMemberName + "' member in type '" + this._itemType.Name + "'");
        Type dictType = typeof(Dictionary<,>).MakeGenericType(_pdPrimaryKey.PropertyType, this._itemType);
        IDictionary dict = (IDictionary)Activator.CreateInstance(dictType);
        foreach (object o in data) dict.Add(_pdPrimaryKey.GetValue(o), o);
        _data = dict;
      }
      else {
        this._data = data;
      }
    }

    public override ICollection GetData(bool requeryFlag) {
      return _data;
    }

    public override void Dispose() {
//      throw new Exception("The method or operation is not implemented.");
    }
  }

}
