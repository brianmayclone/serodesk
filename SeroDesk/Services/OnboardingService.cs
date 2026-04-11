using System;
using System.IO;

namespace SeroDesk.Services
{
    /// <summary>
    /// Manages the first-run onboarding experience.
    /// Tracks whether the user has completed the initial tutorial.
    /// </summary>
    public static class OnboardingService
    {
        private static readonly string CompletedFlagPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SeroDesk", "onboarding_complete");

        /// <summary>
        /// Returns true if the onboarding has already been completed.
        /// </summary>
        public static bool IsComplete => File.Exists(CompletedFlagPath);

        /// <summary>
        /// Marks the onboarding as complete so it won't show again.
        /// </summary>
        public static void MarkComplete()
        {
            try
            {
                var dir = Path.GetDirectoryName(CompletedFlagPath)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(CompletedFlagPath, DateTime.Now.ToString("O"));
            }
            catch { }
        }

        /// <summary>
        /// Shows the onboarding window if this is the first run.
        /// Call this after MainWindow is loaded.
        /// </summary>
        public static void ShowIfNeeded()
        {
            if (!IsComplete)
            {
                var window = new Views.OnboardingWindow();
                window.Show();
            }
        }
    }
}
