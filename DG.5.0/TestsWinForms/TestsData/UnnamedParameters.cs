using System.Diagnostics;
using Tests.Data;

namespace TestsWinForms.TestsData
{
    public static class UnnamedParameters
    {
        private static readonly Tests.Data.DbParameter Tests = new DbParameter();

        public static void RunAllTests()
        {
            Debug.Print($"=========  UnnamedParameter_Mdb Test  ===========");
            UnnamedParameter_Mdb();
            Debug.Print($"=========  UnnamedParameter_Aaacdb Test  ===========");
            UnnamedParameter_Aacdb();
        }

        public static void UnnamedParameter_Mdb() => Tests.UnnamedParameter_Mdb();
        public static void UnnamedParameter_Aacdb() => Tests.UnnamedParameter_Accdb();

    }
}
