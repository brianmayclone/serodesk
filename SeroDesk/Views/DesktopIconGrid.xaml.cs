using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SeroDesk.Models;
using SeroDesk.ViewModels;

namespace SeroDesk.Views
{
    public partial class DesktopIconGrid : UserControl
    {
        private Point _dragStartPoint;
        private bool _isDragging;
        private AppIcon? _draggedIcon;
        private DesktopViewModel? _viewModel;
        
        public DesktopIconGrid()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                _viewModel = DataContext as DesktopViewModel;
            };
        }
        
        private void IconButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("IconButton_Click event triggered");
            
            if (sender is Button button && button.Tag is AppIcon icon)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Icon clicked: {icon.Name} - Path: {icon.ExecutablePath}");
                    
                    // Select the icon
                    if (_viewModel != null)
                    {
                        _viewModel.SelectIcon(icon);
                    }
                    
                    // Show feedback
                    MessageBox.Show($"Clicked: {icon.Name}", "Icon Click", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in IconButton_Click: {ex.Message}");
                    MessageBox.Show($"Error selecting {icon.Name}: {ex.Message}", "Selection Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("IconButton_Click: sender is not Button or Tag is not AppIcon");
            }
        }
        
        private void IconButton_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.Tag is AppIcon icon)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Launching app: {icon.Name} - {icon.ExecutablePath}");
                    icon.Launch();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error launching {icon.Name}: {ex.Message}", "Launch Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        public void ScaleIcons(double scaleFactor)
        {
            if (_viewModel == null) return;
            
            foreach (var icon in _viewModel.DesktopIcons)
            {
                var newScale = Math.Max(0.5, Math.Min(2.0, icon.Scale * scaleFactor));
                AnimateIconScale(icon, newScale);
            }
        }
        
        private void AnimateIconScale(AppIcon icon, double targetScale)
        {
            icon.Scale = targetScale;
        }
        
        private void Icon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is AppIcon icon)
            {
                _dragStartPoint = e.GetPosition(this);
                _draggedIcon = icon;
                
                // Select icon
                if (_viewModel != null)
                {
                    _viewModel.SelectIcon(icon);
                }
                
                element.CaptureMouse();
            }
        }
        
        private void Icon_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedIcon != null)
            {
                var currentPosition = e.GetPosition(this);
                var diff = currentPosition - _dragStartPoint;
                
                if (!_isDragging && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    StartDrag(_draggedIcon, currentPosition);
                }
                else if (_isDragging)
                {
                    UpdateDragVisual(currentPosition);
                }
            }
        }
        
        private void Icon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.ReleaseMouseCapture();
                
                if (_isDragging)
                {
                    EndDrag(e.GetPosition(this));
                }
                else if (_draggedIcon != null && e.ClickCount == 2)
                {
                    // Double-click to launch
                    _draggedIcon.Launch();
                }
                
                _draggedIcon = null;
                _isDragging = false;
            }
        }
        
        
        private void Icon_TouchDown(object sender, TouchEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is AppIcon icon)
            {
                _dragStartPoint = e.GetTouchPoint(this).Position;
                _draggedIcon = icon;
                
                if (_viewModel != null)
                {
                    _viewModel.SelectIcon(icon);
                }
                
                element.CaptureTouch(e.TouchDevice);
                e.Handled = true;
            }
        }
        
        private void Icon_TouchMove(object sender, TouchEventArgs e)
        {
            if (_draggedIcon != null)
            {
                var currentPosition = e.GetTouchPoint(this).Position;
                var diff = currentPosition - _dragStartPoint;
                
                if (!_isDragging && (Math.Abs(diff.X) > 10 || Math.Abs(diff.Y) > 10))
                {
                    StartDrag(_draggedIcon, currentPosition);
                }
                else if (_isDragging)
                {
                    UpdateDragVisual(currentPosition);
                }
                
                e.Handled = true;
            }
        }
        
        private void Icon_TouchUp(object sender, TouchEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.ReleaseTouchCapture(e.TouchDevice);
                
                if (_isDragging)
                {
                    EndDrag(e.GetTouchPoint(this).Position);
                }
                else if (_draggedIcon != null)
                {
                    // Double-tap to launch
                    if (DateTime.Now.Subtract(_draggedIcon.LastAccessed).TotalMilliseconds < 500)
                    {
                        _draggedIcon.Launch();
                    }
                    _draggedIcon.LastAccessed = DateTime.Now;
                }
                
                _draggedIcon = null;
                _isDragging = false;
                e.Handled = true;
            }
        }
        
        private void StartDrag(AppIcon icon, Point position)
        {
            _isDragging = true;
            icon.IsDragging = true;
            
            // Setup drag visual
            DragCanvas.Visibility = Visibility.Visible;
            DragIcon.Source = icon.IconImage;
            DragText.Text = icon.Name;
            
            UpdateDragVisual(position);
            
            // Animate icon scale down
            AnimateIconScale(icon, 0.8);
        }
        
        private void UpdateDragVisual(Point position)
        {
            Canvas.SetLeft(DragVisual, position.X - 60);
            Canvas.SetTop(DragVisual, position.Y - 60);
        }
        
        private void EndDrag(Point position)
        {
            if (_draggedIcon == null) return;
            
            DragCanvas.Visibility = Visibility.Collapsed;
            _draggedIcon.IsDragging = false;
            
            // Animate icon scale back
            AnimateIconScale(_draggedIcon, 1.0);
            
            // Update icon position
            if (_viewModel != null)
            {
                _viewModel.MoveIcon(_draggedIcon, position);
            }
        }
        
        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            
            e.Handled = true;
        }
        
        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var position = e.GetPosition(this);
                
                if (_viewModel != null && files != null)
                {
                    foreach (var file in files)
                    {
                        _viewModel.AddFileIcon(file, position);
                    }
                }
            }
            
            e.Handled = true;
        }
        
        // Simplified drag handling - will be implemented later if needed
        // Focus on basic interaction first
    }
}