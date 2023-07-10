using System;
using System.Collections.Generic;
using System.Reflection;

namespace Models
{
    public class GlDoclineDictionaryStore
    {
        private static int cnt = 0;
        private static int keyCnt = 1;
        private static Dictionary<int, object> keys1 = new Dictionary<int, object>() { { 0, null } };
        private static Dictionary<object, int> keys2 = new Dictionary<object, int>();
        private static int[] defValues;

        static GlDoclineDictionaryStore()
        {
            var pp = typeof(GlDoclineDictionaryStore).GetProperties(BindingFlags.Instance | BindingFlags.Public);
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

        public GlDoclineDictionaryStore()
        {
            ii[0] = cnt++;
        }

        public int[] ii = (int[])defValues.Clone();
        public int _ID_ { get { return ii[0]; } }
        public long DOCKEY
        {
            get { return (long)keys1[ii[1]]; }
            set { ii[1] = SaveValueToDictionary(value); }
        }
        public short LINENO
        {
            get { return (short)keys1[ii[2]]; }
            set { ii[2] = SaveValueToDictionary(value); }
        }
        public string ACCOUNT
        {
            get { return (string)keys1[ii[3]]; }
            set { ii[3] = SaveValueToDictionary(value); }
        }
        public string ALTACC
        {
            get { return (string)keys1[ii[4]]; }
            set { ii[4] = SaveValueToDictionary(value); }
        }
        public decimal DOCAMT
        {
            get { return (decimal)keys1[ii[5]]; }
            set { ii[5] = SaveValueToDictionary(value); }
        }
        public decimal AMT
        {
            get { return (decimal)keys1[ii[6]]; }
            set { ii[6] = SaveValueToDictionary(value); }
        }
        public string ASSIGNMENT
        {
            get { return (string)keys1[ii[7]]; }
            set { ii[7] = SaveValueToDictionary(value); }
        }
        public string PROFITCENTER
        {
            get { return (string)keys1[ii[8]]; }
            set { ii[8] = SaveValueToDictionary(value); }
        }
        public string BUSA
        {
            get { return (string)keys1[ii[9]]; }
            set { ii[9] = SaveValueToDictionary(value); }
        }
        public string CC
        {
            get { return (string)keys1[ii[10]]; }
            set { ii[10] = SaveValueToDictionary(value); }
        }
        public string FMAREA
        {
            get { return (string)keys1[ii[11]]; }
            set { ii[11] = SaveValueToDictionary(value); }
        }
        public string GRANT
        {
            get { return (string)keys1[ii[12]]; }
            set { ii[12] = SaveValueToDictionary(value); }
        }
        public string FUNCA
        {
            get { return (string)keys1[ii[13]]; }
            set { ii[13] = SaveValueToDictionary(value); }
        }
        public long? PO
        {
            get { return (long?)keys1[ii[14]]; }
            set { ii[14] = SaveValueToDictionary(value); }
        }
        public short? POITEM
        {
            get { return (short?)keys1[ii[15]]; }
            set { ii[15] = SaveValueToDictionary(value); }
        }
        public long? SO
        {
            get { return (long?)keys1[ii[16]]; }
            set { ii[16] = SaveValueToDictionary(value); }
        }
        public int? SOITEM
        {
            get { return (int?)keys1[ii[17]]; }
            set { ii[17] = SaveValueToDictionary(value); }
        }
        public string LID
        {
            get { return (string)keys1[ii[18]]; }
            set { ii[18] = SaveValueToDictionary(value); }
        }
        public string MATERIAL
        {
            get { return (string)keys1[ii[19]]; }
            set { ii[19] = SaveValueToDictionary(value); }
        }
        public bool STORNO
        {
            get { return (bool)keys1[ii[20]]; }
            set { ii[20] = SaveValueToDictionary(value); }
        }
        public long? ORDERNO
        {
            get { return (long?)keys1[ii[21]]; }
            set { ii[21] = SaveValueToDictionary(value); }
        }
        public string PARTNER
        {
            get { return (string)keys1[ii[22]]; }
            set { ii[22] = SaveValueToDictionary(value); }
        }
        public string PLANGRP
        {
            get { return (string)keys1[ii[23]]; }
            set { ii[23] = SaveValueToDictionary(value); }
        }
        public string PLANLEVEL
        {
            get { return (string)keys1[ii[24]]; }
            set { ii[24] = SaveValueToDictionary(value); }
        }
        public string PLANT
        {
            get { return (string)keys1[ii[25]]; }
            set { ii[25] = SaveValueToDictionary(value); }
        }
        public string PK
        {
            get { return (string)keys1[ii[26]]; }
            set { ii[26] = SaveValueToDictionary(value); }
        }
        public decimal? QTY
        {
            get { return (decimal?)keys1[ii[27]]; }
            set { ii[27] = SaveValueToDictionary(value); }
        }
        public short? SORTKEY
        {
            get { return (short?)keys1[ii[28]]; }
            set { ii[28] = SaveValueToDictionary(value); }
        }
        public string SP_TRAN_TYPE
        {
            get { return (string)keys1[ii[29]]; }
            set { ii[29] = SaveValueToDictionary(value); }
        }
        public string SP_IND
        {
            get { return (string)keys1[ii[30]]; }
            set { ii[30] = SaveValueToDictionary(value); }
        }
        public string TAX
        {
            get { return (string)keys1[ii[31]]; }
            set { ii[31] = SaveValueToDictionary(value); }
        }
        public string TEXT
        {
            get { return (string)keys1[ii[32]]; }
            set { ii[32] = SaveValueToDictionary(value); }
        }
        public string ASSET
        {
            get { return (string)keys1[ii[33]]; }
            set { ii[33] = SaveValueToDictionary(value); }
        }
        public string ASSET_TRAN_TYPE
        {
            get { return (string)keys1[ii[34]]; }
            set { ii[34] = SaveValueToDictionary(value); }
        }
        public string ASSET_SNO
        {
            get { return (string)keys1[ii[35]]; }
            set { ii[35] = SaveValueToDictionary(value); }
        }
        public string TRAN_KEY
        {
            get { return (string)keys1[ii[36]]; }
            set { ii[36] = SaveValueToDictionary(value); }
        }
        public string TRAN_TYPE
        {
            get { return (string)keys1[ii[37]]; }
            set { ii[37] = SaveValueToDictionary(value); }
        }
        public string WBS
        {
            get { return (string)keys1[ii[38]]; }
            set { ii[38] = SaveValueToDictionary(value); }
        }
        public short? MAT_LINE
        {
            get { return (short?)keys1[ii[39]]; }
            set { ii[39] = SaveValueToDictionary(value); }
        }
        public string order_type
        {
            get { return (string)keys1[ii[40]]; }
            set { ii[40] = SaveValueToDictionary(value); }
        }
        public short orig_lineno
        {
            get { return (short)keys1[ii[41]]; }
            set { ii[41] = SaveValueToDictionary(value); }
        }
        public string orig_CC
        {
            get { return (string)keys1[ii[42]]; }
            set { ii[42] = SaveValueToDictionary(value); }
        }
        public string orig_FMAREA
        {
            get { return (string)keys1[ii[43]]; }
            set { ii[43] = SaveValueToDictionary(value); }
        }
        public string orig_GRANT
        {
            get { return (string)keys1[ii[44]]; }
            set { ii[44] = SaveValueToDictionary(value); }
        }
        public string orig_FUNCA
        {
            get { return (string)keys1[ii[45]]; }
            set { ii[45] = SaveValueToDictionary(value); }
        }
        public long? orig_ORDERNO
        {
            get { return (long?)keys1[ii[46]]; }
            set { ii[46] = SaveValueToDictionary(value); }
        }
        public string orig_WBS
        {
            get { return (string)keys1[ii[47]]; }
            set { ii[47] = SaveValueToDictionary(value); }
        }
        public string orig_tax
        {
            get { return (string)keys1[ii[48]]; }
            set { ii[48] = SaveValueToDictionary(value); }
        }
        public string orig_altacc
        {
            get { return (string)keys1[ii[49]]; }
            set { ii[49] = SaveValueToDictionary(value); }
        }
    }
}
