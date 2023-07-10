using System;
using System.Text.Json.Serialization;

namespace DGCore.Menu
{
  public class DbParameter
  {
    private static DateTime _baseDate = DateTime.Today.AddDays(-DateTime.Today.Day + 1); // first day of current month
    public string Label { get; set; }
    public string Comment { get; set; }
    [JsonIgnore]
    public Type TType { get; set; }
    public string Type
    {
      get => TType.FullName;
      set => TType = System.Type.GetType(value);
    }

    public object DefValue { get; set; }
    public string Lookup { get; set; }
    internal Sql.Parameter Parameter { get; set; }
    internal void Normalize(string parameterName, RootMenu.MainObject mo)
    {
      RootMenu.Lookup lookup = null;
      object defValue = null;

      if (!string.IsNullOrEmpty(Lookup))
        lookup = mo.Lookups[Lookup.Trim()];

      if (DefValue != null)
      {
        switch (DefValue.ToString().Trim().ToUpper())
        {
          case "FIRSTDAYOFCURRENTMONTH": defValue = _baseDate; break;
          case "FIRSTDAYOFPREVIOUSMONTH": defValue = _baseDate.AddMonths(-1); break;
          case "LASTDAYOFPREVIOUSMONTH": defValue = _baseDate.AddDays(-1); break;
          case "PREVIOUSPERIOD": defValue = _baseDate.AddMonths(-1).ToString("yyyy-MM"); break;
          case "CURRENTPERIOD": defValue = _baseDate.ToString("yyyy-MM"); break;
          default:
            //defValue = GetValueFromString(o.DefValue, o.Type);
            defValue = Utils.Tips.ConvertTo(DefValue, TType, null);
            if (defValue == null)
              throw new Exception($"Помилка файла конфігурації. Не можливо визначити вираз по замовчуванню (defValue) для DbParameter.\nParameter name: {parameterName}\nDefValue: {DefValue}");
            break;
        }
      }

      if (lookup == null)
        Parameter = new Sql.Parameter(parameterName, Label, Comment, TType, defValue);
      else if (lookup.ValueList != null && lookup.ValueList.Length > 0 && string.IsNullOrEmpty(lookup.Sql))
        Parameter = new Sql.Parameter(parameterName, Label, Comment, TType, defValue, lookup.ValueList, lookup.IsExclusive);
      else if (!string.IsNullOrEmpty(lookup.Sql))
        Parameter = new Sql.Parameter(parameterName, Label, Comment, TType, defValue, lookup.CS, lookup.Sql, lookup.IsExclusive);
      else
        throw new Exception($"Помилка файла конфігурації. Не можливо створити обєкт parameter із-за помилки в lookup.\nParameter name: {parameterName}");
    }
  }

}
