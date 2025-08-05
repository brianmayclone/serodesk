using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SeroDesk.Models;
using SeroDesk.Views.Widgets;
using Newtonsoft.Json;

namespace SeroDesk.Services
{
    /// <summary>
    /// Manages desktop widgets for the SeroDesk interface, providing creation, positioning, and persistence capabilities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The WidgetManager provides comprehensive widget management functionality for SeroDesk's desktop interface:
    /// <list type="bullet">
    /// <item>Widget creation and removal with automatic view generation</item>
    /// <item>Persistent widget positioning and configuration storage</item>
    /// <item>Default widget creation for new installations</item>
    /// <item>Dynamic widget container management on desktop canvas</item>
    /// <item>JSON-based configuration persistence in user's local app data</item>
    /// </list>
    /// </para>
    /// <para>
    /// The service supports various widget types including ClockWidget and WeatherWidget,
    /// with extensible architecture for adding new widget types. All widgets are automatically
    /// positioned and sized according to user preferences.
    /// </para>
    /// <para>
    /// Widget configurations are persisted to the user's LocalApplicationData folder in
    /// JSON format, ensuring widget layouts survive application restarts and system reboots.
    /// </para>
    /// </remarks>
    public class WidgetManager
    {
        /// <summary>
        /// Singleton instance of the WidgetManager.
        /// </summary>
        private static WidgetManager? _instance;
        
        /// <summary>
        /// Gets the singleton instance of the WidgetManager.
        /// </summary>
        /// <value>The global WidgetManager instance.</value>
        public static WidgetManager Instance => _instance ??= new WidgetManager();
        
        /// <summary>
        /// Collection of all active widgets managed by this instance.
        /// </summary>
        private ObservableCollection<Widget> _widgets;
        
        /// <summary>
        /// The canvas container where widget views are displayed.
        /// </summary>
        private Canvas? _widgetContainer;
        
        /// <summary>
        /// Gets the collection of all active widgets.
        /// </summary>
        /// <value>An observable collection of widgets for data binding scenarios.</value>
        public ObservableCollection<Widget> Widgets => _widgets;
        
        private WidgetManager()
        {
            _widgets = new ObservableCollection<Widget>();
        }
        
        /// <summary>
        /// Initializes the widget manager with the specified container canvas.
        /// </summary>
        /// <param name="widgetContainer">The canvas where widgets will be displayed and positioned.</param>
        /// <remarks>
        /// <para>
        /// This method performs the complete widget system initialization:
        /// <list type="number">
        /// <item>Sets the widget container canvas for visual display</item>
        /// <item>Loads previously saved widgets from persistent storage</item>
        /// <item>Creates default widgets if none exist (first run scenario)</item>
        /// </list>
        /// </para>
        /// <para>
        /// This method should be called after the main UI is loaded and the
        /// widget container canvas is available for widget placement.
        /// </para>
        /// </remarks>
        public void Initialize(Canvas widgetContainer)
        {
            _widgetContainer = widgetContainer;
            LoadWidgetsFromStorage();
            CreateDefaultWidgets();
        }
        
        private void CreateDefaultWidgets()
        {
            if (_widgets.Count == 0)
            {
                // Create default clock widget
                var clockWidget = new ClockWidget
                {
                    Position = new Point(50, 50)
                };
                AddWidget(clockWidget);
                
                // Create default weather widget
                var weatherWidget = new WeatherWidget
                {
                    Position = new Point(280, 50)
                };
                AddWidget(weatherWidget);
            }
        }
        
        /// <summary>
        /// Adds a new widget to the manager and displays it on the desktop.
        /// </summary>
        /// <param name="widget">The widget instance to add and display.</param>
        /// <remarks>
        /// <para>
        /// This method performs the complete widget addition process:
        /// <list type="number">
        /// <item>Adds the widget to the managed collection</item>
        /// <item>Initializes the widget's internal state</item>
        /// <item>Creates and positions the widget's visual representation</item>
        /// <item>Saves the updated widget configuration to persistent storage</item>
        /// </list>
        /// </para>
        /// <para>
        /// The widget will be positioned according to its Position property and
        /// will immediately become visible and interactive on the desktop.
        /// </para>
        /// </remarks>
        public void AddWidget(Widget widget)
        {
            _widgets.Add(widget);
            widget.Initialize();
            
            if (_widgetContainer != null)
            {
                CreateWidgetView(widget);
            }
            
            SaveWidgetsToStorage();
        }
        
        /// <summary>
        /// Removes a widget from the manager and hides it from the desktop.
        /// </summary>
        /// <param name="widget">The widget instance to remove.</param>
        /// <remarks>
        /// <para>
        /// This method performs the complete widget removal process:
        /// <list type="number">
        /// <item>Removes the widget from the managed collection</item>
        /// <item>Removes the widget's visual representation from the canvas</item>
        /// <item>Saves the updated widget configuration to persistent storage</item>
        /// </list>
        /// </para>
        /// <para>
        /// The widget will immediately disappear from the desktop and will not
        /// be restored when the application restarts.
        /// </para>
        /// </remarks>
        public void RemoveWidget(Widget widget)
        {
            _widgets.Remove(widget);
            
            if (_widgetContainer != null)
            {
                var container = _widgetContainer.Children
                    .OfType<WidgetContainer>()
                    .FirstOrDefault(w => w.Widget.Id == widget.Id);
                    
                if (container != null)
                {
                    _widgetContainer.Children.Remove(container);
                }
            }
            
            SaveWidgetsToStorage();
        }
        
        private void CreateWidgetView(Widget widget)
        {
            if (_widgetContainer == null) return;
            
            var widgetView = widget.CreateView();
            var container = new WidgetContainer(widget, widgetView);
            
            Canvas.SetLeft(container, widget.Position.X);
            Canvas.SetTop(container, widget.Position.Y);
            
            _widgetContainer.Children.Add(container);
        }
        
        /// <summary>
        /// Updates the position of a widget and persists the change.
        /// </summary>
        /// <param name="widget">The widget whose position should be updated.</param>
        /// <param name="newPosition">The new position coordinates for the widget.</param>
        /// <remarks>
        /// <para>
        /// This method updates the widget's position property and immediately saves
        /// the configuration to ensure the new position is preserved across application
        /// restarts. The visual position is updated automatically through data binding.
        /// </para>
        /// <para>
        /// This method is typically called in response to user drag operations
        /// or programmatic widget repositioning.
        /// </para>
        /// </remarks>
        public void UpdateWidgetPosition(Widget widget, Point newPosition)
        {
            widget.Position = newPosition;
            SaveWidgetsToStorage();
        }
        
        private async void SaveWidgetsToStorage()
        {
            try
            {
                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk");
                
                Directory.CreateDirectory(configDir);
                
                var savedWidgets = _widgets.Select(widget => new SavedWidget
                {
                    Id = widget.Id,
                    Type = widget.GetType().Name,
                    Title = widget.Title,
                    Position = widget.Position,
                    Size = widget.Size,
                    IsLocked = widget.IsLocked
                }).ToList();
                
                var json = JsonConvert.SerializeObject(savedWidgets, Formatting.Indented);
                await File.WriteAllTextAsync(Path.Combine(configDir, "widgets.json"), json);
            }
            catch { }
        }
        
        private async void LoadWidgetsFromStorage()
        {
            try
            {
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk", "widgets.json");
                
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    var savedWidgets = JsonConvert.DeserializeObject<List<SavedWidget>>(json);
                    
                    if (savedWidgets != null)
                    {
                        foreach (var saved in savedWidgets)
                        {
                            Widget? widget = saved.Type switch
                            {
                                nameof(ClockWidget) => new ClockWidget(),
                                nameof(WeatherWidget) => new WeatherWidget(),
                                _ => null
                            };
                            
                            if (widget != null)
                            {
                                widget.Id = saved.Id;
                                widget.Title = saved.Title;
                                widget.Position = saved.Position;
                                widget.Size = saved.Size;
                                widget.IsLocked = saved.IsLocked;
                                
                                _widgets.Add(widget);
                                widget.Initialize();
                                
                                if (_widgetContainer != null)
                                {
                                    CreateWidgetView(widget);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Represents serializable widget data for persistent storage.
        /// </summary>
        /// <remarks>
        /// This class contains the essential properties needed to recreate widgets
        /// after application restart, including type information, positioning,
        /// sizing, and locking state.
        /// </remarks>
        private class SavedWidget
        {
            /// <summary>Gets or sets the unique identifier of the widget.</summary>
            public string Id { get; set; } = string.Empty;
            /// <summary>Gets or sets the widget type name for recreation.</summary>
            public string Type { get; set; } = string.Empty;
            /// <summary>Gets or sets the widget display title.</summary>
            public string Title { get; set; } = string.Empty;
            /// <summary>Gets or sets the widget position on the desktop.</summary>
            public Point Position { get; set; }
            /// <summary>Gets or sets the widget size dimensions.</summary>
            public Size Size { get; set; }
            /// <summary>Gets or sets whether the widget position is locked.</summary>
            public bool IsLocked { get; set; }
        }
    }
}