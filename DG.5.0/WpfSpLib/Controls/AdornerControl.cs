// based on: https://www.nbdtech.com/Blog/archive/2010/06/28/wpf-adorners-part-2-ndash-placing-any-control-on-the.aspx
// see comment in https://stackoverflow.com/questions/833943/watermark-hint-text-placeholder-textbox
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfSpLib.Controls
{
    public class AdornerControl : Adorner
    {
        public AdornerLayer AdornerLayer => AdornerLayer.GetAdornerLayer(AdornedElement);

        private FrameworkElement _child;

        protected override int VisualChildrenCount => 1;

        public FrameworkElement Child
        {
            get => _child;
            set
            {
                if (_child != null)
                    RemoveVisualChild(_child);
                _child = value;
                if (_child != null)
                    AddVisualChild(_child);
            }
        }

        public AdornerControl(FrameworkElement adornedElement) : base(adornedElement) { }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException();
            return _child;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var newSize = AdornedElement.RenderSize;
            _child.Width = newSize.Width;
            _child.Height = newSize.Height;
            _child.Measure(newSize);

            return _child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _child.Arrange(new Rect(new Point(0, 0), finalSize));
            return new Size(_child.ActualWidth, _child.ActualHeight);
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            // result.Children.Add(new TranslateTransform(0, 0));
            result.Children.Add(base.GetDesiredTransform(transform));
            return result;
        }
    }
}
