using System;
using System.Collections.Generic;
using System.Reflection;

namespace Models
{
    public class UOD_AllUA
    {
        private static int cnt = 0;
        private static int keyCnt = 1;
        private static Dictionary<int, object> keys1 = new Dictionary<int, object>() { { 0, null } };
        private static Dictionary<object, int> keys2 = new Dictionary<object, int>();
        private static int[] defValues;

        static UOD_AllUA()
        {
            var pp = typeof(UOD_AllUA).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            defValues = new int[pp.Length];
            for (var i = 0; i < pp.Length; i++)
            {
                var t = pp[i].PropertyType;
                var defValue = t.IsValueType ? Activator.CreateInstance(t) : null;
                defValues[i] = SaveValueToDictionary(defValue);
            }
        }

        private static int SaveValueToDictionary(object value)
        {
            if (value == DBNull.Value || value == null)
                return 0;

            int newId;
            if (keys2.TryGetValue(value, out newId))
                return newId;

            newId = keyCnt++;
            keys1[newId] = value;
            keys2[value] = newId;
            return newId;
        }

        public UOD_AllUA()
        {
            ii[0] = cnt++;
        }

        public int[] ii = (int[])defValues.Clone();
        public int _ID_ { get { return ii[0]; } }
        public string FILENAME
        {
            get { return (string)keys1[ii[1]]; }
            set { ii[1] = SaveValueToDictionary(value); }
        }
        public byte LINENO
        {
            get { return (byte)keys1[ii[2]]; }
            set { ii[2] = SaveValueToDictionary(value); }
        }
        public string FIO
        {
            get { return (string)keys1[ii[3]]; }
            set { ii[3] = SaveValueToDictionary(value); }
        }
        public int? PHONE
        {
            get { return (int?)keys1[ii[4]]; }
            set { ii[4] = SaveValueToDictionary(value); }
        }

        public DateTime? BIRTH_DATE
        {
            get { return (DateTime?)keys1[ii[5]]; }
            set { ii[5] = SaveValueToDictionary(value); }
        }
        public string LOCALITY
        {
            get { return (string)keys1[ii[6]]; }
            set { ii[6] = SaveValueToDictionary(value); }
        }
        public string STREET
        {
            get { return (string)keys1[ii[7]]; }
            set { ii[7] = SaveValueToDictionary(value); }
        }
        public string HOUSE
        {
            get { return (string)keys1[ii[8]]; }
            set { ii[8] = SaveValueToDictionary(value); }
        }
        public short? KORPUS
        {
            get { return (short?)keys1[ii[9]]; }
            set { ii[9] = SaveValueToDictionary(value); }
        }
        public int? FLATNO
        {
            get { return (int?)keys1[ii[10]]; }
            set { ii[10] = SaveValueToDictionary(value); }
        }
    }
}
