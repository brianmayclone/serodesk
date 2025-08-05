using Microsoft.Win32;
using SeroDesk.Models;
using SeroDesk.Platform;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;

namespace SeroDesk.Services
{
    /// <summary>
    /// Provides functionality to scan and discover installed applications on the Windows system.
    /// This service searches through Start Menu shortcuts, registry entries, and common program locations
    /// to build a comprehensive list of available applications for the desktop shell.
    /// </summary>
    /// <remarks>
    /// The ApplicationScanner performs the following operations:
    /// <list type="bullet">
    /// <item><description>Scans Windows Start Menu for application shortcuts</description></item>
    /// <item><description>Reads registry entries for installed programs</description></item>
    /// <item><description>Searches desktop and common program directories</description></item>
    /// <item><description>Extracts application icons and metadata</description></item>
    /// <item><description>Filters out system applications and duplicates</description></item>
    /// </list>
    /// 
    /// This class is designed to work asynchronously to avoid blocking the UI thread
    /// during the potentially time-consuming scanning process.
    /// </remarks>
    public class ApplicationScanner
    {
        private static readonly string[] CommonProgramFolders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), ""),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "")
        };
        
        private static readonly string[] ExecutableExtensions = new[] 
        { 
            ".exe", ".lnk", ".url", ".appref-ms" 
        };
        
        public static async Task<ObservableCollection<AppIcon>> ScanInstalledApplicationsAsync()
        {
            var appsList = new List<AppIcon>();
            var foundApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            await Task.Run(() =>
            {
                try
                {
                    // Scan Start Menu
                    ScanStartMenu(appsList, foundApps);
                    // Scan Registry
                    ScanRegistry(appsList, foundApps);
                    
                    // Scan common locations
                    ScanCommonLocations(appsList, foundApps);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during scanning: {ex.Message}");
                }
            });
            
            // Sort by name and create ObservableCollection on UI thread
            var sorted = appsList.OrderBy(a => a.Name).ToList();
            var applications = new ObservableCollection<AppIcon>();
            
            foreach (var app in sorted)
            {
                applications.Add(app);
            }
            
            // Removed debug output for cleaner UI
            return applications;
        }
        
        private static void ScanStartMenu(List<AppIcon> applications, 
            HashSet<string> foundApps)
        {
            foreach (var folder in CommonProgramFolders.Take(2)) // Only Start Menu folders
            {
                if (Directory.Exists(folder))
                {
                    ScanDirectory(folder, applications, foundApps);
                }
            }
        }
        
        private static void ScanDirectory(string directory, List<AppIcon> applications,
            HashSet<string> foundApps, int maxDepth = 3, int currentDepth = 0)
        {
            if (currentDepth > maxDepth) return;
            
            try
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    var extension = Path.GetExtension(file).ToLower();
                    if (ExecutableExtensions.Contains(extension))
                    {
                        var appIcon = CreateAppIconFromFile(file);
                        if (appIcon != null && !foundApps.Contains(appIcon.ExecutablePath))
                        {
                            foundApps.Add(appIcon.ExecutablePath);
                            applications.Add(appIcon);
                        }
                    }
                }
                
                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    ScanDirectory(subDir, applications, foundApps, maxDepth, currentDepth + 1);
                }
            }
            catch { }
        }
        
        private static AppIcon? CreateAppIconFromFile(string filePath)
        {
            try
            {
                var appIcon = new AppIcon();
                
                if (filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse shortcut
                    var shortcut = ResolveShortcut(filePath);
                    if (shortcut == null || !File.Exists(shortcut.TargetPath))
                        return null;
                    
                    appIcon.Name = Path.GetFileNameWithoutExtension(filePath);
                    appIcon.ExecutablePath = shortcut.TargetPath;
                    appIcon.Arguments = shortcut.Arguments;
                    appIcon.WorkingDirectory = shortcut.WorkingDirectory;
                    
                    // Get icon from target or shortcut
                    appIcon.IconImage = IconExtractor.GetIconForFile(
                        File.Exists(shortcut.TargetPath) ? shortcut.TargetPath : filePath, true);
                }
                else if (filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    appIcon.Name = GetApplicationName(filePath);
                    appIcon.ExecutablePath = filePath;
                    appIcon.IconImage = IconExtractor.GetIconForFile(filePath, true);
                }
                else if (filePath.EndsWith(".appref-ms", StringComparison.OrdinalIgnoreCase))
                {
                    // ClickOnce application
                    appIcon.Name = Path.GetFileNameWithoutExtension(filePath);
                    appIcon.ExecutablePath = filePath;
                    appIcon.IconImage = IconExtractor.GetIconForExtension(".exe", true);
                }
                else
                {
                    return null;
                }
                
                // Skip Windows system apps
                if (IsSystemApplication(appIcon.ExecutablePath))
                    return null;
                
                return appIcon;
            }
            catch
            {
                return null;
            }
        }
        
        private static void ScanRegistry(List<AppIcon> applications, 
            HashSet<string> foundApps)
        {
            var registryKeys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            
            foreach (var keyPath in registryKeys)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                    if (key == null) continue;
                    
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var subKey = key.OpenSubKey(subKeyName);
                            if (subKey == null) continue;
                            
                            var displayName = subKey.GetValue("DisplayName") as string;
                            var installLocation = subKey.GetValue("InstallLocation") as string;
                            var displayIcon = subKey.GetValue("DisplayIcon") as string;
                            
                            if (string.IsNullOrEmpty(displayName)) continue;
                            
                            // Try to find executable
                            string? executablePath = null;
                            
                            if (!string.IsNullOrEmpty(displayIcon))
                            {
                                var iconParts = displayIcon.Split(',');
                                if (File.Exists(iconParts[0]))
                                {
                                    executablePath = iconParts[0];
                                }
                            }
                            
                            if (executablePath == null && !string.IsNullOrEmpty(installLocation))
                            {
                                executablePath = FindMainExecutable(installLocation, displayName);
                            }
                            
                            if (!string.IsNullOrEmpty(executablePath) && 
                                !foundApps.Contains(executablePath) &&
                                !IsSystemApplication(executablePath))
                            {
                                var appIcon = new AppIcon
                                {
                                    Name = displayName,
                                    ExecutablePath = executablePath,
                                    IconImage = IconExtractor.GetIconForFile(executablePath, true)
                                };
                                
                                foundApps.Add(executablePath);
                                applications.Add(appIcon);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        
        private static void ScanCommonLocations(List<AppIcon> applications,
            HashSet<string> foundApps)
        {
            // Scan desktop
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (Directory.Exists(desktop))
            {
                ScanDirectory(desktop, applications, foundApps, 0, 0);
            }
            
            // Scan common desktop
            var commonDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            if (Directory.Exists(commonDesktop))
            {
                ScanDirectory(commonDesktop, applications, foundApps, 0, 0);
            }
        }
        
        private static string? FindMainExecutable(string directory, string appName)
        {
            try
            {
                if (!Directory.Exists(directory)) return null;
                
                // Look for exe with similar name
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly);
                
                // Try exact match first
                var exactMatch = exeFiles.FirstOrDefault(f => 
                    Path.GetFileNameWithoutExtension(f).Equals(appName, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null) return exactMatch;
                
                // Try partial match
                var partialMatch = exeFiles.FirstOrDefault(f => 
                    Path.GetFileNameWithoutExtension(f).Contains(appName.Split(' ')[0], 
                    StringComparison.OrdinalIgnoreCase));
                if (partialMatch != null) return partialMatch;
                
                // Return first exe if only one
                if (exeFiles.Length == 1) return exeFiles[0];
            }
            catch { }
            
            return null;
        }
        
        private static string GetApplicationName(string exePath)
        {
            try
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
                if (!string.IsNullOrEmpty(versionInfo.ProductName))
                    return versionInfo.ProductName;
                if (!string.IsNullOrEmpty(versionInfo.FileDescription))
                    return versionInfo.FileDescription;
            }
            catch { }
            
            return Path.GetFileNameWithoutExtension(exePath);
        }
        
        private static bool IsSystemApplication(string path)
        {
            if (string.IsNullOrEmpty(path)) return true;
            
            var systemPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                Environment.GetFolderPath(Environment.SpecialFolder.SystemX86)
            };
            
            var lowerPath = path.ToLower();
            
            return systemPaths.Any(sp => lowerPath.StartsWith(sp.ToLower())) ||
                   lowerPath.Contains(@"\windows\system") ||
                   lowerPath.Contains(@"\windows\syswow64") ||
                   lowerPath.Contains(@"\windows\servicing");
        }
        
        private class ShortcutInfo
        {
            public string TargetPath { get; set; } = string.Empty;
            public string Arguments { get; set; } = string.Empty;
            public string WorkingDirectory { get; set; } = string.Empty;
        }
        
        private static ShortcutInfo? ResolveShortcut(string shortcutPath)
        {
            try
            {
                // Use Windows Script Host to parse .lnk files
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return null;
                
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return null;
                
                dynamic? shortcut = shell.CreateShortcut(shortcutPath);
                if (shortcut == null) return null;
                
                var info = new ShortcutInfo
                {
                    TargetPath = shortcut.TargetPath ?? string.Empty,
                    Arguments = shortcut.Arguments ?? string.Empty,
                    WorkingDirectory = shortcut.WorkingDirectory ?? string.Empty
                };
                
                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);
                
                return info;
            }
            catch
            {
                return null;
            }
        }
    }
}