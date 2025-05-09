using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace WpfSpLib.Helpers
{
    public static class DragDropHelper
    {
        public static readonly StartDragInfo StartDrag_Info = new StartDragInfo();
        public static readonly DragInfo Drag_Info = new DragInfo();

        #region ==============  Examples of event handlers  ==============
        public static void DragSource_OnPreviewMouseMove(object sender, MouseEventArgs e, string dragDropFormat = null, Func<object[], bool> allowDrag = null)
        {
            if (_isDragging) return;
            if (!(sender is ItemsControl itemsControl) || e.LeftButton == MouseButtonState.Released)
            {
                StartDrag_Info.Clear(); // (args = null) => !!! important for row selection after drop in GridView
                return;
            }

            var selectedItems = GetSelectedItems();
            var itemsHost = itemsControl.GetItemsHost();
            if (!itemsHost.IsMouseOverElement(e.GetPosition))
            {
                StartDrag_Info.Clear();
                return;
            }

            if (selectedItems.Select(o => (itemsControl.ItemContainerGenerator.ContainerFromItem(o) as FrameworkElement)
                    ?.IsMouseOverElement(e.GetPosition)).All(o => o != true) &&
                itemsControl.GetElementsUnderMouse(e.GetPosition).OfType<FrameworkElement>()
                    .FirstOrDefault(o => selectedItems.Contains(o.DataContext)) == null)
            {
                StartDrag_Info.Clear();
                return;
            }

            if (allowDrag != null && !allowDrag(selectedItems))
            {
                StartDrag_Info.Clear();
                return;
            }

            if (!Equals(itemsControl, StartDrag_Info.DragSource))
            {
                StartDrag_Info.Init(itemsControl, e);
                e.Handled = true;
                return;
            }

            var mousePosition = e.GetPosition(itemsControl);
            if (Math.Abs(mousePosition.X - StartDrag_Info.DragStart.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(mousePosition.Y - StartDrag_Info.DragStart.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (itemsControl is DataGrid dataGrid)
                    dataGrid.CommitEdit();

                var dataObject = new DataObject();
                dataObject.SetData(dragDropFormat ?? sender.GetType().Name, selectedItems);
                try
                {
                    _isDragging = true;
                    var result = DragDrop.DoDragDrop(itemsControl, dataObject, DragDropEffects.Copy);
                }
                finally
                {
                    _isDragging = false;
                    StartDrag_Info.Clear();
                    Drag_Info.Clear();
                    ResetDragDrop(null);
                    e.Handled = true;
                }
            }

            object[] GetSelectedItems()
            {
                if (itemsControl is MultiSelector multiSelector)
                    return multiSelector.SelectedItems.OfType<object>().ToArray();

                if (itemsControl is Selector selector)
                {
                    if (selector.SelectedItem != null)
                        return new[] { selector.SelectedItem };
                    return new object[0];
                }

                if (itemsControl is TreeView treeView)
                {
                    if (treeView.SelectedItem != null)
                        return new[] { treeView.SelectedItem };
                    return new object[0];
                }

                throw new Exception($"Trap! {itemsControl.GetType().Name} is not supported");
            }
        }

        public static void DragSource_OnPreviewGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if (e.Effects == DragDropEffects.Copy)
            {
                e.UseDefaultCursors = false;
                Mouse.SetCursor(Cursors.Arrow);
            }
            else
                e.UseDefaultCursors = true;

            e.Handled = true;
        }

        public static void DropTarget_OnPreviewDragOver(object sender, DragEventArgs e, IEnumerable<string> dragDropFormats = null,
            Action<object[], ItemsControl, DragEventArgs> afterDefiningInsertIndex = null)
        {
            Drag_Info.LastDragLeaveObject = null;
            var dragData = GetDragData(sender, e, dragDropFormats ?? new[] { sender.GetType().Name });
            if (dragData.Length == 0)
            {
                ResetDragDrop(e);
                return;
            }

            var control = (ItemsControl)sender;
            DefineInsertIndex(control, e);

            if (Drag_Info.InsertIndex.HasValue && Drag_Info.DragDropEffect != DragDropEffects.None)
                afterDefiningInsertIndex?.Invoke(dragData, control, e);

            if (!Drag_Info.InsertIndex.HasValue || Drag_Info.DragDropEffect == DragDropEffects.None)
            {
                ResetDragDrop(e);
                return;
            }

            if (afterDefiningInsertIndex == null && Drag_Info.IsBottomOrRightEdge)
            {  // default afterDefiningInsertIndex method
                Drag_Info.InsertIndex++;
                Drag_Info.IsBottomOrRightEdge = false;
            }

            if (_dropTargetAdorner == null)
                _dropTargetAdorner = new DropTargetInsertionAdorner(control);
            _dropTargetAdorner.InvalidateVisual();

            if (_dragAdorner == null)
                _dragAdorner = new DragAdorner(Window.GetWindow(control).Content as UIElement, dragData);
            _dragAdorner.UpdateUI(e, control);

            CheckScroll(control, e);
        }

        public static void DropTarget_OnPreviewDragLeave(object sender, DragEventArgs e)
        {
            Drag_Info.LastDragLeaveObject = sender;
            ((FrameworkElement)sender).Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Equals(Drag_Info.LastDragLeaveObject, sender))
                    ResetDragDrop(e);
                Drag_Info.LastDragLeaveObject = null;
            }), DispatcherPriority.Normal);
        }

        public static async Task DropTarget_OnPreviewDrop(object sender, DragEventArgs e, string[] dragDropFormats = null, Action<object[]> converter = null)
        {
            if (!Drag_Info.InsertIndex.HasValue) return;

            var sourceData = GetDragData(sender, e, dragDropFormats ?? new[] { sender.GetType().Name });
            if (sourceData.Length > 0)
            {
                converter?.Invoke(sourceData);
                var dropControl = (ItemsControl)sender;
                var insertingControl = dropControl;
                var visualItems = dropControl.GetAllVisualItems().ToArray();
                var targetList = (IList)(dropControl.ItemsSource ?? dropControl.Items);
                var insertIndex = 0;
                if (visualItems.Length > 0)
                {
                    var tempIndex = Drag_Info.InsertIndex.Value;
                    var insertItem = visualItems[Math.Min(visualItems.Length - 1, tempIndex)];
                    insertingControl = insertItem.ItemsControl;
                    targetList = (IList)(insertItem.ItemsControl.ItemsSource ?? insertItem.ItemsControl.Items);
                    insertIndex = targetList.IndexOf(insertItem.VisualElement.DataContext) + (Drag_Info.IsBottomOrRightEdge ? 1 : 0);
                    if (insertIndex == -1) // TabControl
                        insertIndex = targetList.IndexOf(insertItem.VisualElement);
                    if (insertIndex == -1)
                        throw new Exception("Trap!!! Drop method");
                    if (visualItems.Length <= tempIndex) insertIndex++;
                }

                await insertingControl.AddOrReorderItems(sourceData, insertIndex);
            }

            // Mouse.OverrideCursor = null;
            e.Handled = true;
        }

        #endregion

        #region =======  Public methods  ========
        public static object[] GetDragData(object sender, DragEventArgs e, IEnumerable<string> dragDropFormats)
        {
            if (dragDropFormats == null)
                dragDropFormats = new[] { sender.GetType().Name };
            foreach (var format in dragDropFormats)
                if (e.Data.GetDataPresent(format))
                    return (e.Data.GetData(format) as object[]) ?? new object[0];
            return new object[0];
        }
        #endregion

        private static DropTargetInsertionAdorner _dropTargetAdorner;
        private static DragAdorner _dragAdorner;
        private static bool _isDragging;

        #region =============  Private methods  ================
        private static void CheckScroll(ItemsControl o, DragEventArgs e)
        {
            var scrollViewer = o.GetVisualChildren<ScrollViewer>().FirstOrDefault();
            if (scrollViewer != null)
            {
                const double scrollMargin = 25.0;
                var scrollStep = scrollViewer.Content is TabPanel ? 10.0 : 1.0;
                var position = e.GetPosition(scrollViewer);
                if (position.X >= scrollViewer.ActualWidth - scrollMargin && scrollViewer.HorizontalOffset <
                    scrollViewer.ExtentWidth - scrollViewer.ViewportWidth)
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + scrollStep);
                else if (position.X < scrollMargin && scrollViewer.HorizontalOffset > 0)
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - scrollStep);
                else if (position.Y >= scrollViewer.ActualHeight - scrollMargin && scrollViewer.VerticalOffset <
                         scrollViewer.ExtentHeight - scrollViewer.ViewportHeight)
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + scrollStep);
                else if (position.Y < scrollMargin && scrollViewer.VerticalOffset > 0)
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - scrollStep);
            }
        }

        internal static Orientation GetItemsPanelOrientation(ItemsControl itemsControl)
        {
            if (itemsControl is TabControl)
            {
                var tabControl = (TabControl)itemsControl;
                return tabControl.TabStripPlacement == Dock.Left || tabControl.TabStripPlacement == Dock.Right
                    ? Orientation.Vertical
                    : Orientation.Horizontal;
            }

            var panel = itemsControl.GetItemsHost();
            var orientationProperty = panel.GetType().GetProperty("Orientation", typeof(Orientation));

            if (orientationProperty != null)
                return (Orientation)orientationProperty.GetValue(panel, null);

            throw new Exception("Trap! Can't define item panel orientation");
        }

        private static void DefineInsertIndex(ItemsControl control, DragEventArgs e)
        {
            var orientation = GetItemsPanelOrientation(control);
            var panel = control.GetItemsHost();
            Drag_Info.FirstItemOffset = panel.Children.Count == 0 ? 0 : control.ItemContainerGenerator.IndexFromContainer(panel.Children[0]);
            Drag_Info.DragDropEffect = StartDrag_Info.DragSource == control ? DragDropEffects.Move : DragDropEffects.Copy;
            SetItemsRects(control);
            var mousePosition = e.GetPosition(control);
            for (var i = 0; i < Drag_Info.ItemsRects.Count; i++)
            {
                if (Drag_Info.ItemsRects[i].Contains(mousePosition))
                {
                    Drag_Info.InsertIndex = i;
                    if (orientation == Orientation.Vertical)
                        Drag_Info.IsBottomOrRightEdge = mousePosition.Y >= (Drag_Info.ItemsRects[i].Top + Drag_Info.ItemsRects[i].Height * 0.5);
                    else
                        Drag_Info.IsBottomOrRightEdge = mousePosition.X >= (Drag_Info.ItemsRects[i].Left + Drag_Info.ItemsRects[i].Width * 0.5);
                    return;
                }
            }

            var headerHost = control.GetHeaderHost();
            if (headerHost != null && headerHost.IsMouseOverElement(e.GetPosition))
            {
                Drag_Info.InsertIndex = 0;
                Drag_Info.IsBottomOrRightEdge = false;
                return;
            }

            if (Drag_Info.ItemsRects.Count == 0 && panel.IsMouseOverElement(e.GetPosition))
            {
                Drag_Info.InsertIndex = 0;
                Drag_Info.IsBottomOrRightEdge = false;
                return;
            }

            if (Drag_Info.ItemsRects.Count > 0 && panel.IsMouseOverElement(e.GetPosition))
            {
                var r = Drag_Info.ItemsRects[Drag_Info.ItemsRects.Count - 1];
                if ((orientation == Orientation.Vertical && r.Y < mousePosition.Y && r.X <= mousePosition.X &&
                     (r.X + r.Width) >= mousePosition.X) ||
                    (orientation == Orientation.Horizontal && r.X < mousePosition.X && r.Y <= mousePosition.Y &&
                     (r.Y + r.Height) >= mousePosition.Y))
                {
                    Drag_Info.InsertIndex = Drag_Info.ItemsRects.Count - 1;
                    Drag_Info.IsBottomOrRightEdge = true;
                    return;
                }
            }

            Drag_Info.InsertIndex = null;
        }

        private static void ResetDragDrop(DragEventArgs e)
        {
            if (_dropTargetAdorner != null)
            {
                _dropTargetAdorner.Detach();
                _dropTargetAdorner = null;
            }
            if (_dragAdorner != null)
            {
                _dragAdorner.Detach();
                _dragAdorner = null;
            }

            Drag_Info.DragDropEffect = DragDropEffects.None;
            Drag_Info.InsertIndex = null;

            if (e != null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private static void SetItemsRects(ItemsControl control)
        {
            var items = control.GetAllVisualItems().ToArray();
            var rects = new List<Rect>();
            foreach (var item in items)
                rects.Add(item.VisualElement.GetVisibleRect(control));

            var orientation = GetItemsPanelOrientation(control);
            for (var i = 0; i < (rects.Count - 1); i++)
            {
                var r1 = rects[i];
                var r2 = rects[i + 1];
                if (r1.IsEmpty || r2.IsEmpty) continue;
                if (orientation == Orientation.Vertical)
                {
                    var delta = r2.Y - (r1.Y + r1.Height);
                    if (delta > 0)
                        rects[i + 1] = new Rect(rects[i + 1].X, rects[i + 1].Y - delta, rects[i + 1].Width, rects[i + 1].Height + delta);
                }
                else
                {
                    var delta = r2.X - (r1.X + r1.Width);
                    if (delta > 0.0)
                        rects[i + 1] = new Rect(rects[i + 1].X - delta, rects[i + 1].Y, rects[i + 1].Width + delta, rects[i + 1].Height);
                }
            }

            Drag_Info.ItemsRects = rects;
        }
        #endregion

        #region =============  Helper classes  ===============
        public class StartDragInfo
        {
            internal Point DragStart { get; private set; }
            public ItemsControl DragSource { get; private set; }
            public void Init(ItemsControl dragSource, MouseEventArgs e)
            {
                DragSource = dragSource;
                DragStart = e.GetPosition(dragSource);
            }
            public void Clear()
            {
                DragSource = null;
                DragStart = new Point(-100, -100);
            }
        }

        public class DragInfo
        {
            public object LastDragLeaveObject;
            public int? InsertIndex;
            public List<Rect> ItemsRects;
            public bool IsBottomOrRightEdge;
            public int FirstItemOffset;
            public DragDropEffects DragDropEffect;
            public FrameworkElement GetHoveredItem(ItemsControl control)
            {
                if (!InsertIndex.HasValue) return null;
                var items = control.GetAllVisualItems().ToArray();
                if (InsertIndex.Value < items.Length)
                    return items[InsertIndex.Value].VisualElement;
                if (InsertIndex.Value == items.Length && items.Length > 0)
                    return items[items.Length - 1].VisualElement;
                return null;
            }

            public void Clear()
            {
                LastDragLeaveObject = null;
                InsertIndex = null;
            }
        }
        #endregion
    }
}
