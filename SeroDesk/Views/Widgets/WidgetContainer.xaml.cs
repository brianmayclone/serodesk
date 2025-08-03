using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SeroDesk.Models;
using SeroDesk.Services;

namespace SeroDesk.Views.Widgets
{
    public partial class WidgetContainer : UserControl
    {
        public Widget Widget { get; private set; }
        private UserControl _widgetView;
        private bool _isDragging = false;
        private Point _startPoint;
        private bool _isEditMode = false;
        
        public WidgetContainer(Widget widget, UserControl widgetView)
        {
            InitializeComponent();
            
            Widget = widget;
            _widgetView = widgetView;
            
            DataContext = widget;
            WidgetContent.Content = widgetView;
            
            // Set size from widget
            Width = widget.Size.Width;
            Height = widget.Size.Height;
            
            // Setup event handlers
            this.MouseEnter += WidgetContainer_MouseEnter;
            this.MouseLeave += WidgetContainer_MouseLeave;
            this.PreviewMouseLeftButtonDown += WidgetContainer_PreviewMouseLeftButtonDown;
            this.PreviewMouseMove += WidgetContainer_PreviewMouseMove;
            this.PreviewMouseLeftButtonUp += WidgetContainer_PreviewMouseLeftButtonUp;
            this.KeyDown += WidgetContainer_KeyDown;
        }
        
        private void WidgetContainer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && _isEditMode)
            {
                RemoveWidget();
            }
        }
        
        private void WidgetContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt)
            {
                _isEditMode = true;
                DragHandle.Visibility = Visibility.Visible;
                this.Opacity = 0.8;
            }
        }
        
        private void WidgetContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDragging)
            {
                _isEditMode = false;
                DragHandle.Visibility = Visibility.Collapsed;
                this.Opacity = 1.0;
            }
        }
        
        private void WidgetContainer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isEditMode && !Widget.IsLocked)
            {
                _isDragging = true;
                _startPoint = e.GetPosition(this.Parent as UIElement);
                this.CaptureMouse();
                e.Handled = true;
            }
        }
        
        private void WidgetContainer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && this.IsMouseCaptured)
            {
                var currentPosition = e.GetPosition(this.Parent as UIElement);
                var deltaX = currentPosition.X - _startPoint.X;
                var deltaY = currentPosition.Y - _startPoint.Y;
                
                var newX = Canvas.GetLeft(this) + deltaX;
                var newY = Canvas.GetTop(this) + deltaY;
                
                // Ensure widget stays within bounds
                var parent = this.Parent as Canvas;
                if (parent != null)
                {
                    newX = Math.Max(0, Math.Min(newX, parent.ActualWidth - this.ActualWidth));
                    newY = Math.Max(0, Math.Min(newY, parent.ActualHeight - this.ActualHeight));
                }
                
                Canvas.SetLeft(this, newX);
                Canvas.SetTop(this, newY);
                
                _startPoint = currentPosition;
            }
        }
        
        private void WidgetContainer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.ReleaseMouseCapture();
                
                // Update widget position
                var newPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
                WidgetManager.Instance.UpdateWidgetPosition(Widget, newPosition);
                
                // Exit edit mode
                _isEditMode = false;
                DragHandle.Visibility = Visibility.Collapsed;
                this.Opacity = 1.0;
            }
        }
        
        private void LockWidget_Click(object sender, RoutedEventArgs e)
        {
            Widget.IsLocked = !Widget.IsLocked;
        }
        
        private void RemoveWidget_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Remove the {Widget.Title} widget?", 
                                       "Remove Widget", 
                                       MessageBoxButton.YesNo, 
                                       MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                RemoveWidget();
            }
        }
        
        private void RemoveWidget()
        {
            WidgetManager.Instance.RemoveWidget(Widget);
        }
    }
}