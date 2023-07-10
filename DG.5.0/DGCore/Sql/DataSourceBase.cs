using System;
using System.Collections;
using System.ComponentModel;

namespace DGCore.Sql {
  
  public abstract class DataSourceBase: IComponent {//: Component {

    public enum DataEventKind { Clear = 0, Loading = 1, Loaded = 2, BeforeRefresh = 3, Refreshed = 4 }

    public class SqlDataEventArgs : EventArgs
    {
      public DataEventKind EventKind;
      public SqlDataEventArgs(DataEventKind eventKind)
      {
        EventKind = eventKind;
      }
    }

    //=================   Event section  ==================
    public delegate void dlgDataStatusChangedDelegate(object sender, SqlDataEventArgs e);

    //================    Object  ================
    protected Type _itemType;
    PropertyDescriptorCollection _pdc;

    public event dlgDataStatusChangedDelegate DataStatusChangedEvent;

//    protected abstract object GetKey();

    public Type ItemType => _itemType;
    public PropertyDescriptorCollection Properties => _pdc ?? (_pdc = PD.MemberDescriptorUtils.GetTypeMembers(ItemType));
    public abstract int RecordCount { get; }
    public abstract bool DataLoadingCancelFlag { set; }
    public abstract bool IsPartiallyLoaded { get; }
    public abstract bool IsDataReady { get; }
    public abstract ICollection GetData(bool requeryFlag);// ICollection: because we took the number of records in frmDGV

    protected void InvokeDataEvent(object sender, SqlDataEventArgs e)
    {
      DataStatusChangedEvent?.Invoke(sender, e);
    }

    public event EventHandler Disposed;
    public abstract void Dispose();
    public ISite Site { get; set; }
  }
}
