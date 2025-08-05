using System;
using System.Collections.Generic;
using System.Linq;
using SeroDesk.Models;

namespace SeroDesk.Services
{
    /// <summary>
    /// Provides intelligent application categorization services that automatically group applications
    /// into logical categories based on their names, publishers, executable paths, and other characteristics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The AppCategorizer uses a rule-based approach to categorize applications into predefined groups
    /// such as Office, Development, Design, Gaming, etc. It analyzes multiple application properties
    /// including name keywords, publisher information, executable patterns, and installation paths.
    /// </para>
    /// <para>
    /// Categories are defined with multiple matching criteria to ensure accurate classification:
    /// <list type="bullet">
    /// <item>Keywords: Partial matches in application names</item>
    /// <item>Publishers: Company/organization names</item>
    /// <item>Executable patterns: Common executable name patterns</item>
    /// <item>System paths: Installation directory patterns (for system apps)</item>
    /// </list>
    /// </para>
    /// <para>
    /// The service also includes intelligent fallback mechanisms for uncategorized applications
    /// and removes categories with insufficient applications to maintain UI cleanliness.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var applications = GetAllApplications();
    /// var categorizedApps = AppCategorizer.CategorizeApplications(applications);
    /// var appGroups = AppCategorizer.CreateAppGroups(categorizedApps);
    /// </code>
    /// </example>
    public class AppCategorizer
    {
        /// <summary>
        /// Dictionary containing all category definitions with their matching patterns and rules.
        /// </summary>
        /// <remarks>
        /// Each category is identified by its display name (including emoji) and contains
        /// comprehensive matching criteria for accurate application classification.
        /// Categories are processed in the order they appear in this dictionary.
        /// </remarks>
        private static readonly Dictionary<string, CategoryDefinition> Categories = new()
        {
            ["📄 Office"] = new CategoryDefinition
            {
                Name = "📄 Office",
                Keywords = new[] { "word", "excel", "powerpoint", "outlook", "onenote", "access", "publisher", "teams", "office", "365" },
                Publishers = new[] { "microsoft" },
                ExecutablePatterns = new[] { "winword", "excel", "powerpnt", "outlook", "onenote", "msaccess", "mspub", "teams" }
            },
            
            ["🛠️ Development"] = new CategoryDefinition
            {
                Name = "🛠️ Development",
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
            
            ["📝 Productivity"] = new CategoryDefinition
            {
                Name = "📝 Productivity",
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
        /// Categorizes a list of applications into intelligent groups based on predefined rules.
        /// </summary>
        /// <param name="applications">The list of applications to categorize.</param>
        /// <returns>
        /// A dictionary where keys are category names and values are lists of applications belonging to that category.
        /// Only categories with at least 2 applications are included in the result.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The categorization process follows these steps:
        /// <list type="number">
        /// <item>Each application is tested against all category definitions</item>
        /// <item>Applications that don't match any category are collected separately</item>
        /// <item>If there are many uncategorized apps (>5), they form an "Other" category</item>
        /// <item>Otherwise, uncategorized apps are retried with relaxed matching criteria</item>
        /// <item>Categories with fewer than 2 applications are removed</item>
        /// </list>
        /// </para>
        /// <para>
        /// This approach ensures that only meaningful groupings are presented to the user
        /// while maximizing the number of applications that get properly categorized.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="applications"/> is null.</exception>
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
                categorizedApps["📱 Other"] = uncategorizedApps;
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
        /// Determines the most appropriate category for a single application using strict matching criteria.
        /// </summary>
        /// <param name="app">The application to categorize.</param>
        /// <returns>
        /// The category name if a match is found; otherwise, an empty string.
        /// </returns>
        /// <remarks>
        /// The method tests each category definition in order, checking:
        /// <list type="number">
        /// <item>Keywords in the application name (case-insensitive)</item>
        /// <item>Publisher name matches</item>
        /// <item>Executable path patterns</item>
        /// <item>System installation paths (for system category only)</item>
        /// </list>
        /// The first matching category is returned, so category order in the definitions matters.
        /// </remarks>
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
        /// Finds the best matching category using relaxed matching criteria for previously uncategorized applications.
        /// </summary>
        /// <param name="app">The application to categorize.</param>
        /// <param name="lowThreshold">If true, uses even more relaxed matching criteria (currently unused).</param>
        /// <returns>
        /// The category name if a partial match is found; otherwise, an empty string.
        /// </returns>
        /// <remarks>
        /// This method is used as a fallback for applications that didn't match any category with strict criteria.
        /// It uses partial keyword matching (first 4 characters) to find potential category matches.
        /// This helps capture applications with slight naming variations or abbreviations.
        /// </remarks>
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
        /// Creates AppGroup objects from a dictionary of categorized applications.
        /// </summary>
        /// <param name="categorizedApps">
        /// Dictionary containing category names as keys and lists of applications as values.
        /// </param>
        /// <returns>
        /// A list of AppGroup objects, sorted alphabetically by group name.
        /// Groups with fewer than 2 applications are automatically excluded.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Each AppGroup is created with:
        /// <list type="bullet">
        /// <item>A unique identifier (GUID)</item>
        /// <item>The category name as the group name</item>
        /// <item>All applications in the category, sorted alphabetically</item>
        /// <item>Each application's GroupId property set to the group's ID</item>
        /// </list>
        /// </para>
        /// <para>
        /// This method ensures data consistency by properly linking applications to their parent groups.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="categorizedApps"/> is null.</exception>
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
    /// Defines a category with its matching criteria for application classification.
    /// </summary>
    /// <remarks>
    /// A CategoryDefinition contains all the rules and patterns used to determine
    /// whether an application belongs to a specific category. Multiple criteria types
    /// are supported to ensure accurate and comprehensive matching.
    /// </remarks>
    public class CategoryDefinition
    {
        /// <summary>
        /// Gets or sets the display name of the category.
        /// </summary>
        /// <value>
        /// The category name, typically including an emoji prefix for visual identification.
        /// </value>
        /// <example>"🛠️ Development"</example>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the keywords to search for in application names.
        /// </summary>
        /// <value>
        /// An array of keywords that, if found in an application's name (case-insensitive),
        /// will classify the application into this category.
        /// </value>
        /// <remarks>
        /// Keywords are matched using case-insensitive substring comparison.
        /// Shorter, more specific keywords should be preferred to avoid false positives.
        /// </remarks>
        /// <example>["visual studio", "code", "git", "compiler"]</example>
        public string[] Keywords { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// Gets or sets the publisher names associated with this category.
        /// </summary>
        /// <value>
        /// An array of publisher/company names that, if matched against an application's
        /// publisher information, will classify the application into this category.
        /// </value>
        /// <remarks>
        /// Publisher matching is case-insensitive and uses substring comparison.
        /// This is useful for grouping all applications from major software vendors.
        /// </remarks>
        /// <example>["microsoft", "adobe", "jetbrains"]</example>
        public string[] Publishers { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// Gets or sets the executable name patterns for this category.
        /// </summary>
        /// <value>
        /// An array of executable name patterns that, if found in an application's
        /// executable path, will classify the application into this category.
        /// </value>
        /// <remarks>
        /// Patterns are matched against the executable file name (case-insensitive).
        /// This is particularly useful for command-line tools and applications with
        /// technical executable names that differ from their display names.
        /// </remarks>
        /// <example>["devenv", "code", "git", "npm"]</example>
        public string[] ExecutablePatterns { get; set; } = Array.Empty<string>();
        
        /// <summary>
        /// Gets or sets the system installation paths for this category.
        /// </summary>
        /// <value>
        /// An array of directory paths that, if an application is installed within them,
        /// will classify the application into this category. Can be null if not applicable.
        /// </value>
        /// <remarks>
        /// <para>
        /// This property is primarily used for the System category to identify
        /// applications installed in Windows system directories.
        /// </para>
        /// <para>
        /// Path matching is case-insensitive and uses prefix comparison.
        /// </para>
        /// </remarks>
        /// <example>["C:\\Windows\\System32", "C:\\Windows\\SysWOW64"]</example>
        public string[]? SystemPaths { get; set; }
    }
}