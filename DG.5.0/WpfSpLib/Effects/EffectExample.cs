using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace WpfSpLib.Effects
{
    public class EffectExample
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
                    Update(control, null);
                }
                else
                {
                    if (_activated.TryRemove(control, out var o)) Deactivate(control);
                }
            }
            else
                Debug.Print($"Effect is not implemented for {d.GetType().Namespace}.{d.GetType().Name} type");

            void Element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e2) => OnPropertyChanged((Control) sender, e2);
        }

        private static void Activate(Control control) { }
        private static void Deactivate(Control control) { }
        private static void Update(object sender, EventArgs e)
        {
            if (!(sender is Control control && control.IsVisible)) return;
        }
        #endregion
    }
}
