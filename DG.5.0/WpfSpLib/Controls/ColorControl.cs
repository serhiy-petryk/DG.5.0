// ToDo:
// 1. ColorBox for Color/CurrentColor.
// 2. ToggleButton + popup
//      may change layout to: ToggleButton -> Grid -> (ContentPresenter + Popup)
//      зовнішній вигляд toggleButton = зовнішній вигляд Content
// 3. Control with 4 tab:
//      - mini size: RGB sliders
//      - mini size: HSL sliders
//      - middle size: current view
//      - middle size with scroll: color boxes of known colors (140 items)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Common.ColorSpaces;
using WpfSpLib.Effects;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public class ColorControl: Control
    {
        static ColorControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorControl), new FrameworkPropertyMetadata(typeof(Calculator)));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(ColorControl), new FrameworkPropertyMetadata(false));
        }

        public event EventHandler ColorChanged;
        private ColorControlViewModel VM => (ColorControlViewModel)DataContext;

        #region =============  Constructor & load/unload events  ================
        // Constructor
        public ColorControl()
        {
            DataContext = new ColorControlViewModel();
            Loaded += ColorControl_Loaded;
            Unloaded += ColorControl_Unloaded;
            VM.Color = Color; // Vm.Color must be equals to Color at the initial state (fixed bug when initial color is White)
        }

        private void ColorControl_Loaded(object sender, RoutedEventArgs e)
        {
            VM.PropertyChanged += VM_PropertyChanged;

            if (GetTemplateChild("TabControl") is TabControl tc)
            {
                tc.SelectionChanged -= OnTabControlSelectionChanged;
                tc.SelectionChanged += OnTabControlSelectionChanged;
            }

            foreach (var slider in this.GetVisualChildren<Canvas>().Where(c => c.DataContext is ColorControl_XYSlider))
            {
                slider.MouseDown -= Slider_MouseDown;
                slider.MouseUp -= Slider_MouseUp;
                slider.MouseMove -= Slider_MouseMove;
                slider.MouseDown += Slider_MouseDown;
                slider.MouseUp += Slider_MouseUp;
                slider.MouseMove += Slider_MouseMove;
            }

            foreach (var textBox in this.GetVisualChildren<TextBox>().Where(c => c.Name == "ComponentValue"))
            {
                textBox.PreviewTextInput -= ValueEditor_OnPreviewTextInput;
                textBox.PreviewTextInput += ValueEditor_OnPreviewTextInput;
            }

            if (GetTemplateChild("LeftPanel") is Panel leftPanel)
            {
                leftPanel.SizeChanged -= Control_OnSizeChanged;
                leftPanel.SizeChanged += Control_OnSizeChanged;
            }
            if (GetTemplateChild("RightPanel") is Panel rightPanel)
            {
                rightPanel.SizeChanged -= Control_OnSizeChanged;
                rightPanel.SizeChanged += Control_OnSizeChanged;
            }

            Control_OnSizeChanged(this, null);
        }

        private void VM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Color = VM.Color;
            IsAlphaSliderVisible = VM.IsAlphaSliderVisible;
        }

        private void ColorControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (GetTemplateChild("TabControl") is TabControl tc)
                tc.SelectionChanged -= OnTabControlSelectionChanged;

            foreach (var slider in this.GetVisualChildren<Canvas>().Where(c => c.DataContext is ColorControl_XYSlider))
            {
                slider.MouseDown -= Slider_MouseDown;
                slider.MouseUp -= Slider_MouseUp;
                slider.MouseMove -= Slider_MouseMove;
            }

            foreach (var textBox in this.GetVisualChildren<TextBox>().Where(c => c.Name == "ComponentValue"))
                textBox.PreviewTextInput -= ValueEditor_OnPreviewTextInput;

            if (GetTemplateChild("LeftPanel") is Panel leftPanel)
                leftPanel.SizeChanged -= Control_OnSizeChanged;
            if (GetTemplateChild("RightPanel") is Panel rightPanel)
                rightPanel.SizeChanged -= Control_OnSizeChanged;

            if (this.IsElementDisposing())
            {
                VM.Dispose();
                VM.PropertyChanged -= VM_PropertyChanged;
                DataContext = null;
            }
        }
        #endregion

        #region ==============  Event handlers  ====================
        private void Control_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            foreach (var cc in fe.GetVisualChildren<Canvas>().Where(cc => cc.DataContext is ColorControl_XYSlider))
                ((ColorControl_XYSlider)cc.DataContext).SetSizeOfControl(cc);

            VM.UpdateUI();
        }

        private void ValueEditor_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
            if (e.Text == "\t") // tab key
                return;
            if (string.IsNullOrWhiteSpace(e.Text) || e.Text.Length != 1)
            {
                Tips.Beep();
                return;
            }

            var valueEditor = (TextBox)sender;
            var newText = valueEditor.Text.Substring(0, valueEditor.SelectionStart) + e.Text +
                          valueEditor.Text.Substring(valueEditor.SelectionStart + valueEditor.SelectionLength);
            if (VM.CurrentCulture.NumberFormat.NativeDigits.Contains(e.Text))
                e.Handled = false;
            if (VM.CurrentCulture.NumberFormat.NumberDecimalSeparator == e.Text)
                e.Handled = valueEditor.Text.Contains(VM.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            else if (VM.CurrentCulture.NumberFormat.NegativeSign == e.Text)
                e.Handled = (newText.Length > 1 && newText.Substring(1, newText.Length - 2)
                                 .Contains(VM.CurrentCulture.NumberFormat.NegativeSign)) ||
                            !(newText.StartsWith(e.Text) || newText.EndsWith(e.Text));

            if (e.Handled)
                Tips.Beep();
        }

        private void Slider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            (sender as UIElement).CaptureMouse();
            Keyboard.ClearFocus();
        }

        private void Slider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            (sender as UIElement).ReleaseMouseCapture();
        }

        private void Slider_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var canvas = sender as Panel;
                var thumb = canvas.Children[0] as FrameworkElement;
                var isVertical = thumb is Grid;
                var x = Math.Max(0, Math.Min(1.0, isVertical
                    ? e.GetPosition(canvas).X / canvas.ActualWidth
                    : (e.GetPosition(canvas).X - thumb.ActualWidth / 2) / (canvas.ActualWidth - thumb.ActualWidth)));
                var y = Math.Max(0, Math.Min(1.0, e.GetPosition(canvas).Y / canvas.ActualHeight));
                ((ColorControl_XYSlider)canvas.DataContext).SetProperties(x, y);
            }
        }
        #endregion

        #region ==============  Color tabs   ===============
        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tc = (TabControl) sender;
            var selectedItem = tc.SelectedItem as TabItem;
            if ((selectedItem?.Content as ScrollViewer)?.Content is WrapPanel panel && panel.Children.Count == 0)
            {
                IEnumerable<KeyValuePair<string, Color>> data = null;
                if (selectedItem.Name == "BootstrapItem")
                    data = GetBootstrapColors();
                else if (selectedItem.Name == "KnownColorsByColorItem")
                    data = ColorUtils.GetKnownColors(false).OrderBy(kvp => GetSortNumberForColor(kvp.Value));
                else if (selectedItem.Name == "KnownColorsByNameItem")
                    data = ColorUtils.GetKnownColors(false).OrderBy(kvp => kvp.Key);

                if (data != null)
                {
                    var btnStyle = Application.Current.Resources["MonochromeButtonBaseStyle"] as Style; // Ripple effect
                    foreach (var kvp in data.Where(kvp => kvp.Value != Colors.Transparent))
                    {
                        var content = new LabelBox
                        {
                            Padding = new Thickness(0),
                            Text = GetColorLabel(kvp),
                            Background = Brushes.Transparent,
                            Foreground = new SolidColorBrush(ColorUtils.GetForegroundColor(kvp.Value)),
                            Cursor = Cursors.Arrow
                        };
                        var btn = new Button
                        {
                            Width = 130,
                            Padding = new Thickness(0, 2, 0, 2),
                            Margin = new Thickness(2),
                            BorderThickness = new Thickness(2),
                            HorizontalContentAlignment = HorizontalAlignment.Center,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            Content = content, // GetColorLabel(kvp),
                            Style = btnStyle
                        };
                        CornerRadiusEffect.SetCornerRadius(btn, new CornerRadius(2));
                        ChromeEffect.SetMonochrome(btn, kvp.Value);
                        ChromeEffect.SetChromeMatrix(btn, "+0%,+70%,+0%,40, +0%,+75%,+0%,100, +0%,+75%,+35%,100");
                        btn.PreviewMouseLeftButtonDown += (o, args) => VM.Color = ((SolidColorBrush)((Control)o).Background).Color;
                        panel.Children.Add(btn);
                    }
                }
            }
        }

        private string GetColorLabel(KeyValuePair<string, Color> kvp)
        {
            var s1 = string.Concat(kvp.Key.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
            var rgb = new RGB(kvp.Value);
            var rgbName = IsAlphaSliderVisible ? rgb.Color.ToString() : rgb.Color.ToString().Remove(1, 2);
            var hsl = new HSL(rgb);

            return $"{s1}{Environment.NewLine}{rgbName}{Environment.NewLine}HSL: {Math.Round(hsl.Hue)}, {Math.Round(hsl.Saturation)}, {Math.Round(hsl.Lightness)}";
        }

        private static int GetSortNumberForColor(Color color)
        {
            var hsl = new HSL(new RGB(color));
            return Convert.ToInt32(hsl.Hue) * 36000 + Convert.ToInt32(hsl.Saturation) * 100 + Convert.ToInt32(hsl.Lightness);
        }

        private static Dictionary<string, Color> GetBootstrapColors()
        {
            string[] bootstrapColorNames =
            {
                "PrimaryColor", "SecondaryColor", "SuccessColor", "DangerColor", "WarningColor", "InfoColor", "LightColor",
                "DarkColor", "BlueColor", "IndigoColor", "PurpleColor", "PinkColor", "RedColor", "OrangeColor",
                "YellowColor", "GreenColor", "TealColor", "CyanColor", "WhiteColor", "GrayColor", "GrayDarkColor"
            };
            var result = new Dictionary<string, Color>();
            foreach (var name in bootstrapColorNames)
                result.Add(name.Remove(name.Length - 5), (Color)Application.Current.Resources[name]);
            return result;
        }
        #endregion

        #region ==============  Dependency Properties  ===============
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color",
            typeof(Color), typeof(ColorControl), new FrameworkPropertyMetadata(Colors.White, OnColorChanged));

        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }
        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cc = (ColorControl)d;
            if (e.NewValue is Color color && cc.VM.Color != color)
                cc.VM.Color = color;
            cc.ColorChanged?.Invoke(cc, EventArgs.Empty);
        }
        //====================
        public static readonly DependencyProperty IsAlphaSliderVisibleProperty = DependencyProperty.Register("IsAlphaSliderVisible",
            typeof(bool), typeof(ColorControl), new FrameworkPropertyMetadata(false, IsAlphaSliderVisibleChanged));

        private static void IsAlphaSliderVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorControl cc && e.NewValue is bool value && cc.VM.IsAlphaSliderVisible != value)
                cc.VM.IsAlphaSliderVisible = value;
        }

        public bool IsAlphaSliderVisible
        {
            get => (bool)GetValue(IsAlphaSliderVisibleProperty);
            set => SetValue(IsAlphaSliderVisibleProperty, value);
        }
        #endregion
    }
}
