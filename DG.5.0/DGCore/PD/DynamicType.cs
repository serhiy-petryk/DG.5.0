using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using DGCore.Common;

namespace DGCore.PD
{
    public static class DynamicType
    {
        private const string FIELD_PREFIX = "m_";
        private static AssemblyBuilder _dynamicAssembly;
        private static ModuleBuilder _moduleBuilder;
        // This event need when you are creating LookupTableTypeConverter when componentType is dynamic type
        static Assembly DynamicType_AssemblyResolve(object sender, ResolveEventArgs args) =>
          _dynamicAssembly != null && _dynamicAssembly.FullName == args.Name ? _dynamicAssembly : null;

        // General method
        public static Type GetDynamicType_FIELDS(IList<string> propertyNames, IList<Type> propertyTypes, Dictionary<string, Attribute[]> customAttributes, string[] primaryKey)
        {

            string moduleName = "PD_DynamicType.dll";
            if (_dynamicAssembly == null)
            {
                // This event need when you are creating LookupTableTypeConverter when componentType is dynamic type
                System.Threading.Thread.GetDomain().AssemblyResolve += new ResolveEventHandler(DynamicType_AssemblyResolve);
                // not working in Net.5.0 _dynamicAssembly = System.Threading.Thread.GetDomain().DefineDynamicAssembly(new AssemblyName("PD_DynamicType"), AssemblyBuilderAccess.RunAndSave);
                _dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("PD_DynamicType"), AssemblyBuilderAccess.Run);
                _moduleBuilder = _dynamicAssembly.DefineDynamicModule(moduleName);
            }
            //      ModuleBuilder moduleBuilder = (ModuleBuilder)_dynamicAssembly.GetModule(moduleName);

            // Add new type to module
            TypeBuilder tb = _moduleBuilder.DefineType("DynamicType_" + Utils.Tips.GetUniqueNumber().ToString(), TypeAttributes.Public);
            Dictionary<string, FieldBuilder> fields = new Dictionary<string, FieldBuilder>();
            // Add fields
            for (int i = 0; i < propertyNames.Count; i++)
            {
                FieldBuilder fieldBuilder = tb.DefineField(propertyNames[i], propertyTypes[i], FieldAttributes.Public);
                fields.Add(propertyNames[i], fieldBuilder);
                if (customAttributes != null && customAttributes.ContainsKey(propertyNames[i]))
                {
                    Attribute[] attrs = customAttributes[propertyNames[i]];
                    foreach (Attribute a in attrs)
                    {
                        string attrCode;
                        CustomAttributeBuilder aBuilder = GetAttributeBuilderFromAttribute(a, out attrCode);
                        fieldBuilder.SetCustomAttribute(aBuilder);
                    }
                }
            }
            // Create some methods if there is primary key
            if (primaryKey != null && primaryKey.Length > 0)
            {
                tb.AddInterfaceImplementation(typeof(IComparable));
                // Prepare primary key fields array
                FieldBuilder[] pkFields = new FieldBuilder[primaryKey.Length];
                for (int i = 0; i < primaryKey.Length; i++) pkFields[i] = fields[primaryKey[i]];

                BuildMethod_GetHashCode(tb, pkFields);
                BuildMethod_ToString(tb, pkFields);
                BuildMethod_Equals(tb, pkFields);
                BuildMethod_CompareTo(tb, pkFields);
                if (primaryKey.Length > 1)
                {// TO DO!!! build the function Get_PK(): Common.PrimaryKey is return value
                }
            }

            Type dynType = tb.CreateType();
            if (File.Exists(moduleName))
                File.Delete(moduleName);

            //  _dynamicAssembly.Save(moduleName);

            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(dynType);
            PropertyInfo piAttributeArray = typeof(PropertyDescriptor).GetProperty("AttributeArray", BindingFlags.NonPublic | BindingFlags.Instance);
            return dynType;
        }

        public static Type GetDynamicType_PROPERTIES(IList<string> propertyNames, IList<Type> propertyTypes, Dictionary<string, Attribute[]> customAttributes, string[] primaryKey)
        {

            string moduleName = "PD_DynamicType.dll";
            if (_dynamicAssembly == null)
            {
                // This event need when you are creating LookupTableTypeConverter when componentType is dynamic type
                System.Threading.Thread.GetDomain().AssemblyResolve += new ResolveEventHandler(DynamicType_AssemblyResolve);
                // not working in Net.5.0 _dynamicAssembly = System.Threading.Thread.GetDomain().DefineDynamicAssembly(new AssemblyName("PD_DynamicType"), AssemblyBuilderAccess.RunAndSave);
                _dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("PD_DynamicType"), AssemblyBuilderAccess.Run);
                _moduleBuilder = _dynamicAssembly.DefineDynamicModule(moduleName);
            }
            //      ModuleBuilder moduleBuilder = (ModuleBuilder)_dynamicAssembly.GetModule(moduleName);

            // Add new type to module
            var tb = _moduleBuilder.DefineType("DynamicType_" + Utils.Tips.GetUniqueNumber(), TypeAttributes.Public);
            Dictionary<string, FieldBuilder> fields = new Dictionary<string, FieldBuilder>();
            Dictionary<string, PropertyBuilder> properties = new Dictionary<string, PropertyBuilder>();
            // Add fields
            for (int i = 0; i < propertyNames.Count; i++)
            {
                var builderInfo = GetDynamicPropertyBuilder(tb, propertyNames[i], propertyTypes[i]);
                fields.Add($"{FIELD_PREFIX}{propertyNames[i]}", builderInfo.Item2);
                properties.Add(propertyNames[i], builderInfo.Item1);

                if (customAttributes != null && customAttributes.ContainsKey(propertyNames[i]))
                {
                    Attribute[] attrs = customAttributes[propertyNames[i]];
                    foreach (Attribute a in attrs)
                    {
                        string attrCode;
                        CustomAttributeBuilder aBuilder = GetAttributeBuilderFromAttribute(a, out attrCode);
                        builderInfo.Item1.SetCustomAttribute(aBuilder);
                        builderInfo.Item2.SetCustomAttribute(aBuilder);
                    }
                }
            }

            // Create some methods if there is primary key
            if (primaryKey != null && primaryKey.Length > 0)
            {
                tb.AddInterfaceImplementation(typeof(IComparable));
                // Prepare primary key fields array
                FieldBuilder[] pkFields = new FieldBuilder[primaryKey.Length];
                for (int i = 0; i < primaryKey.Length; i++)
                    pkFields[i] = fields[$"{FIELD_PREFIX}{primaryKey[i]}"];

                BuildMethod_GetHashCode(tb, pkFields);
                BuildMethod_ToString(tb, pkFields);
                BuildMethod_Equals(tb, pkFields);
                BuildMethod_CompareTo(tb, pkFields);
                if (primaryKey.Length > 1)
                {// TO DO!!! build the function Get_PK(): Common.PrimaryKey is return value
                }
            }

            Type dynType = tb.CreateType();
            if (File.Exists(moduleName))
                File.Delete(moduleName);

            //  _dynamicAssembly.Save(moduleName);

            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(dynType);
            PropertyInfo piAttributeArray = typeof(PropertyDescriptor).GetProperty("AttributeArray", BindingFlags.NonPublic | BindingFlags.Instance);
            return dynType;
        }

        /*    // Use for ITypedList (System.Data.DataView and so on)
            public static Type GetDynamicType(PropertyDescriptorCollection itemProperties, string[] primaryKey) {
              List<string> propertyNames = new List<string>();
              List<Type> propertyTypes=new List<Type>();
              foreach(PropertyDescriptor pd in itemProperties) {
                propertyNames.Add(pd.Name);
                propertyTypes.Add(pd.PropertyType);
              }
              return GetDynamicType(propertyNames, propertyTypes, null,primaryKey);
            }*/

        //==================    Private section   =================================  
        private static Tuple<PropertyBuilder, FieldBuilder> NestedPropertyBuilder(TypeBuilder tb, string propertyName, Type propertyType, string parentPropertyName, Type parentType, MemberInfo baseMember)
        {
            var nestedPropertyName = parentPropertyName + Constants.MDelimiter + propertyName;
            propertyType = Utils.Types.GetNullableType(propertyType);
            var isPropertyValueType = propertyType.IsValueType;

            var aa1 = propertyName.Split(new string[] { Constants.MDelimiter }, StringSplitOptions.None);
            if (aa1.Length > 5)
                return null;

            //Getter
            var methodGetBuilder = tb.DefineMethod($"get_{nestedPropertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType,
                Type.EmptyTypes);

            var il = methodGetBuilder.GetILGenerator();
            var lblGetValue = il.DefineLabel();
            var lblReturn = il.DefineLabel();
            LocalBuilder locValue = null;
            if (isPropertyValueType)
                locValue = il.DeclareLocal(propertyType);

            il.Emit(OpCodes.Ldarg_0);
            if (baseMember is FieldInfo baseFieldInfo)
                il.Emit(OpCodes.Ldfld, baseFieldInfo);
            else if (baseMember is MethodInfo baseMethodInfo)
                il.Emit(OpCodes.Call, baseMethodInfo);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue, lblGetValue);

            il.Emit(OpCodes.Pop);
            if (isPropertyValueType)
            {
                il.Emit(OpCodes.Ldloca, locValue);
                il.Emit(OpCodes.Initobj, propertyType);
                il.Emit(OpCodes.Ldloc_0);
            }
            else
                il.Emit(OpCodes.Ldnull);

            il.Emit(OpCodes.Br, lblReturn);
            il.MarkLabel(lblGetValue);
            var miProperty = parentType.GetMethod($"get_{propertyName}", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            il.Emit(OpCodes.Call, miProperty);
            if (isPropertyValueType)
                il.Emit(OpCodes.Newobj, propertyType.GetConstructor(new[] {Utils.Types.GetNotNullableType(propertyType)}));

            il.MarkLabel(lblReturn);
            il.Emit(OpCodes.Ret);

            var propertyBuilder = tb.DefineProperty(nestedPropertyName, PropertyAttributes.HasDefault, propertyType, null);
            propertyBuilder.SetGetMethod(methodGetBuilder);

            return new Tuple<PropertyBuilder, FieldBuilder>(propertyBuilder, null);
        }

        private static Tuple<PropertyBuilder, FieldBuilder> GetDynamicPropertyBuilder(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder =
                // typeBuilder.DefineField($"{FIELD_PREFIX}{propertyName}", propertyType, FieldAttributes.Private);
                tb.DefineField($"{FIELD_PREFIX}{propertyName}", propertyType, FieldAttributes.Public);

            //Getter
            MethodBuilder methodGetBuilder = tb.DefineMethod($"get_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType,
                Type.EmptyTypes);

            ILGenerator getIL = methodGetBuilder.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getIL.Emit(OpCodes.Ret);

            //Setter
            MethodBuilder methodSetBuilder = tb.DefineMethod($"set_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null,
                new Type[] { propertyType });

            ILGenerator setIL = methodSetBuilder.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, fieldBuilder);
            setIL.Emit(OpCodes.Ret);

            PropertyBuilder propertyBuilder =
                tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            propertyBuilder.SetGetMethod(methodGetBuilder);
            propertyBuilder.SetSetMethod(methodSetBuilder);

            if (propertyType != typeof(string) && !propertyType.IsValueType && propertyType.Name != "RuntimeType")
            {
                var pp = MemberDescriptorUtils.GetPublicProperties(propertyType);
                foreach (var p in pp)
                {
                    // NestedPropertyBuilder(tb, p.Name, p.PropertyType, propertyName, propertyType, methodGetBuilder);
                    NestedPropertyBuilder(tb, p.Name, p.PropertyType, propertyName, propertyType, fieldBuilder);
                }
            }

            return new Tuple<PropertyBuilder, FieldBuilder>(propertyBuilder, fieldBuilder);
        }

        private static CustomAttributeBuilder GetAttributeBuilderFromAttribute(Attribute attr, out string csCode)
        {
            Type attrType = attr.GetType();
            List<string> propNames = new List<string>();
            List<Type> propTypes = new List<Type>();
            List<object> propValues = new List<object>();
            PropertyInfo[] pis = attrType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo pi in pis)
            {
                if (pi.DeclaringType == attrType)
                {
                    propNames.Add(pi.Name);
                    propTypes.Add(pi.PropertyType);
                    propValues.Add(pi.GetValue(attr, null));
                }
            }
            FieldInfo[] fis = attr.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo fi in fis)
            {
                if (fi.DeclaringType == attrType)
                {
                    propNames.Add(fi.Name);
                    propTypes.Add(fi.FieldType);
                    propValues.Add(fi.GetValue(attr));
                }
            }

            ConstructorInfo[] cis = attr.GetType().GetConstructors();
            foreach (ConstructorInfo ci in cis)
            {
                if (ci.GetParameters().Length == propNames.Count)
                {
                    List<ParameterInfo> _params = new List<ParameterInfo>(ci.GetParameters());
                    // clone property infos to new arrays
                    List<string> _propNames = new List<string>(propNames.ToArray());
                    List<Type> _propTypes = new List<Type>(propTypes.ToArray());
                    List<object> _propValues = new List<object>(propValues.ToArray());
                    Dictionary<string, string> matches = new Dictionary<string, string>();
                    for (int i = 0; i < _params.Count; i++)
                    {
                        ParameterInfo p = _params[i];
                        for (int i2 = 0; i2 < _propNames.Count; i2++)
                        {
                            if (string.Compare(p.Name, _propNames[i2], true) == 0 && p.ParameterType == _propTypes[i2])
                            {
                                matches.Add(p.Name, _propNames[i2]);
                                _params.RemoveAt(i--);
                                _propNames.RemoveAt(i2);
                                _propTypes.RemoveAt(i2);
                                _propValues.RemoveAt(i2);
                                break;
                            }
                        }
                    }
                    // Check by type
                    for (int i = 0; i < _params.Count; i++)
                    {
                        Type t = _params[i].ParameterType;
                        int typeCnt = 0;
                        for (int i1 = 0; i1 < _params.Count; i1++)
                        {
                            if (_params[i1].ParameterType == t) typeCnt++;
                        }
                        if (typeCnt == 1)
                        {// There is only one parameter with type t
                            typeCnt = 0;
                            int typeNo = -1;
                            for (int i1 = 0; i1 < _propTypes.Count; i1++)
                            {
                                if (_propTypes[i1] == t)
                                {
                                    typeCnt++;
                                    typeNo = i1;
                                }
                            }
                            if (typeCnt == 1)
                            {
                                matches.Add(_params[i].Name, _propNames[typeNo]);
                                _params.RemoveAt(i--);
                                _propNames.RemoveAt(typeNo);
                                _propTypes.RemoveAt(typeNo);
                                _propValues.RemoveAt(typeNo);
                            }
                        }
                    }
                    // Check by name contains
                    for (int i = 0; i < _params.Count; i++)
                    {
                        string s1 = _params[i].Name.ToUpper();
                        int typeCnt = 0;
                        for (int i1 = 0; i1 < _params.Count; i1++)
                        {
                            if (_params[i1].Name.ToUpper().Contains(s1)) typeCnt++;
                        }
                        if (typeCnt == 1)
                        {// There is only one parameter with name s1
                            typeCnt = 0;
                            int typeNo = -1;
                            for (int i1 = 0; i1 < _propNames.Count; i1++)
                            {
                                if (_propNames[i1].ToUpper().Contains(s1))
                                {
                                    typeCnt++;
                                    typeNo = i1;
                                }
                            }
                            if (typeCnt == 1)
                            {
                                matches.Add(_params[i].Name, _propNames[typeNo]);
                                _params.RemoveAt(i--);
                                _propNames.RemoveAt(typeNo);
                                _propTypes.RemoveAt(typeNo);
                                _propValues.RemoveAt(typeNo);
                            }
                        }
                    }
                    if (_params.Count == 0)
                    {// Successful
                        StringBuilder sbCode = new StringBuilder("[" + attrType.Name + "(");
                        _params = new List<ParameterInfo>(ci.GetParameters());
                        object[] oo = new object[_params.Count];
                        for (int i = 0; i < oo.Length; i++)
                        {
                            string s1 = matches[_params[i].Name];
                            for (int i1 = 0; i1 < propNames.Count; i1++)
                            {
                                if (propNames[i1] == s1)
                                {
                                    oo[i] = propValues[i1];
                                    sbCode.Append((i == 0 ? "" : ", ") + (oo[i] == null ? "null" : oo[i].ToString()));
                                    break;
                                }
                            }
                        }
                        sbCode.Append(")" + Environment.NewLine);
                        csCode = sbCode.ToString();
                        return new CustomAttributeBuilder(ci, oo);
                    }
                }
            }
            throw new Exception("GetAttributeBuilderFromAttribute procedure.Can not convert '" + attrType.Name + "' attribute to CustomAttributeBuilder.");
        }

        // ===============  Builders ===========================
        private static void BuildMethod_CompareTo(TypeBuilder tb, FieldBuilder[] fields)
        {
            //    public int CompareTo(object obj) {
            //      Model.SAP_MaterialClassTest o = (Model.SAP_MaterialClassTest)obj;
            //    int i1 = this.id.CompareTo(o.id);
            //  if (i1 == 0) {
            //  i1 = this.name.CompareTo(o.name);
            //if (i1 == 0) return this.make_buy.CompareTo(o.make_buy);
            //      }
            //    return i1;

            MethodInfo baseMethod = typeof(IComparable).GetMethod("CompareTo");
            MethodBuilder mb = tb.DefineMethod(baseMethod.Name, MethodAttributes.NewSlot | MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
                baseMethod.CallingConvention, baseMethod.ReturnType, new Type[] { typeof(object) });
            mb.SetImplementationFlags(MethodImplAttributes.Managed);
            ILGenerator il = mb.GetILGenerator();

            if (fields.Length == 1)
            {
                il.Emit(OpCodes.Ldarg_0);
                Utils.Emit.EmitLoadField(il, fields[0]);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, tb);
                il.Emit(OpCodes.Ldfld, fields[0]);
                Utils.Emit.EmitCall(il, fields[0].FieldType.GetMethod("CompareTo", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { fields[0].FieldType }, null));
                il.Emit(OpCodes.Ret);
            }
            else
            {
                LocalBuilder lbObject = il.DeclareLocal(tb);
                LocalBuilder lbResult = il.DeclareLocal(typeof(int));
                Label lblExit = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Castclass, tb);
                il.Emit(OpCodes.Stloc, lbObject);// save parameter to var[0]

                for (int i = 0; i < fields.Length; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    Utils.Emit.EmitLoadField(il, fields[i]);
                    il.Emit(OpCodes.Ldloc, lbObject);
                    il.Emit(OpCodes.Ldfld, fields[i]);
                    Utils.Emit.EmitCall(il, fields[i].FieldType.GetMethod("CompareTo", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { fields[i].FieldType }, null));
                    if (i < fields.Length - 1)
                    {
                        il.Emit(OpCodes.Stloc, lbResult);//Save compare result
                        il.Emit(OpCodes.Ldloc, lbResult);
                        il.Emit(OpCodes.Brtrue, lblExit);// Compare != 0 : go to exit
                    }
                    else
                    {// Last compare : not need to save/load the result
                        il.Emit(OpCodes.Ret);
                    }
                }
                il.MarkLabel(lblExit);
                il.Emit(OpCodes.Ldloc, lbResult);
                il.Emit(OpCodes.Ret);
            }
        }

        private static void BuildMethod_Equals(TypeBuilder tb, FieldBuilder[] fields)
        {
            //Emit:       SAP_MaterialClassTest o = obj as SAP_MaterialClassTest;
            //      if (o == null) return false;
            //    return this.id.Equals(o.id) && this.name.Equals(o.name) && this.make_buy.Equals(o.make_buy);

            MethodInfo baseMethod = typeof(object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Instance);
            MethodBuilder mb = tb.DefineMethod(baseMethod.Name, MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
          baseMethod.CallingConvention, baseMethod.ReturnType, new Type[] { typeof(object) });
            mb.SetImplementationFlags(MethodImplAttributes.Managed);
            ILGenerator il = mb.GetILGenerator();

            LocalBuilder lbObject = il.DeclareLocal(tb);
            Label lblFalse = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_1, lbObject);
            il.Emit(OpCodes.Isinst, tb);
            il.Emit(OpCodes.Stloc, lbObject);// Save object to varr[0]

            il.Emit(OpCodes.Ldloc, lbObject);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Brtrue, lblFalse); //== null : go to false section

            for (int i = 0; i < fields.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_0);
                Utils.Emit.EmitLoadField(il, fields[i]);// Get field value of first object
                il.Emit(OpCodes.Ldloc, lbObject);
                il.Emit(OpCodes.Ldfld, fields[i]);// Get field value of second object
                Utils.Emit.EmitCall(il, fields[i].FieldType.GetMethod("Equals", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { fields[i].FieldType }, null));
                il.Emit(OpCodes.Brfalse, lblFalse);
            }
            // True
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Ret);
            // False
            il.MarkLabel(lblFalse);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);

            tb.DefineMethodOverride(mb, baseMethod);
        }

        private static void BuildMethod_GetHashCode(TypeBuilder tb, FieldBuilder[] fields)
        {
            //Emit: return this.id.GetHashCode() ^ this.name.GetHashCode() ^ this.make_buy.GetHashCode();
            MethodInfo baseMethod = typeof(object).GetMethod("GetHashCode");
            MethodBuilder mb = tb.DefineMethod(baseMethod.Name, MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
          baseMethod.CallingConvention, baseMethod.ReturnType, new Type[0]);
            mb.SetImplementationFlags(MethodImplAttributes.Managed);
            ILGenerator il = mb.GetILGenerator();

            for (int i = 0; i < fields.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_0);
                Utils.Emit.EmitLoadField(il, fields[i]);
                MethodInfo miGetHashcode = fields[i].FieldType.GetMethod("GetHashCode", new Type[0]);
                Utils.Emit.EmitCall(il, miGetHashcode);
                if (i > 0) il.Emit(OpCodes.Xor);
            }
            il.Emit(OpCodes.Ret);

            tb.DefineMethodOverride(mb, baseMethod);
        }

        private static void BuildMethod_ToString(TypeBuilder tb, FieldBuilder[] fields)
        {
            //Emit:       return id.ToString()+ "\t" + this.name + "\t" + this.make_buy;
            MethodInfo baseMethod = typeof(object).GetMethod("ToString");
            MethodBuilder mb = tb.DefineMethod(baseMethod.Name, MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
          baseMethod.CallingConvention, baseMethod.ReturnType, new Type[0]);
            mb.SetImplementationFlags(MethodImplAttributes.Managed);
            ILGenerator il = mb.GetILGenerator();

            if (fields.Length == 1)
            {
                il.Emit(OpCodes.Ldarg_0);
                Utils.Emit.EmitLoadField(il, fields[0]);
                MethodInfo miToString = fields[0].FieldType.GetMethod("ToString", new Type[0]);
                Utils.Emit.EmitCall(il, miToString);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                LocalBuilder lbStrings = il.DeclareLocal(typeof(string[]));
                il.Emit(OpCodes.Ldc_I4, fields.Length * 2 - 1);
                il.Emit(OpCodes.Newarr, typeof(string));
                il.Emit(OpCodes.Stloc, lbStrings);
                for (int i = 0; i < fields.Length; i++)
                {
                    il.Emit(OpCodes.Ldloc, lbStrings);
                    il.Emit(OpCodes.Ldc_I4, 2 * i);
                    il.Emit(OpCodes.Ldarg_0);
                    Utils.Emit.EmitLoadField(il, fields[i]);
                    MethodInfo miToString = fields[i].FieldType.GetMethod("ToString", new Type[0]);
                    Utils.Emit.EmitCall(il, miToString);
                    il.Emit(OpCodes.Stelem_Ref);
                    // string Devider
                    if (i != fields.Length - 1)
                    {
                        il.Emit(OpCodes.Ldloc, lbStrings);
                        il.Emit(OpCodes.Ldc_I4, 2 * i + 1);
                        il.Emit(OpCodes.Ldstr, "\t");
                        il.Emit(OpCodes.Stelem_Ref);
                    }
                }
                il.Emit(OpCodes.Ldloc, lbStrings);
                Utils.Emit.EmitCall(il, typeof(string).GetMethod("Concat", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string[]) }, null));
                il.Emit(OpCodes.Ret);
            }

            tb.DefineMethodOverride(mb, baseMethod);
        }
    }
}
