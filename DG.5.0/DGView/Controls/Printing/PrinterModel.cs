using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Media;
using WpfSpLib.Common;

namespace DGView.Controls.Printing
{
    public class PrinterModel
    {
        public PrintQueue PrintQueue { get; }
        public PageViewModel Page { get; internal set; }
        public Geometry Icon { get; }
        public RelayCommand PrinterSelectCommand { get; }

        public PrinterModel(PrintQueue printQueue)
        {
            PrintQueue = printQueue;

            var availablePageSizes = new List<PageViewModel.PageSize>();
            var printCapabilities = PrintQueue.GetPrintCapabilities();
            foreach (var mediaSize in printCapabilities.PageMediaSizeCapability.Where(a => a.PageMediaSizeName.HasValue))
            {
                var pageSize = PageViewModel.PageSize.GetPageSize(mediaSize);
                if (pageSize != null)
                    availablePageSizes.Add(pageSize);
            }

            Page = new PageViewModel(availablePageSizes.ToArray(),
                PrintQueue.DefaultPrintTicket.PageOrientation ?? PageOrientation.Portrait,
                PageViewModel.PageSize.GetPageSize(PrintQueue.DefaultPrintTicket.PageMediaSize),
                    new Thickness(67, 72, 67, 72));

            string iconGeometryName;
            var printerName = printQueue.FullName.ToUpper();
            if (printerName.Contains("XPS"))
                iconGeometryName = "XPSGeometry";
            else if (printerName.Contains("PDF"))
                iconGeometryName = "PDFGeometry";
            else if (printerName.Contains("ONENOTE"))
                iconGeometryName = "OneNoteGeometry";
            else if (printerName == "FAX")
                iconGeometryName = "FaxGeometry";
            else
                iconGeometryName = "PrinterGeometry";
            Icon = (Geometry)Application.Current.Resources[iconGeometryName];

            PrinterSelectCommand = new RelayCommand(o => PrintPreviewViewModel.CurrentPrinter = this);
        }
    }
}
