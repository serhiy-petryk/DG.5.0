using System;
using System.Windows.Forms;
using TestsWinForms.TestsData;

namespace TestsWinForms
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            UnnamedParameters.RunAllTests();

            Application.Run(new Form1());
        }
    }
}
