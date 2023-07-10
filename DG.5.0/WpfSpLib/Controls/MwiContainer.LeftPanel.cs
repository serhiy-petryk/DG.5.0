using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public partial class MwiContainer
    {
        private Grid _leftPanelContainer;
        private ToggleButton _leftPanelButton;

        public static readonly DependencyProperty LeftPanelProperty = DependencyProperty.Register("LeftPanel", typeof(FrameworkElement), typeof(MwiContainer), new FrameworkPropertyMetadata(null, LeftPanel_OnPropertyChanged));
        public FrameworkElement LeftPanel
        {
            get => (FrameworkElement)GetValue(LeftPanelProperty);
            set => SetValue(LeftPanelProperty, value);
        }
        private static void LeftPanel_OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is FrameworkElement oldLeftPanel && d is MwiContainer)
                oldLeftPanel.SetValue(MwiContainerProperty, null);
            if (e.NewValue is FrameworkElement leftPanel && d is MwiContainer)
                leftPanel.SetValue(MwiContainerProperty, d);
        }

        public void HideLeftPanel()
        {
            if (_leftPanelButton != null)
                _leftPanelButton.IsChecked = false;
        }

        private void LeftPanel_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var newWidth = _leftPanelContainer.ActualWidth + e.HorizontalChange;
            if (newWidth >= 0)
                _leftPanelContainer.Width = newWidth;
            e.Handled = true;
        }

        private async void LeftPanelButton_OnCheckedChanged(object sender, RoutedEventArgs e)
        {
            var button = (ToggleButton)sender;

            // bad view of LeftPanelButton animation when MwiPanel.Opacity is animated simultaneously with LeftPanelButton animation
            if (button.IsChecked == true)
            {
                _leftPanelContainer.Visibility = Visibility.Visible;
                await Task.WhenAll(
                    _leftPanelContainer.BeginAnimationAsync(WidthProperty, 0.0, _leftPanelContainer.ActualWidth)
                    // _leftPanelContainer.BeginAnimationAsync(OpacityProperty, 0.5, 1.0)
                );
                MwiPanel.Opacity = 0.875;
            }
            else
            {
                var lastLeftPanelWidth = _leftPanelContainer.ActualWidth;
                await Task.WhenAll(
                    _leftPanelContainer.BeginAnimationAsync(WidthProperty, _leftPanelContainer.ActualWidth, 0.0)
                    // _leftPanelContainer.BeginAnimationAsync(OpacityProperty, _leftPanelContainer.Opacity, 0.5)
                );

                _leftPanelContainer.Visibility = Visibility.Hidden;
                _leftPanelContainer.Width = lastLeftPanelWidth;
                MwiPanel.Opacity = 1.0;
            }
        }

    }
}
