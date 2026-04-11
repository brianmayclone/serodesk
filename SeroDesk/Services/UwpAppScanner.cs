using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SeroDesk.Models;
using SeroDesk.Platform;

namespace SeroDesk.Services
{
    /// <summary>
    /// Scans for UWP/MSIX/Store applications that aren't found by the traditional ApplicationScanner.
    /// Uses PowerShell Get-AppxPackage to enumerate installed modern apps.
    /// </summary>
    public static class UwpAppScanner
    {
        /// <summary>
        /// Scans for installed UWP/Store applications and returns them as AppIcon objects.
        /// </summary>
        public static async Task<List<AppIcon>> ScanUwpAppsAsync()
        {
            var apps = new List<AppIcon>();

            try
            {
                var output = await RunPowerShellAsync(
                    "Get-AppxPackage | Where-Object { $_.IsFramework -eq $false -and $_.SignatureKind -eq 'Store' } | " +
                    "ForEach-Object { $manifest = Get-AppxPackageManifest $_; " +
                    "$displayName = $manifest.Package.Properties.DisplayName; " +
                    "$logo = $manifest.Package.Properties.Logo; " +
                    "$installLocation = $_.InstallLocation; " +
                    "\"$displayName|$installLocation|$logo\" }");

                if (string.IsNullOrEmpty(output)) return apps;

                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        var parts = line.Trim().Split('|');
                        if (parts.Length < 2) continue;

                        var displayName = parts[0].Trim();
                        var installLocation = parts[1].Trim();
                        var logoRelative = parts.Length > 2 ? parts[2].Trim() : "";

                        // Skip system/framework apps
                        if (string.IsNullOrEmpty(displayName) ||
                            displayName.StartsWith("ms-resource:") ||
                            displayName.StartsWith("Microsoft.") && !IsUserFacingMicrosoftApp(displayName))
                            continue;

                        var app = new AppIcon
                        {
                            Name = displayName,
                            ExecutablePath = installLocation,
                            Id = $"uwp:{displayName}",
                            Type = IconType.Application
                        };

                        // Try to load logo
                        if (!string.IsNullOrEmpty(logoRelative) && !string.IsNullOrEmpty(installLocation))
                        {
                            var logoPath = Path.Combine(installLocation, logoRelative);
                            app.IconImage = TryLoadUwpIcon(logoPath, installLocation);
                        }

                        if (app.IconImage == null)
                        {
                            app.IconImage = IconExtractor.GetIconForFile(installLocation, true);
                        }

                        apps.Add(app);
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"Failed to parse UWP app line: {line} - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("UWP app scanning failed", ex);
            }

            Logger.Info($"Found {apps.Count} UWP/Store apps");
            return apps;
        }

        private static bool IsUserFacingMicrosoftApp(string name)
        {
            var userApps = new[]
            {
                "Microsoft.WindowsStore", "Microsoft.WindowsCalculator",
                "Microsoft.WindowsCamera", "Microsoft.Windows.Photos",
                "Microsoft.WindowsAlarms", "Microsoft.WindowsMaps",
                "Microsoft.ZuneMusic", "Microsoft.ZuneVideo",
                "Microsoft.MicrosoftStickyNotes", "Microsoft.ScreenSketch",
                "Microsoft.WindowsTerminal", "Microsoft.Todos",
                "Microsoft.Office", "Microsoft.Teams", "Microsoft.OneDrive",
                "Microsoft.Whiteboard", "Microsoft.PowerAutomateDesktop"
            };
            return userApps.Any(ua => name.Contains(ua, StringComparison.OrdinalIgnoreCase));
        }

        private static System.Windows.Media.ImageSource? TryLoadUwpIcon(string logoPath, string installLocation)
        {
            try
            {
                // UWP apps may have scale-specific icons
                var dir = Path.GetDirectoryName(logoPath);
                var name = Path.GetFileNameWithoutExtension(logoPath);
                var ext = Path.GetExtension(logoPath);

                // Try scale variants in order of preference
                var candidates = new[]
                {
                    $"{name}.scale-200{ext}",
                    $"{name}.scale-150{ext}",
                    $"{name}.scale-100{ext}",
                    Path.GetFileName(logoPath)
                };

                foreach (var candidate in candidates)
                {
                    var fullPath = dir != null ? Path.Combine(dir, candidate) : candidate;
                    if (!Path.IsPathRooted(fullPath))
                        fullPath = Path.Combine(installLocation, fullPath);

                    if (File.Exists(fullPath))
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath);
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 64;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                }
            }
            catch { }

            return null;
        }

        private static async Task<string> RunPowerShellAsync(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -NonInteractive -Command \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return output;
            }
            catch (Exception ex)
            {
                Logger.Error("PowerShell execution failed", ex);
                return string.Empty;
            }
        }
    }
}
