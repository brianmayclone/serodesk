using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using SeroDesk.Platform;

namespace SeroDesk.Models
{
    public class DockItem : INotifyPropertyChanged
    {
        private string _displayName = string.Empty;
        private string _executablePath = string.Empty;
        private ImageSource? _iconImage;
        private bool _isPinned;
        private bool _isRunning;
        private bool _isMinimized;
        private WindowInfo? _windowInfo;

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string ExecutablePath
        {
            get => _executablePath;
            set { _executablePath = value; OnPropertyChanged(); }
        }

        public ImageSource? IconImage
        {
            get => _iconImage;
            set { _iconImage = value; OnPropertyChanged(); }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set { _isPinned = value; OnPropertyChanged(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        public bool IsMinimized
        {
            get => _isMinimized;
            set { _isMinimized = value; OnPropertyChanged(); }
        }

        public WindowInfo? WindowInfo
        {
            get => _windowInfo;
            set
            {
                _windowInfo = value;
                IsRunning = value != null && value.IsRunning;
                IsMinimized = value?.IsMinimized ?? false;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
