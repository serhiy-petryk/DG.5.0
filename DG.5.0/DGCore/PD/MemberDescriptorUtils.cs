using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DGCore.PD
{
    public class MemberDescriptorUtils
    {

        private const int MaxLevel = 0;
        private static readonly Dictionary<Type, PropertyDescriptorCollection> MemberDescriptorLists = new Dictionary<Type, PropertyDescriptorCollection>();
        internal static readonly MethodInfo GenericGroupByMi = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(mi => mi.Name == "GroupBy" && mi.GetParameters().Length == 2).ToArray()[0];

        // ===============   Static section  ===========================
        public static PropertyDescriptorCollection GetTypeMembers(Type instanceType)
        {
            lock (MemberDescriptorLists)
            {
                CheckDictionary(instanceType);
                return MemberDescriptorLists[instanceType];
            }
        }

        public static PropertyDescriptor GetMember(Type instanceType, string memberName, bool ignoreCase)
        {
            lock (MemberDescriptorLists)
            {
                MethodInfo mi = typeof(MemberDescriptorUtils).GetMethod("GetMember", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null,
                  new Type[] { typeof(string), typeof(bool) }, null);
                MethodInfo mi1 = mi.MakeGenericMethod(instanceType);
                return (PropertyDescriptor)mi1.Invoke(null, new object[] { memberName, ignoreCase });
            }
        }

        public static PropertyDescriptor GetMember<T>(string memberName, bool ignoreCase)
        {
            CheckDictionary(typeof(T));
            PropertyDescriptorCollection pdc = MemberDescriptorLists[typeof(T)];
            if (ignoreCase)
            {
                foreach (PropertyDescriptor pd in pdc)
                {
                    MemberDescriptor<T> md = (MemberDescriptor<T>)pd;
                    if (md._member._memberKind == MemberKind.Property && String.Equals(memberName, md.Name, StringComparison.OrdinalIgnoreCase)) return pd;
                }
                foreach (PropertyDescriptor pd in pdc)
                {
                    if (String.Equals(memberName, pd.Name, StringComparison.OrdinalIgnoreCase)) return pd;
                }
            }
            else
            {
                foreach (PropertyDescriptor pd in pdc)
                {
                    if (pd.Name == memberName) return pd;
                }
            }
            MemberDescriptor<T> x = new MemberDescriptor<T>(memberName);
            return (x._member.IsValid ? x : null);
            //      return null;
        }

        static void CreatePropertyDescriptors<T>()
        {
            List<PropertyDescriptor> members = new List<PropertyDescriptor>();
            List<string> sTokens = new List<string>();
            GetMemberList_Nested(typeof(T), 0, MaxLevel, sTokens, "", new List<List<MemberInfo>>(), new List<MemberInfo>(), false, false);// 4 ms
            foreach (string s in sTokens)
            {
                MemberDescriptor<T> md = new MemberDescriptor<T>(s);
                if (md._member.IsValid)
                {
                    //          members.Add(new MemberDescriptor<T>(s));
                    members.Add(md);
                }
                else
                {
                }
            }
            MemberDescriptorLists.Add(typeof(T), new PropertyDescriptorCollection(members.ToArray()));
        }

        static void CheckDictionary(Type instanceType)
        {
            if (!MemberDescriptorLists.ContainsKey(instanceType))
            {
                // Update property list in dictionary
                MethodInfo mi = typeof(MemberDescriptorUtils).GetMethod("CreatePropertyDescriptors", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[0], null);
                MethodInfo mi1 = mi.MakeGenericMethod(instanceType);
                mi1.Invoke(null, new object[0]);
            }
        }
        //==========================================
        public static MemberInfo[] GetMemberInfos(Type type, bool includeFields, bool includeMethods)
        {
            List<MemberInfo> members = new List<MemberInfo>();
            Type t = Utils.Types.GetNotNullableType(type);
            if (includeFields) members.AddRange(GetPublicFields(t).ToArray());
            members.AddRange(GetPublicProperties(t).ToArray());
            if (includeMethods) members.AddRange(GetPublicMethods(t).ToArray());
            return members.ToArray();
        }

        public static List<FieldInfo> GetPublicFields(Type type)
        {
            FieldInfo[] aFI = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            List<FieldInfo> fis = new List<FieldInfo>();
            for (int i = 0; i < aFI.Length; i++)
            {
                FieldInfo fi = aFI[i];
                bool flag1 = true;
                //                       if (IsNullableType(fi.FieldType)) flag1 = false;
                if (flag1 && fi.DeclaringType == typeof(TypeCode) && fi.Name == "value__") flag1 = false;
                if (flag1 && Utils.Types.GetTypeOfType(fi.DeclaringType) == Utils.Types.TypeKind.Enum && fi.Name == "value__") flag1 = false;
                if (flag1) fis.Add(fi);
            }
            return fis;
        }
        public static List<PropertyInfo> GetPublicProperties(Type type)
        {
            PropertyInfo[] aPI = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            List<PropertyInfo> pis = new List<PropertyInfo>();
            for (int i = 0; i < aPI.Length; i++)
            {
                PropertyInfo pi = aPI[i];
                bool flag3 = pi.GetIndexParameters().Length == 0; // there are properties with parameters, for example String.Chars
                                                                  //            bool flag3 = pi.GetGetMethod().GetParameters().Length == 0; // there are properties with parameters, for example String.Chars
                if (flag3 && pi.DeclaringType == typeof(Type) && (pi.Name == "DeclaringMethod" ||
                  pi.Name == "GenericParameterAttributes" || pi.Name == "GenericParameterPosition")) flag3 = false;
                if (flag3 && pi.DeclaringType == typeof(CharEnumerator) && pi.Name == "Current") flag3 = false;
                if (flag3 && pi.DeclaringType.GetInterface("IEnumerator") != null && pi.Name == "Current") flag3 = false;
                if (flag3 && pi.DeclaringType == typeof(IEnumerator) && pi.Name == "Current") flag3 = false;

                if (flag3 && pi.Name == "TypeInitializer") flag3 = false;
                if (flag3 && pi.DeclaringType == typeof(Assembly) && pi.Name == "EntryPoint") flag3 = false;
                if (flag3) pis.Add(pi);
            }
            return pis;
        }
        public static List<MethodInfo> GetPublicMethods(Type type)
        {
            MethodInfo[] aMI = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            List<MethodInfo> mis = new List<MethodInfo>();
            for (int i = 0; i < aMI.Length; i++)
            {
                MethodInfo mi = aMI[i];
                bool flag2 = mi.GetParameters().Length == 0 && mi.ReturnType != typeof(void) && !mi.IsSpecialName;
                //            if (flag2 && IsNullableType(mi.ReturnType)) flag2 = false;
                if (flag2 && (mi.Name == "GetHashCode" || mi.Name == "GetType")) flag2 = false;

                if (flag2 && mi.DeclaringType == typeof(Type) && (mi.Name == "get_DeclaringMethod" || mi.Name == "GetArrayRank" ||
                  mi.Name == "get_GenericParameterAttributes" || mi.Name == "get_GenericParameterPosition" ||
                  mi.Name == "GetGenericParameterConstraints" || mi.Name == "GetGenericTypeDefinition" ||
                  mi.Name.StartsWith("Make")))
                    flag2 = false;
                if (flag2 && mi.DeclaringType == typeof(CharEnumerator) && mi.Name == "get_Current") flag2 = false;
                if (flag2 && mi.DeclaringType.GetInterface("IEnumerator") != null && mi.Name == "get_Current") flag2 = false;
                if (flag2 && mi.DeclaringType == typeof(IEnumerator) && mi.Name == "get_Current") flag2 = false;

                if (flag2 && mi.DeclaringType == typeof(ConstructorInfo) && mi.Name == "GetGenericArguments") flag2 = false;
                if (flag2 && mi.Name == "get_TypeInitializer") flag2 = false;
                if (flag2 && mi.DeclaringType == typeof(Assembly) && mi.Name == "get_EntryPoint") flag2 = false;
                if (flag2 && mi.DeclaringType == typeof(IntPtr) && mi.Name == "ToPointer") flag2 = false;
                if (flag2 && mi.DeclaringType == typeof(DateTime) && mi.Name.StartsWith("ToFileTime")) flag2 = false;
                if (flag2) mis.Add(mi);
            }
            return mis;
        }

        public static void GetMemberList(Type type, int level, int maxLevel, List<string> tokens, string tokenPrefix,
          List<List<MemberInfo>> allMembers, List<MemberInfo> parentMembers, bool includeFields, bool includeMethods)
        {

            if (level > maxLevel) return;
            Type t = Utils.Types.GetNotNullableType(type);
            if (includeFields)
            {
                List<FieldInfo> fis = GetPublicFields(t);
                for (int i = 0; i < fis.Count; i++)
                {
                    FieldInfo fi = fis[i];
                    tokens.Add(tokenPrefix + fi.Name);
                    List<MemberInfo> x = new List<MemberInfo>(parentMembers.ToArray());
                    x.Add(fi);
                    allMembers.Add(x);
                    GetMemberList(fi.FieldType, level + 1, maxLevel, tokens, tokenPrefix + fi.Name + ".", allMembers, x, includeFields, includeMethods);
                }
            }
            List<PropertyInfo> pis = GetPublicProperties(t);
            for (int i = 0; i < pis.Count; i++)
            {
                PropertyInfo pi = pis[i];
                tokens.Add(tokenPrefix + pi.Name);
                List<MemberInfo> x3 = new List<MemberInfo>(parentMembers.ToArray());
                x3.Add(pi);
                allMembers.Add(x3);
                GetMemberList(pi.PropertyType, level + 1, maxLevel, tokens, tokenPrefix + pi.Name + ".", allMembers, x3, includeFields, includeMethods);
            }
            if (includeMethods)
            {
                List<MethodInfo> mis = GetPublicMethods(t);
                for (int i = 0; i < mis.Count; i++)
                {
                    MethodInfo mi = mis[i];
                    tokens.Add(tokenPrefix + mi.Name + "()");
                    List<MemberInfo> x1 = new List<MemberInfo>(parentMembers.ToArray());
                    x1.Add(mi);
                    allMembers.Add(x1);
                    GetMemberList(mi.ReturnType, level + 1, maxLevel, tokens, tokenPrefix + mi.Name + "().", allMembers, x1, includeFields, includeMethods);
                }
            }
        }

        static void GetMemberList_Nested(Type type, int level, int maxLevel, List<string> tokens, string tokenPrefix,
          List<List<MemberInfo>> allMembers, List<MemberInfo> parentMembers, bool includeFields, bool includeMethods)
        {

            if (level > maxLevel) return;
            Type t = Utils.Types.GetNotNullableType(type);
            if (includeFields)
            {
                List<FieldInfo> fis = GetPublicFields(t);
                for (int i = 0; i < fis.Count; i++)
                {
                    FieldInfo fi = fis[i];
                    tokens.Add(tokenPrefix + fi.Name);
                    List<MemberInfo> x = new List<MemberInfo>(parentMembers.ToArray());
                    x.Add(fi);
                    allMembers.Add(x);
                    if (fi.Name == "CC")
                    {
                    }
                    if (fi.FieldType != typeof(string) && fi.FieldType.IsClass)
                    {
                        GetMemberList_Nested(fi.FieldType, level + 1, maxLevel, tokens, tokenPrefix + fi.Name + ".", allMembers, x, includeFields, includeMethods);
                    }
                }
            }
            List<PropertyInfo> pis = GetPublicProperties(t);
            for (int i = 0; i < pis.Count; i++)
            {
                PropertyInfo pi = pis[i];
                tokens.Add(tokenPrefix + pi.Name);
                List<MemberInfo> x3 = new List<MemberInfo>(parentMembers.ToArray());
                x3.Add(pi);
                allMembers.Add(x3);
                if (pi.PropertyType != typeof(string) && pi.PropertyType.IsClass)
                    GetMemberList_Nested(pi.PropertyType, level + 1, maxLevel, tokens, tokenPrefix + pi.Name + ".", allMembers, x3, includeFields, includeMethods);
            }
            if (includeMethods)
            {
                List<MethodInfo> mis = GetPublicMethods(t);
                for (int i = 0; i < mis.Count; i++)
                {
                    MethodInfo mi = mis[i];
                    tokens.Add(tokenPrefix + mi.Name + "()");
                    List<MemberInfo> x1 = new List<MemberInfo>(parentMembers.ToArray());
                    x1.Add(mi);
                    allMembers.Add(x1);
                    if (mi.ReturnType != typeof(string) && mi.ReturnType.IsClass)
                    {
                        GetMemberList_Nested(mi.ReturnType, level + 1, maxLevel, tokens, tokenPrefix + mi.Name + "().", allMembers, x1, includeFields, includeMethods);
                    }
                }
            }
        }
    }
}
