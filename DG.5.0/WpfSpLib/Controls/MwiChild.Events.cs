using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfSpLib.Controls
{
    public partial class MwiChild
    {
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AddLoadedEvents();
            if (!IsWindowed && WindowState == WindowState.Normal)
                AnimateShow();

            if (Icon == null)
            {
                if (GetTemplateChild("Root") is FrameworkElement root)
                    Icon = root.Resources["DefaultIcon"] as DrawingImage;
            }
        }

        private void AddLoadedEvents(bool onlyRemove = false)
        {
            if (MovingThumb != null)
                MovingThumb.MouseDoubleClick -= OnMovingThumbMouseDoubleClick;

            if (onlyRemove)
            {
                MovingThumb = null;
                return;
            }

            if (GetTemplateChild("MovingThumb") is Thumb movingThumb)
            {
                if (MovingThumb != movingThumb)
                    MovingThumb = movingThumb;
                MovingThumb.MouseDoubleClick += OnMovingThumbMouseDoubleClick;
            }
            else if (MovingThumb != null)
                MovingThumb = null;

            UpdateColorTheme(false, true);

            void OnMovingThumbMouseDoubleClick(object sender, MouseButtonEventArgs e)
            {
                var element = (FrameworkElement)sender;
                element.IsEnabled = false;
                ToggleMaximize(null);
                Dispatcher.InvokeAsync(new Action(() => element.IsEnabled = true), DispatcherPriority.Render); // nexttick action to prevent MoveThumb_OnDragDelta event
            }
        }

        private Window _activatedHost;
        private async void AddVisualParentChangedEvents(bool onlyRemove = false)
        {
            if (_activatedHost != null)
            {
                _activatedHost.KeyDown -= OnHostKeyDown;
                _activatedHost.Closed -= OnHostClosed;
                _activatedHost.Activated -= OnWindowActivated;
                _activatedHost.Deactivated -= OnWindowDeactivated;
                _activatedHost = null;
                return;
            }

            if (!IsWindowed || onlyRemove) return;

            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Normal).Task;
            _activatedHost = Parent as Window;
            _activatedHost.Icon = (GetTemplateChild("IconImage") as Image)?.Source;

            if (!string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(_activatedHost.Title))
                _activatedHost.Title = Title;
            else if (string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(_activatedHost.Title))
                Title = _activatedHost.Title;

            OnHostClosed(_activatedHost, null);
            _activatedHost.KeyDown += OnHostKeyDown;
            _activatedHost.Closed += OnHostClosed;
            _activatedHost.Activated += OnWindowActivated;
            _activatedHost.Deactivated += OnWindowDeactivated;

            void OnHostKeyDown(object sender, KeyEventArgs e)
            {
                if (Keyboard.Modifiers == ModifierKeys.Alt && Keyboard.IsKeyDown(Key.F4)) // Is Alt+F4 key pressed
                {
                    CmdClose.Execute(null);
                    e.Handled = true;
                }
            }
            void OnHostClosed(object sender, EventArgs e)
            {
                if (sender is Window wnd)
                {
                    wnd.Closed -= OnHostClosed;
                    wnd.KeyDown -= OnHostKeyDown;
                    wnd.Activated -= OnWindowActivated;
                    wnd.Deactivated -= OnWindowDeactivated;
                }
            }
            void OnWindowActivated(object sender, EventArgs e) => Activate();
            void OnWindowDeactivated(object sender, EventArgs e) => IsActive = false;
        }
    }
}
