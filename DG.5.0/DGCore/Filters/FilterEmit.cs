using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace DGCore.Filters
{
  public class FilterEmit
  {
    private static readonly  Dictionary<string, Delegate> _predicates = new Dictionary<string, Delegate>();
    //==============================
    public static Delegate EmitPredicateFilterItem(Type valueType, Common.Enums.FilterOperand operand, bool ignoreCase)
    {
      string key = valueType.Name + ";" + ((int)operand).ToString() + ";" + (ignoreCase ? "Y" : "N");

      lock (_predicates)
      {
        if (!_predicates.ContainsKey(key))
        {
          // valuetype can not be nullable
          DynamicMethod dm = new DynamicMethod("FilterPredicate", typeof(bool), new Type[] { valueType, valueType, valueType }, valueType, true);
          /*            int paramNumber = Misc.FilterObject.Operand_GetParameterNumber(operand);

                      DynamicMethod dm = null;
                      switch (paramNumber) {
                        case 1:
                          dm = new DynamicMethod("FilterPredicate", typeof(bool), new Type[] { valueType, valueType }, valueType, true);
                          break;
                        case 2:// for Between and NotBetween operand
                          dm = new DynamicMethod("FilterPredicate", typeof(bool), new Type[] { valueType, valueType, valueType }, valueType, true);
                          break;
                        default: throw new Exception("EmitPredicateFilterItem function error. The number parameters of " + operand + " operand must be 1 or 2");
                      }*/

          ILGenerator il = dm.GetILGenerator();

          switch (valueType.Name)
          {
            case "String":
              EmitOperand_String(operand, il, ignoreCase);
              break;

            case "Boolean":
            case "SByte":
            case "Char":
            case "Byte":
            case "Int16":
            case "UInt16":
            case "Int32":
            case "UInt32":
            case "Int64":
            case "UInt64":
            case "Double":
            case "Single":
              EmitOperand_Numbers(operand, il, valueType);
              break;

            default:// decimal, DateTime, TimeSpan, Guid and so on
              EmitOperand_Others(operand, il, valueType);
              break;
          }

          il.Emit(OpCodes.Ret);

          Type delegateType = typeof(Func<,,,>).MakeGenericType(valueType, valueType, valueType, typeof(bool));
          /*            Type delegateType = null;
                      switch (paramNumber) {
                        case 1: delegateType = typeof(Func<,,>).MakeGenericType(valueType, valueType, typeof(bool)); break;
                        case 2: delegateType = typeof(Func<,,,>).MakeGenericType(valueType, valueType, valueType, typeof(bool)); break;
                          break;
                      }*/
          _predicates.Add(key, dm.CreateDelegate(delegateType));
        }
        return _predicates[key];
      }
    }

    //=======EmitOperand_Others
    private static void EmitOperand_Others(Common.Enums.FilterOperand operand, ILGenerator il, Type valueType)
    {

      MethodInfo miGreater = valueType.GetMethod("op_GreaterThan", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] { valueType, valueType }, null);
      MethodInfo miLess = valueType.GetMethod("op_LessThan", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] { valueType, valueType }, null);
      //miCompareTo for objects
      MethodInfo miCompareTo = valueType.GetMethod("CompareTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] { typeof(object) }, null);

      switch (operand)
      {
        case Common.Enums.FilterOperand.Equal:
        case Common.Enums.FilterOperand.NotEqual:
          MethodInfo mi = valueType.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] { valueType }, null);
          //            MethodInfo mi = valueType.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { valueType }, null);
          if (mi != null)
          {
            //IEquatable<T>
            if (valueType.IsValueType) il.Emit(OpCodes.Ldarga_S, 0);
            else il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            Utils.Emit.EmitCall(il, mi);
          }
          else
          {
            mi = valueType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] { valueType, valueType }, null);
            if (mi != null)
            {
              // == support
              il.Emit(OpCodes.Ldarg_0);
              il.Emit(OpCodes.Ldarg_1);
              Utils.Emit.EmitCall(il, mi);
            }
            else
            {
              mi = valueType.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] { typeof(object) }, null);
              if (mi != null)
              {
                if (valueType.IsValueType) il.Emit(OpCodes.Ldarga_S, 0);
                else il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                Utils.Emit.EmitCall(il, mi);
              }
              else
              {
                throw new Exception("Equality function does not defined for " + valueType.Name);
              }
            }
          }
          break;

        case Common.Enums.FilterOperand.Greater:
        case Common.Enums.FilterOperand.NotGreater:
          if (miGreater != null)
          {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            Utils.Emit.EmitCall(il, miGreater);
          }
          else if (miCompareTo != null)
          {// for objects
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            Utils.Emit.EmitCall(il, miCompareTo);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Cgt);
          }
          else throw new Exception("GreaterThan function does not defined for " + valueType.Name);
          break;

        case Common.Enums.FilterOperand.Less:
        case Common.Enums.FilterOperand.NotLess:
          if (miLess != null)
          {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            Utils.Emit.EmitCall(il, miLess);
          }
          else if (miCompareTo != null)
          {// for objects
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            Utils.Emit.EmitCall(il, miCompareTo);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Clt);
          }
          else throw new Exception("LessThan function does not defined for " + valueType.Name);
          break;

        case Common.Enums.FilterOperand.Between:
        case Common.Enums.FilterOperand.NotBetween:
          if (miGreater != null && miLess != null)
          {

            Label lblFirst = il.DefineLabel();
            Label lblSecond = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            Utils.Emit.EmitCall(il, miLess);
            il.Emit(OpCodes.Brtrue_S, lblFirst);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            Utils.Emit.EmitCall(il, miGreater);
            il.Emit(OpCodes.Br, lblSecond);

            il.MarkLabel(lblFirst);
            il.Emit(OpCodes.Ldc_I4_1);
            il.MarkLabel(lblSecond);

          }
          else if (miCompareTo != null)
          {// for objects
            Label lblFirst = il.DefineLabel();
            Label lblSecond = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            Utils.Emit.EmitCall(il, miCompareTo);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Blt, lblFirst);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            Utils.Emit.EmitCall(il, miCompareTo);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Cgt);
            il.Emit(OpCodes.Br, lblSecond);

            il.MarkLabel(lblFirst);
            il.Emit(OpCodes.Ldc_I4_1);
            il.MarkLabel(lblSecond);
          }
          else throw new Exception("LessThan/GreaterThan functions do not defined for " + valueType.Name);
          break;

        default:
          throw new Exception(operand.ToString() + " function emit does not defined for " + valueType.Name);
      }

      // Emit "NOT"
      if (IsNotOperand(operand))
      {
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
      }
    }

    //=======EmitOperand_Numbers
    private static void EmitOperand_Numbers(Common.Enums.FilterOperand operand, ILGenerator il, Type valueType)
    {
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldarg_1);
      switch (operand)
      {
        case Common.Enums.FilterOperand.Equal:
        case Common.Enums.FilterOperand.NotEqual:
          il.Emit(OpCodes.Ceq);
          break;

        case Common.Enums.FilterOperand.Greater:
        case Common.Enums.FilterOperand.NotGreater:
          EmitGreater(il, valueType);
          break;

        case Common.Enums.FilterOperand.Less:
        case Common.Enums.FilterOperand.NotLess:
          EmitLess(il, valueType);
          break;

        case Common.Enums.FilterOperand.Between:
        case Common.Enums.FilterOperand.NotBetween:
          Label lblFirst = il.DefineLabel();
          Label lblSecond = il.DefineLabel();

          //Compare with first value
          EmitGoToShort_OnLess(il, lblFirst, valueType);

          //Compare with second value (load values into stack + compare  + go to end )
          il.Emit(OpCodes.Ldarg_0);
          il.Emit(OpCodes.Ldarg_2);
          EmitGreater(il, valueType);
          il.Emit(OpCodes.Br_S, lblSecond);

          il.MarkLabel(lblFirst);
          il.Emit(OpCodes.Ldc_I4_1);
          il.MarkLabel(lblSecond);
          break;

        default:
          throw new Exception(operand.ToString() + " function emit does not defined for numbers");
      }//switch

      // Emit "NOT"
      if (IsNotOperand(operand))
      {
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
      }
    }

    //=======EmitOperand_String
    private static void EmitOperand_String(Common.Enums.FilterOperand operand, ILGenerator il, bool ignoreCase)
    {
      int iIgnoreCase = Convert.ToInt32(ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
      //          MethodInfo miStringEqual = typeof(string).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding,
      //          null, new Type[] { valueType, valueType, typeof(StringComparison) }, null);
      MethodInfo miStringCompare = typeof(string).GetMethod("Compare", BindingFlags.Static | BindingFlags.Public | BindingFlags.ExactBinding,
        null, new Type[] { typeof(string), typeof(string), typeof(StringComparison) }, null);
      // Load first&second argument and IgnoreCase constant into stack 
      il.Emit(OpCodes.Ldarg_0);
      il.Emit(OpCodes.Ldarg_1);
      il.Emit(OpCodes.Ldc_I4, iIgnoreCase);

      switch (operand)
      {
        case Common.Enums.FilterOperand.Equal:
        case Common.Enums.FilterOperand.NotEqual:
          MethodInfo miStringEqual = typeof(string).GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding,
            null, new Type[] { typeof(string), typeof(StringComparison) }, null);
          Utils.Emit.EmitCall(il, miStringEqual);
          break;

        case Common.Enums.FilterOperand.Greater:
        case Common.Enums.FilterOperand.NotGreater:
          Utils.Emit.EmitCall(il, miStringCompare);
          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Cgt);
          break;

        case Common.Enums.FilterOperand.Less:
        case Common.Enums.FilterOperand.NotLess:
          Utils.Emit.EmitCall(il, miStringCompare);
          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Clt);
          break;

        case Common.Enums.FilterOperand.StartsWith:
        case Common.Enums.FilterOperand.NotStartsWith:
          MethodInfo miStartsWith = typeof(string).GetMethod("StartsWith", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding,
            null, new Type[] { typeof(string), typeof(StringComparison) }, null);
          Utils.Emit.EmitCall(il, miStartsWith);
          break;

        case Common.Enums.FilterOperand.EndsWith:
        case Common.Enums.FilterOperand.NotEndsWith:
          MethodInfo miEndsWith = typeof(string).GetMethod("EndsWith", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding,
            null, new Type[] { typeof(string), typeof(StringComparison) }, null);
          Utils.Emit.EmitCall(il, miEndsWith);
          break;

        case Common.Enums.FilterOperand.Contains:
        case Common.Enums.FilterOperand.NotContains:
          MethodInfo miIndexOf = typeof(string).GetMethod("IndexOf", BindingFlags.Instance | BindingFlags.Public | BindingFlags.ExactBinding,
            null, new Type[] { typeof(string), typeof(StringComparison) }, null);
          Utils.Emit.EmitCall(il, miIndexOf); // IndexOf into stack (x1)
          il.Emit(OpCodes.Ldc_I4_M1);// Load -1 into stack (x2)
          il.Emit(OpCodes.Cgt);// x1>x2
          break;

        case Common.Enums.FilterOperand.Between:
        case Common.Enums.FilterOperand.NotBetween:
          Label lblFirst = il.DefineLabel();
          Label lblSecond = il.DefineLabel();

          //Compare with first value
          Utils.Emit.EmitCall(il, miStringCompare);
          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Blt_S, lblFirst);

          //Compare with second value (load value into stack + compare call + go to end )
          il.Emit(OpCodes.Ldarg_0);
          il.Emit(OpCodes.Ldarg_2);
          il.Emit(OpCodes.Ldc_I4, iIgnoreCase);
          Utils.Emit.EmitCall(il, miStringCompare);
          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Cgt);
          il.Emit(OpCodes.Br_S, lblSecond);

          il.MarkLabel(lblFirst);
          il.Emit(OpCodes.Ldc_I4_1);
          il.MarkLabel(lblSecond);

          break;

        default:
          throw new Exception(operand.ToString() + " function emit does not defined for strings");

      }
      // Emit "NOT"
      if (IsNotOperand(operand))
      {
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ceq);
      }
    }

    //==================================
    private static bool IsNotOperand(Common.Enums.FilterOperand operand)
    {
      return
        operand == Common.Enums.FilterOperand.Between ||// NotBetween is base operand and Between is opposite to NotBetween
        operand == Common.Enums.FilterOperand.NotEqual ||
        operand == Common.Enums.FilterOperand.NotGreater ||
        operand == Common.Enums.FilterOperand.NotLess ||
        operand == Common.Enums.FilterOperand.NotEndsWith ||
        operand == Common.Enums.FilterOperand.NotStartsWith ||
        operand == Common.Enums.FilterOperand.NotContains;
    }

    private static void EmitGreater(ILGenerator il, Type valueType)
    {
      if (valueType == typeof(ulong) || valueType == typeof(uint))
        il.Emit(OpCodes.Cgt_Un);
      else
        il.Emit(OpCodes.Cgt);
    }
    private static void EmitLess(ILGenerator il, Type valueType)
    {
      if (valueType == typeof(ulong) || valueType == typeof(uint))
        il.Emit(OpCodes.Clt_Un);
      else
        il.Emit(OpCodes.Clt);
    }
    private static void EmitGoToShort_OnLess(ILGenerator il, Label label, Type valueType)
    {
      if (valueType == typeof(ulong) || valueType == typeof(uint))
        il.Emit(OpCodes.Blt_Un_S, label);
      else
        il.Emit(OpCodes.Blt_S, label);
    }

  }
}


