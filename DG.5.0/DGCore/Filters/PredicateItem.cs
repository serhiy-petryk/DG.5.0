using System;
using System.Collections.Generic;

namespace DGCore.Filters {
    public class PredicateItem<TValue> {

      //GetWhereDelegate_Nullable
      private static Func<TItem, bool> GetWhereDelegate_Nullable<TItem, TValue>
        (Func<TItem, Nullable<TValue>> getter, List<PredicateItem<TValue>> predicateItems, bool canBeNull, bool notFlag)
    where TValue : struct {

        PredicateItem<TValue>[] pItems = predicateItems.ToArray();// faster on 50% than List<T>
        return delegate(TItem item) {
          Nullable<TValue> x = getter(item);
//          if (!x.HasValue) return canBeNull ^ (!notFlag);
          if (!x.HasValue) return canBeNull ^ notFlag;
          TValue x1 = x.Value;
          foreach (PredicateItem<TValue> pitem in pItems) {
            // 50% slower: if (x1.GetResult(x)) return true;
            if (pitem._predicate(x1, pitem._value1, pitem._value2)) return !notFlag;
          }
          return notFlag;
        };
      }

      //GetWhereDelegate_ValueType
      private static Func<TItem, bool> GetWhereDelegate_ValueType<TItem, TValue>
        (Func<TItem, TValue> getter, List<PredicateItem<TValue>> predicateItems, bool canBeNull, bool notFlag, object dbNullValue)
    where TValue : struct {

        PredicateItem<TValue>[] pItems = predicateItems.ToArray();// faster on 50% than List<T>
        if (dbNullValue == null) {
          return delegate(TItem item) {
            TValue x = getter(item);
            foreach (PredicateItem<TValue> pitem in pItems) {
              // 50% slower: if (x1.GetResult(x)) return true;
              if (pitem._predicate(x, pitem._value1, pitem._value2)) return !notFlag;
            }
            return notFlag;
          };
        }
        else {
          TValue dbNullValue2 = (TValue)dbNullValue;
          return delegate(TItem item) {
            TValue x = getter(item);
            if (x.Equals(dbNullValue2)) return canBeNull ^ (!notFlag);
            foreach (PredicateItem<TValue> pitem in pItems) {
              // 50% slower: if (x1.GetResult(x)) return true;
              if (pitem._predicate(x, pitem._value1, pitem._value2)) return !notFlag;
            }
            return notFlag;
          };
        }
      }

      //GetWhereDelegate_Class
      private static Func<TItem, bool> GetWhereDelegate_Class<TItem>(Func<TItem, TValue> getter, List<PredicateItem<TValue>> predicateItems, bool canBeNull, bool notFlag) {
        PredicateItem<TValue>[] pItems = predicateItems.ToArray();// faster on 50% than List<T>

        return delegate(TItem item) {
          TValue x = getter(item);
          if (x == null) return canBeNull ^ notFlag;
//          if (x == null) return canBeNull ^ (!notFlag);
          foreach (PredicateItem<TValue> x1 in pItems) {
            // 50% slower: if (x1.GetResult(x)) return true;
            if (x1._predicate(x, x1._value1, x1._value2)) return !notFlag;
          }
          return notFlag;
        };
      }

      // =======  Object  ==========
      private readonly Func<TValue, TValue, TValue, bool> _predicate;
      private readonly TValue _value1;
      private readonly TValue _value2;

      public PredicateItem(Common.Enums.FilterOperand operand, bool stringIgnoreCase, TValue value1, object value2) {
        _predicate = (Func<TValue, TValue, TValue, bool>)FilterEmit.EmitPredicateFilterItem(typeof(TValue), operand, stringIgnoreCase);
        _value1 = value1;
        _value2 = (value2 == null ? value1 : (TValue)value2);
      }
    }

}
