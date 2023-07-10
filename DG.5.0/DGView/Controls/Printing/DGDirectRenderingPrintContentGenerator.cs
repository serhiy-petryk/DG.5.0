using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DGCore.Common;
using DGCore.DGVList;
using DGCore.Helpers;
using DGCore.PD;
using DGView.Helpers;
using DGView.ViewModels;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Controls.Printing
{
    internal class DGDirectRenderingPrintContentGenerator : IPrintContentGenerator, IDisposable
    {
        public bool StopPrintGeneration { get; set; }

        private DGViewModel _viewModel;
        private DateTime _timeStamp;
        private readonly SolidColorBrush _headerBackground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF0, 0xF1, 0xF2));
        private readonly SolidColorBrush _gridColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x34, 0x3A, 0x40)); // Bootstrap dark color
        private readonly SolidColorBrush _groupBorderColor = Brushes.DodgerBlue;
        private Pen _gridPen;
        private Pen _groupBorderPen;

        private IList _items;
        private DataGridColumn[] _columns;
        private TextAlignment?[] _columnAlignments;
        private TextWrapping?[] _columnTextWrapping;
        private Size _pageSize;
        private Thickness _pageMargins;
        private double _gridScale;
        private double _halfOfGridLineThickness;
        private double _fontSize;

        private double _pixelsPerDpi;
        private Typeface _baseTypeface;
        private Typeface _titleTypeface;
        private double _titleHeight;
        private double _headerHeight;

        private int[] _rowNumbers;
        private double _actualGridRowHeaderWidth;
        private double _actualGridColumnHeaderHeight;
        private double[] _actualGridRowHeights;
        private double[] _actualGridColumnWidths;
        private List<int[]> _itemsPerPage;

        private Dictionary<int, double> _groupColumnsOffset;
        private Dictionary<int, double> _groupColumnsWidth;
        private double[] _desiredColumnWidths;
        private Func<object, object>[] _columnGetters;
        private bool _isFixedRowWidth;

        private VisualBrush _boolYesBrush;
        private VisualBrush _boolNoBrush;

        public DGDirectRenderingPrintContentGenerator(DGViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void GeneratePrintContent(FixedDocument document, Thickness margins)
        {
            _pageSize = document.DocumentPaginator.PageSize;
            _pageMargins = margins;
            DGHelper.GetSelectedArea(_viewModel.DGControl, out _items, out _columns);
            _isFixedRowWidth = !double.IsNaN(_viewModel.DGControl.RowHeight);
            _columnAlignments = _columns.Select(DGHelper.GetColumnAlignment).ToArray();
            _columnTextWrapping = _columns.Select(DGHelper.GetColumnTextWrapping).ToArray();
            _timeStamp = DateTime.Now;

            if (_items.Count == 0)
            {
                new DialogBox(DialogBox.DialogBoxKind.Warning) { Message = "No items to print!", Buttons = new[] { "OK" } }.ShowDialog();
                return;
            }

            _pixelsPerDpi = VisualTreeHelper.GetDpi(_viewModel.DGControl).PixelsPerDip;
            _baseTypeface = new Typeface(_viewModel.DGControl.FontFamily, _viewModel.DGControl.FontStyle, _viewModel.DGControl.FontWeight, _viewModel.DGControl.FontStretch);
            _titleTypeface = new Typeface(_viewModel.DGControl.FontFamily, _viewModel.DGControl.FontStyle, FontWeights.SemiBold, _viewModel.DGControl.FontStretch);

            _columnGetters = GetColumnGetters();
            _desiredColumnWidths = CalculateDesiredGridColumnWidths();

            _titleHeight = new FormattedText("A", LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _titleTypeface, 16.0, Brushes.Black, _pixelsPerDpi).Height;
            _headerHeight = CalculateHeaderHeight();
            _rowNumbers = CalculateRowNumbers();

            var gridRowHeaderWidth = new FormattedText(
                                         _rowNumbers[_rowNumbers.Length - 1].ToString("N0", LocalizationHelper.CurrentCulture),
                                         LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _baseTypeface,
                                         _viewModel.DGControl.FontSize * 0.8, Brushes.Black, _pixelsPerDpi).Width + 5.0;

            var pageWidth = _pageSize.Width - _pageMargins.Left - _pageMargins.Right;
            // var gridWidth = _columns.Sum(c => c.ActualWidth) + 1 + gridRowHeaderWidth;
            var gridWidth = _desiredColumnWidths.Sum() + 1 + gridRowHeaderWidth;
            _gridScale = gridWidth > pageWidth ? pageWidth / gridWidth : 1.0;
            _halfOfGridLineThickness = _gridScale / 2;
            _fontSize = _viewModel.DGControl.FontSize * _gridScale;

            _boolYesBrush = GetBooleanBrush(true, _gridScale);
            _boolNoBrush = GetBooleanBrush(false, _gridScale);

            _gridPen = new Pen(_gridColor, _gridScale);
            _groupBorderPen = new Pen(_groupBorderColor, _gridScale);

            _actualGridRowHeaderWidth = gridRowHeaderWidth * _gridScale;
            _actualGridColumnHeaderHeight = CalculateActualGridColumnHeaderHeight();
            _actualGridRowHeights = CalculateActualGridRowHeights();
            _actualGridColumnWidths = CalculateActualGridColumnWidths();
            _itemsPerPage = CalculateItemsPerPage();
            CalculateGroupColumnData(); // _groupColumnsOffset && _groupColumnsWidth

            for (var k = 0; k < _itemsPerPage.Count; k++)
            {
                VisualHelper.DoEvents(DispatcherPriority.Render);
                if (StopPrintGeneration)
                    break;
                var pageElement = GetPageElement(k);
                var fixedPage = new FixedPage();
                fixedPage.Children.Add(pageElement);
                var pageContent = new PageContent { Child = fixedPage };
                document.Pages.Add(pageContent);
            }
        }

        private FrameworkElement GetPageElement(int pageNo)
        {
            var renderElement = new RenderElement(pageNo, RenderPage)
            {
                Margin = new Thickness(_pageMargins.Left, _pageMargins.Top, 0, 0),
                Width = _pageSize.Width - _pageMargins.Left - _pageMargins.Right,
                Height = _pageSize.Height - _pageMargins.Top - _pageMargins.Bottom
            };
            return renderElement;
        }

        #region ========  Preparing methods  ============
        private static VisualBrush GetBooleanBrush(bool value, double gridScale)
        {
            var checkbox = new CheckBox
            {
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsChecked = value,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (gridScale < 1.0)
            {
                checkbox.LayoutTransform = new ScaleTransform(gridScale, gridScale);
            }
            var brush = new VisualBrush(checkbox) { Stretch = Stretch.None, TileMode = TileMode.None };
            return brush;
        }

        private double[] CalculateDesiredGridColumnWidths()
        {
            var widths = _columns.Select(c => c.ActualWidth).ToArray();
            var columnsToRecalculate = new List<int>();
            for (var i = 0; i < _columns.Length; i++)
            {
                var width = _columns[i].Width;
                if (width.IsAbsolute || (_columns[i].HeaderStringFormat ?? "").StartsWith(Constants.GroupColumnNamePrefix))
                    widths[i] = width.Value;
                else
                {
                    var property = _viewModel.Properties[_columns[i].SortMemberPath];
                    if (!(property != null && property.PropertyType == typeof(byte[]) && !_isFixedRowWidth))
                    {
                        widths[i] = -1;
                        columnsToRecalculate.Add(i);
                    }
                }
            }

            if (columnsToRecalculate.Count > 0)
            {
                // Recalculate column headers
                for (var i = 0; i < columnsToRecalculate.Count; i++)
                {
                    var text = (_columns[columnsToRecalculate[i]].Header ?? "").ToString();
                    if (string.IsNullOrEmpty(text))
                        widths[columnsToRecalculate[i]] = 10.0;
                    else
                    {
                        var formattedText = new FormattedText(text, LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight,
                            _baseTypeface, _viewModel.DGControl.FontSize, Brushes.Black, _pixelsPerDpi);
                        formattedText.Trimming = TextTrimming.None;
                        formattedText.MaxTextWidth = formattedText.MinWidth; // force to wrap text
                        widths[columnsToRecalculate[i]] = formattedText.Width + 5.0;
                    }
                }

                // Recalculate items
                var formattedCache = new Dictionary<string, double>();
                foreach (var item in _items)
                {
                    foreach (var index in columnsToRecalculate)
                    {
                        if (Math.Abs(widths[index] - _columns[index].MaxWidth)<0.001)
                            continue;
                        if (!(_columns[index] is DataGridTextColumn))
                            continue;

                        var value = _columnGetters[index](item);
                        var text = (value ?? "").ToString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            if (!formattedCache.ContainsKey(text))
                            {
                                var formattedText = new FormattedText(text, LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight,
                                    _baseTypeface, _viewModel.DGControl.FontSize, Brushes.Black, _pixelsPerDpi);
                                formattedText.Trimming = TextTrimming.CharacterEllipsis;
                                // formattedText.MaxTextWidth = _columns[i2].ActualWidth - 5.0;
                                formattedCache.Add(text, formattedText.Width + 5.0);
                            }
                            var width = Math.Min(formattedCache[text], _columns[index].MaxWidth);
                            if (width > widths[index])
                                widths[index] = width;
                        }
                    }
                }
            }
            return widths;
        }

        private Func<object, object>[] GetColumnGetters()
        {
            var getters = new Func<object, object>[_columns.Length];
            for (var i = 0; i < _columns.Length; i++)
            {
                var column = _columns[i];
                if (!string.IsNullOrEmpty(column.SortMemberPath))
                    getters[i] = (new DGCellValueFormatter(_viewModel.Properties[column.SortMemberPath])).ValueForPrinterGetter;
                else if (Equals(column.HeaderStringFormat, Constants.GroupItemCountColumnName))
                    getters[i] = (new DGCellValueFormatter(new PropertyDescriptorForGroupItemCount())).ValueForPrinterGetter;
                else if ((column.HeaderStringFormat ?? "").StartsWith(Constants.GroupColumnNamePrefix))
                    getters[i] = null;
                else
                    throw new Exception("Trap!!!");
            }
            return getters;
        }

        private double CalculateHeaderHeight()
        {
            var headerHeight = 5.0;
            var leftHeaderFormattedText = new FormattedText("A", LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _titleTypeface, 16.0, Brushes.Black, _pixelsPerDpi);
            headerHeight += leftHeaderFormattedText.Height;

            // Draw page subheader
            var subHeaders = _viewModel.Data.GetSubheaders_ExcelAndPrint(_viewModel.StartUpParameters, _viewModel.LastAppliedLayoutName);
            if (subHeaders != null && subHeaders.Length > 0)
            {
                var pageWidth = _pageSize.Width - _pageMargins.Left - _pageMargins.Right;
                var subHeaderText = string.Join(Environment.NewLine, subHeaders);
                var subHeaderFormattedText = new FormattedText(subHeaderText, LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _baseTypeface, 12.0, Brushes.Black, _pixelsPerDpi);
                subHeaderFormattedText.MaxTextWidth = pageWidth;
                subHeaderFormattedText.Trimming = TextTrimming.CharacterEllipsis;
                headerHeight += subHeaderFormattedText.Height + 2.0;
            }

            return headerHeight;
        }

        private int[] CalculateRowNumbers()
        {
            var rowNumbers = new int[_items.Count];
            var currentNo = 0;
            var currentItem = _items[currentNo];
            var cnt = 0;
            foreach (var item in _viewModel.DGControl.ItemsSource)
            {
                cnt++;
                if (item == currentItem)
                {
                    rowNumbers[currentNo++] = cnt;
                    if (currentNo >= _items.Count) break;
                    currentItem = _items[currentNo];
                }
                if (currentNo >= _items.Count) break;
            }

            return rowNumbers;
        }

        private double CalculateActualGridColumnHeaderHeight()
        {
            var headerHeight = new FormattedText("A", LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _baseTypeface, _viewModel.DGControl.FontSize, Brushes.Black, _pixelsPerDpi).Height;
            var cnt = 0;
            foreach (var column in _columns)
            {
                var text = (column.Header ?? "").ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    var formattedText = new FormattedText(text, LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _baseTypeface, _viewModel.DGControl.FontSize, Brushes.Black, _pixelsPerDpi);
                    formattedText.Trimming = TextTrimming.None;
                    // formattedText.MaxTextWidth = column.ActualWidth - 5.0;
                    formattedText.MaxTextWidth = _desiredColumnWidths[cnt] - 5.0;
                    var cellHeight = formattedText.Height;
                    if (cellHeight > headerHeight)
                        headerHeight = cellHeight;
                }

                cnt++;
            }

            return (headerHeight + 5.0) * _gridScale;
        }

        private double[] CalculateActualGridRowHeights()
        {
            var rowHeights = new double[_items.Count];
            var cache = new Dictionary<string, double>();
            var imageColumnIndexes = _columns.Select((c,index)=> new {c=c, index=index}).Where(o =>
                !string.IsNullOrEmpty(o.c.SortMemberPath) &&
                _viewModel.Properties[o.c.SortMemberPath].PropertyType == typeof(byte[])).Select(o=> o.index).ToArray();

            var minRowHeight = new FormattedText("A", LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _baseTypeface, _viewModel.DGControl.FontSize, Brushes.Black, _pixelsPerDpi).Height;
            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var rowHeight = minRowHeight;
                for (var i2 = 0; i2 < _columnTextWrapping.Length; i2++)
                {
                    var wrapping = _columnTextWrapping[i2];
                    if (!string.IsNullOrEmpty(_columns[i2].SortMemberPath) && (wrapping == TextWrapping.Wrap || wrapping == TextWrapping.WrapWithOverflow)) {
                        var value = (_columnGetters[i2](item) ?? "").ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            var key = value + " " + Math.Round(_desiredColumnWidths[i2], 5);
                            if (!cache.ContainsKey(key))
                            {
                                var cellText = new FormattedText(value, LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight,
                                    _baseTypeface, _viewModel.DGControl.FontSize, Brushes.Black, _pixelsPerDpi);
                                // cellText.MaxTextWidth = _columns[i2].ActualWidth - 5.0;
                                cellText.MaxTextWidth = _desiredColumnWidths[i2] - 5.0;
                                cache[key] = cellText.Height;
                            }

                            if (cache[key] > rowHeight)
                                rowHeight = cache[key];
                        }
                    }
                }

                if (!_isFixedRowWidth)
                {
                    for (var i2 = 0; i2 < imageColumnIndexes.Length; i2++)
                    {
                        var index = imageColumnIndexes[i2];
                        var value = _columnGetters[index](item);
                        if (value is byte[] bytes)
                        {
                            var image = new BitmapImage();
                            using (var stream = new MemoryStream(bytes))
                            {
                                stream.Position = 0;
                                image.BeginInit();
                                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                // image.UriSource = null;
                                image.StreamSource = stream;
                                image.EndInit();
                            }
                            image.Freeze();

                            var imageActualWidth = _columns[index].ActualWidth - 3.0;
                            var imageHeight = image.Height * imageActualWidth / image.Width;
                            if (imageHeight - 2.0 > rowHeight)
                                rowHeight = imageHeight - 2.0;

                        }
                    }
                }

                rowHeights[i] = (rowHeight + 5.0) * _gridScale;
            }

            return rowHeights;
        }

        // private double[] CalculateActualGridColumnWidths() => _columns.Select(c => c.ActualWidth * _gridScale).ToArray();
        private double[] CalculateActualGridColumnWidths() => _desiredColumnWidths.Select(c => c * _gridScale).ToArray();

        private List<int[]> CalculateItemsPerPage()
        {
            var itemsPerPage = new List<int[]>();
            var startItemNo = 0;

            var dataAreaHeight = _pageSize.Height - _pageMargins.Top - _pageMargins.Bottom - _headerHeight - _actualGridColumnHeaderHeight - _gridScale;
            while (startItemNo < _items.Count)
            {
                var pageHeight = 0.0;
                var currentItemNo = startItemNo;
                while ((currentItemNo < _items.Count && pageHeight < dataAreaHeight - _actualGridRowHeights[currentItemNo]) || currentItemNo == startItemNo)
                {
                    pageHeight += _actualGridRowHeights[currentItemNo++];
                }
                itemsPerPage.Add(new[] { startItemNo, currentItemNo - startItemNo });
                startItemNo = currentItemNo;
            }

            return itemsPerPage;
        }

        private void CalculateGroupColumnData()
        {
            _groupColumnsOffset = new Dictionary<int, double>();
            _groupColumnsWidth = new Dictionary<int, double>();
            var temp = _actualGridRowHeaderWidth + _gridScale;
            for (var i = 0; i < _actualGridColumnWidths.Length; i++)
            {
                if (!(_columns[i].HeaderStringFormat ?? "").StartsWith(Constants.GroupColumnNamePrefix))
                    break;
                var index = int.Parse(_columns[i].HeaderStringFormat.Substring(Constants.GroupColumnNamePrefix.Length)) + 1;
                _groupColumnsOffset.Add(index, temp);
                _groupColumnsWidth.Add(index, _actualGridColumnWidths[i]);
                temp += _actualGridColumnWidths[i];
            }
        }

        #endregion

        #region ===========  Rendering methods  ================
        private void RenderPage(int pageNo, DrawingContext dc)
        {
            if (dc == null)
                return;

            var minItemNo = _itemsPerPage[pageNo][0];
            var maxItemNo = minItemNo + _itemsPerPage[pageNo][1] - 1;
            var yOffset = 0.0;

            // Draw background for test
            var pageWidth = _pageSize.Width - _pageMargins.Left - _pageMargins.Right;
            var pageHeight = _pageSize.Height - _pageMargins.Top - _pageMargins.Bottom;
            // dc.DrawRectangle(Brushes.Yellow, null, new Rect(0, 0, pageWidth, pageHeight));

            // Draw page header
            var rightHeaderText = $"{_timeStamp:G} / Сторінка {pageNo + 1} з {_itemsPerPage.Count}";
            var rightHeaderFormattedText = new FormattedText(rightHeaderText, LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _baseTypeface, 12.0, Brushes.Black, _pixelsPerDpi);
            rightHeaderFormattedText.MaxTextWidth = pageWidth;
            rightHeaderFormattedText.Trimming = TextTrimming.CharacterEllipsis;
            rightHeaderFormattedText.MaxLineCount = 1;
            var x = Math.Max(0, pageWidth - rightHeaderFormattedText.Width);
            var y = (_titleHeight - rightHeaderFormattedText.Height) / 2;
            dc.DrawText(rightHeaderFormattedText, new Point(x, y));

            var leftHeaderText = _viewModel.Title;
            var lefttHeaderFormattedText = new FormattedText(leftHeaderText, LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _titleTypeface, 16.0, Brushes.Black, _pixelsPerDpi);
            lefttHeaderFormattedText.MaxTextWidth = pageWidth - rightHeaderFormattedText.Width;
            lefttHeaderFormattedText.Trimming = TextTrimming.CharacterEllipsis;
            lefttHeaderFormattedText.MaxLineCount = 1;
            dc.DrawText(lefttHeaderFormattedText, new Point(0.0, 0.0));
            yOffset += lefttHeaderFormattedText.Height;

            // Draw page subheader
            var subHeaders = _viewModel.Data.GetSubheaders_ExcelAndPrint(_viewModel.StartUpParameters, _viewModel.LastAppliedLayoutName);
            if (subHeaders != null && subHeaders.Length > 0)
            {
                var subHeaderText = string.Join(Environment.NewLine, subHeaders);
                var subHeaderFormattedText = new FormattedText(subHeaderText, LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _baseTypeface, 12.0, Brushes.Black, _pixelsPerDpi);
                subHeaderFormattedText.MaxTextWidth = pageWidth;
                subHeaderFormattedText.Trimming = TextTrimming.CharacterEllipsis;
                dc.DrawText(subHeaderFormattedText, new Point(0, yOffset + 2));
                yOffset += subHeaderFormattedText.Height + 2.0;
            }

            yOffset += 5.0;
            var gridLineWidth = _actualGridRowHeaderWidth + _actualGridColumnWidths.Sum() + _gridScale;
            var gridLineHeight = _actualGridColumnHeaderHeight + _actualGridRowHeights.Where((h, index) => index >= minItemNo && index <= maxItemNo).Sum() + _gridScale;

            // Draw background of grid column header
            dc.DrawRectangle(_headerBackground, _gridPen, 
                new Rect(_halfOfGridLineThickness + _actualGridRowHeaderWidth, yOffset + _halfOfGridLineThickness,
                    gridLineWidth - _gridScale - _actualGridRowHeaderWidth, _actualGridColumnHeaderHeight));
            var yGridOffset = yOffset + _halfOfGridLineThickness + _actualGridColumnHeaderHeight;

            // Draw background of grid row header
            dc.DrawRectangle(_headerBackground, _gridPen, new Rect(_halfOfGridLineThickness, yGridOffset, _actualGridRowHeaderWidth, gridLineHeight - _actualGridColumnHeaderHeight - _gridScale));

            // Draw horizontal grid lines
            dc.DrawLine(_gridPen, new Point(_actualGridRowHeaderWidth, yGridOffset), new Point(gridLineWidth, yGridOffset));
            for (var i = minItemNo; i <= maxItemNo; i++)
            {
                yGridOffset += _actualGridRowHeights[i];
                dc.DrawLine(_gridPen, new Point(0, yGridOffset), new Point(gridLineWidth, yGridOffset));
            }

            // Draw vertical grid lines
            var xGridOffset = _halfOfGridLineThickness + _actualGridRowHeaderWidth;
            var yTo = yOffset + gridLineHeight - _gridScale;
            dc.DrawLine(_gridPen, new Point(xGridOffset, yOffset), new Point(xGridOffset, yTo));
            for (var i = 0; i < _actualGridColumnWidths.Length; i++)
            {
                xGridOffset += _actualGridColumnWidths[i];
                if (!(_columns[i].HeaderStringFormat ?? "").StartsWith(Constants.GroupColumnNamePrefix))
                    dc.DrawLine(_gridPen, new Point(xGridOffset, yOffset), new Point(xGridOffset, yTo));
            }

            // Draw column header text
            xGridOffset = _actualGridRowHeaderWidth;
            yGridOffset = yOffset;
            for (var i = 0; i < _columns.Length; i++)
            {
                DrawCellContent(_columns[i].Header, _actualGridColumnWidths[i], _actualGridColumnHeaderHeight, TextAlignment.Left, TextTrimming.None);
                xGridOffset += _actualGridColumnWidths[i];
            }

            // Draw row header text
            xGridOffset = 0.0;
            yGridOffset = yOffset + _actualGridColumnHeaderHeight;
            for (var i = minItemNo; i <= maxItemNo; i++)
            {
                var text = _rowNumbers[i].ToString("N0", LocalizationHelper.CurrentCulture);
                DrawCellContent(text, _actualGridRowHeaderWidth, _actualGridRowHeights[i], TextAlignment.Center, TextTrimming.None, 0.8);
                yGridOffset += _actualGridRowHeights[i];
            }

            // Draw started vertical background & borders of group items
            yGridOffset = yOffset + _actualGridColumnHeaderHeight;
            foreach (var groupLevel in _groupColumnsWidth.Keys)
            {
                var c = DGCore.Helpers.ColorInfo.GetGroupColor(groupLevel);
                var backBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B));

                dc.DrawRectangle(backBrush, null, new Rect(_groupColumnsOffset[groupLevel], yGridOffset + _gridScale, _groupColumnsWidth[groupLevel], yTo - yGridOffset- _gridScale));

                x = _groupColumnsOffset[groupLevel] + _groupColumnsWidth[groupLevel] - _halfOfGridLineThickness;
                dc.DrawLine(_groupBorderPen, new Point(x, yGridOffset + _gridScale), new Point(x, yTo));
            }

            if (_groupColumnsOffset.Count > 0)
            {
                // Draw left vertical grid of headers if group column exists
                var last = _groupColumnsOffset.Keys.Last();
                x = _groupColumnsOffset[last] + _groupColumnsWidth[last];
                dc.DrawLine(_gridPen,
                    new Point(x, yOffset + _halfOfGridLineThickness),
                    new Point(x, yOffset + _halfOfGridLineThickness + _actualGridColumnHeaderHeight));

                // Draw left vertical grid in data area if group column exists (DodgerBlue color)
                var first = _groupColumnsOffset.Keys.First();
                x = _groupColumnsOffset[first] - _halfOfGridLineThickness;
                dc.DrawLine(_groupBorderPen, new Point(x, yGridOffset + _gridScale), new Point(x, yTo + _gridScale));
            }

            // Draw background & borders of group items
            for (var i = minItemNo; i <= maxItemNo; i++)
            {
                var groupItem = _items[i] as IDGVList_GroupItem;
                var nextItemGroupLevel = GetItemGroupLevel(_rowNumbers[i]);

                if (groupItem == null)
                {
                    if (nextItemGroupLevel > 0 && nextItemGroupLevel < int.MaxValue)
                    {
                        // Draw horizontal group line
                        x = _actualGridRowHeaderWidth + _gridScale + _groupColumnsWidth.Keys.Where(k => k < nextItemGroupLevel).Sum(index => _groupColumnsWidth[index]);
                        y = yGridOffset + _actualGridRowHeights[i] + _halfOfGridLineThickness;
                        dc.DrawLine(_groupBorderPen, new Point(x, y), new Point(gridLineWidth - _gridScale, y));
                    }

                    yGridOffset += _actualGridRowHeights[i];
                    continue;
                }

                var c = DGCore.Helpers.ColorInfo.GetGroupColor(groupItem.Level);
                var backBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B));
                if (groupItem.Level == 0)
                {
                    // total line
                    dc.DrawRectangle(backBrush, null,
                        new Rect(_actualGridRowHeaderWidth + _gridScale, yGridOffset + _gridScale,
                            gridLineWidth - _actualGridRowHeaderWidth - _gridScale * 2.0, _actualGridRowHeights[i] - _gridScale));
                    y = yGridOffset + _actualGridRowHeights[i] + _halfOfGridLineThickness;
                    dc.DrawLine(_groupBorderPen,
                        new Point(_actualGridRowHeaderWidth + _gridScale, y),
                        new Point(gridLineWidth - _gridScale, y));
                }
                else if (_groupColumnsOffset.ContainsKey(groupItem.Level))
                {
                    // Horizontal rectangle
                    dc.DrawRectangle(backBrush, null,
                        new Rect(_groupColumnsOffset[groupItem.Level], yGridOffset + _gridScale,
                            gridLineWidth - _groupColumnsOffset[groupItem.Level] - _gridScale, _actualGridRowHeights[i] - _gridScale));
                    // Vertical rectangle
                    if (i < maxItemNo)
                        dc.DrawRectangle(backBrush, null,
                            new Rect(_groupColumnsOffset[groupItem.Level], yGridOffset + _actualGridRowHeights[i],
                                _groupColumnsWidth[groupItem.Level], yTo - (yGridOffset + _actualGridRowHeights[i])));

                    // Horizontal line
                    x = _actualGridRowHeaderWidth + _gridScale + _groupColumnsWidth.Keys.Where(k => k < nextItemGroupLevel).Sum(index => _groupColumnsWidth[index]);
                    y = yGridOffset + _actualGridRowHeights[i] + _halfOfGridLineThickness;
                    dc.DrawLine(_groupBorderPen, new Point(x, y), new Point(gridLineWidth - _gridScale, y));

                    // Vertical line
                    if (i < maxItemNo && nextItemGroupLevel > groupItem.Level)
                    {
                        x = _groupColumnsOffset[groupItem.Level] + _groupColumnsWidth[groupItem.Level] - _halfOfGridLineThickness;
                        dc.DrawLine(_groupBorderPen, new Point(x, yGridOffset + _actualGridRowHeights[i]), new Point(x, yTo));
                    }
                }
                else
                {
                    //  printing data area doesn't contain group column
                    // Horizontal rectangle
                    dc.DrawRectangle(backBrush, null,
                        new Rect(_actualGridRowHeaderWidth + _gridScale, yGridOffset + _gridScale,
                            gridLineWidth - _actualGridRowHeaderWidth - _gridScale *2.0, _actualGridRowHeights[i] - _gridScale));

                    // Horizontal line
                    x = _actualGridRowHeaderWidth + _gridScale + _groupColumnsWidth.Keys.Where(k => k < nextItemGroupLevel).Sum(index => _groupColumnsWidth[index]);
                    y = yGridOffset + _actualGridRowHeights[i] + _halfOfGridLineThickness;
                    dc.DrawLine(_groupBorderPen, new Point(x, y), new Point(gridLineWidth - _gridScale, y));
                }

                yGridOffset += _actualGridRowHeights[i];
            }

            // Draw cell content
            yGridOffset = yOffset + _actualGridColumnHeaderHeight;
            for (var i1 = minItemNo; i1 <= maxItemNo; i1++)
            {
                xGridOffset = _actualGridRowHeaderWidth;
                var item = _items[i1];
                for (var i2 = 0; i2 < _columns.Length; i2++)
                {
                    var getter = _columnGetters[i2];
                    if (getter == null && item is IDGVList_GroupItem groupItem && _columns[i2].HeaderStringFormat == $"{Constants.GroupColumnNamePrefix}{groupItem.Level - 1}")
                        DrawGroupExpander(_actualGridColumnWidths[i2], _actualGridRowHeights[i1], groupItem.IsExpanded);
                    else if (getter !=null)
                    {
                        var value = getter(item);
                        DrawCellContent(value, _actualGridColumnWidths[i2], _actualGridRowHeights[i1], _columnAlignments[i2] ?? TextAlignment.Left);
                    }
                    xGridOffset += _actualGridColumnWidths[i2];
                }
                yGridOffset += _actualGridRowHeights[i1];
            }

            int GetItemGroupLevel(int index)
            {
                var data = (IList) _viewModel.DGControl.ItemsSource;
                if (index >= data.Count) return -1;
                return ((data[index] as IDGVList_GroupItem)?.Level) ?? int.MaxValue;
            }

            void DrawGroupExpander(double cellWidth, double cellHeight, bool isExpanded)
            {
                var x11 = xGridOffset + 3.0 * _gridScale;
                var y11 = yGridOffset + 3.0 * _gridScale; ;
                // dc.DrawRectangle(Brushes.Aqua, null, new Rect(x11, y11, cellWidth - 5.0 * _gridScale, cellHeight - 5.0 * _gridScale));

                var size = 12.0 * _gridScale;
                x = (cellWidth + _gridScale - size) / 2.0;
                y = (cellHeight + _gridScale - size) / 2.0;
                dc.DrawRoundedRectangle(null, new Pen(Brushes.Black, _gridScale), new Rect(xGridOffset + x, yGridOffset + y, size, size), _gridScale, _gridScale);

                size = 7.0 * _gridScale;
                x = (cellWidth + _gridScale - size) / 2.0;
                y = cellHeight / 2.0;
                dc.DrawRoundedRectangle(Brushes.Black, null, new Rect(xGridOffset + x, yGridOffset + y, size, _gridScale), _halfOfGridLineThickness, _halfOfGridLineThickness);

                if (isExpanded) return;

                x = cellWidth / 2.0; 
                y = (cellHeight + _gridScale - size) / 2.0;
                dc.DrawRoundedRectangle(Brushes.Black, null, new Rect(xGridOffset + x, yGridOffset + y, _gridScale, size), _halfOfGridLineThickness, _halfOfGridLineThickness);
            }

            void DrawCellContent(object value, double cellWidth, double cellHeight, TextAlignment textAlignment, TextTrimming textTrimming = TextTrimming.CharacterEllipsis, double fontSizeScale = 1.0)
            {
                if (value is byte[] bytes)
                {
                    var image = new BitmapImage();
                    using (var stream = new MemoryStream(bytes))
                    {
                        stream.Position = 0;
                        image.BeginInit();
                        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        // image.UriSource = null;
                        image.StreamSource = stream;
                        image.EndInit();
                    }
                    image.Freeze();

                    var xFactor = (cellWidth - 3 * _gridScale) / image.Width;
                    var yFactor = (cellHeight - 3 * _gridScale) / image.Height;
                    var factor = Math.Min(xFactor, yFactor);
                    var xImageOffset = (cellWidth - 3.0 * _gridScale - image.Width * factor) / 2.0;
                    var yImageOffset = (cellHeight - 3.0 * _gridScale - image.Height * factor) / 2.0;

                    dc.DrawImage(image, new Rect(xGridOffset + 2 * _gridScale + xImageOffset,
                            yGridOffset + 2 * _gridScale + yImageOffset, image.Width * factor, image.Height * factor));
                    return;
                }

                if (value is bool)
                {
                    dc.DrawRectangle((bool)value ? _boolYesBrush : _boolNoBrush , null, new Rect(xGridOffset, yGridOffset, cellWidth - _gridScale, cellHeight - _gridScale));
                    return;
                }
                var x11 = xGridOffset + 3.0 * _gridScale;
                var y11 = yGridOffset + 3.0 * _gridScale; 
                // dc.DrawRectangle(Brushes.Aqua, null, new Rect(x11, y11, cellWidth - 5.0 * _gridScale, cellHeight - 5.0 * _gridScale));

                var text = (value ?? "").ToString();
                if (string.IsNullOrEmpty(text))
                    return;

                var formattedText = new FormattedText(text, LocalizationHelper.CurrentCulture, FlowDirection.LeftToRight, _baseTypeface, _fontSize * fontSizeScale, Brushes.Black, _pixelsPerDpi);
                formattedText.Trimming = textTrimming;
                formattedText.MaxTextWidth = cellWidth - 4.5 * _gridScale;
                formattedText.MaxTextHeight = cellHeight - 4.5 * _gridScale;
                formattedText.TextAlignment = textAlignment;

                if (textAlignment == TextAlignment.Left)
                    x = xGridOffset + 3.0 * _gridScale;
                else
                    x = xGridOffset + 2.5 * _gridScale;
                y = yGridOffset + (cellHeight + _gridScale - formattedText.Height) / 2.0; // center
                dc.DrawText(formattedText, new Point(x, y));
            }
        }

        #endregion

        #region ===========  IDisposing  ==============
        public void Dispose()
        {
            _viewModel = null;
            _items = null;
            _columns = null;
            _columnAlignments = null;
            _pageSize = Size.Empty;
            _rowNumbers = null;
            _actualGridRowHeights = null;
            _actualGridColumnWidths = null;
            _itemsPerPage = null;
            _groupColumnsOffset = null;
            _groupColumnsWidth = null;
            _desiredColumnWidths = null;
            _columnGetters = null;
            _boolYesBrush = null;
            _boolNoBrush = null;
        }
        #endregion
    }
}
