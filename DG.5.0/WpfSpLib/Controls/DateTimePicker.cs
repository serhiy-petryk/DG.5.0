using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace WpfSpLib.Controls
{
    /// <summary>
    ///     Represents a control that allows the user to select a date and a time.
    /// </summary>
    [TemplatePart(Name = ElementCalendar, Type = typeof(Calendar))]
    [DefaultEvent("SelectedDateChanged")]
    public class DateTimePicker : TimePickerBase
    {
        static DateTimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateTimePicker), new FrameworkPropertyMetadata(typeof(DateTimePicker)));
        }

        private const string ElementCalendar = "PART_Calendar";
        private new TimeSpan? SelectedTime => base.SelectedTime;
        private Calendar _calendar;
        private bool _isDateTimeChanging;
        private DateTime _defaultDate => DisplayDateStart.HasValue && DisplayDateStart.Value > DateTime.Today ? DisplayDateStart.Value : DateTime.Today;

        #region ==========  Global Override  =============
        public override void OnApplyTemplate()
        {
            _calendar = GetTemplateChild(ElementCalendar) as Calendar;
            ApplyBindings();
            base.OnApplyTemplate();
            CoerceValue(SelectedDateTimeProperty);
            IsDateOnlyModeChanged(this, new DependencyPropertyChangedEventArgs(IsDateOnlyModeProperty, null, IsDateOnlyMode));
        }
        private void ApplyBindings()
        {
            if (_calendar != null)
            {
                _calendar.SelectedDatesChanged += (sender, args) =>
                {
                    SelectedDateTime = _calendar.SelectedDate.HasValue
                        ? _calendar.SelectedDate.Value + (SelectedDateTime?.TimeOfDay ?? TimeSpan.Zero) : (DateTime?)null;
                };
                _calendar.SetBinding(Calendar.DisplayDateStartProperty, GetBinding(DisplayDateStartProperty));
                _calendar.SetBinding(Calendar.DisplayDateEndProperty, GetBinding(DisplayDateEndProperty));
                _calendar.SetBinding(Calendar.IsTodayHighlightedProperty, GetBinding(IsTodayHighlightedProperty));
            }
        }
        private Binding GetBinding(DependencyProperty property) => new Binding(property.Name) { Source = this };

        private bool _isPopupOpening;
        protected override void OnPopupOpened(object sender, EventArgs e)
        {
            _isPopupOpening = true;
            InternalOnTextBoxLostFocus(_textBox, null);
            _isPopupOpening = false;

            SetDatePartValues();
            base.OnPopupOpened(sender, e);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);
            if (Mouse.Captured is CalendarItem)
                Mouse.Capture(null);
        }

        #endregion

        #region ==========  TimePickerBase Override  =============
        protected sealed override void ApplyCulture()
        {
            base.ApplyCulture();
            if (_calendar != null)
            {
                _calendar.FirstDayOfWeek = Culture.DateTimeFormat.FirstDayOfWeek;
                // Refresh month/week names of calendar
                var mi = _calendar.GetType().GetMethod("UpdateCellItems", BindingFlags.Instance | BindingFlags.NonPublic);
                mi?.Invoke(_calendar, null);
            }
        }

        protected override void OnBaseValueChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            base.OnBaseValueChanged(sender, e);
            SetDatePartValues();
        }

        protected override string GetValueForTextBox()
        {
            var formatInfo = Culture.DateTimeFormat;
            var dateFormat = SelectedDateFormat == DatePickerFormat.Long ? formatInfo.LongDatePattern : formatInfo.ShortDatePattern;

            var valueForTextBox = SelectedDateTime?.ToString(dateFormat, Culture);
            return valueForTextBox + (!IsDateOnlyMode && SelectedDateTime.HasValue ? " " + base.GetValueForTextBox() : null);
        }

        protected override void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var textBoxValue = ((TextBox)sender).Text;
            DateTime? dt = null;
            if (!string.IsNullOrWhiteSpace(textBoxValue))
            {
                if (DateTime.TryParse(textBoxValue, Culture, System.Globalization.DateTimeStyles.None, out var tempDt))
                    dt = tempDt;
            }

            SelectedDateTime = dt;
            WriteValueToTextBox(); // need in case: isNullbale=false; set textbox=empty twice (no OnSelectedDateChanged event for second call and textbox is empty but SelectedDate!=empty)
        }

        protected override void OnSelectedTimeChanged(RoutedPropertyChangedEventArgs<TimeSpan?> e)
        {
            base.OnSelectedTimeChanged(e);

            if (SelectedDateTime.HasValue)
                SelectedDateTime = SelectedDateTime.Value.Date + (e.NewValue ?? TimeSpan.Zero);
            else if (!SelectedDateTime.HasValue && e.NewValue is TimeSpan span)
                SelectedDateTime = _defaultDate + span;
            else
                SelectedDateTime = null;
        }

        protected override void IsNullableChanged()
        {
            base.IsNullableChanged();
            if (!IsNullable && !SelectedDateTime.HasValue)
                SelectedDateTime = _defaultDate;
        }

        protected override void ClearValue()
        {
            base.ClearValue();
            SelectedDateTime = IsNullable ? (DateTime?)null : _defaultDate;
        }
        #endregion

        #region ============  Internal methods  ==============
        private void SetDatePartValues()
        {
            if (_isDateTimeChanging)
                return;

            _isDateTimeChanging = true;
            if (_calendar != null)
            {
                _calendar.SelectedDate = SelectedDateTime?.Date;
                _calendar.DisplayDate = SelectedDateTime?.Date ?? _defaultDate;
            }
            _isDateTimeChanging = false;
        }
        #endregion

        #region ==================  Properties & Events  =====================
        public static readonly RoutedEvent SelectedDateTimeChangedEvent = EventManager.RegisterRoutedEvent("SelectedDateTimeChanged", RoutingStrategy.Direct,
            typeof(RoutedPropertyChangedEventHandler<DateTime?>), typeof(DateTimePicker));
        /// <summary>
        ///     Occurs when the <see cref="SelectedDateTime" /> property is changed.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<DateTime?> SelectedDateTimeChanged
        {
            add => AddHandler(SelectedDateTimeChangedEvent, value);
            remove => RemoveHandler(SelectedDateTimeChangedEvent, value);
        }
        //=====================================
        public static readonly DependencyProperty SelectedDateTimeProperty = DependencyProperty.Register("SelectedDateTime", typeof(DateTime?),
            typeof(DateTimePicker), new PropertyMetadata(null, OnSelectedDateTimeChanged, CoerceDateTime));
        /// <summary>
        ///     Gets or sets the currently selected date and time.
        /// </summary>
        /// <returns>
        ///     The date and time currently selected. The default is null.
        /// </returns>
        public DateTime? SelectedDateTime
        {
            get => (DateTime?)GetValue(SelectedDateTimeProperty);
            set => SetValue(SelectedDateTimeProperty, value);
        }
        private static void OnSelectedDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (DateTimePicker)d;
            if (picker._isDateTimeChanging)
                return;

            picker._isDateTimeChanging = true;
            var oldDateTimeValue = picker.SelectedDateTime;

            CoerceDateTime(d, e.NewValue);
            picker.SelectedDateTime = (DateTime?)e.NewValue;
            ((TimePickerBase)picker).SelectedTime = picker.SelectedDateTime?.TimeOfDay;

            picker.RaiseEvent(new RoutedPropertyChangedEventArgs<DateTime?>(oldDateTimeValue, picker.SelectedDateTime, SelectedDateTimeChangedEvent));
            picker._isDateTimeChanging = false;

            picker.WriteValueToTextBox();
            if (picker.IsDateOnlyMode && !picker._isPopupOpening)
                picker.ClosePopup();
        }
        private static object CoerceDateTime(DependencyObject d, object basevalue)
        {
            var picker = (DateTimePicker)d;
            var dt = (DateTime?)basevalue;
            if (dt == null)
                return picker.IsNullable ? (DateTime?)null : picker._defaultDate;

            if (picker.IsDateOnlyMode) dt = dt.Value.Date;

            if (picker.DisplayDateStart.HasValue && picker.DisplayDateStart.Value > dt.Value)
                return picker.DisplayDateStart.Value;
            if (picker.DisplayDateEnd.HasValue && picker.DisplayDateEnd.Value.Date < dt.Value.Date)
                return picker.DisplayDateEnd.Value;

            return dt.Value;
        }
        //=====================================
        public static readonly DependencyProperty DisplayDateStartProperty = DatePicker.DisplayDateStartProperty.AddOwner(typeof(DateTimePicker));
        /// <summary>
        ///     Gets or sets the first date to be displayed.
        /// </summary>
        /// <returns>The first date to display.</returns>
        public DateTime? DisplayDateStart
        {
            get => (DateTime?)GetValue(DisplayDateStartProperty);
            set => SetValue(DisplayDateStartProperty, value);
        }
        //=====================================
        public static readonly DependencyProperty DisplayDateEndProperty = DatePicker.DisplayDateEndProperty.AddOwner(typeof(DateTimePicker));
        /// <summary>
        ///     Gets or sets the last date to be displayed.
        /// </summary>
        /// <returns>The last date to display.</returns>
        public DateTime? DisplayDateEnd
        {
            get => (DateTime?)GetValue(DisplayDateEndProperty);
            set => SetValue(DisplayDateEndProperty, value);
        }

        //=====================================
        public static readonly DependencyProperty IsTodayHighlightedProperty = DatePicker.IsTodayHighlightedProperty.AddOwner(typeof(DateTimePicker));
        /// <summary>
        ///     Gets or sets a value that indicates whether the current date will be highlighted.
        /// </summary>
        /// <returns>true if the current date is highlighted; otherwise, false. The default is true. </returns>
        public bool IsTodayHighlighted
        {
            get => (bool)GetValue(IsTodayHighlightedProperty);
            set => SetValue(IsTodayHighlightedProperty, value);
        }
        //=====================================
        public static readonly DependencyProperty SelectedDateFormatProperty = DatePicker.SelectedDateFormatProperty.AddOwner(
            typeof(DateTimePicker), new FrameworkPropertyMetadata(DatePickerFormat.Short, OnSelectedDateFormatChanged));
        /// <summary>
        /// Gets or sets the format that is used to display the selected date.
        /// </summary>
        [Category("Appearance")]
        [DefaultValue(DatePickerFormat.Short)]
        public DatePickerFormat SelectedDateFormat
        {
            get => (DatePickerFormat)GetValue(SelectedDateFormatProperty);
            set => SetValue(SelectedDateFormatProperty, value);
        }
        private static void OnSelectedDateFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = d as DateTimePicker;
            picker?.WriteValueToTextBox();
        }
        //=====================================
        public static readonly DependencyProperty IsDateOnlyModeProperty = DependencyProperty.Register("IsDateOnlyMode", typeof(bool),
            typeof(DateTimePicker), new PropertyMetadata(false, IsDateOnlyModeChanged));
        /// <summary>
        ///     Does picker support date only?
        /// </summary>
        public bool IsDateOnlyMode
        {
            get => (bool)GetValue(IsDateOnlyModeProperty);
            set => SetValue(IsDateOnlyModeProperty, value);
        }
        private static void IsDateOnlyModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (DateTimePicker)d;
            if (picker.GetTemplateChild("PART_Clock") is FrameworkElement clock)
            {
                clock.Visibility = (bool)e.NewValue ? Visibility.Collapsed : Visibility.Visible;
                OnSelectedDateTimeChanged(d, new DependencyPropertyChangedEventArgs(SelectedDateTimeProperty, null, picker.SelectedDateTime));
            }
        }
        #endregion
    }
}