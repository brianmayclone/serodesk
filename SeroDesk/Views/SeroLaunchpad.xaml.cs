using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SeroDesk.Core;
using SeroDesk.Models;
using SeroDesk.ViewModels;

namespace SeroDesk.Views
{
    public partial class SeroLaunchpad : UserControl
    {
        private LaunchpadViewModel? _viewModel;
        private bool _isEditMode = false;
        
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
                    CreateIconViews();
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
            // If this was a tap on empty space and in edit mode, exit edit mode
            if (_isEditMode)
            {
                ExitEditMode();
            }
        }
        
        public void Initialize()
        {
            // Only create a new ViewModel if DataContext is not already set
            // (to allow MainWindow to share its ViewModel)
            if (DataContext == null)
            {
                System.Diagnostics.Debug.WriteLine("SeroLaunchpad: Creating NEW ViewModel (DataContext was null)");
                _viewModel = new LaunchpadViewModel();
                DataContext = _viewModel;
            }
            else if (DataContext is LaunchpadViewModel existingViewModel)
            {
                System.Diagnostics.Debug.WriteLine($"SeroLaunchpad: Using EXISTING ViewModel with {existingViewModel.AllApplications.Count} apps");
                _viewModel = existingViewModel;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SeroLaunchpad: DataContext is not LaunchpadViewModel: {DataContext?.GetType()}");
                _viewModel = new LaunchpadViewModel();
                DataContext = _viewModel;
            }
        }
        
        private bool _isLoading = false;
        
        public async void Show()
        {
            if (Visibility == Visibility.Visible)
                return; // Already visible, prevent multiple calls
                
            Visibility = Visibility.Visible;
            
            // Do NOT load apps in Show() - apps should be loaded by MainWindow
            // Show() should only display the LaunchPad, not load data
            System.Diagnostics.Debug.WriteLine($"SeroLaunchpad.Show(): ViewModel has {_viewModel?.AllApplications.Count ?? 0} apps, Loading: {_isLoading}");
            
            // Ensure pages are created after layout is updated
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_viewModel != null)
                {
                    // Force update of display items and pages
                    _viewModel.RefreshLayout();
                    CreateIconViews();
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            
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
            
            // Focus search box - delayed and forced
            _ = this.Dispatcher.BeginInvoke(new Action(() =>
            {
                SearchBox.Focus();
                Keyboard.Focus(SearchBox);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
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
                
                // For instant feedback, immediately update visibility while debounced search runs
                UpdateIconVisibility(SearchBox.Text);
            }
        }
        
        private void UpdateIconVisibility(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Restore all cached icons to their original positions
                foreach (var cachedIcon in _iconCache.Values)
                {
                    if (_originalPositions.TryGetValue(cachedIcon, out var originalPos))
                    {
                        Canvas.SetLeft(cachedIcon, originalPos.X);
                        Canvas.SetTop(cachedIcon, originalPos.Y);
                        cachedIcon.Visibility = Visibility.Visible;
                    }
                }
                return;
            }
            
            // Hide all icons first
            foreach (var cachedIcon in _iconCache.Values)
            {
                cachedIcon.Visibility = Visibility.Collapsed;
            }
            
            // Find matching icons and reposition them
            var searchLower = searchText.ToLowerInvariant();
            var matchingIcons = new List<SeroIconView>();
            
            foreach (var kvp in _iconCache)
            {
                var item = kvp.Key;
                var iconView = kvp.Value;
                
                bool matches = false;
                if (item is AppIcon app)
                {
                    matches = app.Name.ToLowerInvariant().Contains(searchLower);
                }
                else if (item is AppGroup group)
                {
                    matches = group.Name.ToLowerInvariant().Contains(searchLower) ||
                             group.Apps.Any(a => a.Name.ToLowerInvariant().Contains(searchLower));
                }
                
                if (matches)
                {
                    matchingIcons.Add(iconView);
                }
            }
            
            // Reposition matching icons in a grid starting from top-left
            RepositionSearchResults(matchingIcons);
        }
        
        private (double canvasWidth, double canvasHeight, int columnsPerPage, int rowsPerPage, double horizontalSpacing, double verticalSpacing, double iconWidth, double iconHeight) GetGridDimensions()
        {
            var canvasWidth = IconCanvas?.ActualWidth ?? 1600;
            var canvasHeight = IconCanvas?.ActualHeight ?? 600;
            
            // Ensure we have sane values BEFORE any calculations
            if (canvasWidth <= 0 || double.IsNaN(canvasWidth)) canvasWidth = 1600;
            if (canvasHeight <= 0 || double.IsNaN(canvasHeight)) canvasHeight = 600;
            
            var iconWidth = 90.0;
            var iconHeight = 120.0;
            var columnsPerPage = 7; // Fixed grid layout
            var rowsPerPage = 5;
            
            // Calculate spacing with validated dimensions
            var horizontalSpacing = canvasWidth / columnsPerPage;
            var verticalSpacing = canvasHeight / rowsPerPage;
            
            return (canvasWidth, canvasHeight, columnsPerPage, rowsPerPage, horizontalSpacing, verticalSpacing, iconWidth, iconHeight);
        }
        
        private void RepositionSearchResults(List<SeroIconView> matchingIcons)
        {
            if (IconCanvas == null || matchingIcons.Count == 0) return;
            
            // Use same grid dimensions as CreateIconViews
            var (canvasWidth, canvasHeight, columnsPerPage, rowsPerPage, horizontalSpacing, verticalSpacing, iconWidth, iconHeight) = GetGridDimensions();
            
            // Position icons in grid starting from top-left
            for (int i = 0; i < matchingIcons.Count; i++)
            {
                var iconView = matchingIcons[i];
                var row = i / columnsPerPage;
                var col = i % columnsPerPage;
                
                var x = col * horizontalSpacing + (horizontalSpacing - iconWidth) / 2;
                var y = row * verticalSpacing + (verticalSpacing - iconHeight) / 2;
                
                Canvas.SetLeft(iconView, x);
                Canvas.SetTop(iconView, y);
                iconView.GridPosition = new Point(col, row);
                iconView.Visibility = Visibility.Visible;
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
            // Search through all icon views for the group
            foreach (SeroIconView iconView in IconCanvas.Children.OfType<SeroIconView>())
            {
                if (iconView.IsGroup && iconView.AppGroup?.Id == group.Id)
                {
                    // Return the first button found in the icon view (there should be one in the template)
                    return FindVisualChild<Button>(iconView);
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
        
        // Removed old drag and drop variables - using Canvas system now
        
        // Old mouse handling removed - using Canvas-based system now
        
        // Canvas-based Icon System Event Handlers
        private SeroIconView? _draggedIcon;
        private Point _dragOffset;
        private System.Threading.Timer? _arrangementTimer;
        private Point _lastTargetGridPos = new Point(-1, -1);
        
        private void OnIconClicked(object? sender, IconClickEventArgs e)
        {
            if (e.IsGroup && e.AppGroup != null)
            {
                // Show group expanded view
                ShowGroupExpandedView(e.AppGroup);
            }
            else if (e.AppIcon != null)
            {
                // Launch app
                e.AppIcon.Launch();
                PlayLaunchAnimation(e.IconView);
            }
        }
        
        private void OnIconDragStarted(object? sender, IconDragEventArgs e)
        {
            _draggedIcon = e.IconView;
            
            if (_draggedIcon != null)
            {
                _dragOffset = new Point(
                    e.Position.X - Canvas.GetLeft(_draggedIcon),
                    e.Position.Y - Canvas.GetTop(_draggedIcon)
                );
                
                // Start wiggle animation on all other icons
                StartWiggleAnimationForAll(except: _draggedIcon);
                
                System.Diagnostics.Debug.WriteLine($"Drag started for {(_draggedIcon.AppIcon?.Name ?? _draggedIcon.AppGroup?.Name)}");
            }
        }
        
        private void OnIconDragMoved(object? sender, IconDragEventArgs e)
        {
            if (_draggedIcon == null) return;
            
            // Update dragged icon position
            var newX = e.Position.X - _dragOffset.X;
            var newY = e.Position.Y - _dragOffset.Y;
            
            Canvas.SetLeft(_draggedIcon, newX);
            Canvas.SetTop(_draggedIcon, newY);
            
            // Calculate target grid position
            var targetGridPos = CalculateGridPosition(e.Position);
            
            // Only trigger rearrangement if target position changed
            if (targetGridPos != _lastTargetGridPos)
            {
                _lastTargetGridPos = targetGridPos;
                
                // Cancel previous timer
                _arrangementTimer?.Dispose();
                
                // Start new timer for delayed rearrangement (400ms)
                _arrangementTimer = new System.Threading.Timer(
                    callback: (state) => {
                        Application.Current.Dispatcher.Invoke(() => {
                            if (targetGridPos == _lastTargetGridPos && _draggedIcon != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Delayed rearrangement to ({targetGridPos.X},{targetGridPos.Y})");
                                AnimateIconsToMakeSpace(targetGridPos, _draggedIcon);
                            }
                        });
                    },
                    state: null,
                    dueTime: 400,
                    period: System.Threading.Timeout.Infinite
                );
            }
        }
        
        private void OnIconDragCompleted(object? sender, IconDragEventArgs e)
        {
            if (_draggedIcon == null) return;
            
            // Cancel any pending arrangement timer
            _arrangementTimer?.Dispose();
            _arrangementTimer = null;
            _lastTargetGridPos = new Point(-1, -1);
            
            // Check if dropped on another icon (for group creation)
            var targetIcon = GetIconAtPosition(e.Position, _draggedIcon);
            System.Diagnostics.Debug.WriteLine($"Drop check: targetIcon={targetIcon?.AppIcon?.Name}, isGroup={targetIcon?.IsGroup}");
            
            if (targetIcon != null && !targetIcon.IsGroup && _draggedIcon.AppIcon != null && targetIcon.AppIcon != null)
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to create group from {_draggedIcon.AppIcon.Name} + {targetIcon.AppIcon.Name}");
                // Create group by combining two icons
                CreateGroupFromIcons(_draggedIcon.AppIcon, targetIcon.AppIcon);
                System.Diagnostics.Debug.WriteLine($"Group creation completed");
            }
            else
            {
                // Normal drop - finalize position
                var targetGridPos = CalculateGridPosition(e.Position);
                System.Diagnostics.Debug.WriteLine($"Normal drop to grid position ({targetGridPos.X},{targetGridPos.Y})");
                FinalizeIconPositions(targetGridPos, _draggedIcon);
            }
            
            // Stop wiggle animation
            StopWiggleAnimationForAll();
            
            _draggedIcon = null;
            
            // Save the new layout after drag
            _viewModel?.SaveCurrentLayout();
            
            System.Diagnostics.Debug.WriteLine("Drag completed");
        }
        
        private SeroIconView? GetIconAtPosition(Point position, SeroIconView? excludeIcon = null)
        {
            System.Diagnostics.Debug.WriteLine($"GetIconAtPosition: checking position ({position.X},{position.Y})");
            
            foreach (SeroIconView icon in IconCanvas.Children.OfType<SeroIconView>())
            {
                if (icon == excludeIcon) continue;
                
                var left = Canvas.GetLeft(icon);
                var top = Canvas.GetTop(icon);
                
                // Use default values if Canvas position is NaN
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;
                
                var iconBounds = new Rect(left, top, 90, 120); // Use updated icon size
                
                System.Diagnostics.Debug.WriteLine($"Icon {icon.AppIcon?.Name}: bounds=({left},{top},90,120)");
                
                if (iconBounds.Contains(position))
                {
                    System.Diagnostics.Debug.WriteLine($"Found icon at position: {icon.AppIcon?.Name}");
                    return icon;
                }
            }
            
            System.Diagnostics.Debug.WriteLine("No icon found at position");
            return null;
        }
        
        private void CreateGroupFromIcons(AppIcon app1, AppIcon app2)
        {
            if (_viewModel == null) 
            {
                System.Diagnostics.Debug.WriteLine("CreateGroupFromIcons: _viewModel is null");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"CreateGroupFromIcons: Creating group with {app1.Name} and {app2.Name}");
            
            // Create new group
            var groupName = $"{app1.Name} & {app2.Name}";
            var group = _viewModel.CreateGroup(groupName);
            
            if (group != null)
            {
                System.Diagnostics.Debug.WriteLine($"Group created: {group.Name}");
                
                // Add both apps to group
                _viewModel.AddAppToGroup(app1, group);
                _viewModel.AddAppToGroup(app2, group);
                
                System.Diagnostics.Debug.WriteLine($"Apps added to group. Group now has {group.Apps.Count} apps");
                
                // Refresh the icon display
                CreateIconViews();
                System.Diagnostics.Debug.WriteLine("Icon views refreshed");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to create group");
            }
        }
        
        private Point CalculateGridPosition(Point screenPosition)
        {
            var canvasWidth = IconCanvas.ActualWidth;
            var canvasHeight = IconCanvas.ActualHeight;
            
            // Ensure we have sane values BEFORE any calculations
            if (canvasWidth <= 0 || double.IsNaN(canvasWidth)) canvasWidth = 1600;
            if (canvasHeight <= 0 || double.IsNaN(canvasHeight)) canvasHeight = 600;
            
            var columnsPerPage = 7; // Reduced because icons are larger
            var rowsPerPage = 5; // Reduced because icons are larger
            
            var horizontalSpacing = canvasWidth / columnsPerPage;
            var verticalSpacing = canvasHeight / rowsPerPage;
            
            // Calculate which page we're on (support for multiple pages)
            var currentPageOffset = _viewModel?.CurrentPage ?? 0;
            var adjustedX = screenPosition.X + (currentPageOffset * canvasWidth);
            
            var globalCol = (int)(adjustedX / horizontalSpacing);
            var row = Math.Max(0, Math.Min(rowsPerPage - 1, (int)(screenPosition.Y / verticalSpacing)));
            
            // Keep within current page bounds for now (multi-page drag later)
            var col = Math.Max(0, Math.Min(columnsPerPage - 1, globalCol % columnsPerPage));
            
            System.Diagnostics.Debug.WriteLine($"CalculateGridPosition: screen=({screenPosition.X},{screenPosition.Y}) -> grid=({col},{row})");
            return new Point(col, row);
        }
        
        private void AnimateIconsToMakeSpace(Point targetGridPos, SeroIconView draggedIcon)
        {
            System.Diagnostics.Debug.WriteLine($"AnimateIconsToMakeSpace: target=({targetGridPos.X},{targetGridPos.Y})");
            
            // Get same dimensions as used in CreateIconViews
            var canvasWidth = IconCanvas.ActualWidth;
            var canvasHeight = IconCanvas.ActualHeight;
            
            // Ensure we have sane values BEFORE any calculations
            if (canvasWidth <= 0 || double.IsNaN(canvasWidth)) canvasWidth = 1600;
            if (canvasHeight <= 0 || double.IsNaN(canvasHeight)) canvasHeight = 600;
            
            var iconWidth = 90.0;
            var iconHeight = 120.0;
            var columnsPerPage = 7; // Reduced because icons are larger
            var rowsPerPage = 5; // Reduced because icons are larger
            var iconsPerPage = columnsPerPage * rowsPerPage;
            
            var horizontalSpacing = canvasWidth / columnsPerPage;
            var verticalSpacing = canvasHeight / rowsPerPage;
            
            // Calculate target index in global grid
            var targetIndex = (int)(targetGridPos.Y * columnsPerPage + targetGridPos.X);
            
            // Get all icons except the dragged one, create a complete layout
            var allIcons = IconCanvas.Children.OfType<SeroIconView>()
                .Where(icon => icon != draggedIcon)
                .ToList();
            
            // Create array representing the final layout with target position reserved
            var finalLayout = new SeroIconView?[iconsPerPage];
            
            // Place existing icons, skipping the target position
            int currentLayoutIndex = 0;
            foreach (var icon in allIcons)
            {
                // Skip target position
                if (currentLayoutIndex == targetIndex)
                    currentLayoutIndex++;
                
                // Place icon at current position
                if (currentLayoutIndex < iconsPerPage)
                {
                    finalLayout[currentLayoutIndex] = icon;
                    
                    // Calculate new grid position
                    var newRow = currentLayoutIndex / columnsPerPage;
                    var newCol = currentLayoutIndex % columnsPerPage;
                    var newX = newCol * horizontalSpacing + (horizontalSpacing - iconWidth) / 2;
                    var newY = newRow * verticalSpacing + (verticalSpacing - iconHeight) / 2;
                    
                    // Only animate if position changed
                    var oldIndex = (int)(icon.GridPosition.Y * columnsPerPage + icon.GridPosition.X);
                    if (oldIndex != currentLayoutIndex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Moving {icon.AppIcon?.Name} from {oldIndex} to {currentLayoutIndex} = ({newX},{newY})");
                        icon.AnimateToPosition(new Point(newX, newY), TimeSpan.FromMilliseconds(200));
                        icon.GridPosition = new Point(newCol, newRow);
                    }
                    
                    currentLayoutIndex++;
                }
            }
        }
        
        private void FinalizeIconPositions(Point targetGridPos, SeroIconView draggedIcon)
        {
            System.Diagnostics.Debug.WriteLine($"FinalizeIconPositions: target=({targetGridPos.X},{targetGridPos.Y})");
            
            // Get same dimensions as used in CreateIconViews
            var canvasWidth = IconCanvas.ActualWidth;
            var canvasHeight = IconCanvas.ActualHeight;
            
            // Ensure we have sane values BEFORE any calculations
            if (canvasWidth <= 0 || double.IsNaN(canvasWidth)) canvasWidth = 1600;
            if (canvasHeight <= 0 || double.IsNaN(canvasHeight)) canvasHeight = 600;
            
            var iconWidth = 90.0;
            var iconHeight = 120.0;
            var columnsPerPage = 7; // Reduced because icons are larger
            var rowsPerPage = 5; // Reduced because icons are larger
            
            var horizontalSpacing = canvasWidth / columnsPerPage;
            var verticalSpacing = canvasHeight / rowsPerPage;
            
            var finalX = targetGridPos.X * horizontalSpacing + (horizontalSpacing - iconWidth) / 2;
            var finalY = targetGridPos.Y * verticalSpacing + (verticalSpacing - iconHeight) / 2;
            
            System.Diagnostics.Debug.WriteLine($"Snap to grid: ({finalX},{finalY}) with spacing {horizontalSpacing}x{verticalSpacing}");
            
            draggedIcon.AnimateToPosition(new Point(finalX, finalY), TimeSpan.FromMilliseconds(200));
            draggedIcon.GridPosition = targetGridPos;
        }
        
        private void StartWiggleAnimationForAll(SeroIconView? except = null)
        {
            foreach (SeroIconView icon in IconCanvas.Children.OfType<SeroIconView>())
            {
                if (icon != except)
                {
                    icon.StartWiggleAnimation();
                }
            }
        }
        
        private void StopWiggleAnimationForAll()
        {
            foreach (SeroIconView icon in IconCanvas.Children.OfType<SeroIconView>())
            {
                icon.StopWiggleAnimation();
            }
        }
        
        private void PlayLaunchAnimation(SeroIconView? iconView)
        {
            if (iconView == null) return;
            
            // Simple scale-up animation for launch feedback
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            iconView.RenderTransform = scaleTransform;
            iconView.RenderTransformOrigin = new Point(0.5, 0.5);
            
            var scaleAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(150),
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }
        
        // Canvas Manipulation Events (for page swiping and touch forwarding)
        private void IconCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = IconCanvas;
            e.Mode = ManipulationModes.All;
            
            // Note: ManipulationOrigin is not available in ManipulationStartingEventArgs
            // Icon-level manipulation handling is done in the individual SeroIconView components
        }
        
        private void IconCanvas_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            // Only handle page swiping if not dragging an icon and not in edit mode
            if (_draggedIcon == null && CanNavigatePages)
            {
                // Handle horizontal swiping for page navigation
                var translation = e.CumulativeManipulation.Translation.X;
                var threshold = 150; // Increase threshold to avoid accidental page changes
                
                // Visual feedback during swipe
                UpdateSwipeVisualFeedback(translation);
                
                // Check for page change threshold
                if (Math.Abs(translation) > threshold)
                {
                    if (translation > 0 && (_viewModel?.CurrentPage ?? 0) > 0)
                    {
                        // Swipe right - previous page
                        _viewModel?.PreviousPage();
                        e.Complete(); // Complete the manipulation
                    }
                    else if (translation < 0 && (_viewModel?.CurrentPage ?? 0) < (_viewModel?.TotalPages ?? 1) - 1)
                    {
                        // Swipe left - next page
                        _viewModel?.NextPage();
                        e.Complete(); // Complete the manipulation
                    }
                }
            }
        }
        
        private void IconCanvas_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            // Reset any visual feedback
            ResetSwipeVisualFeedback();
        }
        
        private void UpdateSwipeVisualFeedback(double translation)
        {
            // Visual feedback disabled to prevent overlapping pages bug
            // Instead we'll use opacity changes for subtle feedback
            if (IconCanvas != null)
            {
                var swipeProgress = Math.Abs(translation) / 150.0; // Normalize to 0-1
                var opacity = Math.Max(0.7, 1.0 - (swipeProgress * 0.3));
                IconCanvas.Opacity = opacity;
            }
        }
        
        private void ResetSwipeVisualFeedback()
        {
            // Reset opacity back to normal
            if (IconCanvas != null)
            {
                var resetAnimation = new DoubleAnimation
                {
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                IconCanvas.BeginAnimation(UIElement.OpacityProperty, resetAnimation);
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
        
        // Removed complex drag/drop handlers - will add simple ones later
        
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
                UpdatePagesLayout();
            }
            else if (e.PropertyName == nameof(LaunchpadViewModel.AllApplications) || 
                     e.PropertyName == nameof(LaunchpadViewModel.AppGroups))
            {
                System.Diagnostics.Debug.WriteLine($"PropertyChanged triggered for {e.PropertyName}, creating icon views");
                CreateIconViews();
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
            
            // Simple visual feedback without transforms
            var translation = e.CumulativeManipulation.Translation.X;
            UpdateSwipeVisualFeedback(translation);
        }
        
        private void SeroLaunchpad_ManipulationCompleted(object? sender, ManipulationCompletedEventArgs e)
        {
            _isSwipeInProgress = false;
            
            // Only allow page navigation if not in edit mode
            if (CanNavigatePages)
            {
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
                    else
                    {
                        // Not enough swipe - return to current page
                        UpdatePagesLayout();
                    }
                }
            }
            else
            {
                // In edit mode, just reset the layout
                UpdatePagesLayout();
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
        
        private Dictionary<object, SeroIconView> _iconCache = new Dictionary<object, SeroIconView>();
        private Dictionary<SeroIconView, Point> _originalPositions = new Dictionary<SeroIconView, Point>();
        
        private void CreateIconViews()
        {
            if (_viewModel == null || IconCanvas == null) return;
            
            System.Diagnostics.Debug.WriteLine($"CreateIconViews: DisplayItems={_viewModel.DisplayItems.Count}, SearchText='{_viewModel.SearchText}' (Using cache: {_iconCache.Count} items)");
            
            // Hide all icons first
            foreach (var cachedIcon in _iconCache.Values)
            {
                cachedIcon.Visibility = Visibility.Collapsed;
            }
            
            // Only create views if we have items to display
            if (_viewModel.DisplayItems.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No items to display, all icons hidden");
                return;
            }
            
            // Don't clear canvas - reuse cached icons
            
            // Use shared grid dimensions calculation
            var (canvasWidth, canvasHeight, columnsPerPage, rowsPerPage, horizontalSpacing, verticalSpacing, iconWidth, iconHeight) = GetGridDimensions();
            var iconsPerPage = columnsPerPage * rowsPerPage;
            
            System.Diagnostics.Debug.WriteLine($"Canvas: {canvasWidth}x{canvasHeight}, Spacing: {horizontalSpacing}x{verticalSpacing}");
            
            // Create icon views for display items
            int iconIndex = 0;
            
            // Add items from DisplayItems (handles both search and normal view)
            foreach (var item in _viewModel.DisplayItems)
            {
                SeroIconView? iconView = null;
                
                // Try to get from cache first
                if (_iconCache.TryGetValue(item, out iconView))
                {
                    System.Diagnostics.Debug.WriteLine($"Reusing cached SeroIconView for {(item is AppIcon app ? app.Name : ((AppGroup)item).Name)}");
                }
                else
                {
                    // Create new icon and cache it
                    if (item is AppIcon app)
                    {
                        iconView = new SeroIconView
                        {
                            AppIcon = app,
                            IsGroup = false
                        };
                        System.Diagnostics.Debug.WriteLine($"Creating NEW SeroIconView for app {app.Name}, IconImage={app.IconImage != null}");
                    }
                    else if (item is AppGroup group)
                    {
                        iconView = new SeroIconView
                        {
                            AppGroup = group,
                            IsGroup = true
                        };
                        System.Diagnostics.Debug.WriteLine($"Creating NEW SeroIconView for group {group.Name}");
                    }
                    
                    if (iconView != null)
                    {
                        _iconCache[item] = iconView;
                        IconCanvas.Children.Add(iconView);
                    }
                }
                
                if (iconView != null)
                {
                    // Calculate position - only show icons for current page
                    var pageIndex = iconIndex / iconsPerPage;
                    var localIndex = iconIndex % iconsPerPage;
                    var row = localIndex / columnsPerPage;
                    var col = localIndex % columnsPerPage;
                    
                    // Only position icons that belong to the current page
                    if (pageIndex == (_viewModel?.CurrentPage ?? 0))
                    {
                        var x = col * horizontalSpacing + (horizontalSpacing - iconWidth) / 2;
                        var y = row * verticalSpacing + (verticalSpacing - iconHeight) / 2;
                        
                        Canvas.SetLeft(iconView, x);
                        Canvas.SetTop(iconView, y);
                        iconView.GridPosition = new Point(col, row);
                        iconView.Visibility = Visibility.Visible;
                        
                        // Store original position for search reset
                        _originalPositions[iconView] = new Point(x, y);
                    }
                    else
                    {
                        // Hide icons that don't belong to current page
                        iconView.Visibility = Visibility.Collapsed;
                    }
                    
                    // Hook up event handlers (only if not already cached)
                    if (!_iconCache.ContainsKey(item) || _iconCache[item] != iconView)
                    {
                        iconView.IconClicked += OnIconClicked;
                        iconView.DragStarted += OnIconDragStarted;
                        iconView.DragMoved += OnIconDragMoved;
                        iconView.DragCompleted += OnIconDragCompleted;
                        iconView.PinToDockRequested += OnPinToDockRequested;
                        iconView.UnpinFromDockRequested += OnUnpinFromDockRequested;
                    }
                    
                    // Don't add to canvas again if already cached
                    iconIndex++;
                    
                    var name = (item is AppIcon a) ? a.Name : ((AppGroup)item).Name;
                    var currentX = Canvas.GetLeft(iconView);
                    var currentY = Canvas.GetTop(iconView);
                    System.Diagnostics.Debug.WriteLine($"Added {name} at position ({currentX}, {currentY})");
                }
            }
            
            UpdatePageIndicators();
        }
        
        private void UpdatePagesLayout()
        {
            if (_viewModel == null || IconCanvas == null) return;
            
            // Instead of using transforms, we'll recreate the icon views for the current page
            CreateIconViews();
        }
        
        // Event handlers are now directly in XAML templates
        
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
        
        // Pin/Unpin Event Handlers
        private void OnPinToDockRequested(object? sender, IconContextMenuEventArgs e)
        {
            if (e.AppIcon != null)
            {
                try
                {
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    if (mainWindow?.DockViewModel != null)
                    {
                        mainWindow.DockViewModel.AddToDock(e.AppIcon);
                        System.Diagnostics.Debug.WriteLine($"Pinned {e.AppIcon.Name} to dock");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error pinning to dock: {ex.Message}");
                }
            }
        }
        
        private void OnUnpinFromDockRequested(object? sender, IconContextMenuEventArgs e)
        {
            if (e.AppIcon != null)
            {
                try
                {
                    // Find the corresponding window in the dock and remove it
                    var mainWindow = Window.GetWindow(this) as MainWindow;
                    if (mainWindow?.DockViewModel != null)
                    {
                        // This would need to be implemented differently since we don't have direct WindowInfo
                        // For now, just log the attempt
                        System.Diagnostics.Debug.WriteLine($"Unpinning {e.AppIcon.Name} from dock (not fully implemented)");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error unpinning from dock: {ex.Message}");
                }
            }
        }
        
        // Edit Mode Methods
        public void EnterEditMode()
        {
            if (_isEditMode) return;
            
            _isEditMode = true;
            System.Diagnostics.Debug.WriteLine("Entering edit mode");
            
            // Update all icon views to show edit mode
            foreach (SeroIconView icon in IconCanvas.Children.OfType<SeroIconView>())
            {
                icon.SetEditMode(true);
                icon.StartWiggleAnimation();
            }
            
            // Update search bar visibility (hide during edit mode)
            SearchBar.Visibility = Visibility.Collapsed;
            
            // Provide haptic feedback if available
            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { }
        }
        
        private void ExitEditMode()
        {
            if (!_isEditMode) return;
            
            _isEditMode = false;
            System.Diagnostics.Debug.WriteLine("Exiting edit mode");
            
            // Update all icon views to exit edit mode
            foreach (SeroIconView icon in IconCanvas.Children.OfType<SeroIconView>())
            {
                icon.SetEditMode(false);
                icon.StopWiggleAnimation();
            }
            
            // Show search bar again
            SearchBar.Visibility = Visibility.Visible;
            
            // Save layout after edit mode
            _viewModel?.SaveCurrentLayout();
        }
        
        public bool IsEditMode => _isEditMode;
        
        // Override page navigation to prevent during edit mode
        private bool CanNavigatePages => !_isEditMode;
    }
}