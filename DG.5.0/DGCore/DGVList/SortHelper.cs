// сортировку лучше делать используя GroupBy + Order полученного объекта, чем прямой OrderBy
// Особенно она работает быстрее для строк (в 3 раза)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DGCore.DGVList
{
  public partial class DGVList<TItem>
  {// : BindingList<object>, IIDGVList {

    interface ISortHelper
    {
      IEnumerable SortIGroupingItems(IEnumerable nonSortedGroups, ListSortDirection sortOrder);
      IOrderedEnumerable<DGVList_GroupItem<TItem>> SortItems(IEnumerable nonSortedGroups, ListSortDirection sortOrder, bool isFirstLevel);
      object GetGroupingKey(object o);
    }

    class SortHelper<TKeyType> : ISortHelper
    {
      static Func<IGrouping<TKeyType, TItem>, TKeyType> func = item => item.Key;
      static Func<DGVList_GroupItem<TItem>, TKeyType> funcReoder = item => (TKeyType) item._propertyValue;

      public IEnumerable SortIGroupingItems(IEnumerable nonSortedGroups, ListSortDirection sortOrder)
      {
        if (sortOrder == ListSortDirection.Ascending)
          return Enumerable.OrderBy((IEnumerable<IGrouping<TKeyType, TItem>>)nonSortedGroups, func);
        else
        {
          return Enumerable.OrderByDescending((IEnumerable<IGrouping<TKeyType, TItem>>)nonSortedGroups, func);
        }
      }
      public IOrderedEnumerable<DGVList_GroupItem<TItem>> SortItems(IEnumerable nonSortedGroups, ListSortDirection sortOrder, bool isFirstLevel)
      {
        if (isFirstLevel)
        {
          if (sortOrder == ListSortDirection.Ascending)
          {
            return Enumerable.OrderBy((IEnumerable<DGVList_GroupItem<TItem>>)nonSortedGroups, funcReoder);
          }
          else
          {
            return Enumerable.OrderByDescending((IEnumerable<DGVList_GroupItem<TItem>>)nonSortedGroups, funcReoder);
          }
        }
        else
        {
          if (sortOrder == ListSortDirection.Ascending)
          {
            return Enumerable.ThenBy((IOrderedEnumerable<DGVList_GroupItem<TItem>>)nonSortedGroups, funcReoder);
          }
          else
          {
            return Enumerable.ThenByDescending((IOrderedEnumerable<DGVList_GroupItem<TItem>>)nonSortedGroups, funcReoder);
          }
        }
      }

      public object GetGroupingKey(object o)
      {
        return func((IGrouping<TKeyType, TItem>)o);
      }
    }
  }

}
