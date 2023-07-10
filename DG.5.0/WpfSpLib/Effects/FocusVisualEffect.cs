using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace WpfSpLib.Effects
{
    public class FocusVisualEffect
    {
        #region ===========  OnPropertyChanged  ===========
        private static readonly ConcurrentDictionary<FrameworkElement, object> _activated = new ConcurrentDictionary<FrameworkElement, object>();

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe)
            {
                if (e.Property != UIElement.VisibilityProperty)
                {
                    fe.IsVisibleChanged -= Element_IsVisibleChanged;
                    fe.IsVisibleChanged += Element_IsVisibleChanged;
                }

                if (fe.IsVisible)
                {
                    if (GetFocusControlStyle(fe) is Style)
                    {
                        if (fe.FocusVisualStyle != null)
                            fe.FocusVisualStyle = null;

                        if (_activated.TryAdd(fe, null)) Activate(fe);
                    }
                }
                else
                {
                    if (_activated.TryRemove(fe, out var o)) Deactivate(fe);
                }
            }
            else
                Debug.Print($"FocusVisualEffect is not implemented for {d.GetType().Namespace}.{d.GetType().Name} type");

            void Element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e2) => OnPropertyChanged((Control)sender, e2);
        }

        private static void Activate(FrameworkElement element) => element.IsKeyboardFocusWithinChanged += OnElementFocusChanged;
        private static void Deactivate(FrameworkElement element) => element.IsKeyboardFocusWithinChanged -= OnElementFocusChanged;

        #endregion

        #region ==============  Properties  ==============
        public static readonly DependencyProperty AlwaysShowFocusProperty = DependencyProperty.RegisterAttached(
            "AlwaysShowFocus", typeof(bool), typeof(FocusVisualEffect), new FrameworkPropertyMetadata(false));
        public static bool GetAlwaysShowFocus(DependencyObject obj) => (bool)obj.GetValue(AlwaysShowFocusProperty);
        public static void SetAlwaysShowFocus(DependencyObject obj, bool value) => obj.SetValue(AlwaysShowFocusProperty, value);
        //================
        public static readonly DependencyProperty FocusControlStyleProperty = DependencyProperty.RegisterAttached(
            "FocusControlStyle", typeof(Style), typeof(FocusVisualEffect), new FrameworkPropertyMetadata(null, OnPropertyChanged));
        public static Style GetFocusControlStyle(DependencyObject obj) => (Style)obj.GetValue(FocusControlStyleProperty);
        public static void SetFocusControlStyle(DependencyObject obj, Style value) => obj.SetValue(FocusControlStyleProperty, value);
        #endregion

        #region ===============  Private methods  ===============
        private static void OnElementFocusChanged(object sender, DependencyPropertyChangedEventArgs e) => Element_ChangeFocus(sender, null);

        private static void Element_ChangeFocus(object sender, SizeChangedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            var isFocused = element.IsKeyboardFocusWithin;

            if (isFocused)
            {
                if (!GetAlwaysShowFocus(element))
                {
                    var a1 = element.Resources["FocusEffect_FocusTrack"] as bool?;
                    var isKeyboardMostRecentInputDevice = a1 ?? IsKeyboardMostRecentInputDevice();
                    if (a1 == null)
                        element.Resources.Add("FocusEffect_FocusTrack", isKeyboardMostRecentInputDevice);
                    if (!isKeyboardMostRecentInputDevice)
                        return;
                }

                var layer = AdornerLayer.GetAdornerLayer(element);
                var adornerControl = layer.GetAdorners(element)?.OfType<AdornerControl>().FirstOrDefault(a => a.Child.Name == "FocusControl");
                if (adornerControl == null)
                {
                    var control = new Control
                    {
                        Name = "FocusControl", Focusable = false, IsHitTestVisible = false,
                        Style =  GetFocusControlStyle(element)
                    };

                    adornerControl = new AdornerControl(element) {Child = control, Opacity = 0.0};
                    layer.Add(adornerControl);
                }

                adornerControl.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, AnimationHelper.AnimationDurationSlow));
            }
            else
            {
                if (!GetAlwaysShowFocus(element))
                    element.Resources.Remove("FocusEffect_FocusTrack");
                var layer = AdornerLayer.GetAdornerLayer(element);
                var adorners = layer?.GetAdorners(element) ?? new Adorner[0];
                foreach (var adorner in adorners.OfType<AdornerControl>().Where(a => a.Child.Name == "FocusControl"))
                    adorner.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(adorner.Opacity, 0.0, AnimationHelper.AnimationDurationSlow));

                // if isFocused=false не завжди спрацьовує фокус на новому елементі -> Activate focus on focused element
                var focusedControl = Keyboard.FocusedElement as FrameworkElement;
                if (focusedControl != null && focusedControl != element && GetFocusControlStyle(focusedControl) != null)
                    Element_ChangeFocus(focusedControl, null);
            }
        }

        private static bool IsKeyboardMostRecentInputDevice() => InputManager.Current.MostRecentInputDevice is KeyboardDevice;
        #endregion
    }
}
