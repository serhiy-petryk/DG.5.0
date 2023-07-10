using System;
using System.Windows;
using System.Windows.Media;

namespace DGView.Controls.Printing
{
    public class RenderElement : FrameworkElement, IDisposable
    {
        private int _pageNo;
        private Action<int, DrawingContext> _onRenderAction;
        // private object _userObject;
        public RenderElement(int pageNo, Action<int, DrawingContext> onRenderAction)
        {
            _pageNo = pageNo;
            _onRenderAction = onRenderAction;
            //_userObject = userObject;
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            _onRenderAction?.Invoke(_pageNo, drawingContext);
        }

        public void Dispose()
        {
            _onRenderAction = null;
            // _userObject = null;
        }
    }
}
