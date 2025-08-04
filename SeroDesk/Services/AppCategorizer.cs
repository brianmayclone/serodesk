using System;
using System.Collections.Generic;
using System.Linq;
using SeroDesk.Models;

namespace SeroDesk.Services
{
    /// <summary>
    /// Intelligent application categorization service that automatically groups applications
    /// into logical categories based on their names, publishers, and characteristics.
    /// </summary>
    public class AppCategorizer
    {
        /// <summary>
        /// Category definitions with their patterns and rules
        /// </summary>
        private static readonly Dictionary<string, CategoryDefinition> Categories = new()
        {
            ["📄 Office"] = new CategoryDefinition
            {
                Name = "📄 Office",
                Keywords = new[] { "word", "excel", "powerpoint", "outlook", "onenote", "access", "publisher", "teams", "office", "365" },
                Publishers = new[] { "microsoft" },
                ExecutablePatterns = new[] { "winword", "excel", "powerpnt", "outlook", "onenote", "msaccess", "mspub", "teams" }
            },
            
            ["🛠️ Entwicklung"] = new CategoryDefinition
            {
                Name = "🛠️ Entwicklung",
                Keywords = new[] { "visual studio", "code", "git", "node", "npm", "python", "java", "compiler", "debug", "ide", "sdk", "framework" },
                Publishers = new[] { "microsoft", "jetbrains", "oracle", "eclipse" },
                ExecutablePatterns = new[] { "devenv", "code", "git", "node", "npm", "python", "java", "javac", "gcc" }
            },
            
            ["🎨 Design"] = new CategoryDefinition
            {
                Name = "🎨 Design",
                Keywords = new[] { "photoshop", "illustrator", "indesign", "premiere", "after effects", "figma", "sketch", "canva", "blender", "maya", "3ds max" },
                Publishers = new[] { "adobe", "autodesk", "figma", "sketch" },
                ExecutablePatterns = new[] { "photoshop", "illustrator", "indesign", "premiere", "afterfx", "figma", "sketch", "blender" }
            },
            
            ["⚙️ System"] = new CategoryDefinition
            {
                Name = "⚙️ System",
                Keywords = new[] { "control panel", "registry", "task manager", "device manager", "disk", "system", "admin", "configuration", "settings", "utility" },
                Publishers = new[] { "microsoft corporation" },
                ExecutablePatterns = new[] { "control", "regedit", "taskmgr", "devmgmt", "diskmgmt", "msconfig", "services" },
                SystemPaths = new[] { @"C:\Windows\System32", @"C:\Windows\SysWOW64", @"C:\Program Files\Windows" }
            },
            
            ["🌐 Internet"] = new CategoryDefinition
            {
                Name = "🌐 Internet",
                Keywords = new[] { "browser", "chrome", "firefox", "edge", "safari", "internet", "web", "ftp", "download", "torrent" },
                Publishers = new[] { "google", "mozilla", "microsoft", "opera" },
                ExecutablePatterns = new[] { "chrome", "firefox", "msedge", "iexplore", "opera", "brave" }
            },
            
            ["🎮 Gaming"] = new CategoryDefinition
            {
                Name = "🎮 Gaming",
                Keywords = new[] { "steam", "epic", "origin", "uplay", "battle.net", "game", "gaming", "launcher" },
                Publishers = new[] { "valve", "epic games", "electronic arts", "ubisoft", "blizzard" },
                ExecutablePatterns = new[] { "steam", "epicgameslauncher", "origin", "uplay", "battle.net" }
            },
            
            ["🎵 Multimedia"] = new CategoryDefinition
            {
                Name = "🎵 Multimedia",
                Keywords = new[] { "media player", "vlc", "spotify", "itunes", "music", "video", "audio", "player", "codec" },
                Publishers = new[] { "videolan", "spotify", "apple", "microsoft" },
                ExecutablePatterns = new[] { "vlc", "spotify", "itunes", "wmplayer", "groove" }
            },
            
            ["📝 Produktivität"] = new CategoryDefinition
            {
                Name = "📝 Produktivität",
                Keywords = new[] { "notepad", "calculator", "calendar", "mail", "note", "task", "todo", "productivity", "evernote", "notion" },
                Publishers = new[] { "microsoft", "evernote", "notion" },
                ExecutablePatterns = new[] { "notepad", "calc", "calendar", "mail", "evernote", "notion" }
            },
            
            ["🔧 Utilities"] = new CategoryDefinition
            {
                Name = "🔧 Utilities",
                Keywords = new[] { "zip", "rar", "7zip", "winrar", "archive", "compression", "backup", "antivirus", "cleaner", "optimizer" },
                Publishers = new[] { "winrar", "7-zip", "malwarebytes", "ccleaner" },
                ExecutablePatterns = new[] { "7z", "winrar", "zip", "backup", "cleaner" }
            }
        };
        
        /// <summary>
        /// Categorizes a list of applications into intelligent groups
        /// </summary>
        /// <param name="applications">List of applications to categorize</param>
        /// <returns>Dictionary of category names to lists of applications</returns>
        public static Dictionary<string, List<AppIcon>> CategorizeApplications(List<AppIcon> applications)
        {
            var categorizedApps = new Dictionary<string, List<AppIcon>>();
            var uncategorizedApps = new List<AppIcon>();
            
            foreach (var app in applications)
            {
                var category = DetermineCategory(app);
                if (!string.IsNullOrEmpty(category))
                {
                    if (!categorizedApps.ContainsKey(category))
                        categorizedApps[category] = new List<AppIcon>();
                    
                    categorizedApps[category].Add(app);
                }
                else
                {
                    uncategorizedApps.Add(app);
                }
            }
            
            // Add uncategorized apps to a general category if there are many
            if (uncategorizedApps.Count > 5)
            {
                categorizedApps["📱 Andere"] = uncategorizedApps;
            }
            else
            {
                // If few uncategorized apps, try to add them to existing categories or leave them ungrouped
                foreach (var app in uncategorizedApps)
                {
                    // Try to find the best matching category with lower threshold
                    var bestCategory = FindBestMatchingCategory(app, lowThreshold: true);
                    if (!string.IsNullOrEmpty(bestCategory))
                    {
                        if (!categorizedApps.ContainsKey(bestCategory))
                            categorizedApps[bestCategory] = new List<AppIcon>();
                        categorizedApps[bestCategory].Add(app);
                    }
                }
            }
            
            // Remove categories with only one app (not worth grouping)
            var categoriesToRemove = categorizedApps.Where(kvp => kvp.Value.Count < 2).Select(kvp => kvp.Key).ToList();
            foreach (var category in categoriesToRemove)
            {
                categorizedApps.Remove(category);
            }
            
            return categorizedApps;
        }
        
        /// <summary>
        /// Determines the most appropriate category for a single application
        /// </summary>
        private static string DetermineCategory(AppIcon app)
        {
            var appName = app.Name?.ToLowerInvariant() ?? "";
            var executablePath = app.ExecutablePath?.ToLowerInvariant() ?? "";
            var publisher = app.Publisher?.ToLowerInvariant() ?? "";
            
            foreach (var categoryKvp in Categories)
            {
                var category = categoryKvp.Key;
                var definition = categoryKvp.Value;
                
                // Check keywords in app name
                if (definition.Keywords.Any(keyword => appName.Contains(keyword.ToLowerInvariant())))
                    return category;
                
                // Check publisher
                if (!string.IsNullOrEmpty(publisher) && definition.Publishers.Any(pub => publisher.Contains(pub.ToLowerInvariant())))
                    return category;
                
                // Check executable patterns
                if (definition.ExecutablePatterns.Any(pattern => executablePath.Contains(pattern.ToLowerInvariant())))
                    return category;
                
                // Check system paths for system category
                if (definition.SystemPaths != null && definition.SystemPaths.Any(path => executablePath.StartsWith(path.ToLowerInvariant())))
                    return category;
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Finds the best matching category with relaxed criteria
        /// </summary>
        private static string FindBestMatchingCategory(AppIcon app, bool lowThreshold = false)
        {
            var appName = app.Name?.ToLowerInvariant() ?? "";
            var executablePath = app.ExecutablePath?.ToLowerInvariant() ?? "";
            
            // Try partial matches
            foreach (var categoryKvp in Categories)
            {
                var category = categoryKvp.Key;
                var definition = categoryKvp.Value;
                
                // Partial keyword matching
                if (definition.Keywords.Any(keyword => 
                    appName.Contains(keyword.Substring(0, Math.Min(keyword.Length, 4)).ToLowerInvariant())))
                    return category;
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// Creates AppGroup objects from categorized applications
        /// </summary>
        public static List<AppGroup> CreateAppGroups(Dictionary<string, List<AppIcon>> categorizedApps)
        {
            var appGroups = new List<AppGroup>();
            
            foreach (var categoryKvp in categorizedApps)
            {
                var categoryName = categoryKvp.Key;
                var apps = categoryKvp.Value;
                
                if (apps.Count < 2) continue; // Skip categories with less than 2 apps
                
                var appGroup = new AppGroup
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = categoryName
                };
                
                // Add apps to group and set group ID
                foreach (var app in apps.OrderBy(a => a.Name))
                {
                    appGroup.AddApp(app);
                    app.GroupId = appGroup.Id;
                }
                
                appGroups.Add(appGroup);
            }
            
            return appGroups.OrderBy(g => g.Name).ToList();
        }
    }
    
    /// <summary>
    /// Definition of a category with its matching criteria
    /// </summary>
    public class CategoryDefinition
    {
        public string Name { get; set; } = "";
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public string[] Publishers { get; set; } = Array.Empty<string>();
        public string[] ExecutablePatterns { get; set; } = Array.Empty<string>();
        public string[]? SystemPaths { get; set; }
    }
}