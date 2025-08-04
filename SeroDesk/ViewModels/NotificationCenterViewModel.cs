using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SeroDesk.Models;
using SeroDesk.Platform;

namespace SeroDesk.ViewModels
{
    public class NotificationCenterViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<NotificationItem> _notifications;
        private WindowsNotificationListener? _notificationListener;
        
        public ObservableCollection<NotificationItem> Notifications
        {
            get => _notifications;
            set { _notifications = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNotifications)); }
        }
        
        public bool HasNotifications => _notifications.Count > 0;
        
        public NotificationCenterViewModel()
        {
            _notifications = new ObservableCollection<NotificationItem>();
            
            // Initialize Windows notification listener
            _notificationListener = new WindowsNotificationListener();
            _notificationListener.NotificationReceived += OnWindowsNotificationReceived;
            _notificationListener.StartListening();
            
            // Still subscribe to the old system for backward compatibility
            SystemNotificationMonitor.NotificationReceived += OnSystemNotificationReceived;
        }
        
        private void OnWindowsNotificationReceived(object? sender, NotificationReceivedEventArgs e)
        {
            var notification = new NotificationItem
            {
                Id = e.NotificationId,
                AppName = e.AppDisplayName,
                Title = e.Title,
                Content = e.Content,
                Timestamp = e.Timestamp,
                Type = NotificationType.Information, // Default type
                Priority = NotificationPriority.Normal,
                AppIcon = null // Could be loaded from e.ImagePath
            };
            
            // Add to collection on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _notifications.Insert(0, notification); // Add at top
                OnPropertyChanged(nameof(HasNotifications));
            });
        }
        
        private void LoadTestNotifications()
        {
            // Add some test notifications
            var testNotifications = new[]
            {
                new NotificationItem
                {
                    Id = Guid.NewGuid().ToString(),
                    AppName = "Microsoft Teams",
                    Title = "New message from John Doe",
                    Content = "Hey, can we schedule a meeting for tomorrow?",
                    Timestamp = DateTime.Now.AddMinutes(-5),
                    Type = NotificationType.Message,
                    Priority = NotificationPriority.Normal,
                    AppIcon = IconExtractor.GetIconForFile(@"C:\Program Files\Microsoft\Teams\current\Teams.exe", false)
                },
                new NotificationItem
                {
                    Id = Guid.NewGuid().ToString(),
                    AppName = "Outlook",
                    Title = "New email received",
                    Content = "Project status update from Sarah Wilson",
                    Timestamp = DateTime.Now.AddMinutes(-15),
                    Type = NotificationType.Email,
                    Priority = NotificationPriority.Normal,
                    AppIcon = IconExtractor.GetSystemIcon(SystemIconType.Mail)
                },
                new NotificationItem
                {
                    Id = Guid.NewGuid().ToString(),
                    AppName = "Windows Update",
                    Title = "Update available",
                    Content = "Feature update for Windows 11, version 23H2 is ready to install.",
                    Timestamp = DateTime.Now.AddHours(-1),
                    Type = NotificationType.Information,
                    Priority = NotificationPriority.High,
                    AppIcon = IconExtractor.GetSystemIcon(SystemIconType.Settings)
                }
            };
            
            foreach (var notification in testNotifications)
            {
                _notifications.Add(notification);
            }
        }
        
        private void OnSystemNotificationReceived(object? sender, SystemNotificationEventArgs e)
        {
            var notification = new NotificationItem
            {
                Id = Guid.NewGuid().ToString(),
                AppName = e.AppName,
                Title = e.Title,
                Content = e.Content,
                Timestamp = DateTime.Now,
                Type = MapNotificationType(e.Type),
                Priority = MapNotificationPriority(e.Priority),
                AppIcon = e.AppIcon
            };
            
            // Add to collection on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _notifications.Insert(0, notification); // Add at top
                OnPropertyChanged(nameof(HasNotifications));
            });
        }
        
        public void AddNotification(NotificationItem notification)
        {
            _notifications.Insert(0, notification);
            OnPropertyChanged(nameof(HasNotifications));
        }
        
        public void RemoveNotification(NotificationItem notification)
        {
            _notifications.Remove(notification);
            OnPropertyChanged(nameof(HasNotifications));
        }
        
        public void ClearAllNotifications()
        {
            _notifications.Clear();
            OnPropertyChanged(nameof(HasNotifications));
        }
        
        private NotificationType MapNotificationType(string type)
        {
            return type.ToLower() switch
            {
                "message" => NotificationType.Message,
                "email" => NotificationType.Email,
                "call" => NotificationType.Call,
                "warning" => NotificationType.Warning,
                "error" => NotificationType.Error,
                "success" => NotificationType.Success,
                _ => NotificationType.Information
            };
        }
        
        private NotificationPriority MapNotificationPriority(string priority)
        {
            return priority.ToLower() switch
            {
                "low" => NotificationPriority.Low,
                "high" => NotificationPriority.High,
                "critical" => NotificationPriority.Critical,
                _ => NotificationPriority.Normal
            };
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}