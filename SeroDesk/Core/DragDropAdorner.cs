using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SeroDesk.Core
{
    public class DragDropAdorner : Adorner
    {
        private readonly UIElement _child;
        private double _leftOffset;
        private double _topOffset;

        public DragDropAdorner(UIElement adornedElement, UIElement child) : base(adornedElement)
        {
            _child = child;
            IsHitTestVisible = false;
        }

        public void UpdatePosition(double left, double top)
        {
            _leftOffset = left;
            _topOffset = top;
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _child.Arrange(new Rect(finalSize));
            return finalSize;
        }

        protected override Visual GetVisualChild(int index)
        {
            return _child;
        }

        protected override int VisualChildrenCount => 1;

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(_leftOffset, _topOffset));
            return result;
        }
    }
}