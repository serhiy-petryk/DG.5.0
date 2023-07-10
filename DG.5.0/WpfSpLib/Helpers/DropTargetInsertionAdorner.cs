using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfSpLib.Helpers
{
    public class DropTargetInsertionAdorner : Adorner
    {
        public DropTargetInsertionAdorner(UIElement adornedElement) : base(adornedElement)
        {
            m_AdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            m_AdornerLayer.Add(this);
            IsHitTestVisible = false;
        }

       protected override void OnRender(DrawingContext drawingContext)
        {
            if (!DragDropHelper.Drag_Info.InsertIndex.HasValue)
                return;

            var control = (ItemsControl)AdornedElement;
            var orientation = DragDropHelper.GetItemsPanelOrientation(control);
            var hoveredItem = DragDropHelper.Drag_Info.GetHoveredItem(control);
            if (hoveredItem == null) return;

            var r = hoveredItem.GetVisibleRect(control);
            double insertPosition;
            if (DragDropHelper.Drag_Info.InsertIndex.Value < DragDropHelper.Drag_Info.ItemsRects.Count && !DragDropHelper.Drag_Info.IsBottomOrRightEdge)
                insertPosition = orientation == Orientation.Vertical ? r.Top : r.Left;
            else
                insertPosition = orientation == Orientation.Vertical ? r.Bottom : r.Right;
            var p1 = orientation == Orientation.Vertical ? new Point(r.X, insertPosition) : new Point(insertPosition, r.Y);
            var p2 = orientation == Orientation.Vertical ? new Point(r.X + r.Width, insertPosition) : new Point(insertPosition, r.Y + r.Height);
            var rotation = orientation == Orientation.Vertical ? 0.0 : 90.0;

            drawingContext.DrawLine(m_Pen, p1, p2);
            DrawTriangle(drawingContext, p1, rotation);
            DrawTriangle(drawingContext, p2, rotation + 180.0);
        }

        private void DrawTriangle(DrawingContext drawingContext, Point origin, double rotation)
        {
            drawingContext.PushTransform(new TranslateTransform(origin.X, origin.Y));
            drawingContext.PushTransform(new RotateTransform(rotation));

            drawingContext.DrawGeometry(m_Pen.Brush, null, m_Triangle);

            drawingContext.Pop();
            drawingContext.Pop();
        }

        public void Detach() => m_AdornerLayer.Remove(this);

        private readonly AdornerLayer m_AdornerLayer;

        #region ===========  Static section  =============

        static DropTargetInsertionAdorner()
        {
            var polyLine = new PolyLineSegment(new[] { new Point(0, -5), new Point(0, 5) }, false);
            var pathFigure = new PathFigure(new Point(5, 0), new[] { polyLine }, true);
            m_Triangle = new PathGeometry(new[] { pathFigure });

            m_Pen.Freeze();
            m_Triangle.Freeze();
        }

        private static readonly Pen m_Pen = new Pen(Brushes.Magenta, 2);
        private static readonly PathGeometry m_Triangle;
        #endregion
    }
}