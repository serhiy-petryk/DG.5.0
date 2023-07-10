using System;
using System.Linq;
using System.Windows.Media;

namespace WpfSpLib.Controls
{
    public class ColorControl_ColorComponent : ColorControl_XYSlider, IDisposable
    {
        private double _value;
        public double Value
        {
            get => _value;
            set
            {
                _value = Math.Max(_min, Math.Min(_max, value));
                _owner.UpdateValues(_colorSpace);
            }
        }

        public string Label => Id.Split('_')[1];
        public string ValueLabel { get; }
        public LinearGradientBrush BackgroundBrush { get; private set; }

        private ColorControlViewModel _owner;
        private readonly double _spaceMultiplier;
        private readonly double _min;
        private readonly double _max;
        private ColorControlViewModel.ColorSpace _colorSpace;
        private int _gradientCount => _colorSpace == ColorControlViewModel.ColorSpace.RGB ? 1 : 100;
        private Func<int, Color> _backgroundGradient;

        public ColorControl_ColorComponent(string id, ColorControlViewModel owner, double min, double max,
            string valueLabel = null,
            Func<int, Color> backgroundGradient = null) : base(id, null)
        {
            SetValuesAction = (x, y) => Value = xValue * (_max - _min) + _min;
            _owner = owner;
            _min = min;
            _max = max;
            ValueLabel = valueLabel;
            _backgroundGradient = backgroundGradient;
            _spaceMultiplier = Id.StartsWith("LAB") ? 1 : _max - _min;
            _colorSpace = (ColorControlViewModel.ColorSpace)Enum.Parse(typeof(ColorControlViewModel.ColorSpace), Id.Split('_')[0]);
            BackgroundBrush = new LinearGradientBrush(new GradientStopCollection(Enumerable
                .Range(0, _gradientCount + 1)
                .Select(n => new GradientStop(Colors.Transparent, 1.0 * n / _gradientCount))));
        }

        public override void UpdateUI()
        {
            if (_backgroundGradient != null)
                for (var k = 0; k < BackgroundBrush.GradientStops.Count; k++)
                    BackgroundBrush.GradientStops[k].Color = _backgroundGradient(k);
            OnPropertiesChanged(nameof(xSliderValue), nameof(Value), nameof(BackgroundBrush));
        }

        public override double xSliderValue =>
            (SizeOfSlider.Width - SizeOfThumb.Width) * (Value - _min) / (_max - _min);

        public double SpaceValue => Value / _spaceMultiplier;

        public void SetSpaceValue(double value, bool updateUI = false)
        {
            if (updateUI)
                Value = value * _spaceMultiplier;
            else
                _value = value * _spaceMultiplier;
        }

        public void Dispose()
        {
            _owner = null;
        }
    }
}
