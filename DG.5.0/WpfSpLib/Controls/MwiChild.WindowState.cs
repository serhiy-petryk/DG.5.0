using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public partial class MwiChild
    {
        //  ===============  MwiChild State ===============
        private Point _scale => LayoutTransform is ScaleTransform transform ? new Point(transform.ScaleX, transform.ScaleX) : new Point(1, 1);
        private Size _lastNormalSize;
        private Point _attachedPosition;
        private Point _detachedPosition;
        private WindowState? _beforeMinimizedState { get; set; } // Previous state of minimized window.

        //==========================
        private void ToggleMaximize(object p) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        internal void ToggleMinimize(object obj)
        {
            if (WindowState != WindowState.Minimized)
                RefreshThumbnail();

            if (IsWindowed)
            {
                if (((Window)Parent).WindowState == WindowState.Minimized)
                    ((Window)Parent).WindowState = _beforeMinimizedState ?? WindowState.Normal;
                else
                {
                    _beforeMinimizedState = ((Window)Parent).WindowState;
                    ((Window)Parent).WindowState = WindowState.Minimized;
                }
            }
            else if (WindowState == WindowState.Minimized)
            {
                if (Visibility != Visibility.Visible)
                    Visibility = Visibility.Visible;
                WindowState = _beforeMinimizedState ?? WindowState.Normal;
            }
            else
            {
                _beforeMinimizedState = WindowState;
                WindowState = WindowState.Minimized;
            }
        }

        private async void ToggleDetach(object obj)
        {
            if (MwiContainer == null) return;

            if (WindowState == WindowState.Normal) SaveActualRectangle();
            await AnimateHide();

            if (IsWindowed)
            {
                var wnd = (Window)Parent;
                wnd.Close();
                wnd.Content = null;
                LayoutTransform = Transform.Identity;
                MwiContainer.MwiPanel.Children.Add(this);
                Activate();
                Position = WindowState == WindowState.Maximized ? new Point(0, 0) : _attachedPosition;
            }
            else
            {
                LayoutTransform = GetDialogHost().LayoutTransform;
                Margin = new Thickness();
                MwiContainer.MwiPanel.Children.Remove(this);
                var wnd = new Window { Style = FindResource("HeadlessWindow") as Style, Content = this};
                wnd.Show();
                Activate();

                // Refresh ScrollBar scrolling
                MwiContainer.ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                MwiContainer.ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

                _detachedPosition = new Point(
                    SystemParameters.WorkArea.X + Math.Max(0, (SystemParameters.WorkArea.Width - _lastNormalSize.Width * _scale.X) / 2),
                    SystemParameters.WorkArea.Y + Math.Max(0, (SystemParameters.WorkArea.Height - _lastNormalSize.Height * _scale.Y) / 2));
                Position = null; // Reset position (need for maximized window or when positions before and after detach are equal)
            }

            OnWindowStateValueChanged(this, new DependencyPropertyChangedEventArgs(WindowStateProperty, WindowState, WindowState));
            OnPropertiesChanged(nameof(IsWindowed));
            // BeginAnimation(OpacityProperty, new DoubleAnimation(0.0, 1.0, AnimationHelper.AnimationDuration));
            await this.BeginAnimationAsync(OpacityProperty, 0.0, 1.0);
        }

        private void OnScaleValueChanged(object sender, EventArgs e)
        {
            if (IsWindowed && WindowState == WindowState.Maximized)
            {
                Position = new Point(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Top);
                Width = SystemParameters.WorkArea.Width / _scale.X;
                Height = SystemParameters.WorkArea.Height / _scale.Y;
            }
        }

        #region ==============  OnWindowStateValueChanged  =================
        private static void OnWindowStateValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
            ((MwiChild)sender).WindowStateValueChanged((WindowState)e.NewValue, (WindowState)e.OldValue);
        private async void WindowStateValueChanged(WindowState newWindowState, WindowState previousWindowState)
        {
            if (!IsLoaded) return;

            if (previousWindowState == WindowState.Maximized)
            {
                BindingOperations.ClearBinding(this, WidthProperty);
                BindingOperations.ClearBinding(this, HeightProperty);
            }

            var isDetachEvent = previousWindowState == newWindowState;

            if (previousWindowState == WindowState.Normal && !isDetachEvent)
                SaveActualRectangle();

            if (IsWindowed)
            {
                if (newWindowState == WindowState.Normal)
                {
                    Width = _lastNormalSize.Width;
                    Height = _lastNormalSize.Height;
                    Position = _detachedPosition;
                }

                // ??? Cannot reproduce
                if (newWindowState != WindowState.Minimized && ((Window)Parent).WindowState != WindowState.Normal)
                    ((Window)Parent).WindowState = WindowState.Normal;

                if (newWindowState == WindowState.Maximized)
                    OnScaleValueChanged(null, null);
            }

            if (!IsWindowed && !isDetachEvent)
                await AnimateWindowState(previousWindowState);

            if (!IsWindowed && newWindowState == WindowState.Maximized)
            {
                SetBinding(WidthProperty, new Binding("ActualWidth") { Source = HostPanel });
                SetBinding(HeightProperty, new Binding("ActualHeight") { Source = HostPanel });
            }

            MwiContainer?.InvalidateLayout();
        }

        private void SaveActualRectangle()
        {
            _lastNormalSize = new Size(ActualWidth, ActualHeight);
            SaveActualPosition();
        }
        private void SaveActualPosition()
        {
            if (IsWindowed)
                _detachedPosition = ActualPosition;
            else
                _attachedPosition = ActualPosition;
        }
        #endregion
    }
}
