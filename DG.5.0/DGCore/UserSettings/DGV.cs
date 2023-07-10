using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DGCore.Common;
using DGCore.Utils;

namespace DGCore.UserSettings
{
    public class DGV : ISupportSerializationModifications
    {
        public int ExpandedGroupLevel { get; set; }
        public bool ShowGroupsOfUpperLevels { get; set; }
        public List<Filter> WhereFilter { get; set; } = new List<Filter>();
        public List<Filter> FilterByValue { get; set; } = new List<Filter>();
        public List<Column> AllColumns { get; set; } = new List<Column>();
        public List<string> FrozenColumns { get; set; } = new List<string>();
        public List<Sorting> Groups { get; set; } = new List<Sorting>();
        public List<Sorting> Sorts { get; set; } = new List<Sorting>();
        public List<List<Sorting>> SortsOfGroup { get; set; } = new List<List<Sorting>>();
        public List<TotalLine> TotalLines { get; set; } = new List<TotalLine>();
        public bool ShowTotalRow { get; set; } = false;
        public bool IsGridVisible { get; set; } = true;
        public Common.Enums.DGRowViewMode RowViewMode { get; set; } = Enums.DGRowViewMode.OneRow;
        public string BaseFont { get; set; } = null;
        public string TextFastFilter { get; set; } = null;

        public void ModifyBeforeSerialize()
        {
            foreach (var item in this.GetChildObjects().OfType<ISupportSerializationModifications>().Distinct())
                if (item != this)
                    item.ModifyBeforeSerialize();

            if (FrozenColumns != null)
            {
                for (var i = 0; i < FrozenColumns.Count; i++)
                    FrozenColumns[i] = FrozenColumns[i].Replace(Constants.MDelimiter, ".");
            }
        }

        public void ModifyAfterDeserialized()
        {
            foreach (var item in this.GetChildObjects().OfType<ISupportSerializationModifications>().Distinct())
                if (item != this)
                    item.ModifyAfterDeserialized();

            if (FrozenColumns != null)
            {
                for (var i = 0; i < FrozenColumns.Count; i++)
                    FrozenColumns[i] = FrozenColumns[i].Replace(".", Constants.MDelimiter);
            }
        }
    }

    public class Filter : ISupportSerializationModifications
    {
        public string Name { get; set; }
        public bool Not { get; set; }
        public bool? IgnoreCase { get; set; }
        public List<FilterLine> Lines { get; set; } = new List<FilterLine>();
        public void ModifyBeforeSerialize()
        {
            if (!string.IsNullOrEmpty(Name))
                Name = Name.Replace(Constants.MDelimiter, ".");
        }
        public void ModifyAfterDeserialized()
        {
            if (!string.IsNullOrEmpty(Name))
                Name = Name.Replace(".", Constants.MDelimiter);
        }
    }

    public class FilterLine
    {
        public Enums.FilterOperand Operand { get; set; }
        public object Value1 { get; set; }
        public object Value2 { get; set; }
    }

    public class Column : ISupportSerializationModifications
    {
        public string Id { get; set; }
        public bool IsHidden { get; set; }
        public int? Width { get; set; }
        public string Format { get; set; }
        public void ModifyBeforeSerialize()
        {
            if (!string.IsNullOrEmpty(Id))
                Id = Id.Replace(Constants.MDelimiter, ".");
        }
        public void ModifyAfterDeserialized()
        {
            if (!string.IsNullOrEmpty(Id))
                Id = Id.Replace(".", Constants.MDelimiter);
        }
        public override string ToString() => $"Id={Id}, IsHidden={IsHidden}, Width={Width}";
    }

    public class Sorting : ISupportSerializationModifications
    {
        public string Id { get; set; }
        public ListSortDirection SortDirection { get; set; }
        public void ModifyBeforeSerialize()
        {
            if (!string.IsNullOrEmpty(Id))
                Id = Id.Replace(Constants.MDelimiter, ".");
        }
        public void ModifyAfterDeserialized()
        {
            if (!string.IsNullOrEmpty(Id))
                Id = Id.Replace(".", Constants.MDelimiter);
        }
        public override string ToString() => $"Id={Id}, Direction={SortDirection}";
    }

    public class TotalLine : ITotalLine, ISupportSerializationModifications
    {
        public string Id { get; set; }
        public Enums.TotalFunction TotalFunction { get; set; }
        public void ModifyBeforeSerialize()
        {
            if (!string.IsNullOrEmpty(Id))
                Id = Id.Replace(Constants.MDelimiter, ".");
        }
        public void ModifyAfterDeserialized()
        {
            if (!string.IsNullOrEmpty(Id))
                Id = Id.Replace(".", Constants.MDelimiter);
        }
        public override string ToString() => $"Id={Id}, Function={TotalFunction}";
    }
}