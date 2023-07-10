using System;
using System.Collections;
using System.Reflection;

namespace Models
{
    public class GlDoclineHashTableStore2
    {
        private static int cnt = 0;
        private static int keyCnt = 1;
        // private static Dictionary<int, object> keys1 = new Dictionary<int, object>() { { 0, null } };
        // static new Dictionary<object, int> keys2 = new Dictionary<object, int>();
        private static Hashtable keys1 = new Hashtable() { { 0, null } };
        private static Hashtable keys2 = new Hashtable();
        private static int[] defValues;

        static GlDoclineHashTableStore2()
        {
            var pp = typeof(GlDoclineHashTableStore2).GetProperties(BindingFlags.Instance | BindingFlags.Public);
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

            // int newId;
            //if (keys2.TryGetValue(value, out newId))
            if (keys2.ContainsKey(value))
                return (int)keys2[value];

            var newId = keyCnt++;
            keys1[newId] = value;
            keys2[value] = newId;
            return newId;
        }

        public GlDoclineHashTableStore2()
        {
            ii_0 = cnt++;
        }

        // public int[] ii = (int[])defValues.Clone();
        public int _ID_ { get { return ii_0; } }
        public long DOCKEY
        {
            get { return (long)keys1[ii_1]; }
            set { ii_1 = SaveValueToDictionary(value); }
        }
        public short LINENO
        {
            get { return (short)keys1[ii_2]; }
            set { ii_2 = SaveValueToDictionary(value); }
        }
        public string ACCOUNT
        {
            get { return (string)keys1[ii_3]; }
            set { ii_3 = SaveValueToDictionary(value); }
        }
        public string ALTACC
        {
            get { return (string)keys1[ii_4]; }
            set { ii_4 = SaveValueToDictionary(value); }
        }
        public decimal DOCAMT
        {
            get { return (decimal)keys1[ii_5]; }
            set { ii_5 = SaveValueToDictionary(value); }
        }
        public decimal AMT
        {
            get { return (decimal)keys1[ii_6]; }
            set { ii_6 = SaveValueToDictionary(value); }
        }
        public string ASSIGNMENT
        {
            get { return (string)keys1[ii_7]; }
            set { ii_7 = SaveValueToDictionary(value); }
        }
        public string PROFITCENTER
        {
            get { return (string)keys1[ii_8]; }
            set { ii_8 = SaveValueToDictionary(value); }
        }
        public string BUSA
        {
            get { return (string)keys1[ii_9]; }
            set { ii_9 = SaveValueToDictionary(value); }
        }
        public string CC
        {
            get { return (string)keys1[ii_10]; }
            set { ii_10 = SaveValueToDictionary(value); }
        }
        public string FMAREA
        {
            get { return (string)keys1[ii_11]; }
            set { ii_11 = SaveValueToDictionary(value); }
        }
        public string GRANT
        {
            get { return (string)keys1[ii_12]; }
            set { ii_12 = SaveValueToDictionary(value); }
        }
        public string FUNCA
        {
            get { return (string)keys1[ii_13]; }
            set { ii_13 = SaveValueToDictionary(value); }
        }
        public long? PO
        {
            get { return (long?)keys1[ii_14]; }
            set { ii_14 = SaveValueToDictionary(value); }
        }
        public short? POITEM
        {
            get { return (short?)keys1[ii_15]; }
            set { ii_15 = SaveValueToDictionary(value); }
        }
        public long? SO
        {
            get { return (long?)keys1[ii_16]; }
            set { ii_16 = SaveValueToDictionary(value); }
        }
        public int? SOITEM
        {
            get { return (int?)keys1[ii_17]; }
            set { ii_17 = SaveValueToDictionary(value); }
        }
        public string LID
        {
            get { return (string)keys1[ii_18]; }
            set { ii_18 = SaveValueToDictionary(value); }
        }
        public string MATERIAL
        {
            get { return (string)keys1[ii_19]; }
            set { ii_19 = SaveValueToDictionary(value); }
        }
        public bool STORNO
        {
            get { return (bool)keys1[ii_20]; }
            set { ii_20 = SaveValueToDictionary(value); }
        }
        public long? ORDERNO
        {
            get { return (long?)keys1[ii_21]; }
            set { ii_21 = SaveValueToDictionary(value); }
        }
        public string PARTNER
        {
            get { return (string)keys1[ii_22]; }
            set { ii_22 = SaveValueToDictionary(value); }
        }
        public string PLANGRP
        {
            get { return (string)keys1[ii_23]; }
            set { ii_23 = SaveValueToDictionary(value); }
        }
        public string PLANLEVEL
        {
            get { return (string)keys1[ii_24]; }
            set { ii_24 = SaveValueToDictionary(value); }
        }
        public string PLANT
        {
            get { return (string)keys1[ii_25]; }
            set { ii_25 = SaveValueToDictionary(value); }
        }
        public string PK
        {
            get { return (string)keys1[ii_26]; }
            set { ii_26 = SaveValueToDictionary(value); }
        }
        public decimal? QTY
        {
            get { return (decimal?)keys1[ii_27]; }
            set { ii_27 = SaveValueToDictionary(value); }
        }
        public short? SORTKEY
        {
            get { return (short?)keys1[ii_28]; }
            set { ii_28 = SaveValueToDictionary(value); }
        }
        public string SP_TRAN_TYPE
        {
            get { return (string)keys1[ii_29]; }
            set { ii_29 = SaveValueToDictionary(value); }
        }
        public string SP_IND
        {
            get { return (string)keys1[ii_30]; }
            set { ii_30 = SaveValueToDictionary(value); }
        }
        public string TAX
        {
            get { return (string)keys1[ii_31]; }
            set { ii_31 = SaveValueToDictionary(value); }
        }
        public string TEXT
        {
            get { return (string)keys1[ii_32]; }
            set { ii_32 = SaveValueToDictionary(value); }
        }
        public string ASSET
        {
            get { return (string)keys1[ii_33]; }
            set { ii_33 = SaveValueToDictionary(value); }
        }
        public string ASSET_TRAN_TYPE
        {
            get { return (string)keys1[ii_34]; }
            set { ii_34 = SaveValueToDictionary(value); }
        }
        public string ASSET_SNO
        {
            get { return (string)keys1[ii_35]; }
            set { ii_35 = SaveValueToDictionary(value); }
        }
        public string TRAN_KEY
        {
            get { return (string)keys1[ii_36]; }
            set { ii_36 = SaveValueToDictionary(value); }
        }
        public string TRAN_TYPE
        {
            get { return (string)keys1[ii_37]; }
            set { ii_37 = SaveValueToDictionary(value); }
        }
        public string WBS
        {
            get { return (string)keys1[ii_38]; }
            set { ii_38 = SaveValueToDictionary(value); }
        }
        public short? MAT_LINE
        {
            get { return (short?)keys1[ii_39]; }
            set { ii_39 = SaveValueToDictionary(value); }
        }
        public string order_type
        {
            get { return (string)keys1[ii_40]; }
            set { ii_40 = SaveValueToDictionary(value); }
        }
        public short orig_lineno
        {
            get { return (short)keys1[ii_41]; }
            set { ii_41 = SaveValueToDictionary(value); }
        }
        public string orig_CC
        {
            get { return (string)keys1[ii_42]; }
            set { ii_42 = SaveValueToDictionary(value); }
        }
        public string orig_FMAREA
        {
            get { return (string)keys1[ii_43]; }
            set { ii_43 = SaveValueToDictionary(value); }
        }
        public string orig_GRANT
        {
            get { return (string)keys1[ii_44]; }
            set { ii_44 = SaveValueToDictionary(value); }
        }
        public string orig_FUNCA
        {
            get { return (string)keys1[ii_45]; }
            set { ii_45 = SaveValueToDictionary(value); }
        }
        public long? orig_ORDERNO
        {
            get { return (long?)keys1[ii_46]; }
            set { ii_46 = SaveValueToDictionary(value); }
        }
        public string orig_WBS
        {
            get { return (string)keys1[ii_47]; }
            set { ii_47 = SaveValueToDictionary(value); }
        }
        public string orig_tax
        {
            get { return (string)keys1[ii_48]; }
            set { ii_48 = SaveValueToDictionary(value); }
        }
        public string orig_altacc
        {
            get { return (string)keys1[ii_49]; }
            set { ii_49 = SaveValueToDictionary(value); }
        }

        //=================================
        private int ii_0;
        private int ii_1;
        private int ii_2;
        private int ii_3;
        private int ii_4;
        private int ii_5;
        private int ii_6;
        private int ii_7;
        private int ii_8;
        private int ii_9;
        private int ii_10;
        private int ii_11;
        private int ii_12;
        private int ii_13;
        private int ii_14;
        private int ii_15;
        private int ii_16;
        private int ii_17;
        private int ii_18;
        private int ii_19;
        private int ii_20;
        private int ii_21;
        private int ii_22;
        private int ii_23;
        private int ii_24;
        private int ii_25;
        private int ii_26;
        private int ii_27;
        private int ii_28;
        private int ii_29;
        private int ii_30;
        private int ii_31;
        private int ii_32;
        private int ii_33;
        private int ii_34;
        private int ii_35;
        private int ii_36;
        private int ii_37;
        private int ii_38;
        private int ii_39;
        private int ii_40;
        private int ii_41;
        private int ii_42;
        private int ii_43;
        private int ii_44;
        private int ii_45;
        private int ii_46;
        private int ii_47;
        private int ii_48;
        private int ii_49;
    }
}
