using System.Windows;
using System.Windows.Controls;

namespace WpfSpLib.Controls
{
    public class DragAdornerControl: ItemsControl
    {
        public static readonly DependencyProperty DragDropEffectProperty = DependencyProperty.Register("DragDropEffect",
            typeof(DragDropEffects), typeof(DragAdornerControl), new FrameworkPropertyMetadata(DragDropEffects.Move));
        public DragDropEffects DragDropEffect
        {
            get => (DragDropEffects)GetValue(DragDropEffectProperty);
            set => SetValue(DragDropEffectProperty, value);
        }
    }
}
