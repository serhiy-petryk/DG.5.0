using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfSpLib.Common;
using WpfSpLib.Effects;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public class DialogBox : Control
    {
        public enum DialogBoxKind { Question, Stop, Error, Warning, Info, Success }

        static DialogBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DialogBox), new FrameworkPropertyMetadata(typeof(DialogBox)));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(DialogBox), new FrameworkPropertyMetadata(false));
            FocusableProperty.OverrideMetadata(typeof(DialogBox), new FrameworkPropertyMetadata(false));
        }

        private static readonly string[] _iconColors = {"Primary", "Danger", "Danger", "Warning", "Info", "Success"};

        #region ============  Public Methods  =============
        public string ShowDialog()
        {
            new DialogAdorner(AdornerHost).ShowContentDialog(GetControl());
            return Result;
        }

        public async Task<string> ShowAsync()
        {
            var adorner = new DialogAdorner(AdornerHost);
            await adorner.ShowContentAsync(GetControl());
            await adorner.WaitUntilClosed();
            return Result;
        }

        public void Show() => new DialogAdorner(AdornerHost).ShowContent(GetControl());

        private ContentControl GetControl()
        {
            var control = new ResizableControl
            {
                Content = this,
                LimitPositionToPanelBounds = true
            };
            Dispatcher.BeginInvoke(
                new Action(() => CornerRadiusEffect.SetCornerRadius(control, CornerRadiusEffect.GetCornerRadius(this))),
                DispatcherPriority.Normal);
            return control;
        }
        #endregion

        // =================  Instance  ================
        public string Result { get; private set; }
        public FrameworkElement Host { get; set; }
        private FrameworkElement AdornerHost => Host is IHasDialogHost dialogHost ? dialogHost.GetDialogHost() : Host;
        public string Caption { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string CollapsedDetailsHeader { get; set; } = "Show details";
        public string ExpandedDetailsHeader { get; set; } = "Hide details";
        public Geometry Icon { get; set; }
        public Color? BaseIconColor { get; set; }
        public bool IsCloseButtonVisible { get; set; } = true;

        private Grid _buttonsArea;

        private IEnumerable<string> _buttons;
        public IEnumerable<string> Buttons
        {
            get => _buttons;
            set
            {
                _buttons = value;
                RefreshButtons();
            }
        }

        public Color IconColor => BaseIconColor.HasValue && BaseIconColor.Value != Tips.GetActualBackgroundColor(this)
            ? BaseIconColor.Value
            : (Color) ColorHslBrush.Instance.Convert(BaseIconColor ?? Tips.GetActualBackgroundColor(this), typeof(Color), "+70%", null);

        private RelayCommand _cmdClickButton;

        public DialogBox(DialogBoxKind? boxKind = null)
        {
            _cmdClickButton = new RelayCommand(OnButtonClick);
            if (boxKind.HasValue)
            {
                Icon = Application.Current?.TryFindResource($"{boxKind.Value}Geometry") as Geometry;
                BaseIconColor = Application.Current?.TryFindResource(_iconColors[(int)boxKind] + "Color") as Color?;
                if (BaseIconColor.HasValue)
                    Background = new SolidColorBrush(BaseIconColor.Value);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var expander = GetTemplateChild("DetailsExpander");
            expander.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var btn in expander.GetVisualChildren().OfType<ToggleButton>())
                {
                    FocusVisualEffect.SetAlwaysShowFocus(btn, FocusVisualEffect.GetAlwaysShowFocus(this));
                    FocusVisualEffect.SetFocusControlStyle(btn, FocusButtonStyle);
                    btn.TabIndex = 998;
                }
            }), DispatcherPriority.ApplicationIdle);

            _buttonsArea = GetTemplateChild("PART_ButtonsArea") as Grid;
            RefreshButtons();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateUI();
        }

        private void OnButtonClick(object parameter)
        {
            Result = parameter?.ToString();
            ApplicationCommands.Close.Execute(null, this);
        }

        private void RefreshButtons()
        {
            if (_buttonsArea == null) return;

            _buttonsArea.Children.Clear();
            _buttonsArea.ColumnDefinitions.Clear();

            if ((_buttons ?? new string[0]).Any())
            {
                MinHeight = 120;
                var tabIndex = 0;
                foreach (var content in _buttons)
                {
                    _buttonsArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var button = new Button { Content = content, Command = _cmdClickButton, CommandParameter = content, TabIndex = tabIndex++};
                    Grid.SetColumn(button, _buttonsArea.ColumnDefinitions.Count - 1);
                    _buttonsArea.Children.Add(button);
                }
            }
            else
                MinHeight = 85;
        }

        private bool _isFirst = true;
        private bool _isUpdatingUI = false;
        private async void UpdateUI()
        {
            if (_isUpdatingUI) return;
            _isUpdatingUI = true;

            var noButtons = _buttonsArea.Children.Count == 0;
            var buttonBaseWidth = noButtons
                ? 0
                : _buttonsArea.Children.OfType<ContentControl>().Max(c => ControlHelper.MeasureStringForDisplay((string) c.Content, c).Width) + 8.0;

            // First measure
            if (_isFirst)
            {
                _isFirst = false;

                var panel = this.GetVisualParents().OfType<Panel>().FirstOrDefault(p => p.ActualHeight > (ActualHeight + 0.5) || p.ActualWidth > (ActualWidth + 0.5));
                if (panel != null)
                {
                    MaxWidth = panel.ActualWidth;
                    //MaxHeight = panel.ActualHeight;
                }

                var allButtonWidth = buttonBaseWidth * _buttonsArea.Children.Count * 2;
                var startWidth = Math.Max(allButtonWidth, Math.Min(ActualWidth + 1, MaxWidth / 2));
                Width = Math.Round(Math.Min(MaxWidth, startWidth));
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render).Task;
            }

            if (!noButtons)
            {
                MinWidth = Math.Max(100, Math.Min(MaxWidth, (buttonBaseWidth + 2) * _buttonsArea.Children.Count));
                var space = ActualWidth - MinWidth;
                var padding = Math.Min(20.0, space / (4 * _buttonsArea.Children.Count));
                var buttonWidth = buttonBaseWidth + 2 * padding;

                foreach (ButtonBase button in _buttonsArea.Children)
                    if (!Tips.AreEqual(button.Width, buttonWidth))
                        button.Width = buttonWidth;

                if (!Tips.AreEqual(padding, _buttonsArea.Margin.Left) ||
                    !Tips.AreEqual(padding, _buttonsArea.Margin.Right))
                    _buttonsArea.Margin = new Thickness(padding, 5, padding, 10);
            }

            await Dispatcher.InvokeAsync(() => _isUpdatingUI = false, DispatcherPriority.Render);
        }

        #region ===========  Properties  ==============
        public static readonly DependencyProperty FocusButtonStyleProperty = DependencyProperty.Register("FocusButtonStyle", typeof(Style), typeof(DialogBox), new FrameworkPropertyMetadata(null));
        public Style FocusButtonStyle
        {
            get => (Style)GetValue(FocusButtonStyleProperty);
            set => SetValue(FocusButtonStyleProperty, value);
        }
        #endregion
    }
}
