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
        
        public void Show()
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
                // Remove instant visual feedback - let the ViewModel handle everything
                // to avoid conflicts between cached icons and proper search results
            }
        }
        
        // Removed UpdateIconVisibility method - now handled entirely by ViewModel
        // to avoid conflicts between cached icons and proper search results
        
        private (double canvasWidth, double canvasHeight, int columnsPerPage, int rowsPerPage, double horizontalSpacing, double verticalSpacing, double iconWidth, double iconHeight) GetGridDimensions()
        {
            var canvasWidth = IconCanvas?.ActualWidth ?? 1600;
            var canvasHeight = IconCanvas?.ActualHeight ?? 600;

            if (canvasWidth <= 0 || double.IsNaN(canvasWidth)) canvasWidth = 1600;
            if (canvasHeight <= 0 || double.IsNaN(canvasHeight)) canvasHeight = 600;

            var iconWidth = Constants.UIConstants.IconCellWidth;
            var iconHeight = Constants.UIConstants.IconCellHeight;

            // Dynamic grid calculation based on screen size
            var (columnsPerPage, rowsPerPage, _) = Constants.UIConstants.CalculateGrid(canvasWidth, canvasHeight);

            var horizontalSpacing = canvasWidth / columnsPerPage;
            var verticalSpacing = canvasHeight / rowsPerPage;

            return (canvasWidth, canvasHeight, columnsPerPage, rowsPerPage, horizontalSpacing, verticalSpacing, iconWidth, iconHeight);
        }
        
        // Removed RepositionSearchResults method - now handled by CreateIconViews
        
        private void PlayLaunchAnimation(Button button)
        {
            // Ensure ScaleTransform exists on button
            var scaleTransform = button.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(1, 1);
                button.RenderTransform = scaleTransform;
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            // Scale up
            var scaleUp = new DoubleAnimation
            {
                To = 1.4, Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            // Fade out and restore
            var fadeOut = new DoubleAnimation
            {
                To = 0.3, Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = true,
                BeginTime = TimeSpan.FromMilliseconds(50)
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);
            button.BeginAnimation(OpacityProperty, fadeOut);
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
        
        private void GroupExpandedOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Only close if the click was directly on the overlay background, not on the content
            if (e.OriginalSource == GroupExpandedOverlay)
            {
                CloseGroupExpanded_Click(sender, e);
                e.Handled = true;
            }
        }

        private void CloseGroupExpanded_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("CloseGroupExpanded_Click called - closing group overlay");
            
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
                System.Diagnostics.Debug.WriteLine("Group overlay fade out completed - hiding overlay");
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
        
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
        }

        private void SuggestionApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is AppIcon app)
            {
                app.Launch();
            }
        }

        /// <summary>
        /// Updates the suggestions bar with frequently used apps.
        /// </summary>
        private void UpdateSuggestionsBar()
        {
            if (_viewModel == null) return;

            var suggestions = _viewModel.FrequentlyUsedApps;
            if (suggestions.Count > 0)
            {
                SuggestionsPanel.ItemsSource = suggestions;
                SuggestionsBar.Visibility = Visibility.Visible;
            }
            else
            {
                SuggestionsBar.Visibility = Visibility.Collapsed;
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
            System.Diagnostics.Debug.WriteLine($"OnIconClicked received in SeroLaunchpad - IsGroup: {e.IsGroup}, AppName: {e.AppIcon?.Name}, GroupName: {e.AppGroup?.Name}");
            
            if (e.IsGroup && e.AppGroup != null)
            {
                System.Diagnostics.Debug.WriteLine($"Opening group: {e.AppGroup.Name}");
                // Show group expanded view
                ShowGroupExpandedView(e.AppGroup);
            }
            else if (e.AppIcon != null)
            {
                System.Diagnostics.Debug.WriteLine($"Launching app: {e.AppIcon.Name}");
                // Launch app
                e.AppIcon.Launch();
                PlayLaunchAnimation(e.IconView);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("OnIconClicked - No valid app or group found");
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
        
        // Edge-drag page navigation
        private System.Threading.Timer? _edgeDragTimer;
        private bool _isEdgeDragPending = false;
        private const double EDGE_DRAG_ZONE = 80; // wider zone for easier touch targeting

        private void OnIconDragMoved(object? sender, IconDragEventArgs e)
        {
            if (_draggedIcon == null) return;

            var newX = e.Position.X - _dragOffset.X;
            var newY = e.Position.Y - _dragOffset.Y;
            Canvas.SetLeft(_draggedIcon, newX);
            Canvas.SetTop(_draggedIcon, newY);

            var canvasWidth = IconCanvas.ActualWidth;
            if (canvasWidth <= 0 || double.IsNaN(canvasWidth)) canvasWidth = 1600;

            // Edge detection for page navigation
            bool nearRightEdge = e.Position.X > canvasWidth - EDGE_DRAG_ZONE;
            bool nearLeftEdge = e.Position.X < EDGE_DRAG_ZONE;

            if (nearRightEdge && !_isEdgeDragPending)
            {
                StartEdgeDragTimer(isRightEdge: true);
            }
            else if (nearLeftEdge && !_isEdgeDragPending && (_viewModel?.CurrentPage ?? 0) > 0)
            {
                StartEdgeDragTimer(isRightEdge: false);
            }
            else if (!nearRightEdge && !nearLeftEdge)
            {
                CancelEdgeDragTimer();
            }

            // Calculate target grid position for rearrangement
            var targetGridPos = CalculateGridPosition(e.Position);
            if (targetGridPos != _lastTargetGridPos)
            {
                _lastTargetGridPos = targetGridPos;
                _arrangementTimer?.Dispose();
                _arrangementTimer = new System.Threading.Timer(
                    callback: (state) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (targetGridPos == _lastTargetGridPos && _draggedIcon != null)
                                AnimateIconsToMakeSpace(targetGridPos, _draggedIcon);
                        });
                    },
                    state: null, dueTime: 400,
                    period: System.Threading.Timeout.Infinite);
            }
        }

        private void StartEdgeDragTimer(bool isRightEdge)
        {
            _isEdgeDragPending = true;
            _edgeDragTimer?.Dispose();
            _edgeDragTimer = new System.Threading.Timer(
                callback: (state) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_draggedIcon == null || _viewModel == null)
                        {
                            _isEdgeDragPending = false;
                            return;
                        }

                        // Save reference to dragged icon before page switch
                        var savedIcon = _draggedIcon;

                        if (isRightEdge)
                        {
                            // On last page → create a new one
                            if (_viewModel.CurrentPage >= _viewModel.TotalPages - 1)
                                _viewModel.EnsureExtraPage();
                            else
                                _viewModel.NextPage();
                        }
                        else
                        {
                            _viewModel.PreviousPage();
                        }

                        // Rebuild icons for new page but keep dragged icon visible
                        CreateIconViews();

                        // Ensure dragged icon stays on canvas and visible
                        if (!IconCanvas.Children.Contains(savedIcon))
                            IconCanvas.Children.Add(savedIcon);
                        savedIcon.Visibility = Visibility.Visible;
                        Panel.SetZIndex(savedIcon, 1000);

                        // Center the dragged icon on the new page
                        var centerX = IconCanvas.ActualWidth / 2 - 45;
                        Canvas.SetLeft(savedIcon, centerX);

                        _isEdgeDragPending = false;
                    });
                },
                state: null, dueTime: 500,
                period: System.Threading.Timeout.Infinite);
        }

        private void CancelEdgeDragTimer()
        {
            _isEdgeDragPending = false;
            _edgeDragTimer?.Dispose();
            _edgeDragTimer = null;
        }
        
        private void OnIconDragCompleted(object? sender, IconDragEventArgs e)
        {
            if (_draggedIcon == null) return;

            // Cancel any pending timers
            _arrangementTimer?.Dispose();
            _arrangementTimer = null;
            _edgeDragTimer?.Dispose();
            _edgeDragTimer = null;
            _isEdgeDragPending = false;
            _lastTargetGridPos = new Point(-1, -1);

            // Check if dropped on another icon
            var targetIcon = GetIconAtPosition(e.Position, _draggedIcon);

            if (targetIcon != null && targetIcon.IsGroup && _draggedIcon.AppIcon != null && targetIcon.AppGroup != null)
            {
                // Dropped on an EXISTING GROUP -> add app to that group
                System.Diagnostics.Debug.WriteLine($"Adding {_draggedIcon.AppIcon.Name} to existing group {targetIcon.AppGroup.Name}");
                _viewModel?.AddAppToGroup(_draggedIcon.AppIcon, targetIcon.AppGroup);
                CreateIconViews();
            }
            else if (targetIcon != null && !targetIcon.IsGroup && _draggedIcon.AppIcon != null && targetIcon.AppIcon != null)
            {
                // Dropped on another APP -> create new group from both
                System.Diagnostics.Debug.WriteLine($"Creating group from {_draggedIcon.AppIcon.Name} + {targetIcon.AppIcon.Name}");
                CreateGroupFromIcons(_draggedIcon.AppIcon, targetIcon.AppIcon);
            }
            else
            {
                // Normal drop - finalize position and sync with ViewModel
                var targetGridPos = CalculateGridPosition(e.Position);
                FinalizeIconPositions(targetGridPos, _draggedIcon);
                SyncIconOrderToViewModel();
            }

            // Stop wiggle animation
            StopWiggleAnimationForAll();

            _draggedIcon = null;

            // Save the new layout after drag
            _viewModel?.SaveCurrentLayout();
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
            var (canvasWidth, _, columnsPerPage, rowsPerPage, horizontalSpacing, verticalSpacing, _, _) = GetGridDimensions();

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
            var (_, _, columnsPerPage, rowsPerPage, horizontalSpacing, verticalSpacing, iconWidth, iconHeight) = GetGridDimensions();
            var iconsPerPage = columnsPerPage * rowsPerPage;
            
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
            var (_, _, columnsPerPage, _, horizontalSpacing, verticalSpacing, iconWidth, iconHeight) = GetGridDimensions();
            
            var finalX = targetGridPos.X * horizontalSpacing + (horizontalSpacing - iconWidth) / 2;
            var finalY = targetGridPos.Y * verticalSpacing + (verticalSpacing - iconHeight) / 2;
            
            System.Diagnostics.Debug.WriteLine($"Snap to grid: ({finalX},{finalY}) with spacing {horizontalSpacing}x{verticalSpacing}");
            
            draggedIcon.AnimateToPosition(new Point(finalX, finalY), TimeSpan.FromMilliseconds(200));
            draggedIcon.GridPosition = targetGridPos;
        }
        
        /// <summary>
        /// After a drag-and-drop reorder, sync the visual icon order back into the ViewModel
        /// so the layout can be correctly persisted.
        /// </summary>
        private void SyncIconOrderToViewModel()
        {
            if (_viewModel == null) return;

            var (_, _, columnsPerPage, rowsPerPage, _, _, _, _) = GetGridDimensions();

            // Collect all visible icons with their grid positions
            var iconPositions = IconCanvas.Children.OfType<SeroIconView>()
                .Where(iv => iv.Visibility == Visibility.Visible)
                .Select(iv =>
                {
                    var gridPos = iv.GridPosition;
                    int index = (int)(gridPos.Y * columnsPerPage + gridPos.X);
                    return new { IconView = iv, Index = index };
                })
                .OrderBy(x => x.Index)
                .ToList();

            // Rebuild DisplayItems in the new order
            _viewModel.DisplayItems.Clear();
            foreach (var item in iconPositions)
            {
                if (item.IconView.IsGroup && item.IconView.AppGroup != null)
                    _viewModel.DisplayItems.Add(item.IconView.AppGroup);
                else if (item.IconView.AppIcon != null)
                    _viewModel.DisplayItems.Add(item.IconView.AppIcon);
            }

            // Clean up empty trailing pages
            _viewModel.RemoveEmptyTrailingPages();
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
        
        private int _previousPage = 0;

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LaunchpadViewModel.TotalPages) ||
                e.PropertyName == nameof(LaunchpadViewModel.CurrentPage))
            {
                var currentPage = _viewModel?.CurrentPage ?? 0;
                bool slideLeft = currentPage > _previousPage;
                UpdatePageIndicators();
                UpdatePagesLayout(animateDirection: currentPage != _previousPage, slideLeft: slideLeft);
                _previousPage = currentPage;
            }
            else if (e.PropertyName == nameof(LaunchpadViewModel.AllApplications) ||
                     e.PropertyName == nameof(LaunchpadViewModel.AppGroups) ||
                     e.PropertyName == nameof(LaunchpadViewModel.DisplayItems))
            {
                CreateIconViews();
                UpdateSuggestionsBar();
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

            if (_viewModel.TotalPages <= 1) return;

            for (int i = 0; i < _viewModel.TotalPages; i++)
            {
                bool isCurrent = i == _viewModel.CurrentPage;
                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = isCurrent ? 10 : 7,
                    Height = isCurrent ? 10 : 7,
                    Margin = new Thickness(4, 0, 4, 0),
                    Fill = isCurrent
                        ? Brushes.White
                        : new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Animate the dot
                if (isCurrent)
                {
                    dot.Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Colors.White, BlurRadius = 6, ShadowDepth = 0, Opacity = 0.6
                    };
                }

                var pageIndex = i;
                dot.MouseLeftButtonUp += (s, e) =>
                {
                    _viewModel.GoToPage(pageIndex);
                    e.Handled = true;
                };

                PageIndicators.Children.Add(dot);
            }
        }
        
        private Dictionary<object, SeroIconView> _iconCache = new Dictionary<object, SeroIconView>();
        private Dictionary<SeroIconView, Point> _originalPositions = new Dictionary<SeroIconView, Point>();
        
        private void CreateIconViews()
        {
            if (_viewModel == null || IconCanvas == null) return;
            
            bool isSearchMode = !string.IsNullOrWhiteSpace(_viewModel.SearchText);
            System.Diagnostics.Debug.WriteLine($"CreateIconViews: DisplayItems={_viewModel.DisplayItems.Count}, SearchMode={isSearchMode}, SearchText='{_viewModel.SearchText}'");
            
            // Hide all cached icons first
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
            
            // Use shared grid dimensions calculation
            var (canvasWidth, canvasHeight, columnsPerPage, rowsPerPage, horizontalSpacing, verticalSpacing, iconWidth, iconHeight) = GetGridDimensions();
            var iconsPerPage = columnsPerPage * rowsPerPage;
            
            System.Diagnostics.Debug.WriteLine($"Canvas: {canvasWidth}x{canvasHeight}, Grid: {columnsPerPage}x{rowsPerPage}, Spacing: {horizontalSpacing}x{verticalSpacing}");
            
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
                        System.Diagnostics.Debug.WriteLine($"Creating NEW SeroIconView for app {app.Name}");
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
                        
                        // Hook up event handlers for newly created icons
                        iconView.IconClicked += OnIconClicked;
                        iconView.DragStarted += OnIconDragStarted;
                        iconView.DragMoved += OnIconDragMoved;
                        iconView.DragCompleted += OnIconDragCompleted;
                        iconView.PinToDockRequested += OnPinToDockRequested;
                        iconView.UnpinFromDockRequested += OnUnpinFromDockRequested;
                    }
                }
                
                if (iconView != null)
                {
                    // In search mode, show all results on one page
                    // In normal mode, respect paging
                    bool shouldShow = true;
                    
                    if (!isSearchMode)
                    {
                        // Normal mode: only show icons for current page
                        var pageIndex = iconIndex / iconsPerPage;
                        shouldShow = pageIndex == (_viewModel?.CurrentPage ?? 0);
                    }
                    
                    if (shouldShow)
                    {
                        // Calculate position based on mode
                        int displayIndex = isSearchMode ? iconIndex : iconIndex % iconsPerPage;
                        var row = displayIndex / columnsPerPage;
                        var col = displayIndex % columnsPerPage;
                        
                        var x = col * horizontalSpacing + (horizontalSpacing - iconWidth) / 2;
                        var y = row * verticalSpacing + (verticalSpacing - iconHeight) / 2;
                        
                        Canvas.SetLeft(iconView, x);
                        Canvas.SetTop(iconView, y);
                        iconView.GridPosition = new Point(col, row);
                        iconView.Visibility = Visibility.Visible;
                        
                        var name = (item is AppIcon a) ? a.Name : ((AppGroup)item).Name;
                        System.Diagnostics.Debug.WriteLine($"Positioned {name} at ({x:F0}, {y:F0}) - grid ({col}, {row})");
                    }
                    else
                    {
                        // Hide icons that don't belong to current page
                        iconView.Visibility = Visibility.Collapsed;
                    }
                    
                    iconIndex++;
                }
            }
            
            UpdatePageIndicators();
        }
        
        private void UpdatePagesLayout(bool animateDirection = false, bool slideLeft = false)
        {
            if (_viewModel == null || IconCanvas == null) return;

            // Animate page transition
            var slideDirection = slideLeft ? -1 : 1;
            var slideFrom = slideDirection * 80;

            var slideAnim = new DoubleAnimation
            {
                From = slideFrom,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeAnim = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(200)
            };

            var transform = new System.Windows.Media.TranslateTransform();
            IconCanvas.RenderTransform = transform;

            CreateIconViews();

            if (animateDirection)
            {
                transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, slideAnim);
                IconCanvas.BeginAnimation(OpacityProperty, fadeAnim);
            }
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

            // Hide search bar, show Done button
            SearchBar.Visibility = Visibility.Collapsed;
            if (DoneButton != null)
                DoneButton.Visibility = Visibility.Visible;

            // Provide haptic feedback
            try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
        }
        
        public void ExitEditMode()
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

            // Hide Done button
            if (DoneButton != null)
                DoneButton.Visibility = Visibility.Collapsed;

            // Save layout after edit mode
            _viewModel?.SaveCurrentLayout();
        }
        
        public bool IsEditMode => _isEditMode;
        
        // Override page navigation to prevent during edit mode
        private bool CanNavigatePages => !_isEditMode;
    }
}