using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using DGView.Helpers;
using WpfSpLib.Helpers;

namespace DGView
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DGCore.Common.Shared.MessageBoxProxy = new MessageBoxProxy();

            // Normalize Culture
            var languageName = LocalizationHelper.CurrentCulture.IetfLanguageTag.Split('-')[0];
            if (string.IsNullOrEmpty(languageName))
                languageName = "en";
            LocalizationHelper.SetLanguage(new CultureInfo(languageName));

            // global event handlers 
            EventManager.RegisterClassHandler(typeof(ToolTip), ToolTip.OpenedEvent, new RoutedEventHandler(OnToolTipOpened));
            EventManager.RegisterClassHandler(typeof(ContextMenu), ContextMenu.OpenedEvent, new RoutedEventHandler(OnContextMenuOpened));
            EventManager.RegisterClassHandler(typeof(ToggleButton), ToggleButton.CheckedEvent, new RoutedEventHandler(OnToggleButtonChecked));
            EventManager.RegisterClassHandler(typeof(Label), UIElement.PreviewMouseLeftButtonDownEvent, new RoutedEventHandler(OnLabelClick));

            SelectAllOnFocusForTextBox.ActivateGlobally();

            base.OnStartup(e);
        }

        private void OnLabelClick(object sender, RoutedEventArgs e)
        {
            var label = (Label) sender;
            if (label.Target is UIElement element && element.IsEnabled && element.Focusable)
                element.Focus();
        }
        private void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (!(sender is ContextMenu contextMenu)) return;
            if (!(contextMenu.PlacementTarget is FrameworkElement owner)) return;
            contextMenu.ApplyTransform(owner);
        }
        private void OnToolTipOpened(object sender, RoutedEventArgs e)
        {
            if (!(sender is ToolTip toolTip)) return;
            if (!(toolTip.PlacementTarget is FrameworkElement owner)) return;
            toolTip.ApplyTransform(owner);
        }

        private void OnToggleButtonChecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && Equals(button.IsChecked, true))
            {
                var cm = button.Tag as ContextMenu ?? button.Resources.Values.OfType<ContextMenu>().FirstOrDefault();
                if (cm != null && !cm.IsOpen) // ContextMenu may be already opened (?bug (binding mode=TwoWay=>twice event call when value changed), see SplitButtonStyle)
                {
                    if (cm.PlacementTarget == null)
                    {
                        cm.PlacementTarget = button;
                        cm.Placement = PlacementMode.Bottom;
                        cm.Closed += (senderClosed, eClosed) => ((ToggleButton)sender).IsChecked = false;
                    }
                    cm.IsOpen = true;
                }
            }
        }
    }
}
