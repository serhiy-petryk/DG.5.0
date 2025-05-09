using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WpfSpLib.Helpers;

namespace WpfSpLib.Effects
{
    /// <summary>
    /// DatePickerEffects.IsNullable property: supports IsNullable and DisplayDateStart/End (as Minimum/Maximum dates) for SelectedDate of DatePicker
    /// DatePickerEffects.ClearButton property: hide/show clear button for DatePicker
    /// DatePickerEffects.HideInnerBorder property: hide/show borders of DatePickerTextBox which is child of any framework element
    /// Usage:  <DatePicker controls:DatePickerEffects.ClearButton="True" controls:DatePickerEffects.IsNullable="True" />
    /// </summary>
    public class DatePickerEffects
    {
        #region ===========  OnPropertyChanged  ===========
        private static readonly ConcurrentDictionary<FrameworkElement, object> _activated = new ConcurrentDictionary<FrameworkElement, object>();

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DatePicker dp)
            {
                if (e.Property != UIElement.VisibilityProperty)
                {
                    dp.IsVisibleChanged -= Element_IsVisibleChanged;
                    dp.IsVisibleChanged += Element_IsVisibleChanged;
                }

                if (dp.IsVisible)
                {
                    if (GetIsNullable(dp).HasValue && _activated.TryAdd(dp, null)) Activate_IsNullable(dp);

                    if (e.Property == UIElement.IsVisibleProperty || e.Property == ClearButtonProperty)
                        CheckClearButton(dp);
                    if (e.Property == UIElement.IsVisibleProperty || e.Property == HideInnerBorderProperty)
                        CheckHideInnerBorder(dp);
                    if (e.Property == UIElement.IsVisibleProperty || e.Property == IsNullableProperty)
                        CheckIsNullable(dp);
                }
                else
                {
                    if (_activated.TryRemove(dp, out var o)) Deactivate_IsNullable(dp);
                }
            }
            else
                Debug.Print($"DatePickerEffects is not implemented for {d.GetType().Namespace}.{d.GetType().Name} type");

            void Element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e2) => OnPropertyChanged((Control)sender, e2);
        }

        private static void Activate_IsNullable(DatePicker dp)
        {
            dp.SelectedDateChanged += OnSelectedDateChanged;
            var dpdStart = DependencyPropertyDescriptor.FromProperty(Calendar.DisplayDateStartProperty, typeof(DatePicker));
            dpdStart.AddValueChanged(dp, OnChangeDateLimit);
            var dpdEnd = DependencyPropertyDescriptor.FromProperty(Calendar.DisplayDateEndProperty, typeof(DatePicker));
            dpdEnd.AddValueChanged(dp, OnChangeDateLimit);
        }

        private static void Deactivate_IsNullable(DatePicker dp)
        {
            dp.SelectedDateChanged -= OnSelectedDateChanged;
            var dpdStart = DependencyPropertyDescriptor.FromProperty(Calendar.DisplayDateStartProperty, typeof(DatePicker));
            dpdStart.RemoveValueChanged(dp, OnChangeDateLimit);
            var dpdEnd = DependencyPropertyDescriptor.FromProperty(Calendar.DisplayDateEndProperty, typeof(DatePicker));
            dpdEnd.RemoveValueChanged(dp, OnChangeDateLimit);
        }

        #endregion

        #region ===============  ClearButton attached property  ================
        private const string ClearButtonName = "DatePickerEffectsClearButton";

        public static readonly DependencyProperty ClearButtonProperty = DependencyProperty.RegisterAttached("ClearButton",
            typeof(bool), typeof(DatePickerEffects), new PropertyMetadata(false, propertyChangedCallback: OnPropertyChanged));
        [AttachedPropertyBrowsableForType(typeof(DatePicker))]
        public static void SetClearButton(DependencyObject d, bool value) => d.SetValue(ClearButtonProperty, value);
        [AttachedPropertyBrowsableForType(typeof(DatePicker))]
        public static bool GetClearButton(DependencyObject d) => (bool)d.GetValue(ClearButtonProperty);

        private static void CheckClearButton(DatePicker dp)
        {
            dp.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (GetClearButton(dp))
                    AddClearButton(dp);
                else
                    RemoveClearButton(dp);
            }), DispatcherPriority.Render);
        }

        private static void AddClearButton(DatePicker dp)
        {
            if (dp.GetVisualChildren<Button>().FirstOrDefault(btn => btn.Name == ClearButtonName) != null)
                return;
            if (!(dp.Template.FindName("PART_Button", dp) is Button button)) return;

            var grid = button.Parent as Grid;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var style = dp.FindResource("ClearBichromeButtonStyle") as Style;
            var clearButton = new Button
            {
                Name = ClearButtonName, Style = style, Width = 18, 
                Margin = new Thickness(-2, 0, 1 - dp.Padding.Right, 0),
                Padding = new Thickness(3), Focusable = false
            };

            //if (dp.Background == null || dp.Background == Brushes.Transparent)
            //    dp.Background = Tips.GetActualBackgroundBrush(dp);

            clearButton.Click += ClearButton_Click;
            grid.Children.Add(clearButton);
            Grid.SetColumn(clearButton, grid.ColumnDefinitions.Count - 1);
        }

        private static void RemoveClearButton(DatePicker dp)
        {
            if (dp.GetVisualChildren<Button>().FirstOrDefault(btn => btn.Name == ClearButtonName) is Button clearButton)
            {
                clearButton.Click -= ClearButton_Click;
                var grid = VisualTreeHelper.GetParent(clearButton) as Grid;
                grid?.Children.Remove(clearButton);
            }
        }

        private static void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (((DependencyObject)sender).GetVisualParents<DatePicker>().FirstOrDefault() is DatePicker dp)
            {
                var isNullable = GetIsNullable(dp) ?? false;
                dp.SelectedDate = isNullable ? (DateTime?)null : DateTime.Today;
                dp.Focus();
            }
        }
        #endregion

        #region ===============  IsNullable attached property  ================
        public static readonly DependencyProperty IsNullableProperty = DependencyProperty.RegisterAttached("IsNullable",
            typeof(bool?), typeof(DatePickerEffects), new PropertyMetadata(null, propertyChangedCallback: OnPropertyChanged));
        [AttachedPropertyBrowsableForType(typeof(DatePicker))]
        public static void SetIsNullable(DependencyObject d, bool? value) => d.SetValue(IsNullableProperty, value);
        [AttachedPropertyBrowsableForType(typeof(DatePicker))]
        public static bool? GetIsNullable(DependencyObject d) => (bool?)d.GetValue(IsNullableProperty);

        private static void OnSelectedDateChanged(object sender, SelectionChangedEventArgs e) => CheckIsNullable(sender as DatePicker);
        private static void OnChangeDateLimit(object sender, EventArgs e) => CheckIsNullable(sender as DatePicker);

        private static void CheckIsNullable(DatePicker dp)
        {
            if (!(dp.IsVisible && GetIsNullable(dp) is bool isNullable)) return;
            if (!dp.SelectedDate.HasValue && isNullable) return;

            var value = dp.SelectedDate ?? DateTime.Today;
            if (dp.DisplayDateStart.HasValue && dp.DisplayDateStart.Value > value)
                value = dp.DisplayDateStart.Value;
            else if (dp.DisplayDateEnd.HasValue && dp.DisplayDateEnd.Value < value)
                value = dp.DisplayDateEnd.Value;

            if (dp.SelectedDate != value)
                dp.SelectedDate = value;
        }
        #endregion

        #region ===============  Hide inner borders  ================
        public static readonly DependencyProperty HideInnerBorderProperty = DependencyProperty.RegisterAttached("HideInnerBorder",
            typeof(bool?), typeof(DatePickerEffects), new PropertyMetadata(null, propertyChangedCallback: OnPropertyChanged));
        [AttachedPropertyBrowsableForType(typeof(DatePicker))]
        public static void SetHideInnerBorder(DependencyObject d, bool? value) => d.SetValue(HideInnerBorderProperty, value);
        [AttachedPropertyBrowsableForType(typeof(DatePicker))]
        public static bool? GetHideInnerBorder(DependencyObject d) => (bool?)d.GetValue(HideInnerBorderProperty);
        private static void CheckHideInnerBorder(DatePicker dp)
        {
            var toHide = GetHideInnerBorder(dp);
            if (toHide.HasValue)
                ControlHelper.HideInnerBorderOfDatePickerTextBox(dp, toHide.Value);
        }
        #endregion
    }
}
