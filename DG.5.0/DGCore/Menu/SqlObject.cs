using System;
using System.Collections.Generic;
using System.Linq;

namespace DGCore.Menu
{
  public class SqlObject
  {
    public string CS { get; set; }
    public RootMenu.DbConnection oCS { get; set; }
    public string Sql { get; set; }

    private Dictionary<string, DbParameter> _parameters = new Dictionary<string, DbParameter>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, DbParameter> Parameters
    {
      get => _parameters;
      set => _parameters = GetDictionaryWithOrdinalIgnoreCaseComparer(value);
    }

    private Dictionary<string, RootMenu.Column> _columns = new Dictionary<string, RootMenu.Column>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, RootMenu.Column> Columns
    {
      get => _columns;
      set => _columns = GetDictionaryWithOrdinalIgnoreCaseComparer(value);
    }

    private static Dictionary<string, TValue> GetDictionaryWithOrdinalIgnoreCaseComparer<TValue>(Dictionary<string, TValue> data)
    {
      if (data.Comparer == StringComparer.OrdinalIgnoreCase) return data;
      return data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
    }

    public string SqlForColumnAttributes { get; set; }
    public string ItemType { get; set; }
    public Type oItemType { get; set; }

    public void Normalize(RootMenu.MainObject mo)
    {
      if (!string.IsNullOrEmpty(CS))
        oCS = mo.DbConnections[CS.Trim()];

      if (!string.IsNullOrEmpty(ItemType))
      {
        oItemType = Utils.Types.TryGetType(ItemType);
        if (oItemType == null)
          throw new Exception($"Can not find item type: {ItemType}");
      }

      foreach (var kvp in Parameters)
        kvp.Value.Normalize(kvp.Key, mo);

      foreach (var kvp in Columns)
        kvp.Value.Normalize(kvp.Key, mo);
    }
  }
}
