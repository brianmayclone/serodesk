using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SeroDesk.Platform;
using SeroDesk.Views;

namespace SeroDesk.Services
{
    /// <summary>
    /// Automates a repeatable desktop capture flow for diagnosing Dock and StatusBar layout issues.
    /// </summary>
    public static class DebugCaptureService
    {
        private const int InitialUiSettleDelayMs = 4500;
        private const int LayoutSettleDelayMs = 900;
        private const int MaxWaitForWindowsMs = 15000;
        private const int PollDelayMs = 100;
        private const string LatestCaptureFileName = "latest_layout_capture.png";
        private const string LatestMetadataFileName = "latest_layout_capture.txt";

        public static async Task<string?> RunAsync()
        {
            try
            {
                Logger.Info("Starting debug capture workflow");

                var mainWindow = await WaitForWindowAsync<MainWindow>(nameof(MainWindow));
                var dockWindow = await WaitForWindowAsync<DockWindow>(nameof(DockWindow));
                var statusBarWindow = await WaitForWindowAsync<StatusBarWindow>(nameof(StatusBarWindow));

                await Task.Delay(InitialUiSettleDelayMs);

                dockWindow.PrepareForDebugCapture();
                statusBarWindow.PrepareForDebugCapture();
                mainWindow.PrepareForDebugCapture();
                await Task.Delay(LayoutSettleDelayMs);

                MinimizeForeignWindows();
                await Task.Delay(LayoutSettleDelayMs);

                dockWindow.PrepareForDebugCapture();
                statusBarWindow.PrepareForDebugCapture();
                mainWindow.PrepareForDebugCapture();
                await Task.Delay(LayoutSettleDelayMs);

                var capture = SaveScreenCapture();
                Logger.Info($"Debug capture saved to latest file: {capture.LatestPath}");
                Logger.Info($"Debug capture saved to timestamped file: {capture.TimestampedPath}");

                return capture.LatestPath;
            }
            catch (Exception ex)
            {
                Logger.Error("Debug capture workflow failed", ex);
                return null;
            }
        }

        private static async Task<T> WaitForWindowAsync<T>(string windowName) where T : Window
        {
            for (var elapsed = 0; elapsed < MaxWaitForWindowsMs; elapsed += PollDelayMs)
            {
                if (Application.Current.Windows.OfType<T>().FirstOrDefault(window => window.IsLoaded) is T window)
                {
                    Logger.Info($"{windowName} is ready for debug capture");
                    return window;
                }

                await Task.Delay(PollDelayMs);
            }

            throw new TimeoutException($"Timed out waiting for {windowName} to load.");
        }

        private static void MinimizeForeignWindows()
        {
            var currentProcessId = (uint)Environment.ProcessId;

            NativeMethods.EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    if (!NativeMethods.IsWindowVisible(hWnd))
                    {
                        return true;
                    }

                    NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                    if (processId == currentProcessId)
                    {
                        return true;
                    }

                    var exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                    if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0)
                    {
                        return true;
                    }

                    var titleLength = NativeMethods.GetWindowTextLength(hWnd);
                    if (titleLength <= 0)
                    {
                        return true;
                    }

                    var titleBuilder = new System.Text.StringBuilder(titleLength + 1);
                    NativeMethods.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
                    var title = titleBuilder.ToString();

                    if (ShouldIgnoreWindow(title))
                    {
                        return true;
                    }

                    Logger.Info($"Minimizing external window '{title}'");
                    NativeMethods.ShowWindow(hWnd, NativeMethods.SW_MINIMIZE);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to minimize window {hWnd}", ex);
                }

                return true;
            }, IntPtr.Zero);
        }

        private static bool ShouldIgnoreWindow(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            return title.Equals("Program Manager", StringComparison.OrdinalIgnoreCase) ||
                   title.Contains("Task View", StringComparison.OrdinalIgnoreCase) ||
                   title.Contains("Start", StringComparison.OrdinalIgnoreCase) ||
                   title.Contains("Windows Input Experience", StringComparison.OrdinalIgnoreCase);
        }

        private static CapturePaths SaveScreenCapture()
        {
            var diagnosticsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SeroDesk",
                "diagnostics");

            Directory.CreateDirectory(diagnosticsDirectory);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var timestampedPath = Path.Combine(diagnosticsDirectory, $"layout_capture_{timestamp}.png");
            var latestPath = Path.Combine(diagnosticsDirectory, LatestCaptureFileName);

            var left = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
            var top = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
            var width = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN));
            var height = Math.Max(1, NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN));

            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    left,
                    top,
                    0,
                    0,
                    new System.Drawing.Size(width, height),
                    CopyPixelOperation.SourceCopy);
                bitmap.Save(timestampedPath, ImageFormat.Png);
                bitmap.Save(latestPath, ImageFormat.Png);
            }

            var metadataLines = new[]
            {
                $"CapturedAt={DateTime.Now:O}",
                $"LatestPath={latestPath}",
                $"TimestampedPath={timestampedPath}",
                $"Bounds={left},{top},{width},{height}",
                $"Mode=debug-capture"
            };

            File.WriteAllLines(Path.ChangeExtension(timestampedPath, ".txt"), metadataLines);
            File.WriteAllLines(Path.Combine(diagnosticsDirectory, LatestMetadataFileName), metadataLines);

            return new CapturePaths(latestPath, timestampedPath);
        }

        private readonly record struct CapturePaths(string LatestPath, string TimestampedPath);
    }
}
