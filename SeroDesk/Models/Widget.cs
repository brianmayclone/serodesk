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
    
    public class ClockWidget : Widget
    {
        private string _currentTime = DateTime.Now.ToString("HH:mm");
        private string _currentDate = DateTime.Now.ToString("dddd, MMMM dd");
        private Timer? _timer;
        
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
            _timer = new Timer(UpdateTime, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }
        
        private void UpdateTime(object? state)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm");
                CurrentDate = DateTime.Now.ToString("dddd, MMMM dd");
            });
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
}