using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfSpLib.Common;

namespace WpfSpLib.Helpers
{
    public static class ControlHelper
    {
        public static void AttachedPropertyChangedHandler(this FrameworkElement fe, RoutedEventHandler activate, RoutedEventHandler deactivate, DispatcherPriority dispatcherPriority = DispatcherPriority.Loaded)
        {
            if (deactivate != null)
                fe.Unloaded -= deactivate;
            fe.Loaded -= activate;

            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                if (deactivate != null)
                    fe.Unloaded += deactivate;
                fe.Loaded += activate;
                if (fe.IsVisible)
                    activate(fe, null);
            }, dispatcherPriority);
        }

        public static void SetCurrentValueSmart(this DependencyObject d, DependencyProperty property, object newValue)
        {
            if (Equals(d.GetValue(property), newValue) || property.ReadOnly) return;

            d.SetCurrentValue(property, newValue);
            if (Equals(d.GetValue(property), newValue)) return;

            d.SetValue(property, newValue);
        }

        public static void ApplyTransform(this FrameworkElement target, FrameworkElement source)
        {
            if (source == null || target == null) return;

            var layoutTransform = new TransformGroup();
            var renderTransform = new TransformGroup();
            foreach (var element in source.GetVisualParents().OfType<FrameworkElement>().ToArray())
            {
                if (element.LayoutTransform != Transform.Identity)
                    layoutTransform.Children.Add(element.LayoutTransform.CloneCurrentValue());
                if (element.RenderTransform != Transform.Identity)
                    renderTransform.Children.Add(element.RenderTransform.CloneCurrentValue());
            }

            if (layoutTransform.Children.Count > 1)
                throw new Exception("Trap!!! Check ControlHelper.ApplyTransform");
            else if (layoutTransform.Children.Count == 1)
                target.LayoutTransform = layoutTransform.Children[0];

            //if (renderTransform.Children.Count > 1)
            //  throw new Exception("Trap!!! Check ControlHelper.ApplyTransform");
        }

        public static TransformGroup GetActualLayoutTransforms(this FrameworkElement source)
        {
            if (source == null) return new TransformGroup();

            var layoutTransform = new TransformGroup();
            foreach (var element in source.GetVisualParents().OfType<FrameworkElement>().ToArray())
            {
                if (element.LayoutTransform != Transform.Identity)
                    layoutTransform.Children.Add(element.LayoutTransform.CloneCurrentValue());
            }
            return layoutTransform;
        }

        #region =========  FormattedText/Measure helpers  ==============
        public static Size MeasureStringForDisplay(string candidate, Control fontControl)
        {
            // !!! TextFormattingMode = Display
            var formattedText = new FormattedText(candidate, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(fontControl.FontFamily, fontControl.FontStyle, fontControl.FontWeight,
                    fontControl.FontStretch), fontControl.FontSize, Brushes.Black, null,
                TextFormattingMode.Display, VisualTreeHelper.GetDpi(fontControl).PixelsPerDip);
            return new Size(formattedText.Width, formattedText.Height);
        }

        public static FormattedText GetFormattedText(string candidate, Control fontControl) =>
            // !!! TextFormattingMode = Ideal
            new FormattedText(candidate, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(fontControl.FontFamily, fontControl.FontStyle, fontControl.FontWeight,
                    fontControl.FontStretch), fontControl.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(fontControl).PixelsPerDip);

        public static FormattedText GetFormattedText(string candidate, TextBlock fontControl) =>
            // !!! TextFormattingMode = Ideal
            new FormattedText(candidate, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(fontControl.FontFamily, fontControl.FontStyle, fontControl.FontWeight,
                    fontControl.FontStretch), fontControl.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(fontControl).PixelsPerDip);
        #endregion

        public static void AddIconToControl(string iconId, ContentControl control, bool iconBeforeContent, Geometry icon, Thickness iconMargin, double iconWidth = double.NaN)
        {
            var oldViewBox = control.GetVisualChildren().OfType<Viewbox>().FirstOrDefault(vb => vb.Resources.Contains(iconId));
            if (oldViewBox != null)
            {
                // wrong Margin/Width, якщо повторно виконуємо метод => потрібно ускладнити обробку (??? чи це потрібно)
                // oldViewBox.Margin = iconMargin;
                // oldViewBox.Width = iconWidth;
                if (oldViewBox.Child is Path oldPath)
                    oldPath.Data = icon;
                return;
            }

            var path = new Path { Stretch = Stretch.Uniform, Margin = new Thickness(), Data = icon };
            var viewbox = new Viewbox
            {
                Child = path,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = iconMargin,
                Width = iconWidth,
                Resources = { [iconId] = true }
            };

            if (control.HasContent)
            {
                var grid = new Grid { ClipToBounds = true, Margin = new Thickness(), SnapsToDevicePixels = true };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = iconBeforeContent ? GridLength.Auto : new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = !iconBeforeContent ? GridLength.Auto : new GridLength(1, GridUnitType.Star) });

                grid.Children.Add(viewbox);
                Grid.SetColumn(viewbox, iconBeforeContent ? 0 : 1);

                var contentBinding = control.GetBindingExpression(ContentControl.ContentProperty);
                var contentControl = new ContentPresenter
                {
                    Content = control.Content,
                    Margin = control.Padding,
                    VerticalAlignment = control.VerticalContentAlignment,
                    HorizontalAlignment = control.HorizontalContentAlignment
                };
                if (contentBinding != null)
                    contentControl.SetBinding(ContentControl.ContentProperty, contentBinding.ParentBinding);

                control.Content = null;
                control.Padding = new Thickness();
                // tb.VerticalContentAlignment = VerticalAlignment.Stretch;
                control.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                grid.Children.Add(contentControl);
                Grid.SetColumn(contentControl, iconBeforeContent ? 1 : 0);

                control.Content = grid;
            }
            else
                control.Content = viewbox;
        }

        #region  ==========  Control Border  =============
        public static IEnumerable<T> GetMainElements<T>(FrameworkElement element) where T: FrameworkElement =>
            VisualHelper.GetVisualChildren(element).OfType<T>().Where(b =>
                Math.Abs(b.ActualWidth + b.Margin.Left + b.Margin.Right - element.ActualWidth) < 1.1 &&
                Math.Abs(b.ActualHeight + b.Margin.Top + b.Margin.Bottom - element.ActualHeight) < 1.1);

        public static CornerRadius? GetCornerRadius(FrameworkElement element)
        {
            if (element is Border border)
                return new CornerRadius(
                    GetActualCornerValue(border.CornerRadius.TopLeft, border.BorderThickness.Top, border.BorderThickness.Left),
                    GetActualCornerValue(border.CornerRadius.TopRight, border.BorderThickness.Top, border.BorderThickness.Right),
                    GetActualCornerValue(border.CornerRadius.BottomRight, border.BorderThickness.Bottom, border.BorderThickness.Right),
                    GetActualCornerValue(border.CornerRadius.BottomLeft, border.BorderThickness.Bottom, border.BorderThickness.Left));

            var mainBorder =  GetMainElements<Border>(element).FirstOrDefault(b => b.CornerRadius != new CornerRadius());
            if (mainBorder != null) return mainBorder.CornerRadius;

            var buttonChrome = GetMainElements<Decorator>(element).FirstOrDefault(d=> d.GetType().Name == "ButtonChrome");
            if (buttonChrome != null) return new CornerRadius(3.0);

            return null;
        }

        private static double GetActualCornerValue(double corner, double thickness1, double thickness2)
        {
            if (Tips.AreEqual(corner, 0.0)) return 0.0;
            return corner + (thickness1 + thickness2) / 4;
        }

        public static void HideInnerBorderOfDatePickerTextBox(FrameworkElement fe, bool toHide)
        {
            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                foreach (var textBox in fe.GetVisualChildren().OfType<DatePickerTextBox>())
                {
                    const string name1 = "watermark_decorator", name2 = "ContentElement";
                    var newBorderThickness = new Thickness(toHide ? 0 : 1);
                    var borders = textBox.GetVisualChildren().OfType<Border>().Where(c => c.Name == name1 || c.Name == name2);
                    foreach (var x in borders)
                        x.BorderThickness = newBorderThickness;
                }
            }, DispatcherPriority.Loaded);
        }

        public static void SetBorderOfToolbarComboBoxes(ToolBar toolBar)
        {
            if (DesignerProperties.GetIsInDesignMode(toolBar))
                return; // VS designer error

            // Set border of comboboxes inside of toolbar (default is white)
            foreach (var comboBox in toolBar.Items.OfType<ComboBox>())
            {
                var toggleButton = VisualHelper.GetVisualChildren(comboBox).OfType<ToggleButton>().FirstOrDefault();
                if (toggleButton != null)
                {
                    var b = new Binding("BorderBrush")
                    {
                        RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ComboBox), 1)
                    };
                    toggleButton.SetBinding(Control.BorderBrushProperty, b);
                    // toggleButton.BorderBrush = new SolidColorBrush(Common.ColorSpaces.ColorUtils.StringToColor("#FF0069D6"));
                }
            }
        }

        private const double Tolerance = 0.0001;
        private static readonly CornerRadius ZeroCornerRadius = new CornerRadius();
        public static void ClipToBoundBorder(Border border)
        {
            if (border.Child != null)
                border.Child.Clip = border.CornerRadius == ZeroCornerRadius ? null : GetRoundRectangle(new Rect(border.Child.RenderSize), border.BorderThickness, border.CornerRadius);
        }

        public static Geometry GetRoundRectangle(Rect baseRect, Thickness thickness, CornerRadius cornerRadius)
        {
            // Source: https://wpfspark.wordpress.com/2011/06/08/clipborder-a-wpf-border-that-clips/
            // Normalizing the corner radius
            if (cornerRadius.TopLeft < Tolerance) cornerRadius.TopLeft = 0.0;
            if (cornerRadius.TopRight < Tolerance) cornerRadius.TopRight = 0.0;
            if (cornerRadius.BottomRight < Tolerance) cornerRadius.BottomRight = 0.0;
            if (cornerRadius.BottomLeft < Tolerance) cornerRadius.BottomLeft = 0.0;

            // Taking the border thickness into account
            var leftHalf = thickness.Left * 0.5;
            if (leftHalf < Tolerance) leftHalf = 0.0;
            var topHalf = thickness.Top * 0.5;
            if (topHalf < Tolerance) topHalf = 0.0;
            var rightHalf = thickness.Right * 0.5;
            if (rightHalf < Tolerance) rightHalf = 0.0;
            var bottomHalf = thickness.Bottom * 0.5;
            if (bottomHalf < Tolerance) bottomHalf = 0.0;

            // Create the rectangles for the corners that needs to be curved in the base rectangle 
            // TopLeft Rectangle 
            var topLeftRect = new Rect(baseRect.Location.X,
                                        baseRect.Location.Y,
                                        Math.Max(0.0, cornerRadius.TopLeft - leftHalf),
                                        Math.Max(0.0, cornerRadius.TopLeft - rightHalf));
            // TopRight Rectangle 
            var topRightRect = new Rect(baseRect.Location.X + baseRect.Width - cornerRadius.TopRight + rightHalf,
                                         baseRect.Location.Y,
                                         Math.Max(0.0, cornerRadius.TopRight - rightHalf),
                                         Math.Max(0.0, cornerRadius.TopRight - topHalf));
            // BottomRight Rectangle
            var bottomRightRect = new Rect(baseRect.Location.X + baseRect.Width - cornerRadius.BottomRight + rightHalf,
                                            baseRect.Location.Y + baseRect.Height - cornerRadius.BottomRight + bottomHalf,
                                            Math.Max(0.0, cornerRadius.BottomRight - rightHalf),
                                            Math.Max(0.0, cornerRadius.BottomRight - bottomHalf));
            // BottomLeft Rectangle 
            var bottomLeftRect = new Rect(baseRect.Location.X,
                                           baseRect.Location.Y + baseRect.Height - cornerRadius.BottomLeft + bottomHalf,
                                           Math.Max(0.0, cornerRadius.BottomLeft - leftHalf),
                                           Math.Max(0.0, cornerRadius.BottomLeft - bottomHalf));

            // Adjust the width of the TopLeft and TopRight rectangles so that they are proportional to the width of the baseRect 
            if (topLeftRect.Right > topRightRect.Left)
            {
                var newWidth = (topLeftRect.Width / (topLeftRect.Width + topRightRect.Width)) * baseRect.Width;
                topLeftRect = new Rect(topLeftRect.Location.X, topLeftRect.Location.Y, newWidth, topLeftRect.Height);
                topRightRect = new Rect(baseRect.Left + newWidth, topRightRect.Location.Y, Math.Max(0.0, baseRect.Width - newWidth), topRightRect.Height);
            }

            // Adjust the height of the TopRight and BottomRight rectangles so that they are proportional to the height of the baseRect
            if (topRightRect.Bottom > bottomRightRect.Top)
            {
                var newHeight = (topRightRect.Height / (topRightRect.Height + bottomRightRect.Height)) * baseRect.Height;
                topRightRect = new Rect(topRightRect.Location.X, topRightRect.Location.Y, topRightRect.Width, newHeight);
                bottomRightRect = new Rect(bottomRightRect.Location.X, baseRect.Top + newHeight, bottomRightRect.Width, Math.Max(0.0, baseRect.Height - newHeight));
            }

            // Adjust the width of the BottomLeft and BottomRight rectangles so that they are proportional to the width of the baseRect
            if (bottomRightRect.Left < bottomLeftRect.Right)
            {
                var newWidth = (bottomLeftRect.Width / (bottomLeftRect.Width + bottomRightRect.Width)) * baseRect.Width;
                bottomLeftRect = new Rect(bottomLeftRect.Location.X, bottomLeftRect.Location.Y, newWidth, bottomLeftRect.Height);
                bottomRightRect = new Rect(baseRect.Left + newWidth, bottomRightRect.Location.Y, Math.Max(0.0, baseRect.Width - newWidth), bottomRightRect.Height);
            }

            // Adjust the height of the TopLeft and BottomLeft rectangles so that they are proportional to the height of the baseRect
            if (bottomLeftRect.Top < topLeftRect.Bottom)
            {
                var newHeight = (topLeftRect.Height / (topLeftRect.Height + bottomLeftRect.Height)) * baseRect.Height;
                topLeftRect = new Rect(topLeftRect.Location.X, topLeftRect.Location.Y, topLeftRect.Width, newHeight);
                bottomLeftRect = new Rect(bottomLeftRect.Location.X, baseRect.Top + newHeight, bottomLeftRect.Width, Math.Max(0.0, baseRect.Height - newHeight));
            }

            var roundedRectGeometry = new StreamGeometry();
            using (var context = roundedRectGeometry.Open())
            {
                // Begin from the Bottom of the TopLeft Arc and proceed clockwise
                context.BeginFigure(topLeftRect.BottomLeft, true, true);
                // TopLeft Arc
                context.ArcTo(topLeftRect.TopRight, topLeftRect.Size, 0, false, SweepDirection.Clockwise, true, true);
                // Top Line
                context.LineTo(topRightRect.TopLeft, true, true);
                // TopRight Arc
                context.ArcTo(topRightRect.BottomRight, topRightRect.Size, 0, false, SweepDirection.Clockwise, true, true);
                // Right Line
                context.LineTo(bottomRightRect.TopRight, true, true);
                // BottomRight Arc
                context.ArcTo(bottomRightRect.BottomLeft, bottomRightRect.Size, 0, false, SweepDirection.Clockwise, true, true);
                // Bottom Line
                context.LineTo(bottomLeftRect.BottomRight, true, true);
                // BottomLeft Arc
                context.ArcTo(bottomLeftRect.TopLeft, bottomLeftRect.Size, 0, false, SweepDirection.Clockwise, true, true);
            }

            return roundedRectGeometry;
        }
        #endregion

        #region ======== OnGotFocus of Control (see disassembled DatePicker) ===========
        public static void OnGotFocusOfControl(object sender, RoutedEventArgs e, Control textBox)
        {
            if (!e.Handled && textBox != null)
            {
                if (Equals(e.OriginalSource, sender))
                {
                    textBox.Focus();
                    e.Handled = true;
                    return;
                }
                if (Equals(e.OriginalSource, textBox))
                {
                    // textBox.SelectAll();
                    e.Handled = true;
                }
            }
        }

        public static void SetFocus(UIElement element)
        {
            element?.Dispatcher.InvokeAsync(() =>
            {
                element.Focus();
                element.Dispatcher.InvokeAsync(new Action(() => element.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))), DispatcherPriority.Background);
            }, DispatcherPriority.Background);
        }
        #endregion
    }
}
