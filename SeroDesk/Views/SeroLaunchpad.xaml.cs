using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SeroDesk.Core;
using SeroDesk.Models;
using SeroDesk.ViewModels;

namespace SeroDesk.Views
{
    public partial class SeroLaunchpad : UserControl
    {
        private LaunchpadViewModel? _viewModel;
        
        public SeroLaunchpad()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                _viewModel = DataContext as LaunchpadViewModel;
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    UpdatePageIndicators();
                }
            };
            
            // Add swipe gesture support
            this.ManipulationBoundaryFeedback += SeroLaunchpad_ManipulationBoundaryFeedback;
            this.ManipulationDelta += SeroLaunchpad_ManipulationDelta;
            this.ManipulationCompleted += SeroLaunchpad_ManipulationCompleted;
            this.IsManipulationEnabled = true;
        }
        
        private void LaunchpadGrid_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = LaunchpadGrid;
            e.Mode = ManipulationModes.All;
        }
        
        private void LaunchpadGrid_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            // Check for swipe down from top
            var startY = e.ManipulationOrigin.Y;
            var deltaY = e.CumulativeManipulation.Translation.Y;
            
            if (startY < 100 && deltaY > 50) // Swipe down from top
            {
                var startX = e.ManipulationOrigin.X;
                var screenWidth = this.ActualWidth;
                
                // Get MainWindow to trigger notification/control center
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    if (startX < screenWidth / 2)
                    {
                        mainWindow.ShowNotificationCenter();
                    }
                    else
                    {
                        mainWindow.ShowControlCenter();
                    }
                }
            }
        }
        
        private void LaunchpadGrid_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            // Handle manipulation completed
        }
        
        public void Initialize()
        {
            _viewModel = new LaunchpadViewModel();
            DataContext = _viewModel;
        }
        
        public async void Show()
        {
            Visibility = Visibility.Visible;
            
            // Load apps on first show
            if (_viewModel != null && _viewModel.AllApplications.Count == 0)
            {
                await _viewModel.LoadAllApplicationsAsync();
            }
            
            // Zoom in animation
            var zoomIn = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            
            var scaleTransform = new System.Windows.Media.ScaleTransform();
            LaunchpadGrid.RenderTransform = scaleTransform;
            LaunchpadGrid.RenderTransformOrigin = new Point(0.5, 0.5);
            
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, zoomIn);
            scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, zoomIn);
            LaunchpadGrid.BeginAnimation(OpacityProperty, fadeIn);
            
            // Focus search box
            SearchBox.Focus();
        }
        
        public void Hide()
        {
            // Zoom out animation
            var zoomOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.8,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            
            fadeOut.Completed += (s, e) =>
            {
                Visibility = Visibility.Collapsed;
                SearchBox.Text = string.Empty;
            };
            
            var scaleTransform = LaunchpadGrid.RenderTransform as System.Windows.Media.ScaleTransform;
            if (scaleTransform != null)
            {
                scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, zoomOut);
                scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, zoomOut);
            }
            
            LaunchpadGrid.BeginAnimation(OpacityProperty, fadeOut);
        }
        
        private void AppIcon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AppIcon app)
            {
                app.Launch();
                
                // Play launch animation
                PlayLaunchAnimation(button);
                
                // Don't hide if this is the desktop launchpad
                // Only hide if this is a separate launchpad window
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow == null) // This is in a separate window
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Hide();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }
        
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
        
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.FilterApplications(SearchBox.Text);
            }
        }
        
        private void PlayLaunchAnimation(Button button)
        {
            var launchStoryboard = new Storyboard();
            
            // Scale up then fade out
            var scaleUp = new DoubleAnimation
            {
                To = 1.5,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(100)
            };
            
            var restoreOpacity = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(100),
                BeginTime = TimeSpan.FromMilliseconds(500)
            };
            
            var restoreScale = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(100),
                BeginTime = TimeSpan.FromMilliseconds(500)
            };
            
            Storyboard.SetTarget(scaleUp, button);
            Storyboard.SetTarget(fadeOut, button);
            Storyboard.SetTarget(restoreOpacity, button);
            Storyboard.SetTarget(restoreScale, button);
            
            Storyboard.SetTargetProperty(scaleUp, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
            Storyboard.SetTargetProperty(restoreOpacity, new PropertyPath("Opacity"));
            Storyboard.SetTargetProperty(restoreScale, new PropertyPath("RenderTransform.ScaleX"));
            
            // Ensure button has transforms
            if (button.RenderTransform == null)
            {
                button.RenderTransform = new System.Windows.Media.ScaleTransform();
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            
            launchStoryboard.Children.Add(scaleUp);
            launchStoryboard.Children.Add(fadeOut);
            launchStoryboard.Children.Add(restoreOpacity);
            launchStoryboard.Children.Add(restoreScale);
            
            launchStoryboard.Begin();
        }
        
        private void GroupIcon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AppGroup group)
            {
                // TODO: Show group expanded view
                ShowGroupExpandedView(group);
            }
        }
        
        private void ShowGroupExpandedView(AppGroup group)
        {
            // Set group title
            GroupExpandedTitle.Text = group.Name;
            
            // Populate apps grid
            GroupAppsGrid.ItemsSource = group.Apps;
            
            // Position the expanded view near the group's location
            PositionGroupExpandedView(group);
            
            // Show overlay with animation
            GroupExpandedOverlay.Visibility = Visibility.Visible;
            
            // Fade in animation
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            GroupExpandedOverlay.BeginAnimation(OpacityProperty, fadeIn);
        }
        
        private void PositionGroupExpandedView(AppGroup group)
        {
            // Find the group button in the visual tree
            var groupButton = FindGroupButton(group);
            if (groupButton != null)
            {
                // Get position of the group button relative to the main grid
                var position = groupButton.TransformToAncestor(LaunchpadGrid).Transform(new Point(0, 0));
                
                // Get the expanded view container
                var expandedContainer = GroupExpandedOverlay.Children.OfType<Border>().FirstOrDefault();
                if (expandedContainer != null)
                {
                    // Calculate desired position (above the group if possible)
                    var desiredX = Math.Max(40, Math.Min(position.X - 100, LaunchpadGrid.ActualWidth - 640));
                    var desiredY = Math.Max(40, position.Y - 300);
                    
                    // If too close to top, show below the group instead
                    if (desiredY < 100)
                    {
                        desiredY = position.Y + 160;
                    }
                    
                    // Apply margin to position the container
                    expandedContainer.Margin = new Thickness(desiredX, desiredY, 0, 0);
                    expandedContainer.HorizontalAlignment = HorizontalAlignment.Left;
                    expandedContainer.VerticalAlignment = VerticalAlignment.Top;
                }
            }
        }
        
        private Button? FindGroupButton(AppGroup group)
        {
            // Search through the AllAppsGrid items
            var itemsControl = AllAppsGrid;
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container != null)
                {
                    var button = FindVisualChild<Button>(container);
                    if (button?.Tag is AppGroup buttonGroup && buttonGroup.Id == group.Id)
                    {
                        return button;
                    }
                }
            }
            return null;
        }
        
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
        
        private void CloseGroupExpanded_Click(object sender, RoutedEventArgs e)
        {
            // Fade out animation
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            
            fadeOut.Completed += (s, args) =>
            {
                GroupExpandedOverlay.Visibility = Visibility.Collapsed;
                GroupAppsGrid.ItemsSource = null;
            };
            
            GroupExpandedOverlay.BeginAnimation(OpacityProperty, fadeOut);
        }
        
        private void GroupAppIcon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AppIcon app)
            {
                app.Launch();
                
                // Play launch animation
                PlayLaunchAnimation(button);
                
                // Close group view
                CloseGroupExpanded_Click(sender, e);
                
                // Don't hide if this is the desktop launchpad
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow == null) // This is in a separate window
                {
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Hide();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }
        
        private void CreateGroup_Click(object sender, RoutedEventArgs e)
        {
            var groupName = GroupNameTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(groupName) && _viewModel != null)
            {
                _viewModel.CreateGroup(groupName);
                GroupCreationOverlay.Visibility = Visibility.Collapsed;
                GroupNameTextBox.Text = "New Group";
            }
        }
        
        private void CancelGroupCreation_Click(object sender, RoutedEventArgs e)
        {
            GroupCreationOverlay.Visibility = Visibility.Collapsed;
            GroupNameTextBox.Text = "New Group";
        }
        
        public void ShowGroupCreationDialog()
        {
            GroupCreationOverlay.Visibility = Visibility.Visible;
            GroupNameTextBox.SelectAll();
            GroupNameTextBox.Focus();
        }
        
        // Drag & Drop functionality for app grouping and reordering
        private bool _isDragging = false;
        private Point _startPoint;
        private AppIcon? _draggedApp;
        private DragDropAdorner? _dragAdorner;
        private AdornerLayer? _adornerLayer;
        private Timer? _reorderTimer;
        private Point _lastDragPosition;
        private bool _isReorderMode = false;
        
        private void AppIcon_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.Tag is AppIcon app)
            {
                _startPoint = e.GetPosition(button);
                _draggedApp = app;
            }
        }
        
        private void AppIcon_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _draggedApp != null && !_isDragging)
            {
                if (sender is Button button)
                {
                    Point currentPosition = e.GetPosition(button);
                    var diff = _startPoint - currentPosition;
                    
                    if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        _isDragging = true;
                        
                        // Start iOS-style reorder mode
                        StartReorderMode(button);
                        
                        // Create visual drag feedback
                        StartVisualDrag(button);
                        
                        // Start drag operation with custom tracking
                        StartCustomDragDrop(button);
                        
                        // Clean up after drag
                        EndVisualDrag();
                        EndReorderMode();
                        
                        _isDragging = false;
                        _draggedApp = null;
                    }
                }
            }
        }
        
        private void StartReorderMode(Button sourceButton)
        {
            _isReorderMode = true;
            
            // Start timer for continuous reorder checking
            _reorderTimer = new Timer(CheckForReorder, null, 50, 50);
            
            // Add wiggle animation to all icons (iOS style)
            StartWiggleAnimation();
        }
        
        private void EndReorderMode()
        {
            _isReorderMode = false;
            
            // Stop reorder timer
            _reorderTimer?.Dispose();
            _reorderTimer = null;
            
            // Stop wiggle animation
            StopWiggleAnimation();
        }
        
        private void StartCustomDragDrop(Button sourceButton)
        {
            // Custom drag implementation that updates position continuously
            var window = Window.GetWindow(this);
            if (window == null) return;
            
            MouseEventHandler moveHandler = null;
            MouseButtonEventHandler upHandler = null;
            
            moveHandler = (s, e) =>
            {
                var position = e.GetPosition(AllAppsGrid);
                _lastDragPosition = position;
                
                // Update drag visual position
                if (_dragAdorner != null)
                {
                    _dragAdorner.UpdatePosition(position.X - 50, position.Y - 50);
                }
            };
            
            upHandler = (s, e) =>
            {
                window.MouseMove -= moveHandler;
                window.MouseLeftButtonUp -= upHandler;
                
                // Handle final drop
                HandleIconReorderDrop();
            };
            
            window.MouseMove += moveHandler;
            window.MouseLeftButtonUp += upHandler;
        }
        
        private void CheckForReorder(object? state)
        {
            if (!_isReorderMode || _draggedApp == null) return;
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                var targetIndex = GetGridIndexFromPosition(_lastDragPosition);
                if (targetIndex >= 0 && targetIndex < _viewModel?.DisplayItems.Count)
                {
                    var currentIndex = _viewModel.DisplayItems.IndexOf(_draggedApp);
                    if (currentIndex >= 0 && currentIndex != targetIndex)
                    {
                        // Perform live reorder
                        _viewModel.MoveItem(currentIndex, targetIndex);
                    }
                }
            });
        }
        
        private int GetGridIndexFromPosition(Point position)
        {
            try
            {
                // Calculate grid position based on mouse coordinates
                var gridBounds = AllAppsGrid.RenderSize;
                var itemWidth = gridBounds.Width / 8; // 8 columns
                var itemHeight = gridBounds.Height / 6; // 6 rows
                
                var col = (int)(position.X / itemWidth);
                var row = (int)(position.Y / itemHeight);
                
                col = Math.Max(0, Math.Min(7, col));
                row = Math.Max(0, Math.Min(5, row));
                
                return row * 8 + col;
            }
            catch
            {
                return -1;
            }
        }
        
        private void HandleIconReorderDrop()
        {
            // Final position handling - already done by live updates
            // Just ensure the layout is refreshed
            _viewModel?.RefreshLayout();
        }
        
        private void StartWiggleAnimation()
        {
            var buttons = FindVisualChildren<Button>(AllAppsGrid);
            
            foreach (var button in buttons)
            {
                if (button.Tag == _draggedApp) continue; // Don't wiggle the dragged item
                
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
                button.RenderTransform = rotateTransform;
                button.RenderTransformOrigin = new Point(0.5, 0.5);
                
                Storyboard.SetTarget(rotateAnimation, rotateTransform);
                Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath(RotateTransform.AngleProperty));
                
                wiggleStoryboard.Children.Add(rotateAnimation);
                wiggleStoryboard.Begin();
                
                // Store storyboard for cleanup
                button.Tag = $"{button.Tag}_wiggle:{wiggleStoryboard.GetHashCode()}";
            }
        }
        
        private void StopWiggleAnimation()
        {
            var buttons = FindVisualChildren<Button>(AllAppsGrid);
            
            foreach (var button in buttons)
            {
                // Stop any running storyboards
                button.BeginStoryboard(new Storyboard(), HandoffBehavior.SnapshotAndReplace);
                
                // Reset transform
                button.RenderTransform = null;
                
                // Clean up tag
                if (button.Tag?.ToString()?.Contains("_wiggle:") == true)
                {
                    var originalTag = button.Tag.ToString()?.Split("_wiggle:")[0];
                    // Restore original tag based on type
                    if (originalTag == "SeroDesk.Models.AppIcon")
                        button.Tag = button.DataContext;
                    else
                        button.Tag = originalTag;
                }
            }
        }
        
        private void StartVisualDrag(Button sourceButton)
        {
            try
            {
                // Get the adorner layer
                _adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (_adornerLayer == null) return;

                // Create a visual representation of the dragged item
                var dragVisual = CreateDragVisual(sourceButton);
                if (dragVisual == null) return;

                // Create and add the adorner
                _dragAdorner = new DragDropAdorner(this, dragVisual);
                _adornerLayer.Add(_dragAdorner);

                // Make the original button semi-transparent during drag
                sourceButton.Opacity = 0.5;
            }
            catch
            {
                // Ignore errors during visual drag setup
            }
        }
        
        private UIElement? CreateDragVisual(Button sourceButton)
        {
            try
            {
                // Create a copy of the button for drag visual
                var dragButton = new Button
                {
                    Width = sourceButton.ActualWidth,
                    Height = sourceButton.ActualHeight,
                    Background = sourceButton.Background,
                    BorderThickness = sourceButton.BorderThickness,
                    CornerRadius = new CornerRadius(16),
                    Content = sourceButton.Content,
                    ContentTemplate = sourceButton.ContentTemplate,
                    Opacity = 0.8,
                    IsHitTestVisible = false
                };

                // Apply transform for slight scale and rotation (iOS style)
                var transform = new CompositeTransform
                {
                    ScaleX = 1.1,
                    ScaleY = 1.1,
                    Rotation = 2
                };
                dragButton.RenderTransform = transform;

                return dragButton;
            }
            catch
            {
                return null;
            }
        }
        
        private void EndVisualDrag()
        {
            try
            {
                // Remove the adorner
                if (_dragAdorner != null && _adornerLayer != null)
                {
                    _adornerLayer.Remove(_dragAdorner);
                }

                // Reset all button opacities
                ResetButtonOpacities();

                _dragAdorner = null;
                _adornerLayer = null;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        private void ResetButtonOpacities()
        {
            // Find all buttons in the grid and reset their opacity
            var buttons = FindVisualChildren<Button>(AllAppsGrid);
            foreach (var button in buttons)
            {
                button.Opacity = 1.0;
            }
        }
        
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    yield return result;
                
                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
        
        private void AppIcon_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("AppIcon") && sender is Button targetButton && targetButton.Tag is AppIcon targetApp)
            {
                var sourceApp = e.Data.GetData("AppIcon") as AppIcon;
                if (sourceApp != null && sourceApp != targetApp && _viewModel != null)
                {
                    // Create a new group with both apps
                    var group = _viewModel.CreateGroup($"{sourceApp.Name} & {targetApp.Name}");
                    _viewModel.AddAppToGroup(sourceApp, group);
                    _viewModel.AddAppToGroup(targetApp, group);
                }
            }
            
            // Remove drop visual feedback
            if (sender is Button button && button.Parent is Grid grid)
            {
                grid.Background = System.Windows.Media.Brushes.Transparent;
            }
        }
        
        private void AppIcon_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("AppIcon") && sender is Button button)
            {
                // Add visual feedback
                if (button.Parent is Grid grid)
                {
                    grid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x40, 0x00, 0x7A, 0xFF));
                }
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        private void AppIcon_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Button button && button.Parent is Grid grid)
            {
                grid.Background = System.Windows.Media.Brushes.Transparent;
            }
        }
        
        private void GroupIcon_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("AppIcon") && sender is Button groupButton && groupButton.Tag is AppGroup targetGroup)
            {
                var sourceApp = e.Data.GetData("AppIcon") as AppIcon;
                if (sourceApp != null && _viewModel != null)
                {
                    // Add app to existing group
                    _viewModel.AddAppToGroup(sourceApp, targetGroup);
                }
            }
            
            // Remove drop visual feedback
            if (sender is Button button && button.Parent is Grid grid)
            {
                grid.Background = System.Windows.Media.Brushes.Transparent;
            }
        }
        
        private void GroupIcon_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("AppIcon") && sender is Button button)
            {
                // Add visual feedback
                if (button.Parent is Grid grid)
                {
                    grid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x60, 0x00, 0x7A, 0xFF));
                }
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        
        private void GroupIcon_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Button button && button.Parent is Grid grid)
            {
                grid.Background = System.Windows.Media.Brushes.Transparent;
            }
        }
        
        // Group management
        private AppGroup? _groupBeingRenamed;
        
        // Swipe handling
        private double _manipulationStartX = 0;
        private bool _isSwipeInProgress = false;
        
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LaunchpadViewModel.TotalPages) || 
                e.PropertyName == nameof(LaunchpadViewModel.CurrentPage))
            {
                UpdatePageIndicators();
            }
        }
        
        private void SeroLaunchpad_ManipulationBoundaryFeedback(object? sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }
        
        private void SeroLaunchpad_ManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
        {
            if (!_isSwipeInProgress)
            {
                _manipulationStartX = e.CumulativeManipulation.Translation.X;
                _isSwipeInProgress = true;
            }
            
            // Visual feedback during swipe
            var translation = e.CumulativeManipulation.Translation.X;
            var progress = Math.Max(-1, Math.Min(1, translation / 200.0)); // Normalize to -1 to 1
            
            // Apply transform for swipe preview
            if (AllAppsGrid.RenderTransform is TranslateTransform transform)
            {
                transform.X = progress * 50; // Subtle movement preview
            }
            else
            {
                AllAppsGrid.RenderTransform = new TranslateTransform(progress * 50, 0);
            }
        }
        
        private void SeroLaunchpad_ManipulationCompleted(object? sender, ManipulationCompletedEventArgs e)
        {
            _isSwipeInProgress = false;
            
            // Reset transform
            AllAppsGrid.RenderTransform = new TranslateTransform(0, 0);
            
            var swipeDistance = e.TotalManipulation.Translation.X;
            var swipeThreshold = 100; // Minimum distance for page change
            
            if (_viewModel != null)
            {
                if (swipeDistance > swipeThreshold)
                {
                    // Swipe right - go to previous page
                    _viewModel.PreviousPage();
                }
                else if (swipeDistance < -swipeThreshold)
                {
                    // Swipe left - go to next page
                    _viewModel.NextPage();
                }
            }
        }
        
        private void UpdatePageIndicators()
        {
            if (_viewModel == null) return;
            
            PageIndicators.Children.Clear();
            
            for (int i = 0; i < _viewModel.TotalPages; i++)
            {
                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Margin = new Thickness(4),
                    Fill = i == _viewModel.CurrentPage ? 
                        System.Windows.Media.Brushes.White : 
                        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF)),
                    Cursor = Cursors.Hand
                };
                
                // Add click handler for direct page navigation
                var pageIndex = i;
                dot.MouseLeftButtonUp += (s, e) => _viewModel.GoToPage(pageIndex);
                
                PageIndicators.Children.Add(dot);
            }
        }
        
        private void GroupIcon_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.Tag is AppGroup group)
            {
                _groupBeingRenamed = group;
                if (button.ContextMenu != null)
                {
                    button.ContextMenu.IsOpen = true;
                }
            }
        }
        
        private void GroupIcon_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button && button.Tag is AppGroup group)
            {
                _groupBeingRenamed = group;
                RenameGroup_Click(sender, new RoutedEventArgs());
            }
        }
        
        private void RenameGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_groupBeingRenamed != null)
            {
                GroupRenameTextBox.Text = _groupBeingRenamed.Name;
                GroupRenameOverlay.Visibility = Visibility.Visible;
                GroupRenameTextBox.SelectAll();
                GroupRenameTextBox.Focus();
            }
        }
        
        private void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_groupBeingRenamed != null && _viewModel != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the group '{_groupBeingRenamed.Name}'?\nApps will be moved back to the main screen.", 
                                           "Delete Group", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);
                                           
                if (result == MessageBoxResult.Yes)
                {
                    // Move all apps back to ungrouped state
                    var apps = _groupBeingRenamed.Apps.ToList();
                    foreach (var app in apps)
                    {
                        _viewModel.RemoveAppFromGroup(app);
                    }
                }
                _groupBeingRenamed = null;
            }
        }
        
        private void ConfirmGroupRename_Click(object sender, RoutedEventArgs e)
        {
            var newName = GroupRenameTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(newName) && _groupBeingRenamed != null && _viewModel != null)
            {
                _viewModel.RenameGroup(_groupBeingRenamed, newName);
                GroupRenameOverlay.Visibility = Visibility.Collapsed;
                _groupBeingRenamed = null;
            }
        }
        
        private void CancelGroupRename_Click(object sender, RoutedEventArgs e)
        {
            GroupRenameOverlay.Visibility = Visibility.Collapsed;
            _groupBeingRenamed = null;
        }
        
        // Handle keyboard shortcuts
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Hide();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && !string.IsNullOrEmpty(SearchBox.Text))
            {
                // Launch first search result
                if (_viewModel?.FilteredApplications.FirstOrDefault() is AppIcon firstApp)
                {
                    firstApp.Launch();
                    Hide();
                }
                e.Handled = true;
            }
            
            base.OnKeyDown(e);
        }
    }
}