using System;
using System.Collections.Generic;
using System.ComponentModel;
using DGCore.DB;

namespace Models
{
    public class VCubeMastAccount: CustomTypeDescriptor
    {
        private static PropertyDescriptorCollection _pdc = DGCore.PD.MemberDescriptorUtils.GetTypeMembers(typeof(VCubeMastAccount));
        public override PropertyDescriptorCollection GetProperties() => _pdc;

        private static Dictionary<string, VCubeMastAltAcc> Data;
        static VCubeMastAccount()
        {
            // var a = new BO_LookupTableAttribute("SqlClient;initial catalog=dbOneSAP_DW;Pooling=false;Data Source=localhost;Integrated Security=SSPI;Connection Timeout=300", "select * from vcube_mastaltacc", "ALTACC");
            //DGCore.DB.LookupTableHelper.InitLookupTableTypeConverter(typeof(VCubeMastAltAcc), a);

            string conn = @"SqlClient;initial catalog=dbOneSAP_DW;Pooling=false;Data Source=localhost;Integrated Security=SSPI;Connection Timeout=300;Encrypt=false";
            string sql = "select * from vcube_mastaltacc";
            using (var cmd = new DbCmd(conn, sql))
            {
                var data = new Dictionary<string, VCubeMastAltAcc>();
                cmd.Fill<string, VCubeMastAltAcc>(data, new Func<VCubeMastAltAcc, string>(x => x.ALTACC), null);
                Data = data;
                //var converter = new LookupTableTypeConverter<VCubeMastAltAcc, string>(data);
                // typeof(VCubeMastAccount).GetProperty("ALTACC").Attributes.Add
                // TypeDescriptor.AddAttributes(typeof(VCubeMastAltAcc), new TypeConverterAttribute(typeof(LookupTableTypeConverter<VCubeMastAltAcc, string>)));
            }

        }

        public string ACCOUNT { get; set; }
        private string _altAcc;
        public string ALTACC
        {
            get => _altAcc;
            set
            {
                _altAcc = value;
            }
        }

        public VCubeMastAltAcc oALTACC
        {
            get => Data.ContainsKey(_altAcc) ? Data[_altAcc] : null;
        }

        public string ALTACC__AAA => oALTACC?.SHORTNAME;
        public string ACCOUNT_GROUP { get; set; }
        public bool? BALANCE_SHEET { get; set; }
        public string SHORT_TEXT { get; set; }
        public string LONG_TEXT { get; set; }
        public string SHORT_ALTACC { get; set; }
        public byte? COST_COMPONENT { get; set; }
        public DateTime? DATED { get; set; }
        public int? TEST { get; set; }

    }

    public class VCubeMastAltAcc
    {
        public string ALTACC { get; set; }
        public string SHORTNAME { get; set; }
        public string LONGNAME { get; set; }
        public override string ToString() => $"object: {ALTACC}";
    }

}
