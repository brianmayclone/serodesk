using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace SeroDesk.Models
{
    public abstract class Widget : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _title = string.Empty;
        private Point _position;
        private Size _size;
        private bool _isLocked = false;
        
        public string Id 
        { 
            get => _id; 
            set { _id = value; OnPropertyChanged(); } 
        }
        
        public string Title 
        { 
            get => _title; 
            set { _title = value; OnPropertyChanged(); } 
        }
        
        public Point Position 
        { 
            get => _position; 
            set { _position = value; OnPropertyChanged(); } 
        }
        
        public Size Size 
        { 
            get => _size; 
            set { _size = value; OnPropertyChanged(); } 
        }
        
        public bool IsLocked 
        { 
            get => _isLocked; 
            set { _isLocked = value; OnPropertyChanged(); } 
        }
        
        public abstract UserControl CreateView();
        public abstract void UpdateData();
        public abstract void Initialize();
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class ClockWidget : Widget, IDisposable
    {
        private string _currentTime = DateTime.Now.ToString("HH:mm");
        private string _currentDate = DateTime.Now.ToString("dddd, MMMM dd");
        private Timer? _timer;
        private bool _disposed = false;
        
        public string CurrentTime 
        { 
            get => _currentTime; 
            set { _currentTime = value; OnPropertyChanged(); } 
        }
        
        public string CurrentDate 
        { 
            get => _currentDate; 
            set { _currentDate = value; OnPropertyChanged(); } 
        }
        
        public ClockWidget()
        {
            Title = "Clock";
            Size = new Size(200, 100);
        }
        
        public override void Initialize()
        {
            if (!_disposed)
            {
                _timer = new Timer(UpdateTime, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
        }
        
        private void UpdateTime(object? state)
        {
            if (_disposed) return;
            
            try
            {
                // Check if Application.Current is available
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!_disposed)
                        {
                            CurrentTime = DateTime.Now.ToString("HH:mm");
                            CurrentDate = DateTime.Now.ToString("dddd, MMMM dd");
                        }
                    });
                }
                else
                {
                    // Fallback: Update directly if no dispatcher available
                    // This might happen during application shutdown
                    if (!_disposed)
                    {
                        CurrentTime = DateTime.Now.ToString("HH:mm");
                        CurrentDate = DateTime.Now.ToString("dddd, MMMM dd");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error updating clock time: {ex.Message}");
            }
        }
        
        public override void UpdateData()
        {
            CurrentTime = DateTime.Now.ToString("HH:mm");
            CurrentDate = DateTime.Now.ToString("dddd, MMMM dd");
        }
        
        public override UserControl CreateView()
        {
            return new Views.Widgets.ClockWidgetView { DataContext = this };
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _timer?.Dispose();
                _timer = null;
                _disposed = true;
            }
        }
        
        ~ClockWidget()
        {
            Dispose(false);
        }
    }
    
    public class WeatherWidget : Widget
    {
        private string _temperature = "22°C";
        private string _condition = "Sunny";
        private string _location = "Current Location";
        
        public string Temperature 
        { 
            get => _temperature; 
            set { _temperature = value; OnPropertyChanged(); } 
        }
        
        public string Condition 
        { 
            get => _condition; 
            set { _condition = value; OnPropertyChanged(); } 
        }
        
        public string Location 
        { 
            get => _location; 
            set { _location = value; OnPropertyChanged(); } 
        }
        
        public WeatherWidget()
        {
            Title = "Weather";
            Size = new Size(180, 120);
        }
        
        public override void Initialize()
        {
            // TODO: Initialize weather data source
            UpdateData();
        }
        
        public override void UpdateData()
        {
            // TODO: Fetch real weather data
            // For now, use placeholder data
            Temperature = "22°C";
            Condition = "Sunny";
            Location = "Current Location";
        }
        
        public override UserControl CreateView()
        {
            return new Views.Widgets.WeatherWidgetView { DataContext = this };
        }
    }
    
    /// <summary>
    /// Represents a serializable widget configuration for persistent storage.
    /// </summary>
    /// <remarks>
    /// This class contains the essential properties needed to recreate widgets
    /// after application restart, including type information, positioning,
    /// sizing, and locking state.
    /// </remarks>
    public class SavedWidget
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
        /// <summary>Gets or sets whether the widget is locked from being moved or resized.</summary>
        public bool IsLocked { get; set; }
    }
}