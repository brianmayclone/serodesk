using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SeroDesk.Models;
using SeroDesk.Views.Widgets;
using Newtonsoft.Json;

namespace SeroDesk.Services
{
    public class WidgetManager
    {
        private static WidgetManager? _instance;
        public static WidgetManager Instance => _instance ??= new WidgetManager();
        
        private ObservableCollection<Widget> _widgets;
        private Canvas? _widgetContainer;
        
        public ObservableCollection<Widget> Widgets => _widgets;
        
        private WidgetManager()
        {
            _widgets = new ObservableCollection<Widget>();
        }
        
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
        
        private class SavedWidget
        {
            public string Id { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public Point Position { get; set; }
            public Size Size { get; set; }
            public bool IsLocked { get; set; }
        }
    }
}