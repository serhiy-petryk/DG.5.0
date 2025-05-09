using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using WpfSpLib.Common;
using WpfSpLib.Helpers;

namespace WpfSpLib.Controls
{
    public class FormatBox: Control//, INotifyPropertyChanged
    {
        public RelayCommand CmdExit { get; }

        public FormatBox()
        {
            CmdExit = new RelayCommand(cmdExit);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_Popup") is Popup popup)
                popup.Opened += Popup_Opened;
            if (GetTemplateChild("cbNotDefined") is CheckBox cbNotDefined)
                cbNotDefined.Background = Brushes.Red;
        }

        private void Popup_Opened(object sender, System.EventArgs e)
        {
            var cbNotDefined = GetTemplateChild("cbNotDefined") as CheckBox;
            cbNotDefined.IsChecked = string.IsNullOrEmpty(Value);
            if (Equals(cbNotDefined.IsChecked, true))
                return;

            var popupContent = (GetTemplateChild("PART_Popup") as Popup).Child;
            var formatTypeBtn = popupContent.GetVisualChildren<RadioButton>().FirstOrDefault(rb => rb.GroupName == "NumericFormat" && Value.StartsWith((string)rb.Tag));
            if (formatTypeBtn != null)
            {
                formatTypeBtn.IsChecked = true;
                var dpString = Value.Substring(((string) formatTypeBtn.Tag).Length);
                var dpBtns = popupContent.GetVisualChildren<RadioButton>().Where(rb => rb.GroupName == "DecimalPlaces").ToArray();
                var dpBtn = dpBtns.FirstOrDefault(rb => Equals(rb.Content, dpString)) ??
                            dpBtns.FirstOrDefault(rb => Equals(rb.Tag, "Auto"));
                if (dpBtn != null)
                    dpBtn.IsChecked = true;
            }
        }


        #region ===========  Events && command =============
        private void cmdExit(object obj)
        {
            if (Equals(obj, "OK"))
                Value = GetValue();

            (GetTemplateChild("PART_Popup") as Popup).IsOpen = false;
        }

        private string GetValue()
        {
            if ((GetTemplateChild("cbNotDefined") as CheckBox)?.IsChecked == true)
                return null;

            var popupContent = (GetTemplateChild("PART_Popup") as Popup).Child;
            var formatTypeBtn = popupContent.GetVisualChildren<RadioButton>().FirstOrDefault(rb => rb.GroupName == "NumericFormat" && rb.IsChecked == true);
            if (formatTypeBtn?.Tag is string formatType)
            {
                var dpBtn = popupContent.GetVisualChildren<RadioButton>().FirstOrDefault(rb => rb.GroupName == "DecimalPlaces" && rb.IsChecked == true);
                if (dpBtn != null && int.TryParse(dpBtn.Content.ToString(), out var dp))
                    return formatTypeBtn.Tag + dp.ToString();
                return formatType;
            }
            return Value;
        }
        #endregion
        public string RegionChangeHook
        {
            get
            {
                InternalSetText(Value);
                return null;
            }
        }

        private void InternalSetText(string newValue)
        {
            /*if (_textBox == null) return;
            if (newValue.HasValue)
                _textBox.Text = FormattedValue(newValue, StringFormat, Culture);
            else
                _textBox.Text = null;*/
        }

        //=============================
        // public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value",
          //  typeof(string), typeof(FormatBox), new FrameworkPropertyMetadata("F2", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value",
            typeof(string), typeof(FormatBox), new FrameworkPropertyMetadata(null));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
