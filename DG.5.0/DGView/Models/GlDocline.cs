namespace Models
{
    public class GlDocline
    {
        private static int cnt = 0;
        public int _ID_ { get; } = cnt++;
        public long DOCKEY { get; set; }
        public short LINENO { get; set; }
        public string ACCOUNT { get; set; }
        public string ALTACC { get; set; }
        public decimal DOCAMT { get; set; }
        public decimal AMT { get; set; }
        public string ASSIGNMENT { get; set; }
        public string PROFITCENTER { get; set; }
        public string BUSA { get; set; }
        public string CC { get; set; }
        public string FMAREA { get; set; }
        public string GRANT { get; set; }
        public string FUNCA { get; set; }
        public long? PO { get; set; }
        public short? POITEM { get; set; }
        public long? SO { get; set; }
        public int? SOITEM { get; set; }
        public string LID { get; set; }
        public string MATERIAL { get; set; }
        public bool STORNO { get; set; }
        public long? ORDERNO { get; set; }
        public string PARTNER { get; set; }
        public string PLANGRP { get; set; }
        public string PLANLEVEL { get; set; }
        public string PLANT { get; set; }
        public string PK { get; set; }
        public decimal? QTY { get; set; }
        public short? SORTKEY { get; set; }
        public string SP_TRAN_TYPE { get; set; }
        public string SP_IND { get; set; }
        public string TAX { get; set; }
        public string TEXT { get; set; }
        public string ASSET { get; set; }
        public string ASSET_TRAN_TYPE { get; set; }
        public string ASSET_SNO { get; set; }
        public string TRAN_KEY { get; set; }
        public string TRAN_TYPE { get; set; }
        public string WBS { get; set; }
        public short? MAT_LINE { get; set; }
        public string order_type { get; set; }
        public short orig_lineno { get; set; }
        public string orig_CC { get; set; }
        public string orig_FMAREA { get; set; }
        public string orig_GRANT { get; set; }
        public string orig_FUNCA { get; set; }
        public long? orig_ORDERNO { get; set; }
        public string orig_WBS { get; set; }
        public string orig_tax { get; set; }
        public string orig_altacc { get; set; }
    }
}
