// Articles:
//http://www.sql.ru/forum/actualthread.aspx?tid=620401
//http://www.sql.ru/forum/actualthread.aspx?tid=286272
//http://www.sql.ru/Forum/actualthread.aspx?tid=484315&pg=1&mid=4797192#4797192
//3 russian article ("On Integration waves") 
//http://www.rtfm.4hack.com/articles.php?id1=2&id2=107
//http://www.rtfm.4hack.com/articles.php?id1=2&id2=108
//http://www.rtfm.4hack.com/articles.php?id1=2&id2=109

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DGCore.Helpers {
  public class ExcelApp : IDisposable {
    /* Values of Workbook.colors (Office 2003) (56 colors)
    01: 0, 0, 0
    02: 255, 255, 255
    03: 255, 0, 0
    04: 0, 255, 0
    05: 0, 0, 255
    06: 255, 255, 0
    07: 255, 0, 255
    08: 0, 255, 255
    09: 128, 0, 0
    10: 0, 128, 0
    11: 0, 0, 128
    12: 128, 128, 0
    13: 128, 0, 128
    14: 0, 128, 128
    15: 192, 192, 192
    16: 128, 128, 128
    17: 153, 153, 255
    18: 153, 51, 102
    19: 255, 255, 204
    20: 204, 255, 255
    21: 102, 0, 102
    22: 255, 128, 128
    23: 0, 102, 204
    24: 204, 204, 255
    25: 0, 0, 128
    26: 255, 0, 255
    27: 255, 255, 0
    28: 0, 255, 255
    29: 128, 0, 128
    30: 128, 0, 0
    31: 0, 128, 128
    32: 0, 0, 255
    33: 0, 204, 255
    34: 204, 255, 255
    35: 204, 255, 204
    36: 255, 255, 153
    37: 153, 204, 255
    38: 255, 153, 204
    39: 204, 153, 255
    40: 255, 204, 153
    41: 51, 102, 255
    42: 51, 204, 204
    43: 153, 204, 0
    44: 255, 204, 0
    45: 255, 153, 0
    46: 255, 102, 0
    47: 102, 102, 153
    48: 150, 150, 150
    49: 0, 51, 102
    50: 51, 153, 102
    51: 0, 51, 0
    52: 51, 51, 0
    53: 153, 51, 0
    54: 153, 51, 102
    55: 51, 51, 153
    56: 51, 51, 51
     */

    //??? does not work    private static CultureInfo ci = CultureInfo.InvariantCulture;
    //??? does not work    private static CultureInfo ci = CultureInfo.InstalledUICulture;
    private static CultureInfo ci = new CultureInfo("en-US");

    private static string _defaultExtension=null;
    public static string GetDefaultExtension()
    {
      // instead of long call [ClearTempFiles(Utils.ExcelApp.GetDefaultExtension())]
      // use ClearTempFiles("xlsx")/ClearTempFiles("xls") calls
      if (string.IsNullOrEmpty(_defaultExtension))
      {
        Type officeType = Type.GetTypeFromProgID("Excel.Application");
        if (officeType == null)
        {

        }
        else
        {
          using (ExcelApp x = new ExcelApp())
          {
            _defaultExtension = (x.version >= 12 ? "xlsx" : "xls");
            x.Book_Close(false);
          }
        }
      }
      return _defaultExtension;
    }

    public static bool IsExcelInstalled => Type.GetTypeFromProgID("Excel.Application") != null;

    public static string GetExcelFormatString(Type dataType, string dgvColumnFormat)
    {
      if (dataType == null || dataType == typeof(bool) || dataType == typeof(TimeSpan)) return "General";

      if (dataType == typeof(DateTime))
      {
        if (string.IsNullOrEmpty(dgvColumnFormat)) return GetExcelDateTimeFormatFromVSFormatString("d");// short date
        return ExcelApp.GetExcelDateTimeFormatFromVSFormatString(dgvColumnFormat);
      }

      if (Utils.Types.IsNumericType(dataType))
      {
        int dp = 0;
        if (String.IsNullOrEmpty(dgvColumnFormat) && Utils.Types.IsIntegerNumberType(dataType)) return "0";
        if (String.IsNullOrEmpty(dgvColumnFormat))
        {
          if (Utils.Types.IsIntegerNumberType(dataType)) return "0";
          else return String.Format(@"#,##0." + "0".PadRight(2, (char)48));// decimal, double, single
        }
        else if (dgvColumnFormat.StartsWith("N"))
        {
          if (int.TryParse(dgvColumnFormat.Substring(1), out dp)) return String.Format(@"#,##0" + (dp == 0 ? "" : "." + "0".PadRight(dp, (char)48)));
        }
      }
      //      else if (dataType == typeof(string)) {
      else if (dataType.IsClass)
      {// string or Nested objects
        return @"@";
      }
      throw new Exception("'" + dgvColumnFormat + "' Excel Format does not defined for " + dataType.Name + " datatype");
    }

    public static string GetExcelDateTimeFormatFromVSFormatString(string visualStudioDateTimeFormat) {
      string format = null;
      if (visualStudioDateTimeFormat.Length == 1) {// Char == DateTime patern is used
        char x;
        if (char.TryParse(visualStudioDateTimeFormat, out x)) {

          string[] ss = ci.DateTimeFormat.GetAllDateTimePatterns(x);
          if (ss.Length > 0) format = ss[0];
        }
      }
      else format = visualStudioDateTimeFormat;
      if (!string.IsNullOrEmpty(format)) {
        string[] ss = format.Split('\''); // split by '
        StringBuilder sb = new StringBuilder();
        // Четные элементы содержат символы
        for (int i = 0; i < ss.Length; i++) {
          if (i % 2 == 0) sb.Append(ss[i]);
          else {
            char[] cc = ss[i].ToCharArray();
            string[] ss1 = new string[cc.Length];
            for (int i1 = 0; i1 < ss1.Length; i1++) ss1[i1] = cc[i1].ToString();
            sb.Append(@"\" + String.Join(@"\", ss1));
          }
        }
        return sb.ToString().Replace("\"", "\"\"") ;
      }
      else return null;
    }

    public enum xlHorizontalAlignment : int {
      xlLeft = unchecked((int)0xFFFFEFDD), xlRight = unchecked((int)0xFFFFEFC8), xlCenter = unchecked((int)0xFFFFEFF4),
      xlFill = 5, xlJustify = unchecked((int)0xFFFFEFDE), xlCenterAcrossSelection = 7, xlDistributed = unchecked((int)0xFFFFEFEB), xlGeneral = 1
    }
    public enum xlVerticalAlignment : int {
      xlVAlignTop = unchecked((int)0xFFFFEFC0), xlVAlignBottom = unchecked((int)0xFFFFEFF5),
      xlVAlignCenter = unchecked((int)0xFFFFEFF4), xlVAlignDistributed = unchecked((int)0xFFFFEFEB), xlVAlignJustify = unchecked((int)0xFFFFEFDE)
    }
    public enum xlWindowState : int {
      xlMaximized = unchecked((int)0xFFFFEFD7), xlNormal = unchecked((int)0xFFFFEFD1), xlMinimized = unchecked((int)0xFFFFEFD4) 
    }

    enum InvokeKind { GetProperty, SetProperty, InvokeMethod };

    static object InvokeMember(object instance, string memberName, InvokeKind invokeKind, params object[] parameters) {
      switch (invokeKind) {
        case InvokeKind.GetProperty: return instance.GetType().InvokeMember(memberName, BindingFlags.GetProperty, null, instance, parameters, ci);
        case InvokeKind.SetProperty: return instance.GetType().InvokeMember(memberName, BindingFlags.SetProperty, null, instance, parameters, ci);
        case InvokeKind.InvokeMethod: return instance.GetType().InvokeMember(memberName, BindingFlags.InvokeMethod, null, instance, parameters, ci);
      }
      throw new Exception("dummy");
    }

    int version;
    string sVersion;
    object oApp;
    IEnumerable wBooks;
    object wBook;
    IEnumerable wSheets;
    object currentSheet;
    object currentRange;

    // ======   Constructors ====
    public ExcelApp()
      : this(null, false) {
    }
    public void Activate() {
      // Bring Excel window to front of display == does not need
      // See http://stackoverflow.com/questions/19118881/force-to-bring-excel-window-to-the-front
     // oApp.ActiveWindow.Activate();
      object wnd = InvokeMember(oApp, "ActiveWindow", InvokeKind.GetProperty);
      InvokeMember(wnd, "Activate", InvokeKind.InvokeMethod);
    }
    public ExcelApp(string filename, bool isFilenameTemplate) {
      try {
        // Try to open existing Excel App object
        // oApp = System.Runtime.InteropServices.Marshal.GetActiveObject("Excel.Application");
      }
      catch { }
      if (oApp == null) {// No Active excel app object == you need to create it
        oApp = Activator.CreateInstance(Type.GetTypeFromProgID("Excel.Application"));
      }

      this.sVersion = (string)InvokeMember(oApp, "Version", InvokeKind.GetProperty);
      this.version = Convert.ToInt32(this.sVersion.Split('.')[0]);// decimal.Parse((string)InvokeMember(oApp, "Version", InvokeKind.GetProperty), ci);
      wBooks = (IEnumerable)InvokeMember(oApp, "Workbooks", InvokeKind.GetProperty);
      // Open/Create workbook
      if (String.IsNullOrEmpty(filename)) {// New workbook
        wBook = InvokeMember(wBooks, "Add", InvokeKind.InvokeMethod);
      }
      else if (isFilenameTemplate) {// New workbook from template
        wBook = InvokeMember(wBooks,"Add", InvokeKind.InvokeMethod,  filename, 0 );
      }
      else {// Open existing workbook
        wBook = InvokeMember(wBooks,"Open",  InvokeKind.InvokeMethod,  filename, false);//false ??? - remember in Excel open file list
      }
      // Define current sheet: is the first sheet in wBook
      wSheets = (IEnumerable)InvokeMember(wBook,"Sheets", InvokeKind.GetProperty);
      currentSheet = InvokeMember(wSheets,"Item", InvokeKind.GetProperty,  1 );
    }

    // ==========   Public section  ==================
    // RowLimit
    public int RowLimit {
      get {
//        if (this.version < (decimal)10.5) return Convert.ToInt32(UInt16.MaxValue);
  //      else return Convert.ToInt32(UInt16.MaxValue) * 16;
        if (this.version >=12)return Convert.ToInt32(UInt16.MaxValue) * 16;
        else return Convert.ToInt32(UInt16.MaxValue);
      }
    }

    // Visible
    public bool Visible {
      get { return (bool)InvokeMember(oApp, "Visible", InvokeKind.GetProperty); }
      set {
        InvokeMember(oApp, "Visible", InvokeKind.SetProperty, value);
      }
    }

    // Display alerts
    public bool DisplayAlerts {
      get { return (bool)InvokeMember(oApp, "DisplayAlerts", InvokeKind.GetProperty); }
      set {
        InvokeMember(oApp, "DisplayAlerts", InvokeKind.SetProperty, value);
      }
    }

    // ScreenUpdating
    public bool ScreenUpdating {
      get { return (bool)InvokeMember(oApp, "ScreenUpdating", InvokeKind.GetProperty); }
      set {
        InvokeMember(oApp, "ScreenUpdating", InvokeKind.SetProperty, value);
      }
    }
    public void SetWindowState(xlWindowState windowState) {
      object oWindows = InvokeMember(wBook, "Windows", InvokeKind.GetProperty);
      object oWindow = InvokeMember(oWindows, "Item", InvokeKind.GetProperty, 1);
      InvokeMember(oWindow, "WindowState", InvokeKind.SetProperty, windowState);
      Marshal.ReleaseComObject(oWindows);
      Marshal.ReleaseComObject(oWindow);
      oWindows = null;
      oWindow = null;
    }

    // Save the book
    public void Book_Save(string filename, bool closeFlag) {
      if (File.Exists(filename))// OldFile
        InvokeMember(wBook,"Save", InvokeKind.InvokeMethod);
      else // newfile 
        InvokeMember(wBook,"SaveAs", InvokeKind.InvokeMethod, filename);
      if (closeFlag) Book_Close(true);
    }

    // Close the book
    public void Book_Close(bool saveChanges) {
      InvokeMember(wBook, "Close", InvokeKind.InvokeMethod, saveChanges);
    }

    // FreezePanes
    public void Book_FreezePanes(int columnIndex, int rowIndex, bool freezeValue) {
      Range_SetCurrentByCell(columnIndex, rowIndex);
      Range_Select();
      object oWindows = InvokeMember(wBook, "Windows", InvokeKind.GetProperty);
      object oWindow = InvokeMember(oWindows, "Item", InvokeKind.GetProperty, 1);
      InvokeMember(oWindow, "FreezePanes", InvokeKind.SetProperty, freezeValue);
      Marshal.ReleaseComObject(oWindows);
      Marshal.ReleaseComObject(oWindow);
      oWindows = null;
      oWindow = null;
    }

    // Autofilter
    public void SetAutoFilter(int leftColumn, int topRow, int width, int height) {
      //      Visible = true;
      Range_SetCurrentByRegion(leftColumn, topRow, width, height);
      Range_Select();
      object oWindows = InvokeMember(wBook, "Windows", InvokeKind.GetProperty);
      object oWindow = InvokeMember(oWindows, "Item", InvokeKind.GetProperty, 1);
      InvokeMember(currentRange, "Select", InvokeKind.InvokeMethod);
      object oSelection = InvokeMember(oWindow, "Selection", InvokeKind.GetProperty);
      InvokeMember(oSelection, "AutoFilter", InvokeKind.InvokeMethod);
      Marshal.ReleaseComObject(oWindows);
      Marshal.ReleaseComObject(oWindow);
      Marshal.ReleaseComObject(oSelection);
      //      Marshal.ReleaseComObject(oAutoFilter);
      oWindows = null;
      oWindow = null;
      oSelection = null;
      //    oAutoFilter = null;
    }

    // Copy to clipboard
    public void Clipboard_Copy(int leftColumn, int topRow, int width, int height) {
      //      Visible = true;
      Range_SetCurrentByRegion(leftColumn, topRow, width, height);
      InvokeMember(currentRange, "Copy", InvokeKind.InvokeMethod);
/*      Range_Select();
      object oWindows = InvokeMember(wBook, "Windows", InvokeKind.GetProperty);
      object oWindow = InvokeMember(oWindows, "Item", InvokeKind.GetProperty, 1);
      InvokeMember(currentRange, "Select", InvokeKind.InvokeMethod);
      object oSelection = InvokeMember(oWindow, "Selection", InvokeKind.GetProperty);
      InvokeMember(oSelection, "Copy", InvokeKind.InvokeMethod);
      Marshal.ReleaseComObject(oWindows);
      Marshal.ReleaseComObject(oWindow);
      Marshal.ReleaseComObject(oSelection);
      //      Marshal.ReleaseComObject(oAutoFilter);
      oWindows = null;
      oWindow = null;
      oSelection = null;
      //    oAutoFilter = null;*/
    }

    // Autoformat to the current range
    public void Range_AutoFormat() {
      InvokeMember(currentRange, "AutoFormat", InvokeKind.InvokeMethod);
    }

    // Name of sheet
    public string Sheet_Name {
      get { return (string)InvokeMember(currentSheet,"Name", InvokeKind.GetProperty ); }
      set { 
        string s = (String.IsNullOrEmpty(value)? "Data" : (value.Length>31? value.Substring(0,31): value));
        s = s.Replace(@"\", " ").Replace(@"/", " ").Replace(@"*", " ").Replace(@"?", " ").Replace(@"[", " ").Replace(@"]", " ");
        InvokeMember(currentSheet,"Name", InvokeKind.SetProperty,  s); 
      }
    }
    public void Sheet_Paste() {
      InvokeMember(currentSheet, "Paste", InvokeKind.InvokeMethod);
    }

    public void Range_SetCurrentByCell(int columnIndex,int rowIndex) {
      currentRange = InvokeMember(this.currentSheet, "Cells", InvokeKind.GetProperty, rowIndex+1, columnIndex+1);
    }
    public void Range_SetCurrentByColumn(int columnIndex) {
      string xlRange = string.Format("{0}:{0}", ParseColumnIndex(columnIndex));
      currentRange = InvokeMember(this.currentSheet, "Range", InvokeKind.GetProperty, xlRange);
    }
    public void Range_SetCurrentByRow(int rowIndex) {
      string xlRange = string.Format("{0}:{0}", rowIndex + 1);
      currentRange = InvokeMember(this.currentSheet, "Range", InvokeKind.GetProperty, xlRange);
    }
    public void Range_SetCurrentByRegion(int leftColumn, int topRow, int width, int height) {
      string xlRange = GetRangeStringFromRegion(leftColumn, topRow, width, height);
      currentRange = InvokeMember(this.currentSheet, "Range", InvokeKind.GetProperty, xlRange);
    }
    public void Range_SetCurrentByString(string xlRange) {
      currentRange = InvokeMember(this.currentSheet, "Range", InvokeKind.GetProperty, xlRange);
    }
    public static string GetRangeStringFromRegion(int leftColumn, int topRow, int width, int height) {
      return string.Format("{0}{1}:{2}{3}", ParseColumnIndex(leftColumn), (topRow + 1).ToString(),
        ParseColumnIndex(leftColumn + width - 1), (topRow + height).ToString());
    }
    public void Range_Merge(xlHorizontalAlignment hAlignment) {
      // Warning may occur
      InvokeMember(this.currentRange, "Merge", InvokeKind.InvokeMethod, true);
    }
    public void Range_AutoFitColumns() {
      object oColumns = InvokeMember(currentRange, "Columns", InvokeKind.GetProperty);   
      InvokeMember(oColumns, "AutoFit", InvokeKind.InvokeMethod);
      Marshal.ReleaseComObject(oColumns);
      oColumns = null;
    }
    public bool Range_WrapText {
      get { return (bool)InvokeMember(currentRange, "WrapText", InvokeKind.GetProperty); }
      set {
        InvokeMember(currentRange, "WrapText", InvokeKind.SetProperty, value);
      }
    }
    public void Range_SetValue(object value) {
      InvokeMember(currentRange,"Value2", InvokeKind.SetProperty,  value);
    }
    public void Range_SetHorisontalAlignment(xlHorizontalAlignment hAlignment) {
      InvokeMember(currentRange, "HorizontalAlignment", InvokeKind.SetProperty, hAlignment);
    }
    public void Range_SetVerticalAlignment(xlVerticalAlignment vAlignment) {
      InvokeMember(currentRange, "VerticalAlignment", InvokeKind.SetProperty, vAlignment);
    }
    public void Range_Select() {
      InvokeMember(currentRange, "Select", InvokeKind.InvokeMethod);
    }
    public string Range_Format {
      set { InvokeMember(this.currentRange, "NumberFormat", InvokeKind.SetProperty, value); }
    }
    public void Range_SetFont(bool? isBold, int? size)  {
      object oFont = InvokeMember(this.currentRange, "Font", InvokeKind.GetProperty);
      if (isBold != null) InvokeMember(oFont, "Bold", InvokeKind.SetProperty, isBold.Value);
      if (size != null) InvokeMember(oFont, "Size", InvokeKind.SetProperty, size.Value);
      Marshal.ReleaseComObject(oFont); oFont = null;
    }
    //УСТАНОВИТЬ ВИД РАМКИ ВОКРУГ ЯЧЕЙКИ
    public void Range_SetBorder() {
      object[] args = new object[] { 1 };
      object oBorders = currentRange.GetType().InvokeMember("Borders", BindingFlags.GetProperty, null, currentRange, null, ci);
      currentRange.GetType().InvokeMember("LineStyle", BindingFlags.SetProperty, null, oBorders, args, ci);
      Marshal.ReleaseComObject(oBorders); oBorders = null;
    }

    public void Range_SetBackColor(int excelColor)
    {
      object o = InvokeMember(currentRange, "Interior", InvokeKind.GetProperty);
      InvokeMember(o, "Color", InvokeKind.SetProperty, excelColor);
      Marshal.ReleaseComObject(o);
      o = null;
    }

    //===============  Private section  ====================
    int Books_Count {
      get { return (int)InvokeMember(wBooks,"Count", InvokeKind.GetProperty); }
    }

    static string ParseColumnIndex(int columnIndex) {
      StringBuilder sb = new StringBuilder();
      if (columnIndex <= 0) return "A";
      do {
        sb.Insert(0, (char)('A' + (columnIndex % 26)));
        columnIndex = columnIndex / 26 - 1;
      } while (columnIndex >= 0);

/*      while (columnIndex != 0) {
//        sb.Append((char)('A' + (columnIndex % 26)));
        sb.Insert(0, (char)('A' + (columnIndex % 26)));
        columnIndex /= 26;
      }*/
      return sb.ToString();
    }

    public void Dispose() {
//      if (Disposed != null) Disposed.Invoke(this, new EventArgs());
      this.ScreenUpdating = true;
      if (Books_Count > 0) {// Excel was open early
        if (!Visible) {// need to set to visible
          Visible = true;
        }
      }
      else {
        InvokeMember(oApp, "Quit", InvokeKind.InvokeMethod);// Close excel application
      }

      //!!! Marshal.ReleaseComObject for all objects == close excel instance (in memory)
      if (currentRange != null) Marshal.FinalReleaseComObject(currentRange);
      if (currentSheet != null) Marshal.FinalReleaseComObject(currentSheet);
      Marshal.FinalReleaseComObject(wSheets);
      Marshal.FinalReleaseComObject(wBook);
      Marshal.FinalReleaseComObject(wBooks);
      Marshal.FinalReleaseComObject(oApp);
      /*Marshal.ReleaseComObject(oApp);
      Marshal.ReleaseComObject(wBooks);
      Marshal.ReleaseComObject(wBook);
      Marshal.ReleaseComObject(wSheets);
      if (currentSheet != null) Marshal.ReleaseComObject(currentSheet);
      if (currentRange != null) Marshal.ReleaseComObject(currentRange);*/
      currentRange = null;
      currentSheet = null;
      wSheets = null;
      wBook = null;
      wBooks = null;
      oApp = null;
    }
  }
}
