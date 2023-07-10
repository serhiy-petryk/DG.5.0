using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    /// <summary>
    /// Interaction logic for MwiBar.xaml
    /// </summary>
    public class MwiBar : TabControl, INotifyPropertyChanged
    {
        static MwiBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MwiBar), new FrameworkPropertyMetadata(typeof(MwiBar)));
        }

        #region =======  Drag/Drop event handlers ========
        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (((ICollection)ItemsSource).Count > 1)
                DragDropHelper.DragSource_OnPreviewMouseMove(this, e);
        }

        protected override void OnPreviewGiveFeedback(GiveFeedbackEventArgs e)
        {
            base.OnPreviewGiveFeedback(e);
            DragDropHelper.DragSource_OnPreviewGiveFeedback(this, e);
        }

        protected override void OnPreviewDragOver(DragEventArgs e)
        {
            base.OnPreviewDragOver(e);
            DragDropHelper.DropTarget_OnPreviewDragOver(this, e);
        }

        protected override void OnPreviewDragEnter(DragEventArgs e)
        {
            base.OnPreviewDragEnter(e);
            DragDropHelper.DropTarget_OnPreviewDragOver(this, e);
        }

        protected override void OnPreviewDragLeave(DragEventArgs e)
        {
            base.OnPreviewDragLeave(e);
            DragDropHelper.DropTarget_OnPreviewDragLeave(this, e);
        }

        protected async override void OnPreviewDrop(DragEventArgs e)
        {
            base.OnPreviewDrop(e);
            if (!DragDropHelper.Drag_Info.InsertIndex.HasValue || e.Effects != DragDropEffects.Copy) return;
            var sourceData = DragDropHelper.GetDragData(this, e, new [] {GetType().Name});
            if (sourceData.Length != 1) return;

            var item = sourceData[0];
            var insertIndex = DragDropHelper.Drag_Info.InsertIndex.Value + DragDropHelper.Drag_Info.FirstItemOffset;
            var targetData = (ObservableCollection<object>)ItemsSource;
            var oldIndex = targetData.IndexOf(item);
            var newIndex = Math.Min(targetData.Count - 1, insertIndex);
            if (oldIndex != newIndex)
            {
                var tabItem = ItemContainerGenerator.ContainerFromItem(targetData[oldIndex]) as TabItem;
                await Task.WhenAll(AnimationHelper.GetWidthContentAnimations(tabItem, false));
                tabItem.Width = double.NaN;
                tabItem.Opacity = 1.0;
                targetData.Move(oldIndex, newIndex);
                VisualHelper.DoEvents(DispatcherPriority.Render);
                await Task.WhenAll(AnimationHelper.GetWidthContentAnimations(tabItem, true));
                tabItem.Width = double.NaN;
                tabItem.Opacity = 1.0;
            }
        }
        #endregion

        #region =========  Override methods  ==========
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var view = CollectionViewSource.GetDefaultView(ItemsSource);
            view.CollectionChanged += (o, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        if (ItemContainerGenerator.ContainerFromItem(item) is TabItem tabItem)
                        {
                            tabItem.Opacity = 0;
                            tabItem.Dispatcher.BeginInvoke(new Action(async () =>
                            {
                                tabItem.Opacity = 1.0;
                                await Task.WhenAll(AnimationHelper.GetWidthContentAnimations(tabItem, true));
                                tabItem.Width = double.NaN;
                            }), DispatcherPriority.Background);
                        }
                    }
                }
            };

            _doubleButtonGrid = GetTemplateChild("DoubleButtonGrid") as Grid;
            if (GetTemplateChild("PART_ScrollViewer") is ScrollViewer scrollViewer)
                scrollViewer.ScrollChanged += TabScrollViewer_OnScrollChanged;
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            var model = SelectedItem;
            if (model == null) return;

            var item = ItemContainerGenerator.ContainerFromItem(model) as TabItem;
            var scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
            if (item != null && scrollViewer != null)
            {
                var point = item.TranslatePoint(new Point(), scrollViewer);
                scrollViewer.ScrollToHorizontalOffset(point.X + (item.ActualWidth / 2));
            }

            foreach (var a1 in e.RemovedItems)
                Dispatcher.InvokeAsync(() => AnimateTabButton((TabItem) ItemContainerGenerator.ContainerFromItem(a1)), DispatcherPriority.ContextIdle);

            foreach (var a1 in e.AddedItems)
                Dispatcher.InvokeAsync(() => AnimateTabButton((TabItem) ItemContainerGenerator.ContainerFromItem(a1)), DispatcherPriority.ContextIdle);
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (var item in e.NewItems)
                            TabItem_AttachEvents(ItemContainerGenerator.ContainerFromItem(item) as TabItem, false);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        foreach (var item in Items)
                            TabItem_AttachEvents(ItemContainerGenerator.ContainerFromItem(item) as TabItem, false);
                        break;
                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Remove:
                        break;
                    default: throw new Exception("Please, check code");
                }
            }), DispatcherPriority.Render);
        }
        #endregion

        public bool CanScrollLeft => _scrollableWidth >= Tips.SCREEN_TOLERANCE && !Tips.AreEqual(_horizontalOffset, 0);
        public bool CanScrollRight => _scrollableWidth >= Tips.SCREEN_TOLERANCE && !Tips.AreEqual(_horizontalOffset + _viewportWidth, _extentWidth);
        public Visibility ScrollButtonVisibility =>
            _scrollableWidth < (Tips.SCREEN_TOLERANCE + (_doubleButtonGrid.IsVisible ? _doubleButtonGrid.ActualWidth + _doubleButtonGrid.Margin.Left + _doubleButtonGrid.Margin.Right : 0))
                ? Visibility.Collapsed : Visibility.Visible;

        private Grid _doubleButtonGrid;
        private double _scrollableWidth;
        private double _viewportWidth;
        private double _extentWidth;
        private double _horizontalOffset;

        private void TabScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var newScrollableWidth = ((ScrollViewer) sender).ScrollableWidth;
            var newViewportWidth = ((ScrollViewer) sender).ViewportWidth;
            var newExtentWidth = ((ScrollViewer) sender).ExtentWidth;
            var newHorizontalOffset = ((ScrollViewer) sender).HorizontalOffset;
            if (!Tips.AreEqual(_scrollableWidth, newScrollableWidth) || !Tips.AreEqual(_viewportWidth, newViewportWidth) || !Tips.AreEqual(_extentWidth, newExtentWidth) || !Tips.AreEqual(_horizontalOffset, newHorizontalOffset))
            {
                var oldCanScrollLeft = CanScrollLeft;
                var oldCanScrollRight = CanScrollRight;
                _scrollableWidth = newScrollableWidth;
                _viewportWidth = newViewportWidth;
                _extentWidth = newExtentWidth;
                _horizontalOffset = newHorizontalOffset;

                if (oldCanScrollLeft != CanScrollLeft || oldCanScrollRight != CanScrollRight)
                    OnPropertiesChanged(nameof(CanScrollLeft), nameof(CanScrollRight), nameof(ScrollButtonVisibility));
            }
        }

        internal void UpdateTabItems()
        {
            foreach (var item in this.GetVisualChildren().OfType<TabItem>())
                AnimateTabButton(item);
        }

        #region ==============  Tab item  ==============
        private void TabItem_AttachEvents(TabItem item, bool onlyDetach)
        {
            if (item == null) return;
            var child = VisualTreeHelper.GetChildrenCount(item) > 0 ? VisualTreeHelper.GetChild(item, 0) as FrameworkElement : null;

            item.PreviewMouseLeftButtonDown -= TabItem_OnPreviewMouseLeftButtonDown;
            item.MouseEnter -= OnTabItemMouseEnterOrLeave;
            item.MouseLeave -= OnTabItemMouseEnterOrLeave;
            if (child != null)
                child.ToolTipOpening -= OnTabItemToolTipOpening;
            if (child != null && child.ToolTip is ToolTip childToolTip1)
                childToolTip1.Opened -= OnTabItemToolTipOnOpened;

            if (onlyDetach) return;

            item.PreviewMouseLeftButtonDown += TabItem_OnPreviewMouseLeftButtonDown;
            item.MouseEnter += OnTabItemMouseEnterOrLeave;
            item.MouseLeave += OnTabItemMouseEnterOrLeave;
            if (child != null)
                child.ToolTipOpening += OnTabItemToolTipOpening;
            if (child != null && child.ToolTip is ToolTip childToolTip)
                childToolTip.Opened += OnTabItemToolTipOnOpened;

            void OnTabItemMouseEnterOrLeave(object sender, MouseEventArgs e) => AnimateTabButton((TabItem)sender);
            void OnTabItemToolTipOpening(object sender, ToolTipEventArgs e) => ((MwiChild)((FrameworkElement)sender).DataContext)?.RefreshThumbnail();
            void OnTabItemToolTipOnOpened(object sender, RoutedEventArgs e)
            {
                var toolTip = (ToolTip)sender;
                var tabTextBlock = toolTip.PlacementTarget.GetVisualChildren().OfType<TextBlock>().First();
                toolTip.SetCurrentValueSmart(TagProperty, Tips.IsTextTrimmed(tabTextBlock) ? "1" : null);
            }
        }

        private async void TabItem_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mwiChild = ((FrameworkElement) sender).DataContext as MwiChild;
            var element = Mouse.DirectlyOver as FrameworkElement;
            while (element != null && element.Name != "DeleteTabButton")
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;

            if (element != null) // delete button was pressed
            {
                await Task.WhenAll(AnimationHelper.GetWidthContentAnimations((TabItem)sender, false));
                mwiChild?.CmdClose.Execute(null);
            }
            else
                mwiChild?.Activate();

            e.Handled = true;
        }

        private void AnimateTabButton(TabItem tabItem)
        {
            if (tabItem == null || !tabItem.IsVisible) return;

            LinearGradientBrush newBrush;
            if (tabItem.IsSelected)
                newBrush = TryFindResource("Mwi.BarItem.Selected.BackgroundBrush") as LinearGradientBrush;
            else if (tabItem.IsMouseOver)
                newBrush = TryFindResource("Mwi.BarItem.MouseOver.BackgroundBrush") as LinearGradientBrush;
            else
                newBrush = TryFindResource("Mwi.BarItem.BackgroundBrush") as LinearGradientBrush;

            if (newBrush != null)
                tabItem.SetCurrentValueSmart(BackgroundProperty, AnimationHelper.BeginLinearGradientBrushAnimation(newBrush, (LinearGradientBrush) tabItem.Background));
        }
        #endregion

        #region ===========  INotifyPropertyChanged  ==============
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
