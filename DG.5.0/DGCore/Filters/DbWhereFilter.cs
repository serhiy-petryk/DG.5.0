using System;
using System.Collections.Generic;

namespace DGCore.Filters
{

  public class DbWhereFilter
  {

    public readonly string _whereExpression;
    public readonly List<string> _parameterNames = new List<string>();
    public readonly List<object> _parameterValues = new List<object>();
    string _dbProviderNamespace;

    internal DbWhereFilter(FilterList filterList)
    {
      this._dbProviderNamespace = filterList._dbProviderNamespace;
      List<string> lineTokens = new List<string>();
      foreach (FilterLineBase line in filterList)
      {
        if (line.IsNotEmpty)
        {
          List<object> equalValues = new List<object>();
          List<object> notEqualValues = new List<object>();
          List<string> tokens = new List<string>();

          string columnName = DB.DbMetaData.QuotedColumnName(this._dbProviderNamespace, line.UniqueID);
          foreach (FilterLineSubitem item in line.Items)
          {
            switch (item.FilterOperand)
            {
              case Common.Enums.FilterOperand.CanBeNull:
                tokens.Add("(" + columnName + " is Null)");
                break;
              case Common.Enums.FilterOperand.Between:
                tokens.Add("(" + columnName + " >= " + this.GetNextParameterName(item.Value1) + " AND " +
                  columnName + " <= " + this.GetNextParameterName(item.Value2) + ")");
                break;
              case Common.Enums.FilterOperand.NotBetween:
                tokens.Add("(" + columnName + " < " + this.GetNextParameterName(item.Value1) + ") OR (" +
                  columnName + " > " + this.GetNextParameterName(item.Value2) + ")");
                break;
              case Common.Enums.FilterOperand.Contains:
                tokens.Add("(" + columnName + " Like " + this.GetNextParameterName("%" + item.Value1.ToString() + "%") + ")");
                break;
              case Common.Enums.FilterOperand.NotContains:
                tokens.Add("(" + columnName + " Not Like " + this.GetNextParameterName("%" + item.Value1.ToString() + "%") + ")");
                break;
              case Common.Enums.FilterOperand.StartsWith:
                tokens.Add("(" + columnName + " Like " + this.GetNextParameterName(item.Value1.ToString() + "%") + ")");
                break;
              case Common.Enums.FilterOperand.NotStartsWith:
                tokens.Add("(" + columnName + " Not Like " + this.GetNextParameterName(item.Value1.ToString() + "%") + ")");
                break;
              case Common.Enums.FilterOperand.EndsWith:
                tokens.Add("(" + columnName + " Like " + this.GetNextParameterName("%" + item.Value1.ToString()) + ")");
                break;
              case Common.Enums.FilterOperand.NotEndsWith:
                tokens.Add("(" + columnName + " Not Like " + this.GetNextParameterName("%" + item.Value1.ToString()) + ")");
                break;
              case Common.Enums.FilterOperand.Equal:
                equalValues.Add(item.Value1);
                break;
              case Common.Enums.FilterOperand.NotEqual:
                notEqualValues.Add(item.Value1);
                break;
              case Common.Enums.FilterOperand.Greater:
                tokens.Add("(" + columnName + " > " + this.GetNextParameterName(item.Value1) + ")");
                break;
              case Common.Enums.FilterOperand.NotGreater:
                tokens.Add("(" + columnName + " <= " + this.GetNextParameterName(item.Value1) + ")");
                break;
              case Common.Enums.FilterOperand.Less:
                tokens.Add("(" + columnName + " < " + this.GetNextParameterName(item.Value1) + ")");
                break;
              case Common.Enums.FilterOperand.NotLess:
                tokens.Add("(" + columnName + " >= " + this.GetNextParameterName(item.Value1) + ")");
                break;
              case Common.Enums.FilterOperand.None: break;
              default:
                throw new Exception("GetDBWhereExpression does not defined for  " + item.FilterOperand.ToString() + " operand");
            }//switch (item.FilterOperand) {
          }//foreach (FilterLineItem item in line.Items)
          if (equalValues.Count > 0)
          {
            List<string> ss = new List<string>();
            foreach (object o in equalValues)
            {
              ss.Add(this.GetNextParameterName(o));
            }
            tokens.Add("(" + columnName + " IN (" + String.Join(",", ss.ToArray()) + "))");
          }
          if (notEqualValues.Count > 0)
          {
            List<string> ss = new List<string>();
            foreach (object o in notEqualValues)
            {
              ss.Add(this.GetNextParameterName(o));
            }
            tokens.Add("(" + columnName + " NOT IN (" + String.Join(",", ss.ToArray()) + "))");
          }

          if (tokens.Count > 0)
          {
            if (line.Not) lineTokens.Add("( NOT (" + String.Join(" OR ", tokens.ToArray()) + "))");
            else lineTokens.Add("(" + String.Join(" OR ", tokens.ToArray()) + ")");
          }
        }
      }
      if (lineTokens.Count > 0) this._whereExpression = String.Join(" AND ", lineTokens.ToArray());
    }

    string GetNextParameterName(object parameterValue)
    {
      string parameterName = DB.DbMetaData.QuotedParameterName(this._dbProviderNamespace, "w__" + _parameterValues.Count.ToString());
      _parameterValues.Add(parameterValue);
      _parameterNames.Add(parameterName);
      return parameterName;
    }

    public string GetKey()
    {
      if (this._parameterValues == null) return this._whereExpression;
      List<string> ss = new List<string>();
      foreach (object o in this._parameterValues) ss.Add(o == null ? "" : o.ToString());
      return this._whereExpression + "^" + String.Join("#", ss.ToArray());

    }
    public override string ToString()
    {
      return this._whereExpression;
    }
  }

}



