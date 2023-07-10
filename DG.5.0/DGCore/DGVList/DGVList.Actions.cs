using System.ComponentModel;

namespace DGCore.DGVList
{
  public partial class DGVList<TItem>
  {
    public void A_SetGroupLevel(int? groupLevel, bool showUpperLevels)
    {
      ExpandedGroupLevel = groupLevel ?? int.MaxValue;
      ShowGroupsOfUpperLevels = showUpperLevels;
      RefreshData();
    }

    public void A_ApplySorting(string dataPropertyName, object currentDataItem, ListSortDirection sortDirection)
    {
      if (string.IsNullOrEmpty(dataPropertyName))
        return;

      A_RemoveSorting(dataPropertyName, currentDataItem, false);

      var groupItem = currentDataItem as DGVList_GroupItem<TItem>;
      if (currentDataItem != null && (groupItem == null || groupItem.Level == 0)) // detail area or total row
      {
        Sorts.Insert(0, new ListSortDescription(Properties[dataPropertyName], sortDirection));
        RefreshDataAfterCommonColumnSortChanged();
      }
      else if (groupItem != null) // group row
      {
        if (Groups[groupItem.Level - 1].PropertyDescriptor.Name == dataPropertyName)
          Groups[groupItem.Level - 1].SortDirection = sortDirection;
        else
          SortsOfGroups[groupItem.Level - 1].Insert(0, new ListSortDescription(Properties[dataPropertyName], sortDirection));
        RefreshDataInternal(RefreshMode.AfterTotalGroupSortChanged);
      }
    }

    public void A_RemoveSorting(string dataPropertyName, object currentDataItem) => A_RemoveSorting(dataPropertyName, currentDataItem, true);

    public void A_RemoveSorting(string dataPropertyName, object currentDataItem, bool flagRefresh)
    {
      if (string.IsNullOrEmpty(dataPropertyName))
        return;

      var groupItem = currentDataItem as DGVList_GroupItem<TItem>;
      if (currentDataItem!=null && (groupItem == null || groupItem.Level == 0)) // detail area or total row
      {
        for (int i = 0; i < Sorts.Count; i++)
          if (Sorts[i].PropertyDescriptor.Name == dataPropertyName)
            Sorts.RemoveAt(i--);

        if (flagRefresh)
          RefreshDataAfterCommonColumnSortChanged();
      }
      else if (groupItem != null) // group row
      {
        for (int i = 0; i < SortsOfGroups[groupItem.Level - 1].Count; i++)
          if (SortsOfGroups[groupItem.Level - 1][i].PropertyDescriptor.Name == dataPropertyName)
            SortsOfGroups[groupItem.Level - 1].RemoveAt(i--);

        if (flagRefresh)
          RefreshDataInternal(RefreshMode.AfterTotalGroupSortChanged);
      }
    }

    public void A_SetByValueFilter(string dataPropertyName, object value)
    {
      if (FilterByValue == null)
        FilterByValue = new Filters.FilterList(Properties);

      if (!string.IsNullOrEmpty(dataPropertyName))
      {
        var filterByValuePredicate = FilterByValue.SetFilterByValue(dataPropertyName, value);
        RefreshDataInternal(RefreshMode.AfterFilterByValueChanged, filterByValuePredicate);
      }
    }

    public void A_ClearByValueFilter()
    {
      if (FilterByValue != null)
      {
        FilterByValue = null;
        RefreshData();
      }
    }

    public void A_FastFilterChanged(string newFastFilterValue)
    {
      if (!string.IsNullOrEmpty(TextFastFilter) && !string.IsNullOrEmpty(newFastFilterValue) && newFastFilterValue.Contains(TextFastFilter))
      {
        TextFastFilter = newFastFilterValue;
        RefreshDataInternal(RefreshMode.AfterFastFilterChanged);
      }
      else
      {
        TextFastFilter = newFastFilterValue;
        RefreshData();
      }
    }

  }
}
