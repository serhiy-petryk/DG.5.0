// Taken from https://wpf.2000things.com/tag/isenabled/ - it's readonly text box => allow to copy text in UI
using System.Windows;
using System.Windows.Controls;

namespace WpfSpLib.Controls
{
    public class LabelBox: TextBox
    {
        static LabelBox()
        {
            IsEnabledProperty.OverrideMetadata(typeof(LabelBox), new UIPropertyMetadata(true, IsEnabledPropertyChanged, CoerceIsEnabled));
            HorizontalAlignmentProperty.OverrideMetadata(typeof(LabelBox), new FrameworkPropertyMetadata(System.Windows.HorizontalAlignment.Stretch));
            VerticalAlignmentProperty.OverrideMetadata(typeof(LabelBox), new FrameworkPropertyMetadata(VerticalAlignment.Stretch));
            HorizontalContentAlignmentProperty.OverrideMetadata(typeof(LabelBox), new FrameworkPropertyMetadata(System.Windows.HorizontalAlignment.Left));
            VerticalContentAlignmentProperty.OverrideMetadata(typeof(LabelBox), new FrameworkPropertyMetadata(VerticalAlignment.Center));
        }

        private static void IsEnabledPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            // Overriding PropertyChanged results in merged metadata, which is what we want--
            // the PropertyChanged logic in UIElement.IsEnabled will still get invoked.
        }

        private static object CoerceIsEnabled(DependencyObject source, object value)
        {
            return value;
        }

        public LabelBox()
        {
            IsReadOnly = true;
            Focusable = true;
            IsTabStop = false;
            BorderThickness = new Thickness(0);
        }
    }
}
