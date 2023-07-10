using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfSpLib.Helpers;
using WpfSpLib.Themes;

namespace WpfSpLib.Controls
{
    /// <summary>
    /// Interaction logic for ThemeSelector.xaml
    /// </summary>
    public partial class ThemeSelector: INotifyPropertyChanged
    {
        private IColorThemeSupport _target;

        public IColorThemeSupport Target
        {
            get => _target;
            set
            {
                _target = value;

                var d = _target as DependencyObject;
                DefaultTheme = _target?.ActualTheme;
                if (_target?.Theme != null)
                {
                    var a1 = d.GetVisualParents().OfType<IColorThemeSupport>().FirstOrDefault(a => !Equals(a, _target) && a.Theme != null);
                    DefaultTheme = a1?.Theme ?? MwiThemeInfo.DefaultTheme;
                }

                DefaultThemeColor = _target?.ActualThemeColor ?? MwiThemeInfo.DefaultThemeColor;
                if (_target?.ThemeColor != null)
                {
                    var a1 = d.GetVisualParents().OfType<IColorThemeSupport>().FirstOrDefault(a => !Equals(a, _target) && a.ThemeColor != null);
                    DefaultThemeColor = a1?.ThemeColor ?? MwiThemeInfo.DefaultThemeColor;
                }

                Theme = _target?.Theme;
                ThemeColor = _target?.ThemeColor;
            }
        }

        public List<Tuple<MwiThemeInfo, Color?>> Changes = new List<Tuple<MwiThemeInfo, Color?>>();
        public MwiThemeInfo Theme { get; set; }
        public Color? ThemeColor { get; set; }
        public MwiThemeInfo DefaultTheme { get; set; }
        public Color DefaultThemeColor { get; set; }
        public bool UseDefaultThemeColor => cbUseDefaultTheme.IsChecked == true;
        public bool UseDefaultColor => cbUseDefaultColor.IsChecked == true;
        public MwiThemeInfo ActualTheme => Theme ?? DefaultTheme;
        public Color ActualThemeColor => ActualTheme?.FixedColor ?? (ThemeColor ?? DefaultThemeColor);
        public bool IsThemeSelectorEnabled => !UseDefaultThemeColor;
        public bool IsColorSelectorEnabled => ActualTheme != null && !ActualTheme.FixedColor.HasValue;
        public bool IsColorControlEnabled => IsColorSelectorEnabled && !UseDefaultColor;
        public bool IsApplyButtonEnabled => !(Equals(Target?.Theme, Theme) && Equals(Target?.ThemeColor, ThemeColor));
        public bool IsRestoreButtonEnabled => Changes.Count > 0;

        public ThemeSelector()
        {
            InitializeComponent();
            Loaded += ThemeSelector_Loaded;
            Unloaded += ThemeSelector_Unloaded;
        }

        private void ThemeSelector_Loaded(object sender, RoutedEventArgs e)
        {
            ColorControl.ColorChanged -= OnColorChanged;
            ColorControl.ColorChanged += OnColorChanged;
        }
        private void ThemeSelector_Unloaded(object sender, RoutedEventArgs e)
        {
            ColorControl.ColorChanged -= OnColorChanged;
        }

        private void OnColorChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;
            if (IsColorControlEnabled)
                ThemeColor = ColorControl.Color;
            UpdateUI();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ThemeList.Children.Clear();
            var radioButtonStyle = (Style)Resources["MonochromeRadioButtonStyle"];
            foreach (var theme in MwiThemeInfo.Themes)
            {
                var content = new TextBlock { Background = Brushes.Transparent, TextWrapping = TextWrapping.Wrap };
                var btn = new RadioButton {Content = content, IsChecked = Equals(theme.Value, Theme), Style = radioButtonStyle};
                BindingOperations.SetBinding(content, TextBlock.TextProperty, new Binding("Name") {Source = theme.Value, Mode = BindingMode.OneWay});
                btn.Checked += OnRadioButtonChecked;
                ThemeList.Children.Add(btn);
            }

            UpdateUI();
        }

        private bool _isUpdating;
        private void UpdateUI()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            var checkedRadioButton = ThemeList.Children.OfType<RadioButton>().FirstOrDefault(a => GetThemeFromButton(a) == ActualTheme);
            if (checkedRadioButton != null)
                checkedRadioButton.IsChecked = true;

            ColorControl.Color = ActualThemeColor;

            cbUseDefaultTheme.IsChecked = Theme == null;
            cbUseDefaultColor.IsChecked = !ThemeColor.HasValue;

            OnPropertiesChanged(nameof(ActualTheme), nameof(ActualThemeColor),
                nameof(IsApplyButtonEnabled), nameof(IsRestoreButtonEnabled),
                nameof(IsThemeSelectorEnabled), nameof(IsColorSelectorEnabled), nameof(IsColorControlEnabled));
            
            _isUpdating = false;
        }

        private void OnRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            foreach (RadioButton btn in ThemeList.Children)
            {
                if (Equals(btn.IsChecked, true))
                {
                    Theme = GetThemeFromButton(btn);
                    break;
                }
            }
            _isUpdating = false;

            UpdateUI();
        }

        private static MwiThemeInfo GetThemeFromButton(RadioButton button)
        {
            var content = (TextBlock)button.Content;
            var binding = BindingOperations.GetBinding(content, TextBlock.TextProperty);
            return (MwiThemeInfo)binding.Source;
        }

        private void OnUseDefaultColorChanged(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox) sender;
            ThemeColor = cb.IsChecked == true ? (Color?)null : ActualThemeColor;
            UpdateUI();
        }

        private void OnUseDefaultThemeChanged(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)sender;
            Theme = cb.IsChecked == true ? null : ActualTheme;
            UpdateUI();
        }

        private void OnApplyAndCloseButtonClick(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            ApplicationCommands.Close.Execute(null, this);
        }

        #region ===========  INotifyPropertyChanged  ===============
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void OnApplyButtonClick(object sender, RoutedEventArgs e)
        {
            Changes.Add(new Tuple<MwiThemeInfo, Color?>(Target.Theme, Target.ThemeColor));
            ApplyTheme();
        }

        private void OnRestoreButtonClick(object sender, RoutedEventArgs e)
        {
            if (Changes.Count > 0)
            {
                Theme = Changes[Changes.Count - 1].Item1;
                ThemeColor = Changes[Changes.Count - 1].Item2;
                Changes.RemoveAt(Changes.Count - 1);
                ApplyTheme();
            }
        }

        private void ApplyTheme()
        {
            Target.Theme = Theme;
            if (Target.ActualTheme.FixedColor.HasValue)
            {
                if (Target is Control cntrl)
                    cntrl.Background = null;
            }
            else
                Target.ThemeColor = ThemeColor;
            UpdateUI();
        }
    }
}
