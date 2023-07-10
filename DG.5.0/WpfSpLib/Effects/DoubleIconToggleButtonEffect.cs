using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfSpLib.Helpers;

namespace WpfSpLib.Effects
{
    public class DoubleIconToggleButtonEffect
    {
        #region ===========  OnPropertyChanged  ===========

        private const string IconId = "DoubleIconToggleButtonEffect";
        private static readonly ConcurrentDictionary<FrameworkElement, object> _activated = new ConcurrentDictionary<FrameworkElement, object>();

        private static void OnGeometryPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton tb)
            {
                if (e.Property != UIElement.VisibilityProperty)
                {
                    tb.IsVisibleChanged -= Element_IsVisibleChanged;
                    tb.IsVisibleChanged += Element_IsVisibleChanged;
                }

                if (tb.IsVisible)
                {
                    if ((!(tb.Content is FrameworkElement) || GetViewbox(tb) == null) && GetGeometryOn(tb) != Geometry.Empty && GetGeometryOff(tb) != Geometry.Empty)
                        if (_activated.TryAdd(tb, null)) Activate(tb);
                }
                else
                {
                    if (_activated.TryRemove(tb, out var o)) Deactivate(tb);
                }
            }
            else
                Debug.Print($"DoubleIconToggleButtonEffect is not implemented for {d.GetType().Namespace}.{d.GetType().Name} type");

            void Element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e2) => OnGeometryPropertyChanged((Control)sender, e2);
        }

        private static void Activate(ToggleButton tb)
        {
            tb.Checked += OnToggleButtonCheckChanged;
            tb.Unchecked += OnToggleButtonCheckChanged;
            tb.Dispatcher.InvokeAsync(() => Init(tb), DispatcherPriority.Loaded);
        }

        private static void Deactivate(ToggleButton tb)
        {
            tb.Checked -= OnToggleButtonCheckChanged;
            tb.Unchecked -= OnToggleButtonCheckChanged;
        }
        #endregion

        #region =====================  Dependency Properties  ==========================
        public static readonly DependencyProperty GeometryOnProperty = DependencyProperty.RegisterAttached("GeometryOn",
            typeof(Geometry), typeof(DoubleIconToggleButtonEffect), new FrameworkPropertyMetadata(Geometry.Empty, OnGeometryPropertyChanged));
        public static Geometry GetGeometryOn(DependencyObject obj) => (Geometry)obj.GetValue(GeometryOnProperty);
        public static void SetGeometryOn(DependencyObject obj, Geometry value) => obj.SetValue(GeometryOnProperty, value);
        //================
        public static readonly DependencyProperty GeometryOffProperty = DependencyProperty.RegisterAttached("GeometryOff",
            typeof(Geometry), typeof(DoubleIconToggleButtonEffect), new FrameworkPropertyMetadata(Geometry.Empty, OnGeometryPropertyChanged));
        public static Geometry GetGeometryOff(DependencyObject obj) => (Geometry)obj.GetValue(GeometryOffProperty);
        public static void SetGeometryOff(DependencyObject obj, Geometry value) => obj.SetValue(GeometryOffProperty, value);
        //================
        public static readonly DependencyProperty WidthProperty = DependencyProperty.RegisterAttached("Width",
            typeof(double), typeof(DoubleIconToggleButtonEffect), new FrameworkPropertyMetadata(double.NaN, OnWidthPropertyChanged));
        public static double GetWidth(DependencyObject obj) => (double)obj.GetValue(WidthProperty);
        public static void SetWidth(DependencyObject obj, double value) => obj.SetValue(WidthProperty, value);
        //================
        public static readonly DependencyProperty MarginOnProperty = DependencyProperty.RegisterAttached("MarginOn",
            typeof(Thickness), typeof(DoubleIconToggleButtonEffect), new FrameworkPropertyMetadata(new Thickness(), OnMarginPropertyChanged));
        public static Thickness GetMarginOn(DependencyObject obj) => (Thickness)obj.GetValue(MarginOnProperty);
        public static void SetMarginOn(DependencyObject obj, Thickness value) => obj.SetValue(MarginOnProperty, value);
        //================
        public static readonly DependencyProperty MarginOffProperty = DependencyProperty.RegisterAttached("MarginOff",
            typeof(Thickness), typeof(DoubleIconToggleButtonEffect), new FrameworkPropertyMetadata(new Thickness(), OnMarginPropertyChanged));
        public static Thickness GetMarginOff(DependencyObject obj) => (Thickness)obj.GetValue(MarginOffProperty);
        public static void SetMarginOff(DependencyObject obj, Thickness value) => obj.SetValue(MarginOffProperty, value);
        #endregion

        //=====================================
        private static void OnWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                if (d is ToggleButton tb && GetViewbox(tb) is Viewbox viewbox)
                    viewbox.Width = (double)e.NewValue;
            }, DispatcherPriority.Loaded);
        }
        private static void OnMarginPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                if (d is ToggleButton tb && (GetViewbox(tb) is Viewbox viewbox) &&
                    ((tb.IsChecked == true && e.Property == MarginOnProperty) ||
                     (tb.IsChecked != null && e.Property == MarginOffProperty)))
                    viewbox.Margin = (Thickness)e.NewValue;
            }, DispatcherPriority.Loaded);
        }

        private static void Init(ToggleButton tb)
        {
            ControlHelper.AddIconToControl(IconId, tb, false, tb.IsChecked == true ? GetGeometryOn(tb) : GetGeometryOff(tb),
                tb.IsChecked == true ? GetMarginOn(tb) : GetMarginOff(tb), GetWidth(tb));
        }

        private static async void OnToggleButtonCheckChanged(object sender, RoutedEventArgs e)
        {
            var tb = (ToggleButton)sender;
            var viewbox = GetViewbox(tb);
            var path = (Path)viewbox.Child;

            if (!(viewbox.RenderTransform is ScaleTransform))
                viewbox.RenderTransform = new ScaleTransform(1, 1);
            viewbox.RenderTransformOrigin = new Point(0.5, 0.5);

            await viewbox.RenderTransform.BeginAnimationAsync(ScaleTransform.ScaleXProperty, 1.0, 0.0);
            await viewbox.Dispatcher.InvokeAsync(async () =>
            {
                path.Data = tb.IsChecked == true ? GetGeometryOn(tb) : GetGeometryOff(tb);
                viewbox.Margin = tb.IsChecked == true ? GetMarginOn(tb) : GetMarginOff(tb);
                await viewbox.RenderTransform.BeginAnimationAsync(ScaleTransform.ScaleXProperty, 0.0, 1.0);
            }, DispatcherPriority.Background);
        }

        private static Viewbox GetViewbox(ToggleButton tb) => tb.GetVisualChildren().OfType<Viewbox>().FirstOrDefault(vb => vb.Resources.Contains(IconId));
    }
}
