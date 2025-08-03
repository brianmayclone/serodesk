using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SeroDesk.Models;

namespace SeroDesk.Views
{
    public partial class SeroIconView : UserControl
    {
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private Transform? _originalTransform;
        private bool _hasMovedDuringTouch = false;
        
        public SeroIconView()
        {
            InitializeComponent();
            
            // Enable mouse drag and drop
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseLeave += OnMouseLeave;
            
            // Enable manipulation for touch support (replaces touch events to avoid conflicts)
            this.IsManipulationEnabled = true;
            this.ManipulationStarting += OnManipulationStarting;
            this.ManipulationDelta += OnManipulationDelta;
            this.ManipulationCompleted += OnManipulationCompleted;
        }
        
        // Dependency Properties
        public static readonly DependencyProperty AppIconProperty =
            DependencyProperty.Register("AppIcon", typeof(AppIcon), typeof(SeroIconView),
                new PropertyMetadata(null, OnAppIconChanged));
                
        public static readonly DependencyProperty AppGroupProperty =
            DependencyProperty.Register("AppGroup", typeof(AppGroup), typeof(SeroIconView),
                new PropertyMetadata(null, OnAppGroupChanged));
                
        public static readonly DependencyProperty IsGroupProperty =
            DependencyProperty.Register("IsGroup", typeof(bool), typeof(SeroIconView),
                new PropertyMetadata(false));
                
        public static readonly DependencyProperty GridPositionProperty =
            DependencyProperty.Register("GridPosition", typeof(Point), typeof(SeroIconView),
                new PropertyMetadata(new Point(0, 0)));
        
        public AppIcon? AppIcon
        {
            get => (AppIcon?)GetValue(AppIconProperty);
            set => SetValue(AppIconProperty, value);
        }
        
        public AppGroup? AppGroup
        {
            get => (AppGroup?)GetValue(AppGroupProperty);
            set => SetValue(AppGroupProperty, value);
        }
        
        public bool IsGroup
        {
            get => (bool)GetValue(IsGroupProperty);
            set => SetValue(IsGroupProperty, value);
        }
        
        public Point GridPosition
        {
            get => (Point)GetValue(GridPositionProperty);
            set => SetValue(GridPositionProperty, value);
        }
        
        // Events
        public event EventHandler<IconDragEventArgs>? DragStarted;
        public event EventHandler<IconDragEventArgs>? DragMoved;
        public event EventHandler<IconDragEventArgs>? DragCompleted;
        public event EventHandler<IconClickEventArgs>? IconClicked;
        
        private static void OnAppIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SeroIconView iconView && e.NewValue is AppIcon appIcon)
            {
                iconView.UpdateAppIcon(appIcon);
            }
        }
        
        private static void OnAppGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SeroIconView iconView && e.NewValue is AppGroup appGroup)
            {
                iconView.UpdateAppGroup(appGroup);
            }
        }
        
        private void UpdateAppIcon(AppIcon appIcon)
        {
            IconImage.Source = appIcon.IconImage;
            IconText.Text = appIcon.Name;
            AppCountBadge.Visibility = Visibility.Collapsed;
            IsGroup = false;
            
            System.Diagnostics.Debug.WriteLine($"UpdateAppIcon: {appIcon.Name}, IconImage={appIcon.IconImage != null}, Source set to IconImage");
        }
        
        private void UpdateAppGroup(AppGroup appGroup)
        {
            IconImage.Source = appGroup.GroupIcon;
            IconText.Text = appGroup.Name;
            AppCountText.Text = appGroup.Apps.Count.ToString();
            AppCountBadge.Visibility = Visibility.Visible;
            IsGroup = true;
        }
        
        // Mouse Event Handlers
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            this.CaptureMouse();
            e.Handled = true;
        }
        
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (this.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(this);
                Vector diff = currentPosition - _dragStartPoint;
                
                if (!_isDragging && (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5))
                {
                    // Start dragging
                    _isDragging = true;
                    StartDragVisual();
                    
                    var dragEventArgs = new IconDragEventArgs
                    {
                        IconView = this,
                        Position = e.GetPosition(this.Parent as UIElement),
                        AppIcon = this.AppIcon,
                        AppGroup = this.AppGroup,
                        IsGroup = this.IsGroup
                    };
                    DragStarted?.Invoke(this, dragEventArgs);
                }
                
                if (_isDragging)
                {
                    // Continue dragging
                    var dragEventArgs = new IconDragEventArgs
                    {
                        IconView = this,
                        Position = e.GetPosition(this.Parent as UIElement),
                        AppIcon = this.AppIcon,
                        AppGroup = this.AppGroup,
                        IsGroup = this.IsGroup
                    };
                    DragMoved?.Invoke(this, dragEventArgs);
                }
            }
        }
        
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture();
                
                if (_isDragging)
                {
                    // End dragging
                    EndDragVisual();
                    
                    var dragEventArgs = new IconDragEventArgs
                    {
                        IconView = this,
                        Position = e.GetPosition(this.Parent as UIElement),
                        AppIcon = this.AppIcon,
                        AppGroup = this.AppGroup,
                        IsGroup = this.IsGroup
                    };
                    DragCompleted?.Invoke(this, dragEventArgs);
                    
                    _isDragging = false;
                }
                else
                {
                    // Simple click
                    OnIconClicked();
                }
                
                e.Handled = true;
            }
        }
        
        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging && this.IsMouseCaptured)
            {
                // Continue tracking even when mouse leaves the icon
                return;
            }
        }
        
        private void OnIconClicked()
        {
            var clickEventArgs = new IconClickEventArgs
            {
                IconView = this,
                AppIcon = this.AppIcon,
                AppGroup = this.AppGroup,
                IsGroup = this.IsGroup
            };
            IconClicked?.Invoke(this, clickEventArgs);
        }
        
        // Touch Event Handlers
        // Manipulation Event Handlers (for touch gestures)
        private void OnManipulationStarting(object? sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = this.Parent as UIElement;
            e.Handled = true;
        }
        
        private void OnManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
        {
            if (!_isDragging && (Math.Abs(e.CumulativeManipulation.Translation.X) > 10 || 
                                Math.Abs(e.CumulativeManipulation.Translation.Y) > 10))
            {
                // Start dragging with manipulation
                _isDragging = true;
                _hasMovedDuringTouch = true;
                StartDragVisual();
                
                // Use the actual manipulation position relative to parent
                var parentElement = this.Parent as UIElement;
                var containerElement = e.ManipulationContainer as UIElement;
                Point positionInParent = containerElement != null && parentElement != null ? 
                    containerElement.TranslatePoint(e.ManipulationOrigin, parentElement) : 
                    e.ManipulationOrigin;
                
                var dragEventArgs = new IconDragEventArgs
                {
                    IconView = this,
                    Position = positionInParent,
                    AppIcon = this.AppIcon,
                    AppGroup = this.AppGroup,
                    IsGroup = this.IsGroup
                };
                DragStarted?.Invoke(this, dragEventArgs);
            }
            
            if (_isDragging)
            {
                // Update position during manipulation - calculate current position from origin + translation
                var parentElement = this.Parent as UIElement;
                var containerElement = e.ManipulationContainer as UIElement;
                Point currentPosition = new Point(
                    e.ManipulationOrigin.X + e.CumulativeManipulation.Translation.X,
                    e.ManipulationOrigin.Y + e.CumulativeManipulation.Translation.Y
                );
                
                // If manipulation container is different from parent, transform coordinates
                Point positionInParent = containerElement != null && parentElement != null && containerElement != parentElement ? 
                    containerElement.TranslatePoint(currentPosition, parentElement) : 
                    currentPosition;
                
                var dragEventArgs = new IconDragEventArgs
                {
                    IconView = this,
                    Position = positionInParent,
                    AppIcon = this.AppIcon,
                    AppGroup = this.AppGroup,
                    IsGroup = this.IsGroup
                };
                DragMoved?.Invoke(this, dragEventArgs);
            }
            
            e.Handled = true;
        }
        
        private void OnManipulationCompleted(object? sender, ManipulationCompletedEventArgs e)
        {
            if (_isDragging)
            {
                // End dragging - calculate final position from origin + translation
                var parentElement = this.Parent as UIElement;
                var containerElement = e.ManipulationContainer as UIElement;
                Point finalPosition = new Point(
                    e.ManipulationOrigin.X + e.TotalManipulation.Translation.X,
                    e.ManipulationOrigin.Y + e.TotalManipulation.Translation.Y
                );
                
                Point positionInParent = containerElement != null && parentElement != null && containerElement != parentElement ? 
                    containerElement.TranslatePoint(finalPosition, parentElement) : 
                    finalPosition;
                
                EndDragVisual();
                
                var dragEventArgs = new IconDragEventArgs
                {
                    IconView = this,
                    Position = positionInParent,
                    AppIcon = this.AppIcon,
                    AppGroup = this.AppGroup,
                    IsGroup = this.IsGroup
                };
                DragCompleted?.Invoke(this, dragEventArgs);
                
                _isDragging = false;
            }
            else if (!_hasMovedDuringTouch)
            {
                // Only trigger click if there was NO movement during touch
                OnIconClicked();
            }
            
            // Reset movement flag for next touch sequence
            _hasMovedDuringTouch = false;
            
            e.Handled = true;
        }
        
        // Visual Feedback Methods
        private void StartDragVisual()
        {
            // Apply drag transform (slight scale and rotation)
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1.1, 1.1));
            transformGroup.Children.Add(new RotateTransform(2));
            
            _originalTransform = this.RenderTransform;
            this.RenderTransform = transformGroup;
            this.RenderTransformOrigin = new Point(0.5, 0.5);
            
            // Semi-transparent during drag
            this.Opacity = 0.8;
            
            // Show drag preview
            DragPreview.Visibility = Visibility.Visible;
            
            // Bring to front
            Panel.SetZIndex(this, 1000);
        }
        
        private void EndDragVisual()
        {
            // Restore original transform
            this.RenderTransform = _originalTransform;
            this.Opacity = 1.0;
            DragPreview.Visibility = Visibility.Collapsed;
            Panel.SetZIndex(this, 0);
        }
        
        // Animation Methods
        public void AnimateToPosition(Point targetPosition, TimeSpan duration)
        {
            var currentLeft = Canvas.GetLeft(this);
            var currentTop = Canvas.GetTop(this);
            
            if (double.IsNaN(currentLeft)) currentLeft = 0;
            if (double.IsNaN(currentTop)) currentTop = 0;
            
            var leftAnimation = new DoubleAnimation
            {
                From = currentLeft,
                To = targetPosition.X,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var topAnimation = new DoubleAnimation
            {
                From = currentTop,
                To = targetPosition.Y,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            this.BeginAnimation(Canvas.LeftProperty, leftAnimation);
            this.BeginAnimation(Canvas.TopProperty, topAnimation);
        }
        
        public void StartWiggleAnimation()
        {
            var wiggleStoryboard = new Storyboard();
            wiggleStoryboard.RepeatBehavior = RepeatBehavior.Forever;
            
            var rotateAnimation = new DoubleAnimation
            {
                From = -1,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            var rotateTransform = new RotateTransform();
            this.RenderTransform = rotateTransform;
            this.RenderTransformOrigin = new Point(0.5, 0.5);
            
            Storyboard.SetTarget(rotateAnimation, rotateTransform);
            Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath(RotateTransform.AngleProperty));
            
            wiggleStoryboard.Children.Add(rotateAnimation);
            wiggleStoryboard.Begin();
        }
        
        public void StopWiggleAnimation()
        {
            this.BeginStoryboard(new Storyboard(), HandoffBehavior.SnapshotAndReplace);
            this.RenderTransform = Transform.Identity;
        }
    }
    
    // Event Args Classes
    public class IconDragEventArgs : EventArgs
    {
        public SeroIconView? IconView { get; set; }
        public Point Position { get; set; }
        public AppIcon? AppIcon { get; set; }
        public AppGroup? AppGroup { get; set; }
        public bool IsGroup { get; set; }
    }
    
    public class IconClickEventArgs : EventArgs
    {
        public SeroIconView? IconView { get; set; }
        public AppIcon? AppIcon { get; set; }
        public AppGroup? AppGroup { get; set; }
        public bool IsGroup { get; set; }
    }
}