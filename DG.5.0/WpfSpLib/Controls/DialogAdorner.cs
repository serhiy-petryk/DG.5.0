using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public interface IHasDialogHost
    {
        FrameworkElement GetDialogHost();
    }

    public class DialogAdorner : AdornerControl
    {
        static DialogAdorner()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DialogAdorner), new FrameworkPropertyMetadata(typeof(DialogAdorner)));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(DialogAdorner), new FrameworkPropertyMetadata(false));
            FocusableProperty.OverrideMetadata(typeof(DialogAdorner), new FrameworkPropertyMetadata(false));
        }

        #region =========  Static Private Section  ==============

        private static FrameworkElement GetHost(FrameworkElement host)
        {
            if (host == null) host = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
            if (host is IHasDialogHost dialogHost)
                host = dialogHost.GetDialogHost();
            if (host is Window wnd && wnd.Content is FrameworkElement fe) return fe;
            return host;
        }

        private static CommandBinding _closeCommand = new CommandBinding(ApplicationCommands.Close, async (s, e1) =>
        {
            var content = s as FrameworkElement;
            var adorner = content.GetVisualParents<DialogAdorner>().FirstOrDefault();
            if (adorner == null || content == null) return;

            await Task.WhenAll(AnimationHelper.GetContentAnimations(content, false));

            adorner._panel.Children.Remove(content);
            adorner.ContentClosed?.Invoke(adorner, content);

            if (adorner._panel.Children.Count == 0)
                RemoveAdorner(adorner);
            else if (adorner._panel.Children[adorner._panel.Children.Count - 1] is UIElement element)
                ControlHelper.SetFocus(element);
        });
        #endregion

        #region =========  Property Section  ==============
        public event EventHandler AllContentClosed;
        public event EventHandler<FrameworkElement> ContentClosed;
        public bool CloseOnClickBackground { get; set; } = true;

        private Brush _background = new SolidColorBrush(Color.FromArgb(0x77, 0x77, 0x77, 0x77));
        public Brush Background
        {
            get => _background;
            set
            {
                _background = value;
                if (Child is Panel panel)
                    panel.Background = value;
            }
        }

        private Grid _panel => Child as Grid;
        public FrameworkElement Host { get; }
        #endregion

        public DialogAdorner(FrameworkElement host = null) : base(GetHost(host))
        {
            Host = GetHost(host);
            if (AdornerLayer == null)
                throw new Exception("DialogAdorner constructor error! AdornerLevel can't be null");

            Child = new Grid
            {
                Background = _background,
                Focusable = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            KeyboardNavigation.SetTabNavigation(Child, KeyboardNavigationMode.Cycle);
            KeyboardNavigation.SetDirectionalNavigation(Child, KeyboardNavigationMode.Cycle);
        }

        #region ============  Public Section  ===============
        private readonly SemaphoreSlim _openSemaphore = new SemaphoreSlim(1, 1);
        public async Task ShowContentAsync(FrameworkElement content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            await _openSemaphore.WaitAsync();
            try
            {
                if (_panel.Children.Count == 0)
                {
                    AdornerLayer.Add(this);
                    _panel.MouseLeftButtonDown += Panel_MouseLeftButtonDown;
                    await _panel.BeginAnimationAsync(OpacityProperty, 0.0, 1.0, AnimationHelper.AnimationDuration);
                }
                _panel.Children.Add(content);
            }
            finally
            {
                _openSemaphore.Release();
            }

            content.CommandBindings.Add(_closeCommand);
            content.Visibility = Visibility.Hidden;

            await content.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded).Task;

            ControlHelper.SetFocus(content);

            if (content.HorizontalAlignment != HorizontalAlignment.Left)
                content.HorizontalAlignment = HorizontalAlignment.Left;
            if (content.VerticalAlignment != VerticalAlignment.Top)
                content.VerticalAlignment = VerticalAlignment.Top;

            var xFactor = 1.0;
            var yFactor = 1.0;
            if (RenderTransform is MatrixTransform matrix)
            {
                xFactor = matrix.Matrix.M11;
                yFactor = matrix.Matrix.M22;
            }

            var viewportWidth = ActualWidth;
            var viewportHeight = ActualHeight;
            var xOffset = 0.0;
            var yOffset = 0.0;
            if (this.GetVisualParents<ScrollViewer>().FirstOrDefault() is ScrollViewer scroll)
            {
                viewportWidth = scroll.ViewportWidth / xFactor;
                viewportHeight = scroll.ViewportHeight / yFactor;
                xOffset = scroll.ContentHorizontalOffset/ xFactor;
                yOffset = scroll.ContentVerticalOffset/ yFactor;
            }

            var left = Math.Round(Math.Max(0, (viewportWidth - content.ActualWidth) / 2)) + xOffset;
            var top = Math.Round(Math.Max(0, (viewportHeight - content.ActualHeight) / 2)) + yOffset;
            content.Margin = new Thickness(left, top, 0, 0);
            content.Visibility = Visibility.Visible;

            await Task.WhenAll(AnimationHelper.GetContentAnimations(content, true));
        }

        public async void ShowContent(FrameworkElement content)
        {
            await ShowContentAsync(content);
            ControlHelper.SetFocus(content);
        }

        public void ShowContentDialog(FrameworkElement content)
        {
            ShowContent(content);

            var frame = new DispatcherFrame();
            AllContentClosed += OnAllContentClosed;
            Dispatcher.PushFrame(frame);

            void OnAllContentClosed(object sender, EventArgs e)
            {
                AllContentClosed -= OnAllContentClosed;
                frame.Continue = false;
            }
        }

        public Task WaitUntilClosed()
        {
            var tcs = new TaskCompletionSource<bool>(new Action(() => { }));
            if (_panel.Children.Count != 0)
                AllContentClosed += OnAllContentClosed;
            else
                tcs.SetResult(false);
            return tcs.Task;

            void OnAllContentClosed(object sender, EventArgs e)
            {
                AllContentClosed -= OnAllContentClosed;
                tcs.SetResult(true);
            }
        }
        #endregion

        #region  ==============  Private Section  ==============
        private static void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var adorner = ((FrameworkElement)sender).GetVisualParents<DialogAdorner>().FirstOrDefault();
            if (adorner != null && adorner.CloseOnClickBackground)
            {
                foreach (var content in adorner._panel.Children.OfType<FrameworkElement>().Where(c => c.Visibility == Visibility.Visible))
                {
                    var mousePoint = e.GetPosition(content);
                    var isUnderContent = new Rect(0, 0, content.ActualWidth, content.ActualHeight).Contains(mousePoint);
                    if (isUnderContent) return;
                }

                RemoveAdorner(adorner);
            }
        }

        private static async void RemoveAdorner(DialogAdorner adorner)
        {
            adorner._panel.MouseLeftButtonDown -= Panel_MouseLeftButtonDown;

            var contents = adorner._panel.Children.OfType<FrameworkElement>().ToArray();
            await Task.WhenAll(contents.SelectMany(c => AnimationHelper.GetContentAnimations(c, false)));
            adorner._panel.Children.RemoveRange(0, adorner._panel.Children.Count);
            foreach (var content in contents)
                adorner.ContentClosed?.Invoke(adorner, content);

            await adorner._panel.BeginAnimationAsync(OpacityProperty, 1.0, 0.0, AnimationHelper.AnimationDuration);
            adorner.AdornerLayer?.Remove(adorner);

            adorner.AllContentClosed?.Invoke(adorner, EventArgs.Empty);
        }
    }
    #endregion
}
