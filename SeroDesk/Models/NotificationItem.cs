using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SeroDesk.Models
{
    public class NotificationItem : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _appName = string.Empty;
        private string _title = string.Empty;
        private string _content = string.Empty;
        private DateTime _timestamp;
        private ImageSource? _appIcon;
        private NotificationType _type;
        private NotificationPriority _priority;
        private bool _isRead;
        
        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }
        
        public string AppName
        {
            get => _appName;
            set { _appName = value; OnPropertyChanged(); }
        }
        
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }
        
        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }
        
        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeAgo)); }
        }
        
        public ImageSource? AppIcon
        {
            get => _appIcon;
            set { _appIcon = value; OnPropertyChanged(); }
        }
        
        public NotificationType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }
        
        public NotificationPriority Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(); }
        }
        
        public bool IsRead
        {
            get => _isRead;
            set { _isRead = value; OnPropertyChanged(); }
        }
        
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - _timestamp;
                
                if (diff.TotalMinutes < 1)
                    return "now";
                else if (diff.TotalMinutes < 60)
                    return $"{(int)diff.TotalMinutes}m";
                else if (diff.TotalHours < 24)
                    return $"{(int)diff.TotalHours}h";
                else if (diff.TotalDays < 7)
                    return $"{(int)diff.TotalDays}d";
                else
                    return _timestamp.ToString("MM/dd");
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public enum NotificationType
    {
        Information,
        Warning,
        Error,
        Success,
        Message,
        Call,
        Email,
        App
    }
    
    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}