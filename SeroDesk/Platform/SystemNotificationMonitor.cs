using System.Windows.Media;

namespace SeroDesk.Platform
{
    public static class SystemNotificationMonitor
    {
        public static event EventHandler<SystemNotificationEventArgs>? NotificationReceived;
        
        private static Timer? _notificationTimer;
        private static readonly Random _random = new();
        
        public static void StartMonitoring()
        {
            // For demo purposes, we'll simulate notifications
            // In a real implementation, this would hook into Windows notification system
            _notificationTimer = new Timer(SimulateNotification, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5));
        }
        
        public static void StopMonitoring()
        {
            _notificationTimer?.Dispose();
            _notificationTimer = null;
        }
        
        private static void SimulateNotification(object? state)
        {
            var notifications = new[]
            {
                new SystemNotificationEventArgs
                {
                    AppName = "Visual Studio",
                    Title = "Build completed",
                    Content = "Build succeeded with 0 errors and 2 warnings",
                    Type = "information",
                    Priority = "normal",
                    AppIcon = null
                },
                new SystemNotificationEventArgs
                {
                    AppName = "Discord",
                    Title = "Message from @friend",
                    Content = "Hey, are you available for a quick call?",
                    Type = "message",
                    Priority = "normal",
                    AppIcon = null
                },
                new SystemNotificationEventArgs
                {
                    AppName = "Windows Security",
                    Title = "Quick scan completed",
                    Content = "No threats found. Your device is secure.",
                    Type = "success",
                    Priority = "low",
                    AppIcon = null
                }
            };
            
            var notification = notifications[_random.Next(notifications.Length)];
            NotificationReceived?.Invoke(null, notification);
        }
        
        public static void SendTestNotification(string appName, string title, string content)
        {
            var notification = new SystemNotificationEventArgs
            {
                AppName = appName,
                Title = title,
                Content = content,
                Type = "information",
                Priority = "normal",
                AppIcon = null
            };
            
            NotificationReceived?.Invoke(null, notification);
        }
    }
    
    public class SystemNotificationEventArgs : EventArgs
    {
        public string AppName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "information";
        public string Priority { get; set; } = "normal";
        public ImageSource? AppIcon { get; set; }
    }
}