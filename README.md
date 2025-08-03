# 🌟 SeroDesk - Touch-Optimized Windows 11 Shell Extension

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Windows](https://img.shields.io/badge/Windows-11-blue.svg)](https://www.microsoft.com/windows)
[![WPF](https://img.shields.io/badge/WPF-Framework-purple.svg)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

SeroDesk is an innovative, touch-optimized shell extension for Windows 11 that brings an iOS-inspired user interface directly to your desktop. With modern gestures, a customizable dock, and intelligent widgets, SeroDesk transforms your Windows experience into an intuitive, mobile-like interface.

![SeroDesk Banner](https://via.placeholder.com/800x300/007ACC/FFFFFF?text=SeroDesk+-+Touch-Optimized+Windows+Shell)

## 🚀 Features

### 📱 iOS-Inspired User Interface
- **Touch Gestures**: Native support for swipe, pinch, and tap gestures
- **Launchpad**: Full-screen app overview with folder support
- **Dock**: Customizable dock bar with running apps and favorites
- **Notification Center**: Central notification management (left side)
- **Control Center**: Quick access to system functions (right side)

### 🎛️ Intelligent Widgets
- **Weather Widget**: Live weather information
- **Clock Widget**: Elegant time and date display
- **Customizable Widgets**: Movable, scalable, and lockable
- **Widget Manager**: Easy adding and removing of widgets

### 🪟 Advanced Window Management
- **Shell Replacement**: Complete Explorer integration
- **Multi-Monitor Support**: Optimized for multiple screens
- **Transparency Effects**: Windows 11 acrylic design
- **Always-on-Top**: Intelligent Z-order management

### 🎨 Modern User Interface
- **Fluent Design**: Windows 11 design language
- **Blur Effects**: Background blur for better readability
- **Animations**: Smooth transitions and responses
- **Dark/Light Theme**: Automatic theme detection

## 🏗️ Technical Architecture

### Core Technologies
- **.NET 8.0**: Modern C# development with latest features
- **WPF (Windows Presentation Foundation)**: Native Windows UI framework
- **MVVM Pattern**: Clean separation of UI and business logic
- **Win32 API Integration**: Deep Windows system integration

### Architecture Components

```
SeroDesk/
├── 📁 Core/                    # Base functionalities
│   ├── Converters.cs          # XAML Value Converters
│   ├── DragDropAdorner.cs     # Drag & Drop visualization
│   └── Extensions.cs          # Extension methods
├── 📁 Models/                  # Data models
│   ├── AppGroup.cs            # App grouping
│   ├── AppIcon.cs             # App icon management
│   ├── NotificationItem.cs    # Notifications
│   └── Widget.cs              # Widget base class
├── 📁 Platform/               # Windows integration
│   ├── WindowsIntegration.cs  # Win32 API wrapper
│   ├── ExplorerManager.cs     # Explorer replacement
│   └── GestureRecognizer.cs   # Touch gestures
├── 📁 Services/               # Business logic
│   ├── WidgetManager.cs       # Widget management
│   ├── WindowManager.cs       # Window management
│   ├── NotificationService.cs # Notifications
│   └── SettingsManager.cs     # Settings
├── 📁 ViewModels/            # MVVM ViewModels
│   ├── MainViewModel.cs       # Main ViewModel
│   ├── LaunchpadViewModel.cs  # Launchpad logic
│   └── NotificationCenterViewModel.cs
├── 📁 Views/                 # UI components
│   ├── MainWindow.xaml        # Main window
│   ├── SeroLaunchpad.xaml     # App overview
│   ├── SeroDock.xaml          # Dock bar
│   ├── SeroNotificationCenter.xaml
│   ├── SeroControlCenter.xaml
│   └── Widgets/               # Widget views
└── 📁 Styles/                # XAML styles & templates
```

## 🔧 Technical Details

### Shell Integration
SeroDesk replaces the Windows Explorer shell through:
- **Registry Manipulation**: Registration as alternative shell
- **Process Management**: Automatic termination of Explorer
- **Window Hierarchy**: Positioning as desktop child window

```csharp
// Shell registration
public static void RegisterAsShell(IntPtr hwnd)
{
    SetShellWindow(hwnd);
    SetTaskmanWindow(hwnd);
}
```

### Touch Gesture Recognition
Implements native Windows touch events:
- **Manipulation Events**: WPF touch framework
- **Gesture Recognition**: Custom swipe/pinch algorithms
- **Multi-Touch Support**: Simultaneous gesture processing

```csharp
private void OnSwipeDetected(SwipeDirection direction, Point startPoint)
{
    switch (direction)
    {
        case SwipeDirection.Up:
            if (startY > screenHeight - 200) ShowLaunchpad();
            break;
        case SwipeDirection.Down:
            if (startY < 100) ShowNotificationCenter();
            break;
    }
}
```

### Widget System
Modular widget framework:
- **Abstract Base Class**: `Widget` with standard functionalities
- **Dynamic Loading**: Runtime widget creation
- **Persistence**: JSON-based configuration storage
- **View Creation**: Automatic UI generation

```csharp
public abstract class Widget : INotifyPropertyChanged
{
    public abstract UserControl CreateView();
    public abstract void UpdateData();
    public abstract void Initialize();
}
```

### Window Management
Advanced window management with:
- **Z-Order Control**: Precise window layering
- **DPI Awareness**: High-DPI display support
- **Blur Effects**: DWM integration for transparency
- **Multi-Monitor**: Complete multi-display support

## 🛠️ Installation & Setup

### System Requirements
- **Operating System**: Windows 11 (21H2 or newer)
- **Framework**: .NET 8.0 Runtime
- **Hardware**: Touch display recommended
- **Permissions**: Administrator rights for shell replacement

### Build Instructions

1. **Clone repository**:
```bash
git clone https://github.com/brianmayclone/serodesk.git
cd serodesk
```

2. **Install dependencies**:
```bash
dotnet restore SeroDesk/SeroDesk.csproj
```

3. **Compile project**:
```bash
dotnet build SeroDesk/SeroDesk.csproj -c Release
```

4. **Run as administrator**:
```bash
# Important: Run as administrator for shell replacement
.\SeroDesk\bin\Release\net8.0-windows\SeroDesk.exe
```

### Development

**Visual Studio Setup**:
- Visual Studio 2022 (17.5+)
- .NET 8.0 SDK
- Windows 11 SDK

**Development Build**:
```bash
dotnet run --project SeroDesk/SeroDesk.csproj
```

## 🎮 Usage

### Touch Gestures
| Gesture | Action |
|---------|--------|
| **Swipe up from bottom edge** | Open Launchpad |
| **Swipe down from top edge (left)** | Notification Center |
| **Swipe down from top edge (right)** | Control Center |
| **Swipe down (in Launchpad)** | Close Launchpad |
| **Pinch-to-Zoom** | Resize widgets |

### Keyboard Shortcuts
- **Alt + Tab**: Window Switcher
- **Windows + D**: Show desktop
- **Escape**: Close current overlay
- **Alt + Mouse hover**: Widget edit mode

### Widget Management
- **Alt + Hover**: Show widget handles
- **Drag & Drop**: Move widgets
- **Delete key**: Remove widget
- **Right-click**: Widget context menu

## 🔧 Configuration

### Settings File
Configuration is stored under:
```
%LocalAppData%\SeroDesk\
├── settings.json      # General settings
├── widgets.json       # Widget configuration
└── layout.json        # Window layout
```

### Widget Configuration
```json
{
  "widgets": [
    {
      "id": "clock-001",
      "type": "ClockWidget",
      "position": { "x": 50, "y": 50 },
      "size": { "width": 200, "height": 100 },
      "isLocked": false
    }
  ]
}
```

## 🏗️ Architecture Highlights

### MVVM Implementation
- **ViewModels**: Business logic separation
- **Commands**: UI action binding
- **Data Binding**: Reactive UI updates
- **Services**: Singleton-based services

### Platform Integration
- **Win32 Interop**: Native Windows APIs
- **DWM Composition**: Hardware-accelerated effects
- **Shell Integration**: Explorer replacement mechanism
- **Registry Management**: System configuration

### Performance Optimizations
- **Virtualization**: Lazy-loading for large lists
- **Caching**: Intelligent icon caching
- **Background Processing**: Async/Await pattern
- **Memory Management**: Disposable pattern

## 🤝 Development & Contribution

### Code Structure
The project follows modern C#/.NET development practices:
- **Clean Architecture**: Clear layer separation
- **SOLID Principles**: Object-oriented design principles
- **Async/Await**: Reactive programming
- **Dependency Injection**: Service-based architecture

### Contributing
1. Fork the repository
2. Create feature branch
3. Implement changes
4. Create pull request

### Coding Standards
- **C# 12**: Modern language features
- **Nullable Reference Types**: Null safety
- **Code Analysis**: Static code analysis
- **XML Documentation**: Complete API documentation

## 📋 Roadmap

### Version 1.1 (Planned)
- [ ] Customizable themes
- [ ] More widget types
- [ ] Improved touch gestures
- [ ] Multi-monitor dock

### Version 1.2 (Future)
- [ ] Plugin system
- [ ] Cloud synchronization
- [ ] Advanced animations
- [ ] Performance optimizations

## ⚠️ Known Limitations

- **Administrator rights required** for shell replacement
- **Windows 11 specific** - no backward compatibility
- **Touch-optimized** - limited mouse support
- **Beta software** - possible stability issues

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## 👥 Credits

Developed with ❤️ for the Windows 11 touch community.

**Main Developer**: [Brian May Clone](https://github.com/brianmayclone)

**Inspired by**: iOS SpringBoard, Windows Phone Live Tiles, macOS Mission Control

---

**⭐ If you like SeroDesk, give the project a star!**

**🐛 Found bugs?** [Create an issue](https://github.com/brianmayclone/serodesk/issues)

**💡 Feature requests?** [Start a discussion](https://github.com/brianmayclone/serodesk/discussions)
