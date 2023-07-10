using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DGCore.Utils {
  public static class Tips {

    private static int _uniqueCount = 0;

    public static bool IsDesignMode { get; } = System.Diagnostics.Process.GetCurrentProcess().ProcessName.ToLower() == "devenv" ||
                                 System.Diagnostics.Process.GetCurrentProcess().ProcessName.ToLower() == "vcsexpress";

    public static string GetFullUserName() {
      return Environment.UserDomainName + "\\" + Environment.UserName.ToUpper();
    }

    public static int GetUniqueNumber() {
      return _uniqueCount++;
    }

    public static object GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    /// <summary>
    /// Получить название нового несуществующего файла
    /// </summary>
    /// <param name="path">Путь к файлу</param>
    /// <param name="fileName">Предполагаемое имя файла</param>
    /// <returns>Имя нового файла</returns>
    public static string GetNearestNewFileName(string path, string fileName) {
      string sPath;
      sPath = (path.EndsWith(@"\") ? path : path + @"\");
      if (!File.Exists(sPath + fileName)) return (sPath + fileName);
      int t = fileName.LastIndexOf(".", StringComparison.Ordinal);
      string s1;
      if (t > 0)
        s1 = sPath + fileName.Substring(0, t) + "#{0}." + fileName.Substring(t + 1);
      else {
        s1 = sPath + fileName + "#{0}";
      }
      for (int i = 0; i < 1000; i++) {
        string s2 = String.Format(s1, i.ToString());
        if (!File.Exists(s2)) return s2;
      }
      return "";
    }

    // taken from "C# 4.0 in a Nutshell" (Joseph Albahari and Ben Albahari)
    public static long MemoryUsedInBytes {
      get {
        // clear memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        //
        return GC.GetTotalMemory(true);
      }
    }

    public static IEnumerable<object> GetChildObjects(this object current)
    {
      if (current != null && !current.GetType().IsValueType)
      {
        yield return current;
        var properties = current.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties.Where(p => p.CanWrite && p.GetIndexParameters().Length == 0))
        {
          var value = property.GetValue(current);
          foreach (var item in GetChildObjects(value))
            yield return item;
        }

        if (current is IEnumerable items)
        {
          foreach (var item in items)
          {
            foreach (var a in GetChildObjects(item))
              yield return a;
          }
        }
      }
    }

    public static bool IsValueEquals(object o1, object o2) {
      if (o1 is double && o2 is double) {
        double d1 = (double)o1;
        double d2 = (double)o2;
        if (double.IsNaN(d1)) return double.IsNaN(d2);
        if (double.IsNaN(d2)) return false;
        return Math.Abs(d1 - d2) <= double.Epsilon;
      }
      if (o1 is float && o2 is float) {
        float d1 = (float)o1;
        float d2 = (float)o2;
        if (float.IsNaN(d1)) return float.IsNaN(d2);
        if (float.IsNaN(d2)) return false;
        return Math.Abs(d1 - d2) <= float.Epsilon;
      }
      return Equals(o1, o2);
    }

    public static object ConvertTo(object value, Type destinationType, TypeConverter valueConverter) {
      if (value == null || value==DBNull.Value ) return null;
      Type valueType = value.GetType();
      if (valueType == destinationType) return value;
      if (destinationType == typeof(string)) return value.ToString();
      if (Types.GetNotNullableType(destinationType) == typeof(bool))
      {
          if (Equals(value, "1")) return true;
          if (Equals(value, "0")) return false;
      }
      if (valueConverter != null && valueConverter.CanConvertFrom(valueType)) {
        if (valueConverter is Common.ILookupTableTypeConverter) {
          return ((Common.ILookupTableTypeConverter)valueConverter).GetItemByKeyValue(value);
        }
        else
        {
          return valueConverter.ConvertFrom(value);
        }
      }
      TypeConverter tc = TypeDescriptor.GetConverter(destinationType);
      if (tc != null && tc.CanConvertFrom(valueType)) {
        if (tc is Common.ILookupTableTypeConverter)
          return ((Common.ILookupTableTypeConverter)tc).GetItemByKeyValue(value);
        return tc.ConvertFrom(value);
      }
      if (destinationType.GetInterface("System.IConvertible") != null) {
        if ((Object.Equals(value, double.NaN) || Object.Equals(value, Single.NaN)) && !(destinationType == typeof(double) || destinationType == typeof(Single))) 
          return null;
        return Convert.ChangeType(value, destinationType);
      }
      if (Types.IsNullableType(destinationType)) {
        Type notNullableType = Types.GetNotNullableType(destinationType);
        object newValue = ConvertTo(value, notNullableType, valueConverter);
//        if (newValue == null) return Activator.CreateInstance(destinationType);
        return Activator.CreateInstance(destinationType, new object[] { newValue });
      }
      throw new Exception("Can not convert value of " + value.GetType().Name + " type into " + destinationType.Name +" type");
    }

    public static Func<object, bool> GetContainsTextPredicate(string findString, bool matchCase, bool matchCell, int searchMethod)
    {
      if (searchMethod == -1 && matchCell && matchCase)
        return cellValue => cellValue is string s && string.Equals(findString, (s).Replace((char)160, (char)32), StringComparison.Ordinal);
      if (searchMethod == -1 && matchCell && !matchCase)
        return cellValue => cellValue is string s && string.Equals(findString, s.Replace((char)160, (char)32), StringComparison.OrdinalIgnoreCase);
      if (searchMethod == -1 && !matchCell && matchCase)
        return cellValue => cellValue is string s && s.Replace((char)160, (char)32).IndexOf(findString, StringComparison.Ordinal) >= 0;
      if (searchMethod == -1 && !matchCell && !matchCase)
        return cellValue => cellValue is string s && s.Replace((char)160, (char)32).IndexOf(findString, StringComparison.OrdinalIgnoreCase) >= 0;

      // Regular Expression
      var regexPattern = findString;
      // Wildcards
      if (searchMethod == 1)
      {
        // Convert wildcard to regex:
        regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(findString).Replace("\\*", ".*").Replace("\\?", ".") + "$";
      }
      System.Text.RegularExpressions.RegexOptions strCompare = System.Text.RegularExpressions.RegexOptions.None;
      if (!matchCase)
      {
        strCompare = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
      }
      System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(regexPattern, strCompare);
      return cellValue => cellValue is string s && regex.IsMatch(s.Replace((char)160, (char)32));
    }
  }
}
