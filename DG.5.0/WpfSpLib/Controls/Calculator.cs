using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public class Calculator : Control, INotifyPropertyChanged
    {
        static Calculator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Calculator), new FrameworkPropertyMetadata(typeof(Calculator)));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(Calculator), new FrameworkPropertyMetadata(false));
        }

        private const int IntervalCountWhenChange = 100;
        private const int IntervalStepChange = 10;
        private int _numberOfIntervals = 0;

        internal static decimal GetIntervalMultiplier(int numberOfIntervals)
        {
            var power = numberOfIntervals / IntervalCountWhenChange;
            var result = 1m;
            for (var k = 0; k < power; k++)
                result *= IntervalStepChange;
            return result;
        }

        public Calculator()
        {
            ClickCommand = new RelayCommand(ButtonClickHandler);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var textBox in this.GetVisualChildren().OfType<TextBox>().Where(t => t.IsReadOnly && t.Focusable))
            {
                textBox.LostFocus -= TextBox_LostFocus;
                textBox.LostFocus += TextBox_LostFocus;
            }
        }
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            foreach (var textBox in this.GetVisualChildren().OfType<TextBox>().Where(t => t.IsReadOnly && t.Focusable))
                textBox.LostFocus -= TextBox_LostFocus;
        }

        public CultureInfo Culture => LocalizationHelper.CurrentCulture;
        public RelayCommand ClickCommand { get; }
        public string IndicatorText { get; private set; } = "0";
        public string ErrorText { get; private set; }

        private decimal? _firstOperand;
        private string _operator;
        private bool _lastButtonIsDigit = true;
        private decimal SecondOperand => decimal.Parse(IndicatorText, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Culture);

        public string StatusText => _firstOperand.HasValue && !string.IsNullOrEmpty(_operator)
            ? _firstOperand.Value.ToString(Culture) + " " + _operator
            : "";

        private string _oldDecimalSeparator;
        public string DecimalSeparator
        {
            get
            {
                var ds = Culture.NumberFormat.NumberDecimalSeparator;
                if (!Equals(_oldDecimalSeparator, ds))
                {
                    if (!string.IsNullOrEmpty(_oldDecimalSeparator) && !string.IsNullOrEmpty(IndicatorText))
                        IndicatorText = IndicatorText.Replace(_oldDecimalSeparator, ds);
                    _oldDecimalSeparator = ds;
                    RefrestUI();
                }
                return ds;
            }
        }

        #region ========  Override && events =================
        private void TextBox_LostFocus(object sender, RoutedEventArgs e) => e.Handled = true;

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            ResetInterval();
            Keyboard.Focus(this);
        }

        private static string[] _keyTexts = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "+", "-", "*", "/", "%", "C", ".", ",", "=", "\r", "\b" };
        private static Dictionary<Key, string> _keys = new Dictionary<Key, string>
        {
            {Key.Up, "++"}, {Key.Down, "--"}, {Key.Delete, "Delete"}
        };
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            if (_keyTexts.Contains(e.Text.ToUpper()))
            {
                ButtonClickHandler(e.Text.ToUpper());
                e.Handled = true;
            }
            else if (e.Text.Length == 1)
                Tips.Beep();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (Keyboard.Modifiers == ModifierKeys.None && _keys.ContainsKey(e.Key))
            {
                ButtonClickHandler(_keys[e.Key]);
                e.Handled = true;
            }
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);
            ResetInterval();
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);
            ExecuteOperator(e.Delta > 0 ? "++" : "--");
        }
        #endregion

        // ============================
        private void ButtonClickHandler(object p)
        {
            if (char.IsDigit(((string)p)[0]))
                ExecuteDigit((string)p);
            else
                ExecuteOperator((string)p);
        }

        private void ExecuteDigit(string digit)
        {
            if (!string.IsNullOrEmpty(ErrorText))
                Clear();

            CheckLastButtonIsDigit(true);

            if (IndicatorText == "0")
                IndicatorText = digit;
            else
                IndicatorText += digit;
            _lastButtonIsDigit = true;
            RefrestUI();
        }

        private void ExecuteOperator(string cmd)
        {
            if (!string.IsNullOrEmpty(ErrorText) && cmd != "Clear")
                return;

            switch (cmd)
            {
                case "Delete":
                    CheckLastButtonIsDigit(false);
                    IndicatorText = "0";
                    break;

                case "++":
                    CheckLastButtonIsDigit(false);
                    var multiplier = GetIntervalMultiplier(_numberOfIntervals++);
                    IndicatorText = (SecondOperand + multiplier).ToString(Culture);
                    break;

                case "--":
                    CheckLastButtonIsDigit(false);
                    var multiplier1 = GetIntervalMultiplier(_numberOfIntervals++);
                    IndicatorText = (SecondOperand - multiplier1).ToString(Culture);
                    break;

                case ",":
                case ".":
                    CheckLastButtonIsDigit(true);
                    if (IndicatorText.Contains(DecimalSeparator))
                        IndicatorText = IndicatorText.Replace(DecimalSeparator, "");
                    IndicatorText += DecimalSeparator;
                    break;

                case "Backspace":
                case "\b":
                    CheckLastButtonIsDigit(false);
                    IndicatorText = IndicatorText.Substring(0, IndicatorText.Length - 1);
                    if (IndicatorText.Length == 0 || IndicatorText == "-")
                        IndicatorText = "0";
                    break;

                case "Sign":
                    CheckLastButtonIsDigit(false);
                    if (!string.Equals(IndicatorText, "0", StringComparison.Ordinal))
                    {
                        if (IndicatorText.StartsWith("-"))
                            IndicatorText = IndicatorText.Substring(1);
                        else
                            IndicatorText = "-" + IndicatorText;
                    }
                    break;

                case "Clear":
                case "C":
                    Clear();
                    break;

                case "\r":
                case "=":
                    if (string.IsNullOrEmpty(_operator) || !_firstOperand.HasValue)
                    {
                        IndicatorText = NormalizeDecimalString(IndicatorText);
                        Value = SecondOperand;
                        _lastButtonIsDigit = true;
                    }
                    else
                    {
                        Calculate();
                        if (string.IsNullOrEmpty(ErrorText))
                        {
                            IndicatorText = _firstOperand.Value.ToString(Culture); 
                            Value = _firstOperand;
                            _firstOperand = null;
                            _lastButtonIsDigit = false;
                        }
                    }

                    var popup = this.GetVisualParents().OfType<Popup>().FirstOrDefault();
                    if (popup != null && popup.IsOpen)
                        popup.IsOpen = false;

                    break;

                default:
                    if (_firstOperand.HasValue && !string.IsNullOrEmpty(_operator) && _lastButtonIsDigit)
                    { // do operation
                        Calculate();
                        if (string.IsNullOrEmpty(ErrorText))
                        {
                            _operator = cmd;
                            IndicatorText = "0";
                        }
                    }
                    else if (_firstOperand.HasValue && !string.IsNullOrEmpty(_operator) && !_lastButtonIsDigit)
                        // change operand
                        _operator = cmd;
                    else if (!_firstOperand.HasValue && string.IsNullOrEmpty(_operator))
                    { // add operand
                        _firstOperand = NormalizeDecimal(SecondOperand);
                        _operator = cmd;
                        _lastButtonIsDigit = false;
                        IndicatorText = "0";
                    }
                    else
                        throw new Exception("Unknown state!!!");

                    break;
            }

            RefrestUI();
        }

        private decimal NormalizeDecimal(decimal d) => decimal.Parse(NormalizeDecimalString(d.ToString(Culture)), Culture);

        private string NormalizeDecimalString(string decimalString)
        {
            while (decimalString.Contains(DecimalSeparator))
            {
                if (decimalString.EndsWith(DecimalSeparator) || decimalString.EndsWith("0"))
                    decimalString = decimalString.Substring(0, decimalString.Length - 1);
                else
                    break;
            }

            return decimalString;
        }

        private void Calculate()
        {
            try
            {
                switch (_operator)
                {
                    case "+": _firstOperand += SecondOperand; break;
                    case "-": _firstOperand -= SecondOperand; break;
                    case "*": _firstOperand *= SecondOperand; break;
                    case "/":
                        if (SecondOperand == 0M)
                            ErrorText = "?? /0";
                        else
                            _firstOperand /= SecondOperand;
                        break;
                    case "%": _firstOperand *= SecondOperand / 100M; break;
                }

                _operator = null;
            }
            catch (OverflowException exception)
            {
                ErrorText = "?? Overflow";
            }
            catch (Exception exception)
            {
                ErrorText = "??";
            }

            if (!string.IsNullOrEmpty(ErrorText)) { 
                _firstOperand = null;
                _operator = null;
            }

            if (_firstOperand.HasValue)
                _firstOperand = NormalizeDecimal(Math.Round(_firstOperand.Value, DecimalPlaces));

            _lastButtonIsDigit = false;
        }

        private void CheckLastButtonIsDigit(bool resetValue)
        {
            if (!_lastButtonIsDigit)
            {
                if (resetValue)
                    IndicatorText = "0";
                _lastButtonIsDigit = true;
            }
        }

        private void OnValueChanged(decimal? newValue)
        {
            Clear();
            IndicatorText = newValue.HasValue ? newValue.Value.ToString(Culture) : "0";
            RefrestUI();
        }

        private void Clear()
        {
            _firstOperand = null;
            _lastButtonIsDigit = true;
            _operator = null;
            IndicatorText = "0";
            ErrorText = null;
        }

        private void ResetInterval() => _numberOfIntervals = 0;

        private void RefrestUI()
        {
            if (_firstOperand.HasValue)
                _firstOperand = Math.Round(_firstOperand.Value, DecimalPlaces);

            var endWithDecimalSeparator = IndicatorText.EndsWith(DecimalSeparator);
            IndicatorText = Math.Round(SecondOperand, DecimalPlaces).ToString(Culture);
            if (endWithDecimalSeparator && !IndicatorText.EndsWith(DecimalSeparator))
                IndicatorText += DecimalSeparator;

            OnPropertiesChanged(nameof(IndicatorText), nameof(StatusText), nameof(ErrorText), nameof(DecimalSeparator));
        }

        #region ===========  INotifyPropertyChanged  ===============
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region ===========  DependencyProperty  =================
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(decimal?),
            typeof(Calculator), new PropertyMetadata(null, (d, e) => ((Calculator)d).OnValueChanged(e.NewValue as decimal?)));
        public decimal? Value
        {
            get => (decimal?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        //=============
        public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register("DecimalPlaces",
            typeof(int), typeof(Calculator), new FrameworkPropertyMetadata(8, (d, e) => ((Calculator)d).RefrestUI()));
        /// <summary>
        /// Rounding decimal places (from 0 to +19);
        /// </summary>
        public int DecimalPlaces
        {
            get => (int)GetValue(DecimalPlacesProperty);
            set => SetValue(DecimalPlacesProperty, value);
        }
        #endregion
    }
}
