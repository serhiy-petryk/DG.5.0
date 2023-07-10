using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace WpfSpLib.Effects
{
    /// <summary>
    /// </summary>
    public class CornerRadiusEffect
    {
        #region ===========  OnPropertyChanged  ===========
        private static readonly ConcurrentDictionary<FrameworkElement, object> _activated = new ConcurrentDictionary<FrameworkElement, object>();

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Control control)
            {
                if (e.Property != UIElement.VisibilityProperty)
                {
                    control.IsVisibleChanged -= Element_IsVisibleChanged;
                    control.IsVisibleChanged += Element_IsVisibleChanged;
                }

                if (control.IsVisible)
                {
                    if (_activated.TryAdd(control, null)) Activate(control);
                    control.Dispatcher.InvokeAsync(() => UpdateBorders(control, null), DispatcherPriority.Input);
                }
                else
                {
                    if (_activated.TryRemove(control, out var o)) Deactivate(control);
                }
            }
            else
                Debug.Print($"CornerRadiusEffect is not implemented for {d.GetType().Namespace}.{d.GetType().Name} type");

            void Element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e2) => OnPropertyChanged((Control)sender, e2);
        }
        private static void Activate(FrameworkElement element)
        {
            element.SizeChanged += UpdateBorders;
            var dpd = DependencyPropertyDescriptor.FromProperty(Border.BorderThicknessProperty, typeof(Border));
            dpd.AddValueChanged(element, UpdateBorders);
        }
        private static void Deactivate(FrameworkElement element)
        {
            element.SizeChanged -= UpdateBorders;
            var dpd = DependencyPropertyDescriptor.FromProperty(Border.BorderThicknessProperty, typeof(Border));
            dpd.RemoveValueChanged(element, UpdateBorders);
        }

        #endregion

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
            "CornerRadius", typeof(CornerRadius), typeof(CornerRadiusEffect), new FrameworkPropertyMetadata(new CornerRadius(), OnPropertyChanged));
        public static CornerRadius GetCornerRadius(DependencyObject obj) => (CornerRadius)obj.GetValue(CornerRadiusProperty);
        public static void SetCornerRadius(DependencyObject obj, CornerRadius value) => obj.SetValue(CornerRadiusProperty, value);
        private static void UpdateBorders(object sender, EventArgs e)
        {
            if (!(sender is FrameworkElement element && element.IsVisible)) return;

            var newRadius = GetCornerRadius(element);
            foreach (var border in ControlHelper.GetMainElements<Border>(element))
            {
                var borderSize = border.BorderThickness;
                var radius = new CornerRadius(
                    GetCornerWidth(newRadius.TopLeft, borderSize.Top, borderSize.Left),
                    GetCornerWidth(newRadius.TopRight, borderSize.Top, borderSize.Right),
                    GetCornerWidth(newRadius.BottomRight, borderSize.Bottom, borderSize.Right),
                    GetCornerWidth(newRadius.BottomLeft, borderSize.Bottom, borderSize.Left));

                border.CornerRadius = radius;
                ControlHelper.ClipToBoundBorder(border);
            }
        }

        private static double GetCornerWidth(double requiredCornerWidth, double firstBorderWidth, double secondBorderWidth)
        {
            if (Tips.AreEqual(requiredCornerWidth, 0.0)) return 0.0;
            var cornerWidth = requiredCornerWidth - (firstBorderWidth + secondBorderWidth) / 4;
            return Math.Max(Tips.SCREEN_TOLERANCE, cornerWidth);
        }
    }
}
