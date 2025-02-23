using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DGCore.DGVList
{
  public interface IDGVList : ITypedList, IDisposable
  {
      Dictionary<string, string> Formats { get; }
    PropertyDescriptorCollection Properties { get; }
    Sql.DataSourceBase UnderlyingData { get; }
    List<ListSortDescription> Sorts { get; }
    List<ListSortDescription> Groups { get; }
    List<List<ListSortDescription>> SortsOfGroups { get; }
    Misc.TotalLine[] TotalLines { get; }
    List<Misc.TotalLine> LiveTotalLines { get; }
    int ExpandedGroupLevel { get; set; }
    bool ShowGroupsOfUpperLevels { get; set; }
    bool ShowTotalRow { get; }
    bool IsGroupMode { get; }
    int CurrentExpandedGroupLevel { get; }
    int LastRefreshedTimeInMsecs { get; }
    int FilteredRowCount { get; }
    bool IsBindingsReseting { get; }

    Filters.FilterList WhereFilter { get; }
    Filters.FilterList FilterByValue { get; }
    string TextFastFilter { get; }

    string[] GetSubheaders_ExcelAndPrint(string startUpParameters, string lastAppliedLayoutName);

    bool IsPropertyVisible(string propertyName);
    bool IsGroupColumnVisible(int groupIndex);
    // ======== Settings ============
    void ResetSettings();
    void SetSettings(UserSettings.DGV settingInfo);
    // =======  Actions ===========
    void A_SetGroupLevel(int? groupLevel, bool showUpperLevels);
    void A_ApplySorting(string dataPropertyName, object currentDataItem, ListSortDirection sortDirection);
    void A_RemoveSorting(string dataPropertyName, object currentDataItem);
    void A_SetByValueFilter(string dataPropertyName, object value);
    void A_ClearByValueFilter();
    void A_FastFilterChanged(string newFastFilterValue);
    // ============================
    void RequeryData();
    void RefreshData();
    void ClearData();
    void ItemExpandedChanged(int rowIndex);

    void ResetBindings();
    bool NoDataFilter { get; set; }

    event Sql.DataSourceBase.dlgDataStatusChangedDelegate DataStateChanged;
  }

}
