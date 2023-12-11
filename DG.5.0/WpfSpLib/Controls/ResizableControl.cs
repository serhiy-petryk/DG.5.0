using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public partial class ResizableControl : ContentControl, INotifyPropertyChanged
    {
        private static int controlId = 0;
        private int _controlId = controlId ++;

        static ResizableControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizableControl), new FrameworkPropertyMetadata(typeof(ResizableControl)));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(ResizableControl), new FrameworkPropertyMetadata(false));
            FocusableProperty.OverrideMetadata(typeof(ResizableControl), new FrameworkPropertyMetadata(true));
        }

        public const string MovingThumbName = "MovingThumb";
        internal static int ZIndexCount = 1;
        public bool LimitPositionToPanelBounds { get; set; } = false;
        public bool IsWindowed => Parent is Window;
        protected Grid HostPanel => VisualTreeHelper.GetParent(this) as Grid;

        public ResizableControl()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            AddContentChangedEvents(oldContent, true);
            AddContentChangedEvents(newContent);
        }

        private static void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            // If unnecessary scrollbars are visible -> remove them
            if (sv.ScrollableWidth > 0 && sv.ExtentWidth <= sv.ActualWidth)
            {
                sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            if (sv.ScrollableHeight > 0 && sv.ExtentHeight <= sv.ActualHeight)
            {
                sv.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            AddVisualParentChangedEvents();
        }

        #region ==========  Focus  ============
        /*protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            Activate();
        }*/

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            Activate();
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);
            if (Equals(e.NewValue, true))
                Activate();
        }

        public virtual void Activate()
        {
            // ??? MwiChild bringToFront: only when mouse is captured: have other issues
            // var isUnderMouse = Mouse.DirectlyOver is FrameworkElement fe && fe.GetVisualParents().FirstOrDefault(a => a == this) != null;
            // if (Focusable && !IsKeyboardFocusWithin && !isUnderMouse) Focus();
            if (Focusable && !IsKeyboardFocusWithin) ControlHelper.SetFocus(this);
            if (!IsWindowed && Panel.GetZIndex(this) != ZIndexCount)
                Panel.SetZIndex(this, ++ZIndexCount);
        }
        #endregion

        #region ==========  Moving && resizing  =========
        protected virtual void MoveThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var mousePosition = IsWindowed ? PointToScreen(Mouse.GetPosition(this)) : Mouse.GetPosition(HostPanel);
            if (mousePosition.X < 0 || mousePosition.Y < 0) return;

            var oldPosition = ActualPosition;
            var newX = oldPosition.X + e.HorizontalChange;
            if (LimitPositionToPanelBounds)
            {
                if (newX + ActualWidth > HostPanel.ActualWidth)
                    newX = HostPanel.ActualWidth - ActualWidth;
                if (newX < 0) newX = 0;
            }

            var newY = oldPosition.Y + e.VerticalChange;
            if (LimitPositionToPanelBounds)
            {
                if (newY + ActualHeight > HostPanel.ActualHeight)
                    newY = HostPanel.ActualHeight - ActualHeight;
                if (newY < 0) newY = 0;
            }

            SetCurrentValue(PositionProperty, new Point(newX, newY));
            e.Handled = true;

            var sv = HostPanel.GetVisualParents().OfType<ScrollViewer>().FirstOrDefault();
            if (sv != null)
            {
                // Smooth scrolling
                if (Math.Abs(e.HorizontalChange) > Tips.SCREEN_TOLERANCE && (newX + ActualWidth) > sv.ActualWidth)
                    sv.ScrollToHorizontalOffset(Math.Max(0, sv.HorizontalOffset + e.HorizontalChange * 0.5));
                if (Math.Abs(e.VerticalChange) > Tips.SCREEN_TOLERANCE && (newY + ActualHeight) > sv.ActualHeight)
                    sv.ScrollToVerticalOffset(Math.Max(0, sv.VerticalOffset + e.VerticalChange * 0.5));
            }
        }

        protected virtual void ResizeThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var mousePosition = IsWindowed ? PointToScreen(Mouse.GetPosition(this)) : Mouse.GetPosition(HostPanel);
            if ((mousePosition.X < 0 || mousePosition.Y < 0) && !LimitPositionToPanelBounds)
                return;

            var thumb = (Thumb)sender;
            if (thumb.HorizontalAlignment == HorizontalAlignment.Left)
                OnResizeLeft(e.HorizontalChange);
            else if (thumb.HorizontalAlignment == HorizontalAlignment.Right)
                OnResizeRight(e.HorizontalChange);

            if (thumb.VerticalAlignment == VerticalAlignment.Top)
                OnResizeTop(e.VerticalChange);
            else if (thumb.VerticalAlignment == VerticalAlignment.Bottom)
                OnResizeBottom(e.VerticalChange);

            e.Handled = true;
        }

        private void OnResizeLeft(double horizontalChange)
        {
            var oldPosition = ActualPosition;
            var change = Math.Min(horizontalChange, ActualWidth - MinWidth);

            if (LimitPositionToPanelBounds)
            {
                if (oldPosition.X + change < 0) change = -oldPosition.X;
                if ((ActualWidth - change) > MaxWidth) change = ActualWidth - MaxWidth;
            }

            if (!Tips.AreEqual(0.0, change))
            {
                SetCurrentValue(WidthProperty, ActualWidth - change);
                SetCurrentValue(PositionProperty, new Point(oldPosition.X + change, oldPosition.Y));
            }
        }
        private void OnResizeTop(double verticalChange)
        {
            var oldPosition = ActualPosition;
            var change = Math.Min(verticalChange, ActualHeight - MinHeight);

            if (LimitPositionToPanelBounds)
            {
                if (oldPosition.Y + change < 0) change = -oldPosition.Y;
                if ((Height - change) > MaxHeight) change = Height - MaxHeight;
            }

            if (!Tips.AreEqual(0.0, change))
            {
                SetCurrentValue(HeightProperty, ActualHeight - change);
                SetCurrentValue(PositionProperty, new Point(oldPosition.X, oldPosition.Y + change));
            }
        }
        private void OnResizeRight(double horizontalChange)
        {
            var change = Math.Min(-horizontalChange, ActualWidth - MinWidth);
            if (LimitPositionToPanelBounds)
            {
                var oldX = ActualPosition.X;
                if ((ActualWidth - change) > MaxWidth)
                    change = ActualWidth - MaxWidth;
                if ((oldX + ActualWidth - change) > HostPanel.ActualWidth)
                    change = oldX + ActualWidth - HostPanel.ActualWidth;
            }

            if (!Tips.AreEqual(0.0, change))
                SetCurrentValue(WidthProperty, ActualWidth - change);
        }
        private void OnResizeBottom(double verticalChange)
        {
            var change = Math.Min(-verticalChange, ActualHeight - MinHeight);
            if (LimitPositionToPanelBounds)
            {
                var oldY = ActualPosition.Y;
                if ((ActualHeight - change) > MaxHeight)
                    change = ActualHeight - MaxHeight;
                if ((oldY + ActualHeight - change) > HostPanel.ActualHeight)
                    change = oldY + ActualHeight - HostPanel.ActualHeight;
            }

            if (!Tips.AreEqual(0.0, change))
                SetCurrentValue(HeightProperty, ActualHeight - change);
        }
        #endregion

        #region ==========  Properties  ===============

        public Point ActualPosition => IsWindowed
            ? new Point(((Window) Parent).Left, ((Window) Parent).Top)
            : new Point(Margin.Left, Margin.Top);

        //======================
        public static readonly DependencyProperty MovingThumbProperty = DependencyProperty.Register("MovingThumb",
            typeof(Thumb), typeof(ResizableControl), new PropertyMetadata(null, OnMovingThumbValueChanged));
        public Thumb MovingThumb
        {
            get => (Thumb)GetValue(MovingThumbProperty);
            set => SetValue(MovingThumbProperty, value);
        }

        private static void OnMovingThumbValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ResizableControl control)
            {
                if (e.OldValue is Thumb oldThumb)
                    oldThumb.DragDelta -= control.MoveThumb_OnDragDelta;
                if (e.NewValue is Thumb newThumb)
                    newThumb.DragDelta += control.MoveThumb_OnDragDelta;
            }
        }
        //================================
        public static readonly DependencyProperty ResizableProperty = DependencyProperty.Register(nameof(Resizable),
            typeof(bool), typeof(ResizableControl), new FrameworkPropertyMetadata(true));
        public bool Resizable
        {
            get => (bool)GetValue(ResizableProperty);
            set => SetValue(ResizableProperty, value);
        }
        //=========================
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register("Position", typeof(Point?), typeof(ResizableControl), new FrameworkPropertyMetadata(null, OnPositionValueChanged));
        public Point? Position
        {
            get => (Point?)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }
        private static void OnPositionValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (Equals(e.NewValue, e.OldValue) || !(e.NewValue is Point newPosition)) return;

            var resizableControl = (ResizableControl)d;
            if (resizableControl.Parent is Window wnd)
            {
                var newTop = Math.Min(SystemParameters.PrimaryScreenHeight, newPosition.Y);
                if (!Tips.AreEqual(newTop, wnd.Top))
                    wnd.Top = newTop;
                var newLeft = Math.Min(SystemParameters.PrimaryScreenWidth, newPosition.X);
                if (!Tips.AreEqual(newLeft, wnd.Left))
                    wnd.Left = newLeft;
            }
            else
                resizableControl.Margin = new Thickness(newPosition.X, newPosition.Y, -1, -1);

            resizableControl.OnPropertiesChanged(nameof(ActualPosition));
        }
        //================================
        public static readonly DependencyProperty ShadowVisibilityProperty = DependencyProperty.Register(nameof(ShadowVisibility),
            typeof(Visibility), typeof(ResizableControl), new FrameworkPropertyMetadata(Visibility.Visible));
        public Visibility ShadowVisibility
        {
            get => (Visibility)GetValue(ShadowVisibilityProperty);
            set => SetValue(ShadowVisibilityProperty, value);
        }
        #endregion

        #region =================  INotifyPropertyChanged  ==================
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
