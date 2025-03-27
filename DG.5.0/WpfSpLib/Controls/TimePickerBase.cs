// From MahApps.Metro.Controls

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfSpLib.Common;
using WpfSpLib.Controls.TimePickerHelper;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    /// <summary>
    ///     Represents a base-class for time picking.
    /// </summary>
    public class TimePickerBase : Control, INotifyPropertyChanged
    {
        [Flags]
        public enum Buttons
        {
            Popup = 1,
            Clear = 2,
            Separator1px = 4,
            Separator = 8
        }

        public static readonly RoutedEvent SelectedTimeChangedEvent = EventManager.RegisterRoutedEvent("SelectedTimeChanged", RoutingStrategy.Direct,
            typeof(RoutedPropertyChangedEventHandler<TimeSpan?>), typeof(TimePickerBase));

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool),
            typeof(TimePickerBase), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty SelectedTimeProperty = DependencyProperty.Register("SelectedTime", typeof(TimeSpan?),
            typeof(TimePickerBase), new FrameworkPropertyMetadata(default(TimeSpan?), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTimeChanged, CoerceSelectedTime));

        public static readonly DependencyProperty SelectedTimeFormatProperty = DependencyProperty.Register(nameof(SelectedTimeFormat), typeof(DatePickerFormat),
            typeof(TimePickerBase), new PropertyMetadata(DatePickerFormat.Long, OnSelectedTimeFormatChanged));

        public static readonly DependencyProperty VisibleButtonsProperty = DependencyProperty.Register("VisibleButtons",
            typeof(Buttons?), typeof(TimePickerBase), new FrameworkPropertyMetadata(null, OnVisibleButtonsChanged));

        #region Do not change order of fields inside this region

        /// <summary>
        /// This readonly dependency property is to control whether to show the date-picker (in case of <see cref="TimePicker"/>) or hide it (in case of <see cref="DateTimePicker"/>.
        /// </summary>
        private static readonly DependencyPropertyKey IsDatePickerVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
          "IsDatePickerVisible", typeof(bool), typeof(TimePickerBase), new PropertyMetadata(true));

        [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:ElementsMustBeOrderedByAccess", Justification = "Otherwise we have \"Static member initializer refers to static member below or in other type part\" and thus resulting in having \"null\" as value")]
        public static readonly DependencyProperty IsDatePickerVisibleProperty = IsDatePickerVisiblePropertyKey.DependencyProperty;

        #endregion

        private static readonly TimeSpan MinTimeOfDay = TimeSpan.Zero;
        private static readonly TimeSpan MaxTimeOfDay = TimeSpan.FromDays(1) - TimeSpan.FromTicks(1);

        public string RegionChangeHook
        {
            get
            {
                ApplyCulture();
                return null;
            }
        }

        public CultureInfo Culture => LocalizationHelper.CurrentCulture;
        public bool IsPopupButtonVisible => (VisibleButtons & Buttons.Popup) == Buttons.Popup;
        public bool IsClearButtonVisible => (VisibleButtons & Buttons.Clear) == Buttons.Clear;
        public bool IsSeparatorVisible => (VisibleButtons & Buttons.Separator) == Buttons.Separator ||
                                          (VisibleButtons & Buttons.Separator1px) == Buttons.Separator1px;
        public double SeparatorWidth => (VisibleButtons & Buttons.Separator) == Buttons.Separator ? BorderThickness.Right : 1.0;

        private Button _clearButton;
        private bool _isTimeChanging;
        private bool _isTextChanging;
        private bool _textInputChanged;
        protected Popup _popup;
        protected TextBox _textBox;

        static TimePickerBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePickerBase), new FrameworkPropertyMetadata(typeof(TimePickerBase)));
            EventManager.RegisterClassHandler(typeof(TimePickerBase), GotFocusEvent,
                new RoutedEventHandler((sender, args) => ControlHelper.OnGotFocusOfControl(sender, args, ((TimePickerBase)sender)._textBox)));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(TimePickerBase), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(TimePickerBase), new FrameworkPropertyMetadata(false));
            VerticalContentAlignmentProperty.OverrideMetadata(typeof(TimePickerBase), new FrameworkPropertyMetadata(VerticalAlignment.Center));
        }

        protected TimePickerBase()
        {
        }

        /// <summary>
        ///     Occurs when the <see cref="SelectedTime" /> property is changed.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TimeSpan?> SelectedTimeChanged
        {
            add => AddHandler(SelectedTimeChangedEvent, value);
            remove => RemoveHandler(SelectedTimeChangedEvent, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the date can be selected or not. This property is read-only.
        /// </summary>
        public bool IsDatePickerVisible
        {
            get => (bool)GetValue(IsDatePickerVisibleProperty);
            protected set => SetValue(IsDatePickerVisiblePropertyKey, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the contents of the <see cref="TimePickerBase" /> are not editable.
        /// </summary>
        /// <returns>
        ///     true if the <see cref="TimePickerBase" /> is read-only; otherwise, false. The default is false.
        /// </returns>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        ///     Gets or sets the currently selected time.
        /// </summary>
        /// <returns>
        ///     The time currently selected. The default is null.
        /// </returns>
        public TimeSpan? SelectedTime
        {
            get => (TimeSpan?)GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);
        }

        /// <summary>
        /// Gets or sets the format that is used to display the selected time.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(DatePickerFormat.Long)]
        public DatePickerFormat SelectedTimeFormat
        {
            get => (DatePickerFormat)GetValue(SelectedTimeFormatProperty);
            set => SetValue(SelectedTimeFormatProperty, value);
        }

        public Buttons? VisibleButtons
        {
            get => (Buttons?)GetValue(VisibleButtonsProperty);
            set => SetValue(VisibleButtonsProperty, value);
        }

        protected void ClosePopup()
        {
            if (_popup != null && _popup.IsOpen)
                _popup.IsOpen = false;
            Dispatcher.InvokeAsync(new Action(() => _textBox?.Focus()), DispatcherPriority.ContextIdle);
        }

        /// <summary>
        ///     When overridden in a derived class, is invoked whenever application code or internal processes call
        ///     <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            UnSubscribeEvents();

            _popup = GetTemplateChild("PART_Popup") as Popup;
            _clearButton = GetTemplateChild("PART_ClearButton") as Button;
            _textBox = GetTemplateChild("PART_TextBox") as TextBox;

            _nightHourSelector = GetTemplateChild("PART_NightHourSelector") as DataGrid;
            _dayHourSelector = GetTemplateChild("PART_DayHourSelector") as DataGrid;
            _minuteSelector = GetTemplateChild("PART_MinuteSelector") as DataGrid;
            _secondSelector = GetTemplateChild("PART_SecondSelector") as DataGrid;

            CoerceValue(SelectedTimeProperty);
            SetSecondVisibility();
            SubscribeEvents();
            ApplyCulture();
        }

        private void DrawClockDivisions()
        {
            if (!((GetTemplateChild("PART_Clock") as Viewbox)?.Child is Canvas canvas) || canvas.Children.Count > 60)
                return;

            var brush = FindResource("BlackBrush") as Brush;
            for (var k = 0; k < 60; k++)
            {
                var isMinute = k % 5 == 0;
                var division = new Rectangle
                {
                    Width = 2, Height = isMinute ? 6 : 2, Fill = brush,
                    Margin = new Thickness(59, isMinute ? 3 : 5, 0, 0),
                    RenderTransform = new RotateTransform(6 * k, 1, isMinute ? 57 : 55)
                };
                canvas.Children.Insert(0, division);
            }

            for (var k = 0; k < 12; k++)
            {
                var label = new TextBlock
                {
                    Text = (k == 0 ? 12 : k).ToString(), Foreground = brush,
                    Margin = new Thickness(), Padding = new Thickness(),
                    FontWeight = FontWeights.Normal, FontSize = 14, FontFamily = new FontFamily("Segoe UI")
                };

                label.Measure(new Size(double.MaxValue, double.MaxValue));
                label.Margin = new Thickness(60 - label.DesiredSize.Width / 2, label.DesiredSize.Height / 2, 0, 0);
                var transform = new TransformGroup();
                transform.Children.Add(new RotateTransform(-30 * k, label.DesiredSize.Width / 2, label.DesiredSize.Height / 2));
                transform.Children.Add(new RotateTransform(30 * k, label.DesiredSize.Width / 2, 60 - label.DesiredSize.Height / 2));
                label.RenderTransform = transform;

                canvas.Children.Insert(0, label);
            }
        }

        protected virtual void ApplyCulture()
        {
            _isTimeChanging = true;
            if (_nightHourSelector != null)
                _nightHourSelector.ItemsSource = IsAmPmMode ? SelectorRow.NightHourRows: SelectorRow.HourRows;
            SetSelectorValues(); // ???
            _isTimeChanging = false;

            OnPropertiesChanged(nameof(AMText), nameof(PMText), nameof(IsAmPmMode));
            WriteValueToTextBox();
        }

        protected virtual void SubscribeEvents()
        {
            if (_clearButton != null)
                _clearButton.Click += OnClearButtonClick;

            if (_textBox != null)
            {
                _textBox.TextChanged += OnTextChanged;
                _textBox.LostFocus += InternalOnTextBoxLostFocus;
            }

            if (_popup != null)
                _popup.Opened += OnPopupOpened;

            if (_nightHourSelector != null)
            {
                _nightHourSelector.AutoGeneratingColumn += PART_HourSelector_OnAutoGeneratingColumn;
                _nightHourSelector.PreviewMouseLeftButtonUp += OnSelectorPreviewMouseLeftButtonDown;
            }

            if (_dayHourSelector != null)
            {
                _dayHourSelector.AutoGeneratingColumn += PART_HourSelector_OnAutoGeneratingColumn;
                _dayHourSelector.PreviewMouseLeftButtonUp += OnSelectorPreviewMouseLeftButtonDown;
            }

            if (_minuteSelector != null)
                _minuteSelector.PreviewMouseLeftButtonUp += OnSelectorPreviewMouseLeftButtonDown;

            if (_secondSelector != null)
                _secondSelector.PreviewMouseLeftButtonUp += OnSelectorPreviewMouseLeftButtonDown;
        }
        protected virtual void UnSubscribeEvents()
        {
            if (_clearButton != null)
                _clearButton.Click -= OnClearButtonClick;

            if (_textBox != null)
            {
                _textBox.TextChanged -= OnTextChanged;
                _textBox.LostFocus -= InternalOnTextBoxLostFocus;
            }

            if (_popup != null)
                _popup.Opened -= OnPopupOpened;

            if (_nightHourSelector != null)
            {
                _nightHourSelector.AutoGeneratingColumn -= PART_HourSelector_OnAutoGeneratingColumn;
                _nightHourSelector.PreviewMouseLeftButtonUp -= OnSelectorPreviewMouseLeftButtonDown;
            }

            if (_dayHourSelector != null)
            {
                _dayHourSelector.AutoGeneratingColumn -= PART_HourSelector_OnAutoGeneratingColumn;
                _dayHourSelector.PreviewMouseLeftButtonUp -= OnSelectorPreviewMouseLeftButtonDown;
            }

            if (_minuteSelector != null)
                _minuteSelector.PreviewMouseLeftButtonUp -= OnSelectorPreviewMouseLeftButtonDown;

            if (_secondSelector != null)
                _secondSelector.PreviewMouseLeftButtonUp -= OnSelectorPreviewMouseLeftButtonDown;
        }

        protected virtual string GetValueForTextBox()
        {
            var format = SelectedTimeFormat == DatePickerFormat.Long ? string.Intern(Culture.DateTimeFormat.LongTimePattern) : string.Intern(Culture.DateTimeFormat.ShortTimePattern);
            var valueForTextBox = (DateTime.MinValue + SelectedTime)?.ToString(string.Intern(format), Culture);
            return valueForTextBox;
        }

        protected virtual void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var textBoxValue = ((TextBox)sender).Text;
            TimeSpan? ts = null;
            if (!string.IsNullOrWhiteSpace(textBoxValue))
            {
                var text = string.Intern($"{DateTime.MinValue.ToString(Culture.DateTimeFormat.ShortDatePattern)} {((TextBox)sender).Text}");
                DateTime dt;
                if (DateTime.TryParse(text, Culture, DateTimeStyles.None, out dt))
                    ts = dt.TimeOfDay;
            }
            SelectedTime = ts;
            WriteValueToTextBox();
        }

        protected virtual void OnSelectedTimeChanged(RoutedPropertyChangedEventArgs<TimeSpan?> e)
        {
            RaiseEvent(e);

            var oldSeconds = e.OldValue.HasValue ? 360.0 / 60 * (e.OldValue.Value.TotalSeconds % 60.0) : 0;
            var newSeconds = e.NewValue.HasValue ? 360.0 / 60 * (e.NewValue.Value.TotalSeconds % 60.0) : 0;
            var secondHand = GetTemplateChild("PART_SecondHand") as Shape;
            AnimateHand(secondHand, newSeconds, oldSeconds);

            var oldMinutes = e.OldValue.HasValue ? 360.0 / 60 * (e.OldValue.Value.TotalMinutes % 60.0) : 0;
            var newMinutes = e.NewValue.HasValue ? 360.0 / 60 * (e.NewValue.Value.TotalMinutes % 60.0) : 0;
            var minuteHand = GetTemplateChild("PART_MinuteHand") as Shape;
            AnimateHand(minuteHand, newMinutes, oldMinutes);

            var oldHours = e.OldValue.HasValue ? 360.0 / 12 * (e.OldValue.Value.TotalHours % 12.0) : 0;
            var newHours = e.NewValue.HasValue ? 360.0 / 12 * (e.NewValue.Value.TotalHours % 12.0) : 0;
            var hourHand = GetTemplateChild("PART_HourHand") as Shape;
            AnimateHand(hourHand, newHours, oldHours);
        }

        private void AnimateHand(Shape hand, double newValue, double oldValue)
        {
            if (hand == null || Tips.AreEqual(newValue, oldValue)) return;

            if (!(hand.RenderTransform is RotateTransform))
                hand.RenderTransform = new RotateTransform {Angle = oldValue};

            hand.RenderTransform.BeginAnimation(RotateTransform.AngleProperty,new DoubleAnimation(oldValue, newValue, AnimationHelper.AnimationDuration));
        }

        private static void OnSelectedTimeFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimePickerBase tp)
            {
                tp.SelectedTime = (TimeSpan?)CoerceSelectedTime(d, tp.SelectedTime);
                tp.SetSecondVisibility();
                tp.WriteValueToTextBox();
            }
        }

        private void OnSelectorPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Equals(sender, _nightHourSelector))
                _dayHourSelector.SelectedCells.Clear();
            else if (Equals(sender, _dayHourSelector))
                _nightHourSelector.SelectedCells.Clear();
            
            OnBaseValueChanged(sender, null);

            if (Equals(sender, _secondSelector) || (Equals(sender, _minuteSelector) && SelectedTimeFormat == DatePickerFormat.Short))
                ClosePopup();

            e.Handled = true;
        }

        protected virtual void OnPopupOpened(object sender, EventArgs e)
        {
            InternalOnTextBoxLostFocus(_textBox, null);
            DrawClockDivisions();
            SetSelectorValues();
        }

        protected virtual void WriteValueToTextBox()
        {
            if (_textBox != null)
            {
                _isTextChanging = true;
                _textBox.Text = GetValueForTextBox();
                _isTextChanging = false;
            }
        }

        protected virtual void InternalOnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (_textInputChanged)
            {
                _textInputChanged = false;
                OnTextBoxLostFocus(sender, e);
            }
        }

        private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (TimePickerBase)d;
            if (picker._isTimeChanging)
                return;

            picker.SetSelectorValues();
            picker.OnSelectedTimeChanged(new RoutedPropertyChangedEventArgs<TimeSpan?>((TimeSpan?)e.OldValue, (TimeSpan?)e.NewValue, SelectedTimeChangedEvent));
            picker.WriteValueToTextBox();
        }
        private static object CoerceSelectedTime(DependencyObject d, object baseValue)
        {
            var ts = (TimeSpan?)baseValue;
            var picker = (TimePickerBase)d;
            if (!ts.HasValue && !picker.IsNullable)
                ts = MinTimeOfDay;
            if (ts < MinTimeOfDay)
                ts = MinTimeOfDay;
            if (ts > MaxTimeOfDay)
                ts = MaxTimeOfDay;
            if (ts.HasValue)
                ts = new TimeSpan(ts.Value.Hours, ts.Value.Minutes, picker.SelectedTimeFormat == DatePickerFormat.Long ? ts.Value.Seconds : 0);
            return ts;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isTextChanging)
                _textInputChanged = true;
        }

        private void SetSecondVisibility()
        {
            if (_secondSelector != null)
                _secondSelector.Visibility = SelectedTimeFormat == DatePickerFormat.Short ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SetSelectorValues()
        {
            if (_isTimeChanging)
                return;

            _isTimeChanging = true;
            if (_nightHourSelector != null)
            {
                _nightHourSelector.SelectedCells.Clear();
                if (_nightHourSelector.Columns.Count > 0 && SelectedTime.HasValue && (SelectedTime.Value.Hours < 12 || !IsAmPmMode))
                    _nightHourSelector.SelectedCells.Add(new DataGridCellInfo(_nightHourSelector.Items[SelectedTime.Value.Hours / 3], _nightHourSelector.Columns[SelectedTime.Value.Hours % 3]));
            }
            if (_dayHourSelector != null)
            {
                _dayHourSelector.SelectedCells.Clear();
                if (_dayHourSelector.Columns.Count > 0 && SelectedTime.HasValue && SelectedTime.Value.Hours >= 12 && IsAmPmMode)
                    _dayHourSelector.SelectedCells.Add(new DataGridCellInfo(_dayHourSelector.Items[(SelectedTime.Value.Hours - 12) / 3], _dayHourSelector.Columns[SelectedTime.Value.Hours % 3]));
            }
            if (_minuteSelector != null)
            {
                _minuteSelector.SelectedCells.Clear();
                if (_minuteSelector.Columns.Count > 0 && SelectedTime.HasValue)
                    _minuteSelector.SelectedCells.Add(new DataGridCellInfo(_minuteSelector.Items[SelectedTime.Value.Minutes / 5], _minuteSelector.Columns[SelectedTime.Value.Minutes % 5]));
            }
            if (_secondSelector != null)
            {
                _secondSelector.SelectedCells.Clear();
                if (_secondSelector.Columns.Count > 0 && SelectedTime.HasValue)
                    _secondSelector.SelectedCells.Add(new DataGridCellInfo(_secondSelector.Items[SelectedTime.Value.Seconds / 5], _secondSelector.Columns[SelectedTime.Value.Seconds % 5]));
            }

            _isTimeChanging = false;
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e) => ClearValue();
        protected virtual void ClearValue() => SelectedTime = IsNullable ? (TimeSpan?)null : TimeSpan.Zero;

        private static void OnVisibleButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimePickerBase) d).OnPropertiesChanged(nameof(IsPopupButtonVisible), nameof(IsClearButtonVisible),
                nameof(IsSeparatorVisible), nameof(SeparatorWidth));
        }

        //=============================================
        public static readonly DependencyProperty IsNullableProperty = DependencyProperty.Register("IsNullable", typeof(bool), typeof(TimePickerBase), new FrameworkPropertyMetadata(false, OnIsNullableChanged));
        /// <summary>
        /// Can the value be null?
        /// </summary>
        public bool IsNullable
        {
            get => (bool)GetValue(IsNullableProperty);
            set => SetValue(IsNullableProperty, value);
        }

        private static void OnIsNullableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
                ((TimePickerBase)d).IsNullableChanged();
        }

        protected virtual void IsNullableChanged()
        {
            if (!IsNullable && !SelectedTime.HasValue)
                SelectedTime = TimeSpan.Zero;
        }

        //=================================================
        // =============  Time selector =================
        //=================================================

        private DataGrid _nightHourSelector;
        private DataGrid _dayHourSelector;
        private DataGrid _minuteSelector;
        private DataGrid _secondSelector;

        //==================================
        protected virtual void OnBaseValueChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            SelectedTime = GetSelectedTimeFromSelectors();
        }

        private void PART_HourSelector_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Cancel = e.PropertyName == "Item3" || e.PropertyName == "Item4";
        }

        protected TimeSpan? GetSelectedTimeFromSelectors()
        {
            if (_nightHourSelector != null && _dayHourSelector != null && _minuteSelector != null && _secondSelector != null)
            {
                var hours = GetSelectorValue(_nightHourSelector) + GetSelectorValue(_dayHourSelector);
                var minutes = GetSelectorValue(_minuteSelector);
                var seconds = GetSelectorValue(_secondSelector);
                return new TimeSpan(hours, minutes, seconds);
            }
            return SelectedTime;
        }

        private static int GetSelectorValue(DataGrid selector)
        {
            var value = 0;
            if (selector.SelectedCells.Count > 0)
            {
                var selectedCell = selector.SelectedCells[0];
                value = ((SelectorRow)selectedCell.Item).Offset + selectedCell.Column.DisplayIndex;
            }
            return value;
        }

        public string AMText => Culture.DateTimeFormat.AMDesignator;
        public string PMText => Culture.DateTimeFormat.PMDesignator;
        public bool IsAmPmMode => !string.IsNullOrEmpty(Culture.DateTimeFormat.AMDesignator);

        #region ===========  INotifyPropertyChanged  ===============
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}