using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace DGCore.PD
{
  public partial class MemberElement {
    public static partial class EmitHelper {
      //===========  Constructors =============
      public static ConstructorHandler CreateInstantiateObjectHandler(Type type) {
        ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);
        if (constructorInfo == null) {
          throw new ApplicationException(string.Format("The type {0} must declare an empty constructor (the constructor may be private, internal, protected, protected internal, or public).", type));
        }

        DynamicMethod dynamicMethod = new DynamicMethod("InstantiateObject", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(object), null, type, true);
        ILGenerator generator = dynamicMethod.GetILGenerator();
        generator.Emit(OpCodes.Newobj, constructorInfo);
        generator.Emit(OpCodes.Ret);
        return (ConstructorHandler)dynamicMethod.CreateDelegate(typeof(ConstructorHandler));
      }

      public static ConstructorHandler MyCreateInstantiateObjectHandler(Type type) {
        // There is error: does not work
        DynamicMethod dynamicMethod = new DynamicMethod("InstantiateObject", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(object), null, type, true);
        ILGenerator il = dynamicMethod.GetILGenerator();
        LocalBuilder var = il.DeclareLocal(type);
        if (type.IsValueType && !Utils.Types.IsNullableType(type)) {
          il.Emit(OpCodes.Ldloca, var);
        }
        else {
          il.Emit(OpCodes.Ldloc, var);

//          il.Emit(OpCodes.Ldloca, var);// load the address of new object
  //        il.Emit(OpCodes.Initobj, type);//init new object 
    //      il.Emit(OpCodes.Ldloc, var);// load new object into stack

        }
        BoxIfNeeded(type, il);
        il.Emit(OpCodes.Ret);

        return (ConstructorHandler)dynamicMethod.CreateDelegate(typeof(ConstructorHandler));
      }

      //CreateSetHandler
      internal static SetHandler CreateSetHandler(PropertyInfo propertyInfo) {
        MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);
        if (setMethodInfo == null || setMethodInfo.IsPrivate) return null;// not public writable property
        DynamicMethod dm = new DynamicMethod("DynamicSet", typeof(void), new Type[] { typeof(object), typeof(object) }, propertyInfo.ReflectedType, true);
        ILGenerator il = dm.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        if (propertyInfo.ReflectedType.IsValueType) {
          il.Emit(OpCodes.Unbox_Any, propertyInfo.ReflectedType);
          LocalBuilder var = il.DeclareLocal(propertyInfo.ReflectedType);
          il.Emit(OpCodes.Stloc, var);
          il.Emit(OpCodes.Ldloca, var);
        }
        il.Emit(OpCodes.Ldarg_1);
        UnboxIfNeeded(propertyInfo.PropertyType, il);
        il.Emit(OpCodes.Call, setMethodInfo);
        il.Emit(OpCodes.Ret);

        return (SetHandler)dm.CreateDelegate(typeof(SetHandler));
      }

      // CreateSetDelegate
      internal static SetHandler CreateSetHandler(FieldInfo fieldInfo) {
        if (fieldInfo.IsInitOnly) return null; // readonly field == can not write
        DynamicMethod dm = new DynamicMethod("DynamicSet", typeof(void), new Type[] { typeof(object), typeof(object) }, fieldInfo.ReflectedType, true);
        ILGenerator il = dm.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        if (fieldInfo.ReflectedType.IsValueType) {
          il.Emit(OpCodes.Unbox_Any, fieldInfo.ReflectedType);
          LocalBuilder var = il.DeclareLocal(fieldInfo.ReflectedType);
          il.Emit(OpCodes.Stloc, var);
          il.Emit(OpCodes.Ldloca, var);
        }
        il.Emit(OpCodes.Ldarg_1);
        UnboxIfNeeded(fieldInfo.FieldType, il);
        il.Emit(OpCodes.Stfld, fieldInfo);
        il.Emit(OpCodes.Ret);

        return (SetHandler)dm.CreateDelegate(typeof(SetHandler));
      }

      //CreateNativeGetHandler
      internal static Delegate CreateNativeGetHandler(MemberElement root) {
//        Type handlerReturnType = (root._canBeNull ? Utils.Types.GetNullableType(root._lastReturnType) : root.LastReturnType);
        DynamicMethod dm = new DynamicMethod("DynamicNativeGet",root._lastNullableReturnType, new Type[] { root._instanceType }, root._instanceType, true);
        ILGenerator il = dm.GetILGenerator();
        Label lblNull = il.DefineLabel();
        bool isLblNullUsed = false;
        // Emit code
        if (root._instanceType.IsValueType) {
          il.Emit(OpCodes.Ldarga, 0);
        }
        else {
          il.Emit(OpCodes.Ldarg, 0);
        }
        Type returnType = EmitMember(il, root, lblNull, ref isLblNullUsed, true);

        Debug.Assert(returnType == root._lastReturnType, "Assert: Incorect last return type definition");
        Debug.Assert(isLblNullUsed == root._canBeNull, "Assert: Incorect CanBeNull definition");
        // Check for Nullable<> in return value and create necessary code
        if (Utils.Types.IsNullableType(root._lastNullableReturnType) && !Utils.Types.IsNullableType(returnType)) {
          // Convert return value of value type to nullable
          ConstructorInfo ci = root._lastNullableReturnType.GetConstructor(new Type[] { returnType });
          Debug.Assert(ci != null, "Assert: Constructor can not be null");
          il.Emit(OpCodes.Newobj, ci);
        }
        il.Emit(OpCodes.Ret);

        // if Null value occur
        if (isLblNullUsed) {
          if (Utils.Types.IsNullableType(root._lastNullableReturnType)) {
            // Return new instance of nullable type
            il.MarkLabel(lblNull);
            LocalBuilder var = il.DeclareLocal(root._lastNullableReturnType);
            il.Emit(OpCodes.Ldloca, var);
            il.Emit(OpCodes.Initobj, root._lastNullableReturnType);
            il.Emit(OpCodes.Ldloc, var);
            il.Emit(OpCodes.Ret);
          }
          else {
            // Return null reference for class
            il.MarkLabel(lblNull);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
          }
        }
        Type delegateType = typeof(Func<,>).MakeGenericType(new Type[] { root._instanceType,root._lastNullableReturnType});
        if (root._memberInfo.Name.ToLower() == "username") {
        }
        return dm.CreateDelegate(delegateType);
      }

      internal static GetHandler CreateGetHandler(MemberElement root) {
        DynamicMethod dm = new DynamicMethod("DynamicGet", MethodAttributes.Static | MethodAttributes.Public,
          CallingConventions.Standard, typeof(object), new Type[] { typeof(object) }, root._instanceType, true);
        ILGenerator il = dm.GetILGenerator();
        Label lblNull = il.DefineLabel();
        bool isLblNullUsed = false;
        // Emit code
        il.Emit(OpCodes.Ldarg_0);
        UnboxIfNeeded(root._instanceType, il);
/*        if (root._instanceType.IsValueType) {
          il.Emit(OpCodes.Unbox_Any, root._instanceType);
        }
        else {
          il.Emit(OpCodes.Castclass,root._instanceType);
        }*/

        Type returnType = EmitMember(il, root, lblNull, ref isLblNullUsed, false);

        Debug.Assert(returnType == root._lastReturnType, "Assert: Incorect last return type definition");
        Debug.Assert(isLblNullUsed == root._canBeNull, "Assert: Incorect CanBeNull definition");

        BoxIfNeeded(returnType, il);
        il.Emit(OpCodes.Ret);
        if (isLblNullUsed) {
          il.MarkLabel(lblNull);
          il.Emit(OpCodes.Ldnull);
          il.Emit(OpCodes.Ret);
        }
        return (GetHandler)dm.CreateDelegate(typeof(GetHandler));
      }

    }

    //=================   Utils  ======================
    static Type EmitMember(ILGenerator il, MemberElement member, Label lblNull, ref bool isLblNullUsed, bool loadByAddress) {
      if (Utils.Types.IsNullableType(member._instanceType)) {
        LocalBuilder var = il.DeclareLocal(member._instanceType);
        il.Emit(OpCodes.Stloc, var);// Save value
        if (member._parent != null) {// Check null is doing for non-first member only
          isLblNullUsed = true;
          il.Emit(OpCodes.Ldloca, var);// restore value address
          MethodInfo miHasValue = member._instanceType.GetMethod("get_HasValue");
          Utils.Emit.EmitCall(il, miHasValue);
          il.Emit(OpCodes.Ldc_I4_0);
          il.Emit(OpCodes.Ceq);
          il.Emit(OpCodes.Brtrue, lblNull);
        }
        il.Emit(OpCodes.Ldloca, var);// restore value
        MethodInfo miValue = member._instanceType.GetMethod("get_Value");
        Utils.Emit.EmitCall(il, miValue);
        if (member.IsField) {// Value is Field
          EmitField(il, member);
        }
        else {
          LocalBuilder var1 = il.DeclareLocal(Utils.Types.GetNotNullableType(member._instanceType));
          il.Emit(OpCodes.Stloc, var1);
          il.Emit(OpCodes.Ldloca, var1);
          Utils.Emit.EmitCall(il, (MethodInfo)member._memberInfo);
        }
      }
      else if (member._instanceType.IsValueType) {// Value type
        if (member.IsField) {
          EmitField(il, member);
        }
        else {
          if (!loadByAddress) EmitAddressIfNeed(il, member);
          Utils.Emit.EmitCall(il, (MethodInfo)member._memberInfo);
        }
      }
      else {// Class
        if (member._parent != null) { //not root
          // Null check
          isLblNullUsed = true;
          LocalBuilder var = il.DeclareLocal(member._instanceType);
          il.Emit(OpCodes.Stloc, var);// Save value
          il.Emit(OpCodes.Ldloc, var);// restore value
          il.Emit(OpCodes.Ldnull);
          il.Emit(OpCodes.Ceq);// compare
          il.Emit(OpCodes.Brtrue, lblNull); // go if null
          il.Emit(OpCodes.Ldloc, var);// Restore value in stack
        }
        if (member.IsField) {
          EmitField(il, member);
        }
        else {
          Utils.Emit.EmitCall(il, (MethodInfo)member._memberInfo);
        }
      }
      if (member._child != null) return EmitMember(il, member._child, lblNull, ref isLblNullUsed, false);
      else return member.ReturnType;
    }
    // BoxIfNeeded
    static void BoxIfNeeded(Type type, ILGenerator generator) {
      if (type.IsValueType) {
        generator.Emit(OpCodes.Box, type);
      }
    }
    // UnboxIfNeeded
    static void UnboxIfNeeded(Type type, ILGenerator generator) {
      if (type.IsValueType) {
        generator.Emit(OpCodes.Unbox_Any, type);
      }
    }

    //==========
    static void EmitAddressIfNeed(ILGenerator il, MemberElement member) {
      if (member._parent == null || !IsFieldAddress(member._parent) || !member._parent.IsField) {
        LocalBuilder var = il.DeclareLocal(member._instanceType);
        il.Emit(OpCodes.Stloc, var);// Save value
        il.Emit(OpCodes.Ldloca, var);// restore value
      }
    }

    static void EmitField(ILGenerator il, MemberElement member) {
      il.Emit((IsFieldAddress(member) ? OpCodes.Ldflda : OpCodes.Ldfld), (FieldInfo)member._memberInfo);
    }
    static bool IsFieldAddress(MemberElement member) {
      Type returnType = member.ReturnType;
      return (returnType.IsValueType && !Utils.Types.IsNullableType(returnType) && member._child != null);
    }
  }

}

