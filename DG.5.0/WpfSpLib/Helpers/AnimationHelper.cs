using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WpfSpLib.Common;

namespace WpfSpLib.Helpers
{
    public static class AnimationHelper
    {
        public const double AnimationTime = 120.0;
        public static readonly Duration AnimationDuration = TimeSpan.FromMilliseconds(AnimationTime);
        public static readonly Duration AnimationDurationSlow = TimeSpan.FromMilliseconds(AnimationTime * 2);
        public static readonly TimeSpan AnimationTimespanSlow = TimeSpan.FromMilliseconds(AnimationTime * 2);

        #region =======  Content animations  ========
        public static Task[] GetContentAnimations(FrameworkElement content, bool show, bool xDimension = true, bool yDimension = true)
        {
            var tasks = new List<Task>();
            if (!(content.RenderTransform is ScaleTransform))
            {
                content.RenderTransformOrigin = new Point(0.5, 0.5);
                content.RenderTransform = new ScaleTransform(1, 1);
            }
            var from = show ? 0.0 : 1.0;
            var to = show ? 1.0 : 0.0;
            if (xDimension)
                tasks.Add(content.RenderTransform.BeginAnimationAsync(ScaleTransform.ScaleXProperty, from, to, AnimationDurationSlow));
            if (yDimension)
                tasks.Add(content.RenderTransform.BeginAnimationAsync(ScaleTransform.ScaleYProperty, from, to, AnimationDurationSlow));
            tasks.Add(content.BeginAnimationAsync(UIElement.OpacityProperty, from, to, AnimationDurationSlow));

            return tasks.ToArray();
        }

        public static Task[] GetHeightContentAnimations(FrameworkElement content, bool show) => GetSizeContentAnimations(content, show, FrameworkElement.HeightProperty, content.ActualHeight);
        public static Task[] GetWidthContentAnimations(FrameworkElement content, bool show) => GetSizeContentAnimations(content, show, FrameworkElement.WidthProperty, content.ActualWidth);
        private static Task[] GetSizeContentAnimations(FrameworkElement content, bool show, DependencyProperty property, double size)
        {
            return new[]
            {
                content.BeginAnimationAsync(property, show ? 0.0 : size, show ? size : 0.0, AnimationDurationSlow),
                // content.BeginAnimationAsync(UIElement.OpacityProperty, show ? 0.0 : 1.0, show ? 1.0 : 0.0, AnimationDurationSlow)
            };
        }
        #endregion

        public static LinearGradientBrush BeginLinearGradientBrushAnimation(LinearGradientBrush newBrush, LinearGradientBrush oldBrush)
        {
            // usage: tabItem.Background = AnimationHelper.RunLinearGradientBrushAnimation(newBrush, (LinearGradientBrush)tabItem.Background);
            if (newBrush.GradientStops.Count != oldBrush.GradientStops.Count)
                throw new Exception("RunLinearGradientBrushAnimation error! Different size of newBrush and oldBrush GradientStops collection");

            newBrush = newBrush.Clone();
            for (var k = 0; k < newBrush.GradientStops.Count; k++)
            {
                var a = new ColorAnimation(oldBrush.GradientStops[k].Color, newBrush.GradientStops[k].Color, AnimationDurationSlow);
                a.Freeze();
                newBrush.GradientStops[k].BeginAnimation(GradientStop.ColorProperty, a);
            }

            return newBrush;
        }

        #region ================  Begin animation async ===================
        public static Task BeginFrameAnimationAsync(this IAnimatable element, DependencyProperty property, object value)
        {
            if (element == null || property == null || value == null)
                throw new NullReferenceException();
            if (!property.PropertyType.IsInstanceOfType(value))
                throw new Exception("BeginFrameAnimationAsync error. Data type of 'value' parameter doesn't match property.PropertyType.");

            var animation = new ObjectAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new DiscreteObjectKeyFrame { Value = value, KeyTime = AnimationDuration.TimeSpan });
            return element.BeginAnimationAsync(property, animation);
        }

        public static Task BeginAnimationAsync(this IAnimatable element, DependencyProperty property, object from, object to, Duration? duration = null)
        {
            if (element == null || property == null || from == null || to == null) throw new NullReferenceException();

            if (Equals(from, to) || Equals(from, Colors.Transparent) || Equals(to, Colors.Transparent) || (element is UIElement uiElement && !uiElement.IsVisible))
            {
                ((DependencyObject)element).SetCurrentValueSmart(property, to);
                return Task.FromResult(true);
            }

            var pType = Tips.GetNotNullableType(property.PropertyType);
            if (!pType.IsInstanceOfType(from))
                throw new Exception("BeginAnimationAsync error. Data type of 'from' parameter doesn't match property.PropertyType.");
            if (!pType.IsInstanceOfType(to))
                throw new Exception("BeginAnimationAsync error. Data type of 'to' parameter doesn't match property.PropertyType.");

            var d = duration ?? AnimationDuration;
            AnimationTimeline animation = null;
            if (pType == typeof(double)) animation = new DoubleAnimation((double)from, (double)to, d);
            else if (pType == typeof(Point)) animation = new PointAnimation((Point)from, (Point)to, d);
            else if (pType == typeof(Thickness)) animation = new ThicknessAnimation((Thickness)from, (Thickness)to, d);
            else if (pType == typeof(Rect)) animation = new RectAnimation((Rect)from, (Rect)to, d);
            else if (pType == typeof(Color)) animation = new ColorAnimation((Color)from, (Color)to, d);
            if (animation == null)
                throw new NotImplementedException();
            animation.FillBehavior = FillBehavior.Stop;

            return element.BeginAnimationAsync(property, animation, to);
        }
        #endregion

        #region ================  Animation as Task  ================
        private static Task BeginAnimationAsync(this IAnimatable element, DependencyProperty property, AnimationTimeline animation, object endValue = null)
        {
            // state in constructor == animation cancel action
            // var tcs = new TaskCompletionSource<bool>(new Action(() => element.BeginAnimation(property, null)));
            var tcs = new TaskCompletionSource<bool>();
            animation.Completed += (s, e) =>
            {
                if (endValue != null)
                    ((DependencyObject)element).SetCurrentValueSmart(property, endValue);
                tcs.SetResult(true);
                tcs.Task.Dispose();
            };
            animation.Freeze(); // one time animation
            // animation.Completed += OnAnimationCompleted;
            element.BeginAnimation(property, animation);
            return tcs.Task;

            /*void OnAnimationCompleted(object sender, EventArgs e)
            {
                // animation.Completed -= OnAnimationCompleted;
                EventHelper.RemovePropertyChangeEventHandlers(animation);
                if (endValue != null)
                    ((DependencyObject)element).SetCurrentValueSmart(property, endValue);
                tcs.SetResult(true);

                // EventHelper.RemovePropertyChangeEventHandlers(animation);
            }*/
        }

        #endregion
    }

    #region =====  ScrollViewerAnimator  =====
    // Helper class to animate ScrollViewer.Horizontal/Vertical Offset
    // from https://marlongrech.wordpress.com/2009/09/14/how-to-set-wpf-scrollviewer-verticaloffset-and-horizontal-offset/
    public class ScrollViewerAnimator
    {
        // #region HorizontalOffset
        public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.RegisterAttached("HorizontalOffset", typeof(double), typeof(ScrollViewerAnimator), new FrameworkPropertyMetadata((double)0.0, OnHorizontalOffsetChanged));
        public static double GetHorizontalOffset(DependencyObject d) => (double)d.GetValue(HorizontalOffsetProperty);
        public static void SetHorizontalOffset(DependencyObject d, double value) => d.SetValue(HorizontalOffsetProperty, value);
        private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ScrollViewer)d).ScrollToHorizontalOffset((double)e.NewValue);

        // #region VerticalOffset
        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerAnimator), new FrameworkPropertyMetadata((double)0.0, OnVerticalOffsetChanged));
        public static double GetVerticalOffset(DependencyObject d) => (double)d.GetValue(VerticalOffsetProperty);
        public static void SetVerticalOffset(DependencyObject d, double value) => d.SetValue(VerticalOffsetProperty, value);
        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ScrollViewer)d).ScrollToVerticalOffset((double)e.NewValue);
    }
    #endregion

}
