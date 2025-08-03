using System.Windows;
using System.Windows.Input;

namespace SeroDesk.Platform
{
    public enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
    
    public class GestureRecognizer
    {
        private Point _startPoint;
        private DateTime _startTime;
        private bool _isTracking;
        private readonly double _swipeThreshold = 50;
        private readonly double _pinchThreshold = 0.1;
        
        public event Action<SwipeDirection>? SwipeDetected;
        public event Action<double>? PinchDetected;
        public event Action? TapDetected;
        public event Action? DoubleTapDetected;
        public event Action? LongPressDetected;
        
        public Point StartPoint => _startPoint;
        
        private DateTime _lastTapTime = DateTime.MinValue;
        private readonly TimeSpan _doubleTapTimeout = TimeSpan.FromMilliseconds(300);
        private readonly TimeSpan _longPressTimeout = TimeSpan.FromMilliseconds(500);
        
        public void OnManipulationStarting(ManipulationStartingEventArgs e)
        {
            _startPoint = new Point(0, 0); // Will be set on first delta
            _startTime = DateTime.Now;
            _isTracking = true;
        }
        
        public void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            if (!_isTracking) return;
            
            // Set start point on first delta if not set
            if (_startPoint.X == 0 && _startPoint.Y == 0)
            {
                _startPoint = e.ManipulationOrigin;
            }
            
            // Check for pinch gesture using scale
            if (Math.Abs(e.DeltaManipulation.Scale.X - 1.0) > _pinchThreshold ||
                Math.Abs(e.DeltaManipulation.Scale.Y - 1.0) > _pinchThreshold)
            {
                var scaleFactor = (e.DeltaManipulation.Scale.X + e.DeltaManipulation.Scale.Y) / 2.0;
                PinchDetected?.Invoke(scaleFactor);
            }
        }
        
        public void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            if (!_isTracking) return;
            
            _isTracking = false;
            var endPoint = new Point(
                _startPoint.X + e.TotalManipulation.Translation.X,
                _startPoint.Y + e.TotalManipulation.Translation.Y);
            
            var deltaX = endPoint.X - _startPoint.X;
            var deltaY = endPoint.Y - _startPoint.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            var duration = DateTime.Now - _startTime;
            
            // Check for tap gestures
            if (distance < 10)
            {
                if (duration > _longPressTimeout)
                {
                    LongPressDetected?.Invoke();
                }
                else
                {
                    var timeSinceLastTap = DateTime.Now - _lastTapTime;
                    if (timeSinceLastTap < _doubleTapTimeout)
                    {
                        DoubleTapDetected?.Invoke();
                        _lastTapTime = DateTime.MinValue;
                    }
                    else
                    {
                        TapDetected?.Invoke();
                        _lastTapTime = DateTime.Now;
                    }
                }
                return;
            }
            
            // Check for swipe
            if (distance > _swipeThreshold)
            {
                var direction = GetSwipeDirection(deltaX, deltaY);
                if (direction != SwipeDirection.None)
                {
                    SwipeDetected?.Invoke(direction);
                }
            }
        }
        
        private SwipeDirection GetSwipeDirection(double deltaX, double deltaY)
        {
            var absX = Math.Abs(deltaX);
            var absY = Math.Abs(deltaY);
            
            if (absX > absY)
            {
                return deltaX > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            }
            else if (absY > absX)
            {
                return deltaY > 0 ? SwipeDirection.Down : SwipeDirection.Up;
            }
            
            return SwipeDirection.None;
        }
        
        private double GetDistance(Point p1, Point p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        
        // Mouse support
        private bool _isMouseTracking;
        private Point _mouseStartPoint;
        private DateTime _mouseStartTime;
        
        public void OnMouseDown(MouseButtonEventArgs e, IInputElement element)
        {
            _mouseStartPoint = e.GetPosition(element);
            _mouseStartTime = DateTime.Now;
            _isMouseTracking = true;
        }
        
        public void OnMouseMove(MouseEventArgs e, IInputElement element)
        {
            if (!_isMouseTracking || e.LeftButton != MouseButtonState.Pressed) return;
            
            // Mouse doesn't support multi-touch, so no pinch detection
        }
        
        public void OnMouseUp(MouseButtonEventArgs e, IInputElement element)
        {
            if (!_isMouseTracking) return;
            
            _isMouseTracking = false;
            var endPoint = e.GetPosition(element);
            var deltaX = endPoint.X - _mouseStartPoint.X;
            var deltaY = endPoint.Y - _mouseStartPoint.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            var duration = DateTime.Now - _mouseStartTime;
            
            // Check for click (tap equivalent)
            if (distance < 5)
            {
                if (duration > _longPressTimeout)
                {
                    LongPressDetected?.Invoke();
                }
                else if (e.ClickCount == 2)
                {
                    DoubleTapDetected?.Invoke();
                }
                else
                {
                    TapDetected?.Invoke();
                }
                return;
            }
            
            // Check for drag (swipe equivalent)
            if (distance > _swipeThreshold)
            {
                var direction = GetSwipeDirection(deltaX, deltaY);
                if (direction != SwipeDirection.None)
                {
                    SwipeDetected?.Invoke(direction);
                }
            }
        }
    }
}