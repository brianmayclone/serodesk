using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SeroDesk.Services
{
    /// <summary>
    /// Simple file-based logger for production diagnostics.
    /// Writes to %LocalAppData%\SeroDesk\logs\serodesk.log with daily rotation.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new();
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SeroDesk", "logs");

        private static string CurrentLogPath => Path.Combine(LogDirectory, $"serodesk_{DateTime.Now:yyyyMMdd}.log");

        static Logger()
        {
            try { Directory.CreateDirectory(LogDirectory); } catch { }
        }

        public static void Info(string message, [CallerMemberName] string? caller = null)
        {
            Write("INFO", message, caller);
        }

        public static void Warn(string message, [CallerMemberName] string? caller = null)
        {
            Write("WARN", message, caller);
        }

        public static void Error(string message, Exception? ex = null, [CallerMemberName] string? caller = null)
        {
            var msg = ex != null ? $"{message}: {ex.Message}" : message;
            Write("ERROR", msg, caller);
        }

        public static void Debug(string message, [CallerMemberName] string? caller = null)
        {
#if DEBUG
            Write("DEBUG", message, caller);
#endif
        }

        private static void Write(string level, string message, string? caller)
        {
            try
            {
                var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{caller}] {message}";
                System.Diagnostics.Debug.WriteLine(line);

                lock (_lock)
                {
                    File.AppendAllText(CurrentLogPath, line + Environment.NewLine);
                }
            }
            catch { /* Never let logging crash the app */ }
        }

        /// <summary>
        /// Deletes log files older than the specified number of days.
        /// </summary>
        public static void CleanOldLogs(int keepDays = 7)
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-keepDays);
                foreach (var file in Directory.GetFiles(LogDirectory, "serodesk_*.log"))
                {
                    if (File.GetCreationTime(file) < cutoff)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { }
        }
    }
}
