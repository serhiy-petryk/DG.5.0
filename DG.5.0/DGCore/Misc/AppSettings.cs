using System;
using System.IO;

namespace DGCore.Misc
{
  public class AppSettings
  {
    public const string CONFIG_FILE_NAME = @"config.json";
    public static string settingsStorage;

    static AppSettings()
    {
      // Clear temp Excel files
      // long time ClearTempFiles(Utils.ExcelApp.GetDefaultExtension());
      ClearTempFiles("xlsx");
      ClearTempFiles("xls");
      ClearTempFiles("txt");
    }

    static void ClearTempFiles(string fileExtension)
    {
      string folder = Path.GetTempPath();
      FileInfo[] files = (new DirectoryInfo(folder)).GetFiles("DGV_*." + fileExtension, SearchOption.TopDirectoryOnly);
      foreach (FileInfo fi in files)
      {
        if (fi.LastWriteTime < DateTime.Now)
        {
          TimeSpan ts = DateTime.Now - fi.LastWriteTime;
          if (ts.TotalDays > 3) fi.Delete();
        }
      }
    }

  }
}
