using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DGCore.DB {

  public static partial class DbUtils {
    public class Reader {
      private static readonly Dictionary<string, Delegate> _delegates = new Dictionary<string, Delegate>();
      private static readonly List<TypeConverter> _typeConvertersCache = new List<TypeConverter>();

      public static TypeObject ConvertToObject<TypeObject>(object value, int converterNumber) {
        // convert string/valuetype to object using MasterDataTypeConverter (is used in DbReader delegate)
        return (TypeObject)_typeConvertersCache[converterNumber].ConvertFrom(null, null, value);
      }

//      internal static Func<DbDataReader, T> GetDelegate_FromDataReaderToObject<T>(DbCommand cmd, IEnumerable<DbColumnMapElement> columnMap) {
      public static Func<DbDataReader, T> GetDelegate_FromDataReaderToObject<T>(DB.DbCmd cmd, IEnumerable<DbColumnMapElement> columnMap) {
        if (columnMap == null) columnMap = DbColumnMapElement.GetDefaultColumnMap(cmd, typeof(T));
        return GetDelegate_ObjectFromDataReader<T>(columnMap);
      }

      static string GetColumnMapKey(IEnumerable<DbColumnMapElement> columnMap, Type itemType) {
        string char1 = ((char)1).ToString();
        string char2 = ((char)2).ToString();
        StringBuilder sb = new StringBuilder(itemType.FullName + char1);
        foreach (DbColumnMapElement e in columnMap) {
          if (e.IsValid) {
            sb.Append(e.DbColumn.SqlName + char1 + e.DbColumn.DataType.FullName + char1 +
              (((PD.IMemberDescriptor)e.MemberDescriptor).DbNullValue == null ? "" : ((PD.IMemberDescriptor)e.MemberDescriptor).DbNullValue.ToString()) + char1 +
              (e.DbColumn.IsNullable ? "1" : "0") + char1 + e.MemberDescriptor.Name + char1 + e.DbColumn.Position.ToString() + char2);
          }
        }
        return sb.ToString();
      }

      static Func<DbDataReader, T> GetDelegate_ObjectFromDataReader<T>(IEnumerable<DbColumnMapElement> columnMap) {
        string key = GetColumnMapKey(columnMap, typeof(T));
        lock (_delegates) {
          if (_delegates.ContainsKey(key)) {
            return (Func<DbDataReader, T>)_delegates[key];
          }
          //          DynamicMethod dm = new DynamicMethod("", typeof(T), new Type[] { typeof(DbDataReader) });
          DynamicMethod dm = new DynamicMethod("DbReader", MethodAttributes.Static | MethodAttributes.Public,
            CallingConventions.Standard, typeof(T), new Type[] { typeof(DbDataReader) }, typeof(T), true);
          ILGenerator il = dm.GetILGenerator();
          LocalBuilder lbNewObject = il.DeclareLocal(typeof(T));

          // Create new object and save it to lbNewObject
          il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(new Type[0]));
          il.Emit(OpCodes.Stloc, lbNewObject);
          il.Emit(OpCodes.Ldloc, lbNewObject);

          int columnCount = 0;
          //        for (int i = 0; i < cols.Count; i++) {
          foreach (DbColumnMapElement e in columnMap) {
            if (!e.IsValid) continue;
            Type dbType = e.DbColumn.DataType;
            Type notNullableObjectType = Utils.Types.GetNotNullableType(e.ItemDataType);
            TypeConverter objectTypeConverter;
            if (!DbReader_isColumnValid(dbType, notNullableObjectType, e.MemberDescriptor.Converter, out objectTypeConverter)) continue;

            object dbNullValue = ((PD.IMemberDescriptor)e.MemberDescriptor).DbNullValue;
            string propName = e.MemberDescriptor.Name;
            // Check column

            Label lblNull = il.DefineLabel();
            Label lblSaveProperty = il.DefineLabel();
            //          TypeClass readerTypeClass = GetTypeClass(dbType);
            //          TypeClass objectTypeClass = GetTypeClass(notNullableObjectType);

            // ============== load parameter (position in DbDataReader)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(e.DbColumn.Position));
            if (e.CanBeNull) {
              MethodInfo mi = typeof(DbDataReader).GetMethod("IsDBNull", new Type[] { typeof(int) });
              Utils.Emit.EmitCall(il, mi); // is null ?
//              il.Emit(OpCodes.Callvirt, mi); // is null ?
              il.Emit(OpCodes.Brtrue, lblNull);// if null go to end
              // Restore column position in datareader
              il.Emit(OpCodes.Ldarg_0);
              il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(e.DbColumn.Position));
            }
            // ===========   Read value from datareader column
            MethodInfo miReadFromReader = null;
            if (dbType == typeof(float)) {
              miReadFromReader = typeof(DbDataReader).GetMethod("GetFloat", new Type[] { typeof(int) });
            }
            else if (dbType == typeof(string) || dbType.IsValueType) {
              miReadFromReader = typeof(DbDataReader).GetMethod("Get" + dbType.Name, new Type[] { typeof(int) });
            }
            bool flagGetValue = false;
            if (miReadFromReader == null) {
              miReadFromReader = typeof(DbDataReader).GetMethod("GetValue", new Type[] { typeof(int) });
              flagGetValue = true;
            }
            Utils.Emit.EmitCall(il, miReadFromReader);
//            il.Emit(OpCodes.Callvirt, miReadFromReader);
            if (flagGetValue && dbType.IsValueType) {// Need to unbox value type if no direct value exists (after (object)GetValue call)
              il.Emit(OpCodes.Unbox_Any, dbType);
            }
            else {
              if (e.ItemDataType == typeof(byte[])) {
                il.Emit(OpCodes.Castclass, e.ItemDataType);
              }
            }
            // ===========  Conversion
            if (notNullableObjectType != dbType) {// Need to convert
              if (objectTypeConverter == null) {
                Utils.Emit.EmitCall(il, Utils.Types.GetConvertMethodInfo(dbType, notNullableObjectType));
//                il.Emit(OpCodes.Call, Utils.Types.GetConvertMethodInfo(dbType, notNullableObjectType));
                /*  StandardVersion : does not work: error code for line: il.Emit(OpCodes.Ldtoken, notNullableObjectType);      
                 * if (dbType.IsValueType) {
                              il.Emit(OpCodes.Box);
                            }
                            // Load DestinationType
                            il.Emit(OpCodes.Ldtoken, notNullableObjectType);
                            MethodInfo miType = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
                //            ilgen.Emit(OpCodes.st.Stelem_Ref);
                            il.Emit(OpCodes.Call, miType);
                            il.Emit(OpCodes.Ldnull);
                            MethodInfo miConvert = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type), typeof(IFormatProvider) });
                            il.Emit(OpCodes.Call, miConvert);
                            if (notNullableObjectType.IsValueType) {
                              il.Emit(OpCodes.Unbox_Any, notNullableObjectType);
                            }
                            else {
                              il.Emit(OpCodes.Castclass, notNullableObjectType);
                            }*/

                /*  MyOwnVersion   if (notNullableObjectType == typeof(string)) {
                              // Load reference of value type
                              LocalBuilder lb1 = il.DeclareLocal(dbType);
                              il.Emit(OpCodes.Stloc, lb1);
                              il.Emit(OpCodes.Ldloca, lb1);
                              if (Utils.Types.IsConvertible(dbType)) { // Call ToString(System.IFormatProvider)
                                il.Emit(OpCodes.Call, miInvariantCulture);
                                if (dbType == typeof(DateTime)) {
                                  il.Emit(OpCodes.Callvirt, miFormatInfo_Datetime);
                                }
                                else {// numbers + bool
                                  il.Emit(OpCodes.Callvirt, miFormatInfo_Number);
                                }
                                MethodInfo miToString = dbType.GetMethod("ToString", new Type[] { typeof(System.IFormatProvider) });
                                il.Emit(OpCodes.Call, miToString);
                              }
                              else {// Call ToString()
                                MethodInfo miToString = dbType.GetMethod("ToString", new Type[0]);
                                il.Emit(OpCodes.Call, miToString);
                              }
                            }
                            // Convert To value type (Parse + FromString(Guid))
                            else if (notNullableObjectType.IsValueType) {
                              if (dbType == typeof(string)) {// From String to value object (parse)
                                if (notNullableObjectType == typeof(DateTime)) {// Datetime
                                  MethodInfo miParse = notNullableObjectType.GetMethod("Parse", new Type[] { typeof(string), typeof(System.IFormatProvider) });
                                  il.Emit(OpCodes.Call, miInvariantCulture);
                                  il.Emit(OpCodes.Callvirt, miFormatInfo_Datetime);
                                  il.Emit(OpCodes.Call, miParse);
                                }
                                else {// Numbers or Boolean or Guid
                                  MethodInfo miParse = notNullableObjectType.GetMethod("Parse", new Type[] { typeof(string), typeof(System.IFormatProvider) });
                                  if (miParse == null) {// Guid
                                    ConstructorInfo ci = notNullableObjectType.GetConstructor(new Type[] { typeof(string) });// constructor for Guid: new obj (string) 
                                    if (ci == null) throw new Exception("Error !!!!!");
                                    il.Emit(OpCodes.Newobj, ci);// New Guid(string)
                                  }
                                  else {// Numbers or Boolean
                                    il.Emit(OpCodes.Call, miInvariantCulture);
                                    il.Emit(OpCodes.Callvirt, miFormatInfo_Number);
                                    il.Emit(OpCodes.Call, miParse);
                                  }
                                }
                              }
                              else {// Between convertible types (numbers & boolean): Convert.ConvertTo(o)
                                MethodInfo miConvertTo = typeof(Convert).GetMethod("To" + notNullableObjectType.Name, new Type[] { dbType });
                                il.Emit(OpCodes.Call, miConvertTo);
                              }
                            }
                            else if (notNullableObjectType == typeof(object)) {
                              if (!dbType.IsValueType) {// Need box for value type
                                il.Emit(OpCodes.Box, dbType);
                              }
                            }
                            else {
                              throw new Exception("hhhhhh");
                            }*/
              }
              else {
                if (objectTypeConverter != null) {
                  // Get index no of type converters in cache
                  int i = _typeConvertersCache.IndexOf(objectTypeConverter);
                  if (i < 0) {
                    _typeConvertersCache.Add(objectTypeConverter);
                    i = _typeConvertersCache.IndexOf(objectTypeConverter);
                  }
                  if (dbType.IsValueType) {// Box database value
                    il.Emit(OpCodes.Box, dbType );
//                    il.Emit(OpCodes.Ldc_I4, i);// The next line will be NOP without this line 
                  }
                  il.Emit(OpCodes.Ldc_I4, i);// load element number in List
                  MethodInfo miConvert = typeof(Reader).GetMethod("ConvertToObject", BindingFlags.Static | BindingFlags.Public);
                  Utils.Emit.EmitCall(il, miConvert.MakeGenericMethod(notNullableObjectType));
//                  il.Emit(OpCodes.Call, miConvert.MakeGenericMethod(notNullableObjectType));
                }
                else {
                  throw new Exception("AAA");
                }
              }//if (objectTypeConverter == null) {
            }//if (notNullableObjectType != dbTyp
            // ====== Transform to nullable if need
            if (Utils.Types.IsNullableType(e.ItemDataType)) {
              if (e.CanBeNull) {
                ConstructorInfo ciNullableGeneric = e.ItemDataType.GetConstructor(new Type[] { notNullableObjectType });
                il.Emit(OpCodes.Newobj, ciNullableGeneric);
                il.Emit(OpCodes.Br_S, lblSaveProperty);
                // Init null result (stack)
                il.MarkLabel(lblNull);
                LocalBuilder lbNullableObject = il.DeclareLocal(e.ItemDataType);
                il.Emit(OpCodes.Ldloca, lbNullableObject);// load the address of new object
                il.Emit(OpCodes.Initobj, e.ItemDataType);//init new object 
                il.Emit(OpCodes.Ldloc, lbNullableObject);// load new object into stack
                // Label Save result
                il.MarkLabel(lblSaveProperty);
              }
              else {// Value can not be null (for primary keys and so on)
                ConstructorInfo ciNullableGeneric = e.ItemDataType.GetConstructor(new Type[] { notNullableObjectType });
                il.Emit(OpCodes.Newobj, ciNullableGeneric);
              }
            }
            else if (e.CanBeNull) {
              il.Emit(OpCodes.Br_S, lblSaveProperty);
              il.MarkLabel(lblNull);
              if (dbNullValue == null) {
                il.Emit(OpCodes.Ldnull);
              }
              else {// dBNullValue is defined
                switch (dbNullValue.GetType().Name) {
                  case "Boolean":
                    if ((bool)dbNullValue == true) il.Emit(OpCodes.Ldc_I4_1);
                    else il.Emit(OpCodes.Ldc_I4_0);
                    break;
                  case "SByte": il.Emit(OpCodes.Ldc_I4_S, (SByte)dbNullValue); break;
                  case "Char":
                  case "Byte":
                  case "Int16":
                  case "UInt16":
                  case "Int32": il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(dbNullValue)); break;
                  case "UInt32": il.Emit(OpCodes.Ldc_I4, unchecked((Int32)(UInt32)dbNullValue)); break;
                  case "Int64": il.Emit(OpCodes.Ldc_I8, (Int64)dbNullValue); break;
                  case "UInt64": il.Emit(OpCodes.Ldc_I8, unchecked((Int64)(UInt64)dbNullValue)); break;
                  case "Double": il.Emit(OpCodes.Ldc_R8, (double)dbNullValue); break;
                  case "Single": il.Emit(OpCodes.Ldc_R4, (Single)dbNullValue); break;
                  case "Decimal":
                    il.Emit(OpCodes.Ldc_R8, Convert.ToDouble(dbNullValue));
                    ConstructorInfo ci1 = typeof(Decimal).GetConstructor(new Type[] { typeof(double) });
                    il.Emit(OpCodes.Newobj, ci1);
                    break;
                  case "DateTime":
                    il.Emit(OpCodes.Ldc_I8, ((DateTime)dbNullValue).Ticks);
                    ConstructorInfo ci2 = typeof(DateTime).GetConstructor(new Type[] { typeof(long) });
                    il.Emit(OpCodes.Newobj, ci2);
                    break;
                  case "TimeSpan":
                    il.Emit(OpCodes.Ldc_I8, ((TimeSpan)dbNullValue).Ticks);
                    ConstructorInfo ci3 = typeof(TimeSpan).GetConstructor(new Type[] { typeof(long) });
                    il.Emit(OpCodes.Newobj, ci3);
                    break;
                  default:
                    throw new Exception("Load constant procedure does not defined for " + dbNullValue.GetType().Name);
                }
              }
              // Save result
              il.MarkLabel(lblSaveProperty);
            }
            else { } // nothing to do
            // Save result
            if (e.IsField) {
              il.Emit(OpCodes.Stfld, (FieldInfo)((PD.IMemberDescriptor)e.MemberDescriptor).ReflectedMemberInfo);
            }
            else {
              Utils.Emit.EmitCall(il, ((PropertyInfo)((PD.IMemberDescriptor)e.MemberDescriptor).ReflectedMemberInfo).GetSetMethod());
//              il.Emit(OpCodes.Callvirt, ((PropertyInfo)((PD.IMemberDescriptor)e.MemberDescriptor).ReflectedMemberInfo).GetSetMethod());
            }

            // load new object
            il.Emit(OpCodes.Ldloc, lbNewObject);
            columnCount++;
          }
          // Return new object
          il.Emit(OpCodes.Ret);

          Func<DbDataReader, T> del1 = (Func<DbDataReader, T>)dm.CreateDelegate(typeof(Func<DbDataReader, T>));
          _delegates.Add(key, del1);
          return del1;
        }
      }

      static bool DbReader_isColumnValid(Type dbReaderValueType, Type objectNotNullableType, TypeConverter propertyConverter, out TypeConverter objectTypeConverter) {
        objectTypeConverter = null;
        TypeClass readerTypeClass = GetTypeClass(dbReaderValueType);
        TypeClass objectTypeClass = GetTypeClass(objectNotNullableType);
        // Numbers
        if (objectTypeClass == TypeClass.NumberOrBoolean) {
          return (readerTypeClass == TypeClass.NumberOrBoolean || readerTypeClass == TypeClass.String);
        }
        // Other structures
        if (objectTypeClass == TypeClass.OtherStructure) {
          if (readerTypeClass == TypeClass.String) {
            if (objectNotNullableType == typeof(DateTime)) return true;// Parse + IFormatProvider(DateTime)
            if (objectNotNullableType.GetMethod("FromString", new Type[0]) != null) return true; // Guid
            return false;
          }
          return (dbReaderValueType == objectNotNullableType);// types must be the same
        }

        //propertyConverter
        if (propertyConverter != null && propertyConverter.CanConvertFrom(dbReaderValueType))
        {
          objectTypeConverter = propertyConverter;
          return true;
        }

        // String
        if (objectTypeClass == TypeClass.String)
        {
          return (readerTypeClass == TypeClass.NumberOrBoolean || readerTypeClass == TypeClass.OtherStructure ||
                  readerTypeClass == TypeClass.String);
        }
        // Object
        if (objectTypeClass == TypeClass.Object) return true; // everything can convert to object
        // Others
        if (objectTypeClass == TypeClass.Others) {
          if (dbReaderValueType == objectNotNullableType) return true; ;// types are the same
          //see upper(propertyConverter): if (propertyConverter is PD.ILookupTableTypeConverter && propertyConverter.CanConvertFrom(dbReaderValueType)) {
                                                                        //objectTypeConverter = propertyConverter;
                                                                        //return true;
                                                                        //}
          TypeConverter tc = TypeDescriptor.GetConverter(objectNotNullableType);
          if (tc != null && tc.CanConvertFrom(dbReaderValueType)) {
            objectTypeConverter = tc;
            return true;
          }
        }
        return false;
      }

      // =================    Utils  =========================
      enum TypeClass { NumberOrBoolean, OtherStructure, String, Object, Others };

      static TypeClass GetTypeClass(Type type) {
        if (type == typeof(string)) return TypeClass.String;
        if (type == typeof(DateTime)) return TypeClass.OtherStructure;
        if (type == typeof(object)) return TypeClass.Object;
        if (type.IsValueType && Utils.Types.IsConvertible(type)) return TypeClass.NumberOrBoolean;
        if (type.IsValueType) return TypeClass.OtherStructure;
        return TypeClass.Others;
      }

    }

  }
}
