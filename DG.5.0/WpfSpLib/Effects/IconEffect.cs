using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfSpLib.Helpers;

namespace WpfSpLib.Effects
{
    /// <summary>
    /// </summary>
    public class IconEffect
    {
        private const string IconId = "IconEffect";
        public static readonly DependencyProperty GeometryProperty = DependencyProperty.RegisterAttached(
            "Geometry", typeof(Geometry), typeof(IconEffect), new FrameworkPropertyMetadata(null, OnPropertiesChanged));

        public static Geometry GetGeometry(DependencyObject obj) => (Geometry)obj.GetValue(GeometryProperty);
        public static void SetGeometry(DependencyObject obj, Geometry value) => obj.SetValue(GeometryProperty, value);

        //==================
        public static readonly DependencyProperty MarginProperty = DependencyProperty.RegisterAttached("Margin",
            typeof(Thickness), typeof(IconEffect), new FrameworkPropertyMetadata(new Thickness(), OnPropertiesChanged));
        public static Thickness GetMargin(DependencyObject obj) => (Thickness)obj.GetValue(MarginProperty);
        public static void SetMargin(DependencyObject obj, Thickness value) => obj.SetValue(MarginProperty, value);
        //==================
        private static void OnPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ContentControl control) || !(e.NewValue is Geometry geometry)) return;

            control.Dispatcher.InvokeAsync(
                () => ControlHelper.AddIconToControl(IconId, control, true, geometry, GetMargin(control)),
                DispatcherPriority.Loaded);
        }
    }
}

