using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DGCore.Utils {
  public static class Emit {

    public static void EmitLoadField(ILGenerator il, FieldInfo fi) {// Not checked for static fields
      if (fi.FieldType.IsValueType) il.Emit(OpCodes.Ldflda, fi);
      else il.Emit(OpCodes.Ldfld, fi);
    }

    public static void EmitCall(ILGenerator il, MethodInfo method) {
      /// !!! It works ok (Sergei)
      if (method.CallingConvention == CallingConventions.VarArgs) {
        throw new Exception("Error.UnexpectedVarArgsCall(method.FormatSignature())");
      }

      OpCode callOp = UseVirtual(method) ? OpCodes.Callvirt : OpCodes.Call;
      if (callOp == OpCodes.Callvirt && method.ReflectedType.IsValueType) {
        il.Emit(OpCodes.Constrained, method.ReflectedType);
      }
      il.Emit(callOp, method);
    }

    private static bool UseVirtual(MethodInfo mi) {
      if (mi.IsStatic || mi.DeclaringType.IsValueType) return false;
      return true;
    }

    internal static void EmitConstant(ILGenerator il, object constant) {
      switch (constant.GetType().Name) {
        case "Boolean":
          if ((bool)constant == true) il.Emit(OpCodes.Ldc_I4_1);
          else il.Emit(OpCodes.Ldc_I4_0);
          break;
        case "SByte": il.Emit(OpCodes.Ldc_I4_S, (SByte)constant); break;
        case "Char":
        case "Byte":
        case "Int16":
        case "UInt16":
        case "Int32": il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(constant)); break;
        case "UInt32": il.Emit(OpCodes.Ldc_I4, unchecked((Int32)(UInt32)constant)); break;
        case "Int64": il.Emit(OpCodes.Ldc_I8, (Int64)constant); break;
        case "UInt64": il.Emit(OpCodes.Ldc_I8, unchecked((Int64)(UInt64)constant)); break;
        case "Double": il.Emit(OpCodes.Ldc_R8, (double)constant); break;
        case "Single": il.Emit(OpCodes.Ldc_R4, (Single)constant); break;
        case "Decimal":
          il.Emit(OpCodes.Ldc_R8, Convert.ToDouble(constant));
          ConstructorInfo ci1 = typeof(Decimal).GetConstructor(new Type[] { typeof(double) });
          il.Emit(OpCodes.Newobj, ci1);
          break;
        case "DateTime":
          il.Emit(OpCodes.Ldc_I8, ((DateTime)constant).Ticks);
          ConstructorInfo ci2 = typeof(DateTime).GetConstructor(new Type[] { typeof(long) });
          il.Emit(OpCodes.Newobj, ci2);
          break;
        case "TimeSpan":
          il.Emit(OpCodes.Ldc_I8, ((TimeSpan)constant).Ticks);
          ConstructorInfo ci3 = typeof(TimeSpan).GetConstructor(new Type[] { typeof(long) });
          il.Emit(OpCodes.Newobj, ci3);
          break;
        case "Guid":
          il.Emit(OpCodes.Ldstr, ((Guid)constant).ToString());
          ConstructorInfo ci4 = typeof(Guid).GetConstructor(new Type[] { typeof(string) });
          il.Emit(OpCodes.Newobj, ci4);
          break;
        case "String": il.Emit(OpCodes.Ldstr, (string)constant); break;
        default:
          throw new Exception("Load constant procedure does not defined for " + constant.GetType().Name);
      }
    }

  }
}
