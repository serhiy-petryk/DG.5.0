using System;
using System.Windows;
using System.Windows.Controls;
using WpfSpLib.Common;

namespace WpfSpLib.Controls
{
// ========  Hue/SaturationAndValue Sliders  =======
    public class ColorControl_XYSlider : NotifyPropertyChangedAbstract
    {
        protected string Id { get; }
        public double xValue { get; private set; }
        public double yValue { get; private set; }
        public virtual double xSliderValue => SizeOfSlider.Width * xValue - SizeOfThumb.Width / 2;
        public virtual double ySliderValue => SizeOfSlider.Height * yValue - SizeOfThumb.Height / 2;

        protected Action<double, double> SetValuesAction;

        protected Size SizeOfSlider;
        protected Size SizeOfThumb;

        public ColorControl_XYSlider(string id, Action<double, double> setValuesAction)
        {
            Id = id; SetValuesAction = setValuesAction;
        }
        public void SetProperties(double xValue, double yValue, bool updateUI = false)
        {
            this.xValue = xValue;
            this.yValue = yValue;
            SetValuesAction?.Invoke(xValue, yValue);
            if (updateUI)
                UpdateUI();
        }

        public override void UpdateUI() => OnPropertiesChanged(nameof(xSliderValue), nameof(ySliderValue));

        public void SetSizeOfControl(Panel panel)
        {
            SizeOfSlider = new Size(panel.ActualWidth, panel.ActualHeight);
            var thumb = panel.Children[0] as FrameworkElement;
            SizeOfThumb = new Size(thumb.ActualWidth, thumb.ActualHeight);
        }

    }
}
