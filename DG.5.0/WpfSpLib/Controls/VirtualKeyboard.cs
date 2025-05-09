using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WpfSpLib.Common.ColorSpaces;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    /// <summary>
    /// Interaction logic for VirtualKeyboard.xaml
    /// </summary>
    public partial class VirtualKeyboard : Control, INotifyPropertyChanged
    {
        private static string _lastActiveLanguage;
        private const string DefaultLanguage = "EN";

        static VirtualKeyboard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VirtualKeyboard), new FrameworkPropertyMetadata(typeof(VirtualKeyboard)));
        }

        public VirtualKeyboard()
        {
            AvailableKeyboardLayouts = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures)
                .Where(a => KeyModel.KeyDefinition.LanguageDefinitions.Keys.Contains(a.IetfLanguageTag.ToUpper())).OrderBy(a => a.DisplayName)
                .Select(a => new LanguageModel(a.IetfLanguageTag)).ToArray();
            PrepareKeyboardSet(DefaultLanguage);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            OnUnloaded(sender, e);
            foreach (var language in AvailableKeyboardLayouts)
                language.OnSelect += Language_OnSelect;
            foreach (var button in this.GetVisualChildren<ButtonBase>().Where(a => a.DataContext is KeyModel))
                button.Click += Key_OnClick;
            if (!string.IsNullOrEmpty(_lastActiveLanguage))
                PrepareKeyboardSet(_lastActiveLanguage);
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            foreach (var language in AvailableKeyboardLayouts)
                language.OnSelect -= Language_OnSelect;
            foreach (var button in this.GetVisualChildren<ButtonBase>().Where(a => a.DataContext is KeyModel))
                button.Click -= Key_OnClick;
        }

        public event EventHandler OnReturnKeyClick;
        public LanguageModel[] AvailableKeyboardLayouts { get; }
        public LanguageModel SelectedKeyboardLayout => AvailableKeyboardLayouts.FirstOrDefault(a => a.IsSelected);// { get; set; }//  = _AvailableKeyboardLayouts.First(a => a.Item1.IetfLanguageTag == "en");
        public Dictionary<string, KeyModel> KeyboardSet { get; private set; }

        public bool IsCapsLock { get; private set; }
        public bool IsShifted { get; private set; }
        public bool IsExtra { get; private set; }

        private void Key_OnClick(object sender, EventArgs e)
        {
            var element = Keyboard.FocusedElement as FrameworkElement;
            var model = (KeyModel)((FrameworkElement)sender).DataContext;
            if (model.Id == KeyModel.KeyDefinition.KeyCode.VK_CAPITAL)
            {
                IsCapsLock = Equals(((ToggleButton)sender).IsChecked, true);
                OnPropertiesChanged(nameof(IsCapsLock));
            }
            else if (model.Id == KeyModel.KeyDefinition.KeyCode.VK_LSHIFT || model.Id == KeyModel.KeyDefinition.KeyCode.VK_RSHIFT)
            {
                IsShifted = Equals(((ToggleButton)sender).IsChecked, true);
                OnPropertiesChanged(nameof(IsShifted));
            }
            else if (model.Id == KeyModel.KeyDefinition.KeyCode.VK_EXTRA)
            {
                IsExtra = Equals(((ToggleButton)sender).IsChecked, true);
                OnPropertiesChanged(nameof(IsExtra));
            }
            else if (model.Id == KeyModel.KeyDefinition.KeyCode.VK_TAB)
            {
                if (element!=null) {
                    // var request = new TraversalRequest(IsShifted ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next) { Wrapped = true };
                    var request = new TraversalRequest(IsShifted ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next);
                    element.MoveFocus(request);
                }
            }
            else if (model.Id == KeyModel.KeyDefinition.KeyCode.VK_RETURN)
            {
                OnReturnKeyClick?.Invoke(this, EventArgs.Empty);
            }
            else
                EditTextBox(model);
        }

        private void EditTextBox(KeyModel keyModel)
        {
            var textBox = Keyboard.FocusedElement as TextBox;
            if (textBox != null && !textBox.IsReadOnly && textBox.IsEnabled)
            {
                var cursorPosition = textBox.SelectionStart;
                string newText = "";
                if (!keyModel.IsCommand)
                {
                    newText = keyModel.GetKeyText(IsCapsLock, IsShifted, IsExtra);
                    textBox.Text = textBox.Text.Substring(0, cursorPosition) + newText +
                                   textBox.Text.Substring(cursorPosition + textBox.SelectionLength);
                }
                else if (keyModel.Id == KeyModel.KeyDefinition.KeyCode.VK_LEFT)
                {
                    if (cursorPosition > 0) cursorPosition--;
                }
                else if (keyModel.Id == KeyModel.KeyDefinition.KeyCode.VK_RIGHT)
                {
                    if (textBox.SelectionLength > 0)
                        cursorPosition = Math.Min(textBox.Text.Length, cursorPosition + textBox.SelectionLength);
                    else if (cursorPosition < textBox.Text.Length) cursorPosition++;
                }
                else if (keyModel.Id == KeyModel.KeyDefinition.KeyCode.VK_BACK)
                {
                    if (textBox.SelectionLength > 0)
                    {
                        textBox.Text = textBox.Text.Substring(0, cursorPosition) +
                                             textBox.Text.Substring(cursorPosition + textBox.SelectionLength);
                    }
                    else if (cursorPosition > 0)
                    {
                        textBox.Text = textBox.Text.Substring(0, cursorPosition - 1) +
                                             textBox.Text.Substring(cursorPosition);
                        cursorPosition--;
                    }
                }
                else if (keyModel.Id == KeyModel.KeyDefinition.KeyCode.VK_DELETE)
                {
                    if (textBox.SelectionLength > 0)
                    {
                        textBox.Text = textBox.Text.Substring(0, cursorPosition) +
                                             textBox.Text.Substring(cursorPosition + textBox.SelectionLength);
                    }
                    else if (cursorPosition < textBox.Text.Length)
                    {
                        textBox.Text = textBox.Text.Substring(0, cursorPosition) +
                                             textBox.Text.Substring(cursorPosition + 1);
                    }
                }

                // Focused textbox & set selection
                if (!textBox.IsFocused)
                    textBox.Focus();
                textBox.SelectionStart = cursorPosition + (newText ?? "").Length;
                textBox.SelectionLength = 0;
            }
        }

        private void Language_OnSelect(object sender, EventArgs e)
        {
            PrepareKeyboardSet(((LanguageModel)sender).Id);
            _lastActiveLanguage = ((LanguageModel) sender).Id;
        }

        private void PrepareKeyboardSet(string language)
        {
            KeyboardSet = KeyModel.GetKeyboardSet(language);
            foreach (var item in AvailableKeyboardLayouts)
                item.IsSelected = item.Id == language;
            OnPropertiesChanged(nameof(KeyboardSet), nameof(AvailableKeyboardLayouts), nameof(SelectedKeyboardLayout));
        }

        #region ===========  INotifyPropertyChanged  ===============
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region ============== Properties/Events  ===================
        public static readonly DependencyProperty BaseHslProperty = DependencyProperty.Register("BaseHsl",
            typeof(HSL_Observable), typeof(VirtualKeyboard), new FrameworkPropertyMetadata(null));
        public HSL_Observable BaseHsl
        {
            get => (HSL_Observable)GetValue(BaseHslProperty);
            set => SetValue(BaseHslProperty, value);
        }
        //=======================
        public static readonly DependencyProperty BaseFontSizeProperty = DependencyProperty.Register("BaseFontSize",
            typeof(double), typeof(VirtualKeyboard), new FrameworkPropertyMetadata(0.0));
        public double BaseFontSize
        {
            get => (double)GetValue(BaseFontSizeProperty);
            set => SetValue(BaseFontSizeProperty, value);
        }
        #endregion
    }
}
