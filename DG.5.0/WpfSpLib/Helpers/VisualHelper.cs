using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfSpLib.Helpers
{
    public static class VisualHelper
    {
        public static IEnumerable<T> GetVisualParents<T>(this DependencyObject current)
        {
            while (current != null)
            {
                if (current is T current1) yield return current1;
                current = VisualTreeHelper.GetParent(current) ?? (current as FrameworkElement)?.Parent;
            }
        }

        public static IEnumerable<T> GetVisualChildren<T>(this DependencyObject current)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
            {
                var child = VisualTreeHelper.GetChild(current, i);
                if (child is T child1) yield return child1;

                foreach (var childOfChild in GetVisualChildren<DependencyObject>(child))
                    if (childOfChild is T child2) yield return child2;
            }
        }

        #region ======  DoEvents  =========
        // see https://docs.microsoft.com/ru-ru/dotnet/api/system.windows.threading.dispatcherframe?view=windowsdesktop-5.0
        public static void DoEvents(DispatcherPriority priority)
        {
            var nestedFrame = new DispatcherFrame();
            var exitOperation = Dispatcher.CurrentDispatcher.BeginInvoke(priority, _exitFrameCallback, nestedFrame);
            Dispatcher.PushFrame(nestedFrame);

            if (exitOperation.Status != DispatcherOperationStatus.Completed)
                exitOperation.Abort();
        }
        private static readonly DispatcherOperationCallback _exitFrameCallback = (state) =>
        {
            ((DispatcherFrame)state).Continue = false;
            return null;
        };
        #endregion

        public static bool IsMouseOverElement(this FrameworkElement element, Func<IInputElement, Point> getPositionOfMouse)
        {
            var p = getPositionOfMouse(element);
            var bounds = GetBoundsOfElement(element);
            return bounds.Contains(p);
        }
        public static List<DependencyObject> GetElementsUnderMouse(this UIElement sender, Func<IInputElement, Point> getPositionOfMouse)
        {
            var hitTestResults = new List<DependencyObject>();
            VisualTreeHelper.HitTest(sender, null, result => GetHitTestResult(result, hitTestResults), new PointHitTestParameters(getPositionOfMouse(sender)));
            return hitTestResults;
        }
        private static HitTestResultBehavior GetHitTestResult(HitTestResult result, List<DependencyObject> hitTestResults)
        {
            // Add the hit test result to the list that will be processed after the enumeration.
            hitTestResults.Add(result.VisualHit);
            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;
        }


        public static Rect GetBoundsOfElement(this FrameworkElement element) => new Rect(-element.Margin.Left, -element.Margin.Top, element.ActualWidth + element.Margin.Left + element.Margin.Right, element.ActualHeight + element.Margin.Top + element.Margin.Bottom);

        public static Rect GetVisibleRect(this FrameworkElement item, Visual visualAncestor)
        {
            // based on https://social.msdn.microsoft.com/Forums/vstudio/en-US/b57531cc-fdb1-4d0e-9650-324630343d62/screen-rectangle-for-visible-part-of-wpf-uielement-windows-forms-wpf-interop?forum=wpf
            // Visual _rootVisual = HwndSource.FromVisual(item).RootVisual;
            var transformToRoot = item.TransformToAncestor(visualAncestor);
            // Rect screenRect = new Rect(transformToRoot.Transform(new Point(0, 0)), transformToRoot.Transform(new Point(item.ActualWidth, item.ActualHeight)));
            var screenRect = transformToRoot.TransformBounds(GetBoundsOfElement(item));
            var parent = VisualTreeHelper.GetParent(item);
            while (parent != null && parent != visualAncestor)
            {
                if (parent is Visual visual && parent is FrameworkElement element)
                {
                    transformToRoot = visual.TransformToAncestor(visualAncestor);
                    var pointAncestorTopLeft = transformToRoot.Transform(new Point(0, 0));
                    var pointAncestorBottomRight = transformToRoot.Transform(new Point(element.ActualWidth, element.ActualHeight));
                    var ancestorRect = new Rect(pointAncestorTopLeft, pointAncestorBottomRight);
                    screenRect.Intersect(ancestorRect);
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            // at this point screenRect is the bounding rectangle for the visible portion of "this" element
            return screenRect;
        }


    }
}
