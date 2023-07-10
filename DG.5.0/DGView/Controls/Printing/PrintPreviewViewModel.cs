using System;
using System.ComponentModel;
using System.Linq;
using System.Printing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using System.Windows.Threading;
using System.Windows.Xps;
using WpfSpLib.Common;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Controls.Printing
{
    public class PrintPreviewViewModel: INotifyPropertyChanged, IDisposable
    {
        private static readonly string _defaultPrinterName = new LocalPrintServer().DefaultPrintQueue.FullName;

        public static PrinterModel[] Printers { get; } = new LocalPrintServer().GetPrintQueues().Select(p => new PrinterModel(p)).ToArray();

        private static PrinterModel _currentPrinter = Printers.FirstOrDefault(p => _defaultPrinterName == p.PrintQueue.FullName) ?? (Printers.Length > 0 ? Printers[0] : null);
        public static PrinterModel CurrentPrinter {
            get=> _currentPrinter;
            set
            {
                _currentPrinter = value;
                OnStaticPropertiesChanged(nameof(CurrentPrinter));
            }
        }

        //=====================================
        internal FrameworkElement _notificationOfGeneration;
        internal FrameworkElement _notificationOfPrinting;
        private DocumentViewer _documentViewer;
        private IPrintContentGenerator _printContentGenerator;
        private XpsDocumentWriter _xpsDocumentWriter;

        private int _printedPageCount;
        public int PrintedPageCount
        {
            get => _printedPageCount;
            private set
            {
                _printedPageCount = value;
                OnPropertiesChanged(nameof(PrintedPageCount), nameof(AreActionsEnabled));
            }
        }

        private int _loadingPageCount;
        public int LoadingPageCount
        {
            get => _loadingPageCount;
            private set
            {
                _loadingPageCount = value;
                OnPropertiesChanged(nameof(LoadingPageCount), nameof(AreActionsEnabled));
            }
        }

        private bool _isGenerating;
        public bool IsGenerating
        {
            get => _isGenerating;
            private set
            {
                _isGenerating = value;
                OnPropertiesChanged(nameof(IsGenerating), nameof(AreActionsEnabled));
            }
        }

        private bool _isPrinting;
        public bool IsPrinting
        {
            get => _isPrinting;
            private set
            {
                _isPrinting = value;
                OnPropertiesChanged(nameof(IsPrinting), nameof(AreActionsEnabled));
            }
        }

        public bool AreActionsEnabled => !IsGenerating && !IsPrinting;

        public RelayCommand PageSetupCommand { get; set; }
        public RelayCommand PrintCommand { get; }

        public PrintPreviewViewModel(DocumentViewer documentViewer, IPrintContentGenerator printContentGenerator)
        {
            _documentViewer = documentViewer;
            _printContentGenerator = printContentGenerator;

            PageSetupCommand = new RelayCommand(o =>
            {
                var wnd = new PageSetupWindow(CurrentPrinter.Page) {Owner = Window.GetWindow(_documentViewer)};
                if (wnd.ShowDialog() == true)
                {
                    CurrentPrinter.Page = wnd.ViewModel.GetPageModel();
                    GenerateContent();
                }
            });

            PrintCommand = new RelayCommand(o =>
            {
                if (CurrentPrinter != null)
                {
                    VisualHelper.DoEvents(DispatcherPriority.Render);
                    PrintDocument();
                }
            });

        }

        public async void GenerateContent()
        {
            if (_printContentGenerator != null)
            {
                IsGenerating = true;
                LoadingPageCount = 0;
                var newDocument = new FixedDocument();
                ChangeDocument(newDocument);

                await Task.WhenAll(AnimationHelper.GetContentAnimations(_notificationOfGeneration, true));

                var margins = CurrentPrinter.Page.Margins;
                VisualHelper.DoEvents(DispatcherPriority.Render);
                _printContentGenerator.GeneratePrintContent(newDocument, margins);

                foreach (var page in newDocument.Pages)
                {
                    LoadingPageCount++;
                    VisualHelper.DoEvents(DispatcherPriority.Render);

                    if (_printContentGenerator == null || _printContentGenerator.StopPrintGeneration)
                        break;
                    var element = page.Child.Children[0] as FrameworkElement;
                    element?.Arrange(new Rect(element.DesiredSize));
                }

                IsGenerating = false;

                if (_notificationOfGeneration != null)
                    await Task.WhenAll(AnimationHelper.GetContentAnimations(_notificationOfGeneration, false));
            }
        }

        private bool _cancelPrinting;
        public async void PrintDocument()
        {
            IsPrinting = true;
            await Task.WhenAll(AnimationHelper.GetContentAnimations(_notificationOfPrinting, true));

            // Invalidate PageSize before printing
            var mi = typeof(DocumentViewerBase).GetMethod("DocumentChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(_documentViewer, new[] { _documentViewer.Document, _documentViewer.Document });

            _xpsDocumentWriter = PrintQueue.CreateXpsDocumentWriter(CurrentPrinter.PrintQueue);
            _xpsDocumentWriter.WritingProgressChanged += PrintAsync_WritingProgressChanged;
            VisualHelper.DoEvents(DispatcherPriority.Render);

            try
            {
                // WriteAsync is very slow (80 secs/17 pages). Compare with Write: 4 secs/17 pages.
                _xpsDocumentWriter.Write((FixedDocument) _documentViewer.Document);
            }
            catch (Exception ex)
            {
                if (!(ex is PrintingCanceledException))
                    new DialogBox(DialogBox.DialogBoxKind.Error)
                    {
                        Host = Window.GetWindow(_documentViewer), Caption = "Помилка", Message = ex.Message,
                        Details = ex.ToString()
                    }.ShowDialog();
            }
            finally
            {
                PrintedPageCount = 0;
                _xpsDocumentWriter.WritingProgressChanged -= PrintAsync_WritingProgressChanged;
                IsPrinting = false;
                await Task.WhenAll(AnimationHelper.GetContentAnimations(_notificationOfPrinting, false));
            }

            void PrintAsync_WritingProgressChanged(object sender, WritingProgressChangedEventArgs e)
            {
                if (e.WritingLevel == WritingProgressChangeLevel.FixedPageWritingProgress)
                    PrintedPageCount = e.Number;
                VisualHelper.DoEvents(DispatcherPriority.Render);
                if (_cancelPrinting)
                {
                    _cancelPrinting = false;
                    throw new PrintingCanceledException();
                }
            }
        }

        public void CancelPrinting()
        {
            _cancelPrinting = true;
        }

        private void ChangeDocument(FixedDocument newDocument)
        {
            var oldDocument = _documentViewer.Document as FixedDocument;
            _documentViewer.Document = newDocument;
            if (newDocument != null)
                newDocument.DocumentPaginator.PageSize = new Size(CurrentPrinter.Page.ActualPageWidth, CurrentPrinter.Page.ActualPageHeight);

            oldDocument?.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var page in oldDocument.Pages)
                {
                    if (page.Child.Children[0] is IDisposable disposable)
                        disposable.Dispose();
                    page.Child.Children.Clear();
                }
            }), DispatcherPriority.ApplicationIdle);
        }

        #region ===========  INotifyPropertyChanged  ==============
        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // ========  For static properties  ========
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;
        internal static void OnStaticPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region ===========  IDisposable  ==============
        public void Dispose()
        {
            if (_printContentGenerator != null)
                _printContentGenerator.StopPrintGeneration = true;
            _notificationOfGeneration = null;
            _notificationOfPrinting = null;
            ChangeDocument(null);
            _documentViewer = null;
            if (_printContentGenerator is IDisposable disposable)
                disposable.Dispose();
            _printContentGenerator = null;
            _xpsDocumentWriter = null;
        }
        #endregion
    }
}
