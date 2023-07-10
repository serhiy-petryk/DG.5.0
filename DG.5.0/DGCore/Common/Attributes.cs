using System;
using System.ComponentModel;
using System.Reflection;

namespace DGCore.Common {

/*  //BO_AttributeForDynamicType
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
  public class BO_AttributeForDynamicType : Attribute {// for attributes in dynamic type
    public readonly Attribute _baseAttribute;

    public BO_AttributeForDynamicType(Attribute baseAttribute) {
      this._baseAttribute=baseAttribute;
    }
  }*/

  //BO_DisplayNameAttribute
  [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
  public class BO_DisplayNameAttribute : DisplayNameAttribute {// need for fields
    public BO_DisplayNameAttribute(string displayName):base(displayName) {
    }
  }

/*  public class BO_BrowsableAttribute : Attribute {//??? Standard browsable attribute can work with all member types (property/field/method)
    public readonly bool _browsable;
    public BO_BrowsableAttribute(bool browsable) {
      this._browsable = browsable;
    }
  }*/

  //BO_LookupTableAttribute
  [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
  public class BO_LookupTableAttribute : Attribute {

    public readonly string _connection;
    public readonly string _sql;
    public readonly string _keyMember;

//  public BO_LookupTableAttribute(string connection, string sql, string keyMember, string displayMember, string columnMembers) {
// Display member сложно использовать, потому что:
// 1. В функциях ConvertTo/From нужно различать, что имеется ввиду DisplayMemeber or KeyMember   
    // 2. Нужно иметь 2 словаря данных (для DisplayMember и KeyMember)
// ColumnMembers нецелесообразно использовать, для этого можно указать список полей в sql запросе
    public BO_LookupTableAttribute(string connection, string sql, string keyMember) {
      this._connection = connection; this._sql = sql; this._keyMember = keyMember;
    }

    /*public BO_LookupTableAttribute(Type lookupObjectType) {
      Attribute a = TypeDescriptor.GetAttributes(lookupObjectType)[typeof(BO_LookupTableAttribute)];
    }*/
  }

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  public class BO_DbSqlAttribute : Attribute {

    public readonly string _connection;
    public readonly string _sql;

    public BO_DbSqlAttribute(string connection, string sql) {
      this._connection = connection; this._sql = sql;
    }
  }

  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
  public class BO_DbColumnAttribute : Attribute {
    public readonly string _dbColumnName;
    public readonly string _format;
//    public System.Drawing.ContentAlignment? _alignment=null;
    public readonly object _dbNullValue;

    public BO_DbColumnAttribute(string dbColumnName, string format, object dbNullValue) {
      // Usage example:     
      //    [BO_DbColumn(null, null, "System.DateTime;MinValue")]
      //    [BO_DbColumn(null, null, System.Double.NaN)]
      this._dbColumnName = dbColumnName;
      this._format = format;
      if (dbNullValue is string) {
        string s = (string)dbNullValue;
        int i = s.IndexOf(";", StringComparison.Ordinal);
        if (i > 0) {
          Type t = Type.GetType(s.Substring(0, i));
          if (t != null) {
            FieldInfo fi = t.GetField(s.Substring(i + 1), BindingFlags.Static | BindingFlags.Public);
            this._dbNullValue = fi.GetValue(null);
            return;
          }
        }
      }
      this._dbNullValue = dbNullValue;
    }
/*    public BO_DbColumnAttribute(string dbColumnName, string format, System.Drawing.ContentAlignment alignment, object dbNullValue):this(dbColumnName, format, dbNullValue) {
      this._alignment = alignment;
    }*/
  }

}
