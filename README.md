# 🌟 SeroDesk - Touch-optimierte Windows 11 Shell Extension

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)
[![Windows](https://img.shields.io/badge/Windows-11-blue.svg)](https://www.microsoft.com/windows)
[![WPF](https://img.shields.io/badge/WPF-Framework-purple.svg)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

SeroDesk ist eine innovative, touch-optimierte Shell Extension für Windows 11, die eine iOS-inspirierte Benutzeroberfläche direkt auf Ihrem Desktop bringt. Mit modernen Gesten, einem anpassbaren Dock und intelligenten Widgets transformiert SeroDesk Ihr Windows-Erlebnis in eine intuitive, mobile-ähnliche Oberfläche.

![SeroDesk Banner](https://via.placeholder.com/800x300/007ACC/FFFFFF?text=SeroDesk+-+Touch-optimierte+Windows+Shell)

## 🚀 Features

### 📱 iOS-inspirierte Benutzeroberfläche
- **Touch-Gesten**: Native Unterstützung für Wisch-, Pinch- und Tap-Gesten
- **Launchpad**: Vollbildschirm App-Übersicht mit Ordner-Unterstützung
- **Dock**: Anpassbare Dock-Leiste mit laufenden Apps und Favoriten
- **Notification Center**: Zentrale Benachrichtigungsverwaltung (links)
- **Control Center**: Schnellzugriff auf Systemfunktionen (rechts)

### 🎛️ Intelligente Widgets
- **Wetter-Widget**: Live-Wetterinformationen
- **Uhr-Widget**: Elegante Zeit- und Datumsanzeige
- **Anpassbare Widgets**: Verschiebbar, skalierbar und sperrbar
- **Widget-Manager**: Einfaches Hinzufügen und Entfernen von Widgets

### 🪟 Erweiterte Fensterverwaltung
- **Shell-Ersatz**: Vollständige Explorer-Integration
- **Multi-Monitor-Support**: Optimiert für mehrere Bildschirme
- **Transparenz-Effekte**: Windows 11 Acryl-Design
- **Always-on-Top**: Intelligente Z-Order-Verwaltung

### 🎨 Moderne Benutzeroberfläche
- **Fluent Design**: Windows 11 Design-Sprache
- **Blur-Effekte**: Hintergrund-Unschärfe für bessere Lesbarkeit
- **Animationen**: Flüssige Übergänge und Reaktionen
- **Dark/Light Theme**: Automatische Theme-Erkennung

## 🏗️ Technische Architektur

### Kern-Technologien
- **.NET 8.0**: Moderne C# Entwicklung mit neuesten Features
- **WPF (Windows Presentation Foundation)**: Native Windows UI-Framework
- **MVVM Pattern**: Saubere Trennung von UI und Geschäftslogik
- **Win32 API Integration**: Tiefe Windows-Systemintegration

### Architektur-Komponenten

```
SeroDesk/
├── 📁 Core/                    # Basis-Funktionalitäten
│   ├── Converters.cs          # XAML Value Converter
│   ├── DragDropAdorner.cs     # Drag & Drop Visualisierung
│   └── Extensions.cs          # Extension Methods
├── 📁 Models/                  # Datenmodelle
│   ├── AppGroup.cs            # App-Gruppierung
│   ├── AppIcon.cs             # App-Icon Verwaltung
│   ├── NotificationItem.cs    # Benachrichtigungen
│   └── Widget.cs              # Widget-Basisklasse
├── 📁 Platform/               # Windows-Integration
│   ├── WindowsIntegration.cs  # Win32 API Wrapper
│   ├── ExplorerManager.cs     # Explorer Ersatz
│   └── GestureRecognizer.cs   # Touch-Gesten
├── 📁 Services/               # Business Logic
│   ├── WidgetManager.cs       # Widget-Verwaltung
│   ├── WindowManager.cs       # Fenster-Management
│   ├── NotificationService.cs # Benachrichtigungen
│   └── SettingsManager.cs     # Einstellungen
├── 📁 ViewModels/            # MVVM ViewModels
│   ├── MainViewModel.cs       # Haupt-ViewModel
│   ├── LaunchpadViewModel.cs  # Launchpad-Logic
│   └── NotificationCenterViewModel.cs
├── 📁 Views/                 # UI-Komponenten
│   ├── MainWindow.xaml        # Haupt-Fenster
│   ├── SeroLaunchpad.xaml     # App-Übersicht
│   ├── SeroDock.xaml          # Dock-Leiste
│   ├── SeroNotificationCenter.xaml
│   ├── SeroControlCenter.xaml
│   └── Widgets/               # Widget-Views
└── 📁 Styles/                # XAML Styles & Templates
```

## 🔧 Technische Details

### Shell-Integration
SeroDesk ersetzt die Windows Explorer Shell durch:
- **Registry-Manipulation**: Registrierung als alternative Shell
- **Process-Management**: Automatisches Beenden des Explorers
- **Window-Hierarchy**: Positionierung als Desktop-Child-Window

```csharp
// Shell-Registrierung
public static void RegisterAsShell(IntPtr hwnd)
{
    SetShellWindow(hwnd);
    SetTaskmanWindow(hwnd);
}
```

### Touch-Gesten-Erkennung
Implementiert native Windows Touch-Events:
- **Manipulation Events**: WPF Touch-Framework
- **Gesture Recognition**: Custom Swipe/Pinch-Algorithmen
- **Multi-Touch Support**: Gleichzeitige Gesten-Verarbeitung

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

### Widget-System
Modulares Widget-Framework:
- **Abstract Base Class**: `Widget` mit Standard-Funktionalitäten
- **Dynamic Loading**: Laufzeit-Widget-Erstellung
- **Persistence**: JSON-basierte Konfigurationsspeicherung
- **View Creation**: Automatische UI-Generierung

```csharp
public abstract class Widget : INotifyPropertyChanged
{
    public abstract UserControl CreateView();
    public abstract void UpdateData();
    public abstract void Initialize();
}
```

### Window-Management
Erweiterte Fensterverwaltung mit:
- **Z-Order Control**: Präzise Fenster-Schichtung
- **DPI Awareness**: High-DPI Display-Unterstützung
- **Blur Effects**: DWM-Integration für Transparenz
- **Multi-Monitor**: Vollständige Multi-Display-Unterstützung

## 🛠️ Installation & Setup

### Systemanforderungen
- **Betriebssystem**: Windows 11 (21H2 oder neuer)
- **Framework**: .NET 8.0 Runtime
- **Hardware**: Touch-Display empfohlen
- **Berechtigungen**: Administrator-Rechte für Shell-Ersatz

### Build-Anweisungen

1. **Repository klonen**:
```bash
git clone https://github.com/brianmayclone/serodesk.git
cd serodesk
```

2. **Dependencies installieren**:
```bash
dotnet restore SeroDesk/SeroDesk.csproj
```

3. **Projekt kompilieren**:
```bash
dotnet build SeroDesk/SeroDesk.csproj -c Release
```

4. **Als Administrator ausführen**:
```bash
# Wichtig: Als Administrator für Shell-Ersatz
.\SeroDesk\bin\Release\net8.0-windows\SeroDesk.exe
```

### Entwicklung

**Visual Studio Setup**:
- Visual Studio 2022 (17.5+)
- .NET 8.0 SDK
- Windows 11 SDK

**Development Build**:
```bash
dotnet run --project SeroDesk/SeroDesk.csproj
```

## 🎮 Bedienung

### Touch-Gesten
| Geste | Aktion |
|-------|--------|
| **Vom unteren Rand nach oben wischen** | Launchpad öffnen |
| **Vom oberen Rand nach unten wischen (links)** | Notification Center |
| **Vom oberen Rand nach unten wischen (rechts)** | Control Center |
| **Nach unten wischen (im Launchpad)** | Launchpad schließen |
| **Pinch-to-Zoom** | Widget-Größe ändern |

### Tastenkombinationen
- **Alt + Tab**: Window Switcher
- **Windows + D**: Desktop anzeigen
- **Escape**: Aktuelle Overlay schließen
- **Alt + Mauszeiger**: Widget-Bearbeitungsmodus

### Widget-Verwaltung
- **Alt + Hover**: Widget-Handles anzeigen
- **Drag & Drop**: Widget verschieben
- **Delete-Taste**: Widget entfernen
- **Rechtsklick**: Widget-Kontextmenü

## 🔧 Konfiguration

### Einstellungen-Datei
Konfiguration wird gespeichert unter:
```
%LocalAppData%\SeroDesk\
├── settings.json      # Allgemeine Einstellungen
├── widgets.json       # Widget-Konfiguration
└── layout.json        # Fenster-Layout
```

### Widget-Konfiguration
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

## 🏗️ Architektur-Highlights

### MVVM Implementation
- **ViewModels**: Geschäftslogik-Trennung
- **Commands**: UI-Action-Binding
- **Data Binding**: Reaktive UI-Updates
- **Services**: Singleton-basierte Services

### Platform Integration
- **Win32 Interop**: Native Windows-APIs
- **DWM Composition**: Hardware-beschleunigte Effekte
- **Shell Integration**: Explorer-Ersatz-Mechanismus
- **Registry Management**: Systemkonfiguration

### Performance Optimierungen
- **Virtualization**: Lazy-Loading für große Listen
- **Caching**: Intelligent Icon-Caching
- **Background Processing**: Async/Await-Pattern
- **Memory Management**: Disposable-Pattern

## 🤝 Entwicklung & Beitrag

### Code-Struktur
Das Projekt folgt modernen C#/.NET Entwicklungspraktiken:
- **Clean Architecture**: Klare Schichtentrennung
- **SOLID Principles**: Objektorientierte Designprinzipien
- **Async/Await**: Reaktive Programmierung
- **Dependency Injection**: Service-basierte Architektur

### Entwicklung beitragen
1. Fork des Repositories
2. Feature-Branch erstellen
3. Änderungen implementieren
4. Pull Request erstellen

### Coding Standards
- **C# 12**: Moderne Sprachfeatures
- **Nullable Reference Types**: Null-Safety
- **Code Analysis**: Static Code Analysis
- **XML Documentation**: Vollständige API-Dokumentation

## 📋 Roadmap

### Version 1.1 (Geplant)
- [ ] Anpassbare Themes
- [ ] Mehr Widget-Typen
- [ ] Verbesserte Touch-Gesten
- [ ] Multi-Monitor-Dock

### Version 1.2 (Zukunft)
- [ ] Plugins-System
- [ ] Cloud-Synchronisation
- [ ] Erweiterte Animationen
- [ ] Performance-Optimierungen

## ⚠️ Bekannte Einschränkungen

- **Administrator-Rechte erforderlich** für Shell-Ersatz
- **Windows 11 spezifisch** - keine Rückwärtskompatibilität
- **Touch-optimiert** - begrenzte Maus-Unterstützung
- **Beta-Software** - mögliche Stabilitätsprobleme

## 📄 Lizenz

Dieses Projekt ist unter der MIT-Lizenz lizenziert. Siehe [LICENSE](LICENSE) für Details.

## 👥 Credits

Entwickelt mit ❤️ für die Windows 11 Touch-Community.

**Hauptentwickler**: [Brian May Clone](https://github.com/brianmayclone)

**Inspiriert durch**: iOS SpringBoard, Windows Phone Live Tiles, macOS Mission Control

---

**⭐ Wenn Ihnen SeroDesk gefällt, geben Sie dem Projekt einen Stern!**

**🐛 Bugs gefunden?** [Issue erstellen](https://github.com/brianmayclone/serodesk/issues)

**💡 Feature-Wünsche?** [Discussion starten](https://github.com/brianmayclone/serodesk/discussions)
