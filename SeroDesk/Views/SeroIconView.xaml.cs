using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SeroDesk.Models;
using SeroDesk.ViewModels;

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
        
        // Context menu events
        public event EventHandler<IconContextMenuEventArgs>? PinToDockRequested;
        public event EventHandler<IconContextMenuEventArgs>? UnpinFromDockRequested;
        
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
            
            // Extract dominant color from icon for background
            var dominantColor = ExtractDominantColorFromIcon(appIcon.IconImage);
            UpdateIconBackground(dominantColor);
            
            System.Diagnostics.Debug.WriteLine($"UpdateAppIcon: {appIcon.Name}, IconImage={appIcon.IconImage != null}, DominantColor={dominantColor}");
        }
        
        private void UpdateAppGroup(AppGroup appGroup)
        {
            IconImage.Source = appGroup.GroupIcon;
            IconText.Text = appGroup.Name;
            AppCountText.Text = appGroup.Apps.Count.ToString();
            AppCountBadge.Visibility = Visibility.Visible;
            IsGroup = true;
            
            // Extract dominant color from group icon for background
            var dominantColor = ExtractDominantColorFromIcon(appGroup.GroupIcon);
            UpdateIconBackground(dominantColor);
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
        
        // Color extraction method
        private Color ExtractDominantColorFromIcon(ImageSource? imageSource)
        {
            try
            {
                if (imageSource is BitmapSource bitmap)
                {
                    // Convert to a format we can work with
                    var formatConvertedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgr32, null, 0);
                    
                    // Create pixel array
                    int stride = formatConvertedBitmap.PixelWidth * 4;
                    byte[] pixels = new byte[formatConvertedBitmap.PixelHeight * stride];
                    formatConvertedBitmap.CopyPixels(pixels, stride, 0);
                    
                    // Sample pixels and find dominant color
                    var colorCounts = new Dictionary<Color, int>();
                    int sampleStep = Math.Max(1, pixels.Length / 1000); // Sample up to 1000 pixels
                    
                    for (int i = 0; i < pixels.Length; i += sampleStep * 4)
                    {
                        if (i + 3 < pixels.Length)
                        {
                            byte b = pixels[i];
                            byte g = pixels[i + 1];
                            byte r = pixels[i + 2];
                            byte a = pixels[i + 3];
                            
                            // Skip transparent or very dark/light pixels
                            if (a > 100 && (r > 30 || g > 30 || b > 30) && (r < 225 || g < 225 || b < 225))
                            {
                                // Quantize colors to reduce noise
                                r = (byte)((r / 32) * 32);
                                g = (byte)((g / 32) * 32);
                                b = (byte)((b / 32) * 32);
                                
                                var color = Color.FromRgb(r, g, b);
                                colorCounts[color] = colorCounts.GetValueOrDefault(color, 0) + 1;
                            }
                        }
                    }
                    
                    if (colorCounts.Any())
                    {
                        var dominantColor = colorCounts.OrderByDescending(kv => kv.Value).First().Key;
                        
                        // Enhance the color saturation and adjust brightness for better visibility
                        var hsv = RgbToHsv(dominantColor);
                        hsv.S = Math.Min(1.0, hsv.S * 1.3); // Increase saturation
                        hsv.V = Math.Max(0.4, Math.Min(0.8, hsv.V)); // Adjust brightness
                        
                        return HsvToRgb(hsv);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting dominant color: {ex.Message}");
            }
            
            // Fallback to a nice default color
            return Color.FromRgb(100, 150, 255); // Nice blue default
        }
        
        private void UpdateIconBackground(Color dominantColor)
        {
            try
            {
                // Create gradient brush with the dominant color
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };
                
                // Create lighter and darker variants
                var lightColor = Color.FromArgb(180, dominantColor.R, dominantColor.G, dominantColor.B);
                var mediumColor = Color.FromArgb(120, dominantColor.R, dominantColor.G, dominantColor.B);
                var darkColor = Color.FromArgb(80, dominantColor.R, dominantColor.G, dominantColor.B);
                
                gradientBrush.GradientStops.Add(new GradientStop(lightColor, 0.0));
                gradientBrush.GradientStops.Add(new GradientStop(mediumColor, 0.5));
                gradientBrush.GradientStops.Add(new GradientStop(darkColor, 1.0));
                
                // Update the glass background
                var glassBackground = this.FindName("GlassBackground") as Border;
                if (glassBackground != null)
                {
                    glassBackground.Background = gradientBrush;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating icon background: {ex.Message}");
            }
        }
        
        // HSV color space conversion helpers
        private (double H, double S, double V) RgbToHsv(Color rgb)
        {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;
            
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;
            
            double h = 0;
            if (delta != 0)
            {
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else if (max == b) h = 60 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;
            
            double s = max == 0 ? 0 : delta / max;
            double v = max;
            
            return (h, s, v);
        }
        
        private Color HsvToRgb((double H, double S, double V) hsv)
        {
            double c = hsv.V * hsv.S;
            double x = c * (1 - Math.Abs((hsv.H / 60) % 2 - 1));
            double m = hsv.V - c;
            
            double r = 0, g = 0, b = 0;
            
            if (hsv.H >= 0 && hsv.H < 60) { r = c; g = x; b = 0; }
            else if (hsv.H >= 60 && hsv.H < 120) { r = x; g = c; b = 0; }
            else if (hsv.H >= 120 && hsv.H < 180) { r = 0; g = c; b = x; }
            else if (hsv.H >= 180 && hsv.H < 240) { r = 0; g = x; b = c; }
            else if (hsv.H >= 240 && hsv.H < 300) { r = x; g = 0; b = c; }
            else if (hsv.H >= 300 && hsv.H < 360) { r = c; g = 0; b = x; }
            
            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255)
            );
        }
        
        // Context Menu Event Handlers
        private void IconRoot_RightClick(object sender, MouseButtonEventArgs e)
        {
            // Update context menu items based on current state
            UpdateContextMenuItems();
            e.Handled = true;
        }
        
        private void UpdateContextMenuItems()
        {
            var pinMenuItem = this.FindName("PinToDockMenuItem") as MenuItem;
            var unpinMenuItem = this.FindName("UnpinFromDockMenuItem") as MenuItem;
            
            if (pinMenuItem != null && unpinMenuItem != null)
            {
                // Check if app is already pinned
                bool isPinned = IsAppPinnedToDock();
                
                pinMenuItem.Visibility = isPinned ? Visibility.Collapsed : Visibility.Visible;
                unpinMenuItem.Visibility = isPinned ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        private bool IsAppPinnedToDock()
        {
            // Check if the app is currently pinned to dock
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow?.DockViewModel != null && AppIcon != null)
                {
                    return mainWindow.DockViewModel.IsAppPinned(AppIcon);
                }
            }
            catch { }
            
            return false;
        }
        
        private void ContextMenu_Open(object sender, RoutedEventArgs e)
        {
            OnIconClicked();
        }
        
        private void ContextMenu_PinToDock(object sender, RoutedEventArgs e)
        {
            if (AppIcon != null)
            {
                PinToDockRequested?.Invoke(this, new IconContextMenuEventArgs { AppIcon = AppIcon });
            }
        }
        
        private void ContextMenu_UnpinFromDock(object sender, RoutedEventArgs e)
        {
            if (AppIcon != null)
            {
                UnpinFromDockRequested?.Invoke(this, new IconContextMenuEventArgs { AppIcon = AppIcon });
            }
        }
        
        private void ContextMenu_OpenFileLocation(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = AppIcon?.ExecutablePath;
                if (!string.IsNullOrEmpty(filePath))
                {
                    var folderPath = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                    {
                        Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open file location: {ex.Message}");
            }
        }
        
        private void ContextMenu_Properties(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = AppIcon?.ExecutablePath;
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "rundll32.exe",
                        Arguments = $"shell32.dll,Properties_RunDLL \"{filePath}\"",
                        UseShellExecute = true
                    };
                    Process.Start(processInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show properties: {ex.Message}");
            }
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
    
    public class IconContextMenuEventArgs : EventArgs
    {
        public AppIcon? AppIcon { get; set; }
        public AppGroup? AppGroup { get; set; }
    }
}