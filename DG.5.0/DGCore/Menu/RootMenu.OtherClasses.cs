using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DGCore.Menu
{
  public partial class RootMenu
  {
    public class DbConnection
    {
      public DbProvider Provider { get; set; }
      public string CS { get; set; }
      public string GetConnectionString() => Provider + ";" + CS;
    }

    public class Lookup: SqlObject
    {
      public string[] ValueList { get; set; }// ToDo: replaced by custom datadriver (json, csv, ..)
      public bool IsExclusive { get; set; } = true;
      public string KeyColumnName { get; set; }
    }

    public class Column
    {
      public string Label { get; set; }
      public string Comment { get; set; }
      public string Lookup { get; set; }
      public string Format { get; set; }
      internal List<Attribute> Attributes { get; } = new List<Attribute>();
      internal void Normalize(string columnName, RootMenu.MainObject mo)
      {
        if (!string.IsNullOrEmpty(Label))
          Attributes.Add(new DisplayNameAttribute(Label));

        if (!string.IsNullOrEmpty(Comment))
          Attributes.Add(new DescriptionAttribute(Comment));

        if (!string.IsNullOrEmpty(Lookup))
        {
          var lookup = mo.Lookups[Lookup];
          if (!string.IsNullOrEmpty(lookup.Sql))
            Attributes.Add(new Common.BO_LookupTableAttribute(lookup.CS, lookup.Sql, lookup.KeyColumnName));
          // ToDo: BO_LookupTableAttribute for list values
        }

        if (!string.IsNullOrEmpty(Format))
          Attributes.Add(new DisplayFormatAttribute(){DataFormatString = Format});
      }
    }

  }
}
