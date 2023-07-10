using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfSpLib.Controls;
using WpfSpLib.Helpers;

namespace WpfSpLib.Effects
{
    public class ComboBoxEffects
    {
        private const string GridColumnPrefix = "ComboBoxClearButtonColumn";
        private const string ElementPrefix = "ComboBoxEffects";

        #region ===========  OnPropertyChanged  ===========
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox)
            {
                if (e.Property != UIElement.VisibilityProperty)
                {
                    comboBox.IsVisibleChanged -= Element_IsVisibleChanged;
                    comboBox.IsVisibleChanged += Element_IsVisibleChanged;
                }

                if (comboBox.IsVisible)
                {
                    Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                    {
                        RemoveClearButton(comboBox);
                        AddClearButton(comboBox);
                    }, DispatcherPriority.Loaded);
                }
                else
                    RemoveClearButton(comboBox);
            }
            else
                Debug.Print($"ComboBoxEffects is not implemented for {d.GetType().Namespace}.{d.GetType().Name} type");

            void Element_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e2) => OnPropertyChanged((Control)sender, e2);
        }

        #endregion

        #region ================  Properties  =====================
        public static readonly DependencyProperty ClearButtonStyleProperty = DependencyProperty.RegisterAttached("ClearButtonStyle",
            typeof(Style), typeof(ComboBoxEffects), new PropertyMetadata(null, propertyChangedCallback: OnPropertyChanged));
        public static void SetClearButtonStyle(DependencyObject d, Style value) => d.SetValue(ClearButtonStyleProperty, value);
        public static Style GetClearButtonStyle(DependencyObject d) => (Style)d.GetValue(ClearButtonStyleProperty);
        #endregion

        #region ===============  Private methods  ===================
        private static void RemoveClearButton(ComboBox comboBox)
        {
            var grid = comboBox.GetVisualChildren().OfType<Grid>().FirstOrDefault();
            if (grid != null)
            {
                foreach (var element in grid.GetVisualChildren().OfType<FrameworkElement>().Where(c => c.Name.StartsWith(ElementPrefix)).ToArray())
                {
                    if (element.Name.Contains("Clear"))
                        element.PreviewMouseLeftButtonDown -= ClearButton_OnClick;

                    grid.Children.Remove(element);
                }

                foreach (var cd in grid.ColumnDefinitions.Where(c => c.Name.StartsWith(GridColumnPrefix)).ToArray())
                    grid.ColumnDefinitions.Remove(cd);
            }
        }
        private static void AddClearButton(ComboBox comboBox)
        {
            var buttonStyle = GetClearButtonStyle(comboBox);
            if (buttonStyle == null)
            {
                RemoveClearButton(comboBox);
                return;
            }

            var grid = comboBox.GetVisualChildren().OfType<Grid>().FirstOrDefault();
            if (grid != null)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, Name = GridColumnPrefix + "Clear" });
                var clearButton = new Button
                {
                    Name = ElementPrefix + "Clear",
                    Style = buttonStyle
                };

                if (ChromeEffect.GetChromeMatrix(comboBox) is string chromeMatrix)
                {
                    var ss = new List<string>(chromeMatrix.Split(','));
                    if (ss.Count == 12)
                        ChromeEffect.SetChromeMatrix(clearButton, chromeMatrix + ", +60%,+60%/+75%,+60%/+50%,100");
                }

                clearButton.PreviewMouseLeftButtonDown += ClearButton_OnClick;
                grid.Children.Add(clearButton);
                Grid.SetColumn(clearButton, grid.ColumnDefinitions.Count - 1);
            }
        }

        private static void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            var current = (DependencyObject)sender;
            while (current != null && !(current is ComboBox))
                current = VisualTreeHelper.GetParent(current) ?? (current as FrameworkElement)?.Parent;

            if (current is ComboBox cb)
            {
                cb.SelectedIndex = -1;
                if (cb.IsDropDownOpen)
                    cb.IsDropDownOpen = false;
            }
        }

        private static void Popup_OnOpened(object sender, EventArgs e) => ((Popup)sender).PlacementTarget.Focus();
        #endregion

    }
}
