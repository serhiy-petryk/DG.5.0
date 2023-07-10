using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DGCore.Sql {

  //======================  Static section  ===============================
  public partial class DbDataSource : DataSourceBase {//, IDisposable {

    interface IDbDataSourceExtension {//: IDisposable {
      ICollection GetData(bool requeryFlag);
      int RecordCount { get; }
      bool DataLoadingCancelFlag { set; }
    }

    /* Not need: use LookupTableConverter  public class DbDataSourceExtension<TItemType, TKeyType> : IDbDataSourceExtension {
          Dictionary<TKeyType, TItemType> _data;
          public ICollection GetData(bool requeryFlag) {
            return _data.Values;
          }
        }*/
    // ===============  Extension subclass  ====================
    public class DbDataSourceExtension<TItemType> : IDbDataSourceExtension {

      private DbDataSource _owner;
      private List<TItemType> _data;

      public DbDataSourceExtension(DbDataSource owner)
      {
        _owner = owner;
      }

      public int RecordCount => _data?.Count ?? 0;
      public bool DataLoadingCancelFlag { get; set; }

      public ICollection GetData(bool requeryFlag) {
        if (_data == null || requeryFlag)
          DataLoad();
        return _data;
      }

      void DataLoad() {
        // Clear data
        if (_data == null) {
          if (_owner._pdPrimaryKey == null) {
            _data = new List<TItemType>();
          }
          else {
            throw new Exception("not ready!");
            Type t = typeof(Dictionary<,>).MakeGenericType(this._owner._pdPrimaryKey.PropertyType, typeof(TItemType));
            //            this._data = Activator.CreateInstance(t);
          }
        }
        else
          _data.Clear();

        _owner._partiallyLoaded = false;
        _owner._isDataReady = false;

        // Send event
        _owner.InvokeDataEvent(this._owner, new SqlDataEventArgs(DataEventKind.Clear));
        // Load data (use background if current thread is not in background)
        if (System.Threading.Thread.CurrentThread.IsBackground) {
          // Run in current thread
          DoLoadData();
          _owner._isDataReady = true;
          _owner.InvokeDataEvent(_owner, new SqlDataEventArgs(DataEventKind.Loaded));
        }
        else {
          throw new Exception("Trap! Load data in background thread");
        }
      }

      private void DoLoadData()
      {
        DataLoadingCancelFlag = false;

        // Activate DependentObjectManager & Check LookupTableTypeConverters
        var pdc = PD.MemberDescriptorUtils.GetTypeMembers(typeof(TItemType));
        var tasks = new List<Task>();
        var sqlKeys = new Dictionary<string, object>();
        foreach (var converter in pdc.OfType<PropertyDescriptor>().Where(d => d.Converter is Common.ILookupTableTypeConverter).Select(d => d.Converter))
        {
          var sqlKey = ((Common.ILookupTableTypeConverter) converter).SqlKey;
          if (!sqlKeys.ContainsKey(sqlKey))
          {
            var task = Task.Factory.StartNew(() => ((Common.ILookupTableTypeConverter) converter).LoadData(this._owner));
            tasks.Add(task);
            sqlKeys.Add(sqlKey, null);
          }
        }

        Task.WaitAll(tasks.ToArray());
        tasks.ForEach(t => t.Dispose());

        var func = DB.DbUtils.Reader.GetDelegate_FromDataReaderToObject<TItemType>(_owner._cmd, null);
        _owner._cmdData.Connection_Open();
        _owner.InvokeDataEvent(_owner, new SqlDataEventArgs(DataEventKind.Loading));
        using (var reader = _owner._cmdData._dbCmd.ExecuteReader())
          while (reader.Read())
          {
            try
            {
              if (DataLoadingCancelFlag)
              {
                _owner._partiallyLoaded = true;
                _owner._isDataReady = true;
                _owner._cmdData._dbCmd.Cancel();
                break;
              }

              _data.Add(func(reader));
            }
            catch (Exception exception)
            {
              var values = new object[reader.FieldCount];
              reader.GetValues(values);
              throw;
            }
          }
      }
    }
  }

}
