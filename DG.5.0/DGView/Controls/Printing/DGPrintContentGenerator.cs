using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using DGCore.Common;
using DGView.Helpers;
using DGView.ViewModels;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace DGView.Controls.Printing
{
    public class DGPrintContentGenerator : IPrintContentGenerator, INotifyPropertyChanged, IDisposable
    {
        public bool StopPrintGeneration { get; set; }

        private int _generatedPages;
        public int GeneratedPages
        {
            get => _generatedPages;
            set
            {
                _generatedPages = value;
                OnPropertiesChanged(nameof(GeneratedPages));
            }
        }

        private DGViewModel _viewModel;
        private readonly SolidColorBrush _headerBackground = new SolidColorBrush(Color.FromRgb(240, 241, 242));

        private IList _items;
        private DataGridColumn[] _columns;
        private Size _pageSize;
        private Thickness _pageMargins;

        private int[] _rowNumbers;
        private double _rowHeaderWidth;
        private int _currentItemNo;
        private DateTime _timeStamp;

        public DGPrintContentGenerator(DGViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void GeneratePrintContent(FixedDocument document, Thickness margins)
        {
            DGHelper.GetSelectedArea(_viewModel.DGControl, out var items, out var columns);
            _items = items;
            _columns = columns;
            _timeStamp = DateTime.Now;

            if (_items.Count == 0)
            {
                new DialogBox(DialogBox.DialogBoxKind.Warning) {Message = "No items to print!", Buttons = new[] {"OK"}}.ShowDialog();
                return;
            }

            PrepareRowNumbers();

            _pageSize = document.DocumentPaginator.PageSize;
            _pageMargins = margins;

            GeneratedPages = 0;
            _currentItemNo = 0;
            while (_currentItemNo < _items.Count && !StopPrintGeneration)
            {
                VisualHelper.DoEvents(DispatcherPriority.Render);
                var fixedPage = new FixedPage();
                fixedPage.Children.Add(GetPageContent());
                var pageContent = new PageContent { Child = fixedPage };
                document.Pages.Add(pageContent);
                GeneratedPages++;
            }
        }

        private FrameworkElement GetPageContent()
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(_pageMargins.Left, _pageMargins.Top, 0, 0),
                Width = _pageSize.Width - _pageMargins.Left - _pageMargins.Right
            };

            var offset = 0.0;

            // Print header
            var headerRow = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
            stackPanel.Children.Add(headerRow);
            var leftHeader = new TextBlock { Text = _viewModel.Title, FontSize = 16, FontWeight = FontWeights.SemiBold };
            headerRow.Children.Add(leftHeader);

            var rightHeader = new TextBlock
            {
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            var format = $"{_timeStamp:G} / Сторінка {GeneratedPages + 1} з " + "{0}";
            var b = new Binding { Path = new PropertyPath("GeneratedPages"), Source = this, StringFormat = format };
            rightHeader.SetBinding(TextBlock.TextProperty, b);
            headerRow.Children.Add(rightHeader);

            offset += ControlHelper.GetFormattedText(leftHeader.Text, leftHeader).Height;

            // Print subHeader
            var subHeaders = _viewModel.Data.GetSubheaders_ExcelAndPrint(_viewModel.StartUpParameters, _viewModel.LastAppliedLayoutName);
            if (subHeaders != null && subHeaders.Length > 0)
            {
                var subHeaderText = string.Join(Environment.NewLine, subHeaders);
                var subHeaderControl = new TextBlock
                    { Text = subHeaderText, HorizontalAlignment = HorizontalAlignment.Left, FontSize = 11 };
                stackPanel.Children.Add(subHeaderControl);
                offset += ControlHelper.GetFormattedText(subHeaderControl.Text, subHeaderControl).Height;
            }

            var gridArea = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 5, 0, 0)
            };
            stackPanel.Children.Add(gridArea);
            offset += 5;

            var pageWidth = _pageSize.Width - _pageMargins.Left - _pageMargins.Right;
            var gridWidth = _columns.Sum(c => c.ActualWidth) + 1 + _rowHeaderWidth;
            var availableHeight = _pageSize.Height - _pageMargins.Top - _pageMargins.Bottom - offset;
            if (gridWidth > pageWidth)
            {
                var gridScale = pageWidth / gridWidth;
                gridArea.LayoutTransform = new ScaleTransform(gridScale, gridScale);
                availableHeight /= gridScale;
            }

            var gridOffset = 0.0;
            var headerElement = PrintGridHeader(out var headerHeight);
            gridArea.Children.Add(headerElement);
            gridOffset += headerHeight;

            var thisPageItems = 0;
            for (var k1 = _currentItemNo; k1 < _items.Count; k1++)
            {
                var rowElement = PrintGridRow(_currentItemNo, out var rowHeight);
                if (thisPageItems == 0 || (gridOffset + rowHeight) <= availableHeight)
                {
                    gridArea.Children.Add(rowElement);
                    gridOffset += rowHeight;
                    _currentItemNo++;
                }
                else
                    break;

                thisPageItems++;
            }
            return stackPanel;
        }

        private void PrepareRowNumbers()
        {
            _rowNumbers = new int[_items.Count];
            var currentNo = 0;
            var currentItem = _items[currentNo];
            var cnt = 0;
            foreach (var item in _viewModel.DGControl.ItemsSource)
            {
                cnt++;
                if (item == currentItem)
                {
                    _rowNumbers[currentNo++] = cnt;
                    if (currentNo >= _items.Count) break;
                    currentItem = _items[currentNo];
                }
                if (currentNo >= _items.Count) break;
            }
            _rowHeaderWidth = ControlHelper.GetFormattedText(
                                  _rowNumbers[_rowNumbers.Length - 1].ToString("N0", LocalizationHelper.CurrentCulture),
                                  _viewModel.DGControl).Width + 5.0;
        }

        private FrameworkElement PrintGridHeader(out double headerHeight)
        {
            var headerBorder = new Border
            {
                BorderThickness = new Thickness(1, 1, 0, 0),
                BorderBrush = Brushes.Black,
                Background = _headerBackground
            };
            var headers = new StackPanel() { Orientation = Orientation.Horizontal };
            headerBorder.Child = headers;

            headerHeight = ControlHelper.GetFormattedText("A", _viewModel.DGControl).Height + 5.0;
            // row header column
            var border = new Border
            {
                BorderThickness = new Thickness(0, 0, 1, 1),
                Width = _rowHeaderWidth,
                BorderBrush = Brushes.Black
            };
            headers.Children.Add(border);

            // data columns
            for (var k = 0; k < _columns.Length; k++)
            {
                var column = _columns[k];
                border = new Border
                {
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Width = column.ActualWidth,
                    BorderBrush = Brushes.Black
                };
                var dgHeader = new TextBlock
                {
                    Text = (column.Header ?? "").ToString(),
                    Width = column.ActualWidth - 1,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Left,
                    Padding = new Thickness(3, 2, 3, 2)
                };
                border.Child = dgHeader;
                headers.Children.Add(border);

                var headerText = ControlHelper.GetFormattedText(dgHeader.Text, dgHeader);
                headerText.MaxTextWidth = column.ActualWidth - 1.0 - dgHeader.Padding.Left - dgHeader.Padding.Top;
                if ((headerText.Height + 5.0) > headerHeight)
                    headerHeight = headerText.Height + 5.0;
            }
            return headerBorder;
        }

        private FrameworkElement PrintGridRow(int rowNo, out double rowHeight)
        {
            // DoEventsHelper.DoEvents();
            var textWrapping = _viewModel.RowViewMode == Enums.DGRowViewMode.WordWrap
                ? TextWrapping.Wrap
                : TextWrapping.NoWrap;

            var rowBorder = new Border
            {
                BorderThickness = new Thickness(1, 0, 0, 0),
                BorderBrush = Brushes.Black,
            };
            var rowPanel = new StackPanel { Orientation = Orientation.Horizontal };
            rowBorder.Child = rowPanel;

            rowHeight = ControlHelper.GetFormattedText("A", _viewModel.DGControl).Height + 5.0;
            // row header column
            var border = new Border
            {
                BorderThickness = new Thickness(0, 0, 1, 1),
                BorderBrush = Brushes.Black,
                Background = _headerBackground,
                Width = _rowHeaderWidth
            };
            rowPanel.Children.Add(border);
            var content = new TextBlock
            {
                Text = _rowNumbers[rowNo].ToString("N0", LocalizationHelper.CurrentCulture),
                Width = _rowHeaderWidth,
                TextWrapping = TextWrapping.NoWrap,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(2)
            };
            border.Child = content;

            // data columns
            for (var k = 0; k < _columns.Length; k++)
            {
                var column = _columns[k];
                border = new Border
                {
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Width = column.ActualWidth,
                    BorderBrush = Brushes.Black
                };

                if (!string.IsNullOrEmpty(column.SortMemberPath))
                {
                    var value = _viewModel.Properties[column.SortMemberPath].GetValue(_items[rowNo]);
                    if (value != null)
                    {
                        content = new TextBlock
                        {
                            Text = value.ToString(),
                            Width = column.ActualWidth - 1,
                            TextWrapping = textWrapping,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Left,
                            Padding = new Thickness(2)
                        };
                        border.Child = content;

                        var cellText = ControlHelper.GetFormattedText(content.Text, content);
                        if (_viewModel.RowViewMode == Enums.DGRowViewMode.WordWrap)
                            cellText.MaxTextWidth = column.ActualWidth - 1.0 - content.Padding.Left - content.Padding.Top;
                        if ((cellText.Height + 5.0) > rowHeight)
                            rowHeight = cellText.Height + 5.0;
                    }
                }

                rowPanel.Children.Add(border);

            }
            return rowBorder;
        }

        #region ===========  INotifyPropertyChanged  ==============
        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public void Dispose()
        {
            _viewModel = null;
            _items = null;
            _columns = null;
            _pageSize = Size.Empty;
            _rowNumbers = null;
        }
}
}
