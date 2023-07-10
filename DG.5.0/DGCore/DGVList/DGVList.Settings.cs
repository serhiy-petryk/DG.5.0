using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DGCore.Misc;

namespace DGCore.DGVList
{
  public partial class DGVList<TItem>
  {
    // 3. Add Settings for DGVList (split ApplySettings for DGVList/DGVCube); remove public sets for some DGVList properties
    public void ResetSettings()
    {
      var whereFilter = (UserSettings.IUserSettingSupport<List<UserSettings.Filter>>)WhereFilter;
      whereFilter?.SetSetting(whereFilter.GetBlankSetting());
      var filterByValue = (UserSettings.IUserSettingSupport<List<UserSettings.Filter>>)FilterByValue;
      filterByValue?.SetSetting(filterByValue.GetBlankSetting());
      ShowTotalRow = false;
      ExpandedGroupLevel = 0;
      ShowGroupsOfUpperLevels = false;
      TextFastFilter = null;

      Groups?.Clear();
      Sorts?.Clear();
      SortsOfGroups?.Clear();
      TotalLines = new TotalLine[0];
    }

    public void SetSettings(UserSettings.DGV settingInfo)
    {
      TextFastFilter = settingInfo.TextFastFilter;

      WhereFilter.ClearFilter();
      if (settingInfo.WhereFilter != null && settingInfo.WhereFilter.Count > 0)
        ((UserSettings.IUserSettingSupport<List<UserSettings.Filter>>)WhereFilter).SetSetting(settingInfo.WhereFilter);

      FilterByValue = null;
      if (settingInfo.FilterByValue != null && settingInfo.FilterByValue.Count > 0)
      {
        FilterByValue = new Filters.FilterList(Properties);
        ((UserSettings.IUserSettingSupport<List<UserSettings.Filter>>)FilterByValue).SetSetting(settingInfo.FilterByValue);
      }

      Groups.Clear();
      SortsOfGroups.Clear();
      for (var i = 0; i < settingInfo.Groups.Count; i++)
      {
        var item = settingInfo.Groups[i];
        var pd = Properties[item.Id];
        if (pd != null)
        {
          Groups.Add(new ListSortDescription(pd, item.SortDirection));
          var sortOfGroup = new List<ListSortDescription>();
          SortsOfGroups.Add(sortOfGroup);
          if (settingInfo.SortsOfGroup.Count > i)
          {
            var savedSortOfGroup = settingInfo.SortsOfGroup[i];
            foreach (var x in savedSortOfGroup)
            {
              var pd1 = Properties[x.Id];
              if (pd1 != null)
                sortOfGroup.Add(new ListSortDescription(pd1, x.SortDirection));
            }
          }
        }
      }

      ExpandedGroupLevel = settingInfo.ExpandedGroupLevel;
      CurrentExpandedGroupLevel = ExpandedGroupLevel > 0 ? ExpandedGroupLevel : int.MaxValue;
      ShowGroupsOfUpperLevels = settingInfo.ShowGroupsOfUpperLevels;
      ShowTotalRow = settingInfo.ShowTotalRow;

      // Restore sorts columns
      Sorts.Clear();
      Sorts.AddRange(settingInfo.Sorts.Where(item => Properties[item.Id] != null)
        .Select(item => new ListSortDescription(Properties[item.Id], item.SortDirection)));

      // Restore totals
      Misc.TotalLine.ApplySettings(TotalLines, settingInfo.TotalLines);
      PrepareLiveTotalLines();
    }

    public void GetSettings() { }
  }
}
