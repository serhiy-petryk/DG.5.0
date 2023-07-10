using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public partial class MwiChild
    {
        internal Task SetRectWithAnimation(Rect to, double fromOpacity, double toOpacity)
        {
            var tasks = new List<Task>
            {
                this.BeginAnimationAsync(PositionProperty, ActualPosition, new Point(to.X, to.Y)),
                this.BeginAnimationAsync(WidthProperty, ActualWidth, to.Width),
                this.BeginAnimationAsync(HeightProperty, ActualHeight, to.Height),
                this.BeginAnimationAsync(OpacityProperty, fromOpacity, toOpacity)
            };

            Position = ActualPosition; // to allow maximize ThemeSelector dialog
            return Task.WhenAll(tasks.ToArray());
        }

        private Task AnimateShow() => SetRectWithAnimation(new Rect(ActualPosition.X, ActualPosition.Y, ActualWidth, ActualHeight), 0, 1);

        private Task AnimateHide() => SetRectWithAnimation(new Rect(ActualPosition.X, ActualPosition.Y, ActualWidth, ActualHeight), 1, 0);

        private async Task AnimateWindowState(WindowState previousWindowState)
        {
            Rect to;
            if (WindowState == WindowState.Normal)
                to = new Rect(_attachedPosition.X, _attachedPosition.Y, _lastNormalSize.Width, _lastNormalSize.Height);
            else if (WindowState == WindowState.Maximized)
                to = new Rect(0, 0, HostPanel.ActualWidth, HostPanel.ActualHeight);
            else
                to = new Rect(ActualPosition.X, 0, ActualWidth, MinHeight);

            await SetRectWithAnimation(to, previousWindowState == WindowState.Minimized ? 0 : 1, WindowState == WindowState.Minimized ? 0 : 1);

            if (WindowState == WindowState.Minimized)
            {
                Visibility = Visibility.Collapsed;
                if (MwiContainer != null && MwiContainer.ActiveMwiChild == this)
                    MwiContainer.GetTopChild(MwiContainer.InternalWindows.Where(w => w.WindowState != WindowState.Minimized))?.Activate();
            }
        }
    }
}
