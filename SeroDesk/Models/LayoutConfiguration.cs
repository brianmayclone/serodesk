using System;
using System.Collections.Generic;

namespace SeroDesk.Models
{
    /// <summary>
    /// Represents the complete saved layout configuration for the LaunchPad interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class serves as the root configuration object that gets serialized to JSON
    /// for persistent storage of the user's LaunchPad layout. It contains:
    /// <list type="bullet">
    /// <item>Individual application positions within the grid</item>
    /// <item>Group definitions and their contained applications</item>
    /// <item>Configuration metadata (version, last modified date)</item>
    /// <item>First-run detection for automatic categorization</item>
    /// <item>Tool patterns for intelligent categorization</item>
    /// </list>
    /// </para>
    /// <para>
    /// The configuration is automatically loaded at startup and saved whenever
    /// the user makes changes to the layout, ensuring persistence across sessions.
    /// </para>
    /// </remarks>
    public class LayoutConfiguration
    {
        /// <summary>
        /// Gets or sets the configuration format version for future migration compatibility.
        /// </summary>
        public int Version { get; set; } = 1;
        
        /// <summary>
        /// Gets or sets the timestamp when this configuration was last modified.
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Gets or sets the list of saved application positions in the LaunchPad grid.
        /// </summary>
        public List<SavedAppPosition> AppPositions { get; set; } = new List<SavedAppPosition>();
        
        /// <summary>
        /// Gets or sets the list of application groups created by the user or auto-categorization.
        /// </summary>
        public List<SavedGroup> Groups { get; set; } = new List<SavedGroup>();
        
        /// <summary>
        /// Gets or sets a value indicating whether this is the first run of the application.
        /// </summary>
        /// <remarks>
        /// When true, the application will perform automatic categorization of installed applications.
        /// This flag is set to false after the initial setup is complete.
        /// </remarks>
        public bool IsFirstRun { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the list of tool patterns used for automatic application categorization.
        /// </summary>
        public List<string> ToolPatterns { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents the saved position and metadata for an individual application in the LaunchPad grid.
    /// </summary>
    /// <remarks>
    /// This class stores all the information needed to restore an application's exact position
    /// in the LaunchPad layout, including its page, row, column, and group membership.
    /// </remarks>
    public class SavedAppPosition
    {
        /// <summary>
        /// Gets or sets the unique identifier of the application.
        /// </summary>
        public string AppId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the full path to the application's executable file.
        /// </summary>
        /// <remarks>
        /// This path is used for application identification and launch operations.
        /// </remarks>
        public string AppPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the zero-based page index where this application is located.
        /// </summary>
        public int PageIndex { get; set; }
        
        /// <summary>
        /// Gets or sets the row position within the page grid (0-based).
        /// </summary>
        public int Row { get; set; }
        
        /// <summary>
        /// Gets or sets the column position within the page grid (0-based).
        /// </summary>
        public int Column { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the group this application belongs to, or null if ungrouped.
        /// </summary>
        public string? GroupId { get; set; }
    }

    /// <summary>
    /// Represents a saved application group with its position and member applications.
    /// </summary>
    /// <remarks>
    /// This class stores the persistent state of an application group, including
    /// its display name, position in the grid, and the list of applications it contains.
    /// </remarks>
    public class SavedGroup
    {
        /// <summary>
        /// Gets or sets the unique identifier for this group.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the display name of the group.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the list of application IDs that belong to this group.
        /// </summary>
        public List<string> AppIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the zero-based page index where this group is displayed.
        /// </summary>
        public int PageIndex { get; set; }
        
        /// <summary>
        /// Gets or sets the row position of the group within the page grid (0-based).
        /// </summary>
        public int Row { get; set; }
        
        /// <summary>
        /// Gets or sets the column position of the group within the page grid (0-based).
        /// </summary>
        public int Column { get; set; }
    }

    /// <summary>
    /// Common tool patterns for automatic categorization
    /// </summary>
    public static class ToolPatterns
    {
        public static readonly List<string> SystemTools = new List<string>
        {
            // Windows system tools
            "regedit", "cmd", "powershell", "mmc", "services", "eventvwr", "perfmon",
            "resmon", "taskmgr", "msconfig", "dxdiag", "msinfo32", "devmgmt", "diskmgmt",
            "compmgmt", "gpedit", "secpol", "lusrmgr", "certmgr", "hdwwiz", "sysdm",
            "netplwiz", "control", "wf.msc", "fsmgmt", "printmanagement", "wbadmin",
            "recoverydrive", "sdclt", "rstrui", "msdt", "mdsched", "chkdsk", "sfc",
            "dism", "diskpart", "bcdedit", "bcdboot", "bootrec", "netsh", "ipconfig",
            "nslookup", "ping", "tracert", "pathping", "arp", "route", "netstat",
            "hostname", "systeminfo", "whoami", "wmic", "fsutil", "cipher", "compact",
            "defrag", "cleanmgr", "wsreset", "winver", "optionalfeatures", "appwiz",
            
            // Development tools
            "git", "node", "npm", "yarn", "python", "pip", "java", "javac", "maven",
            "gradle", "dotnet", "nuget", "gcc", "g++", "make", "cmake", "cargo",
            "rustc", "go", "ruby", "gem", "perl", "php", "composer", "docker",
            "kubectl", "terraform", "ansible", "vagrant", "virtualbox", "vmware",
            
            // Command line tools
            "curl", "wget", "ssh", "scp", "sftp", "ftp", "telnet", "putty", "winscp",
            "rsync", "robocopy", "xcopy", "tar", "7z", "rar", "unzip", "zip",
            
            // Database tools
            "mysql", "psql", "sqlcmd", "sqlite3", "mongo", "redis-cli",
            
            // Text processing
            "findstr", "grep", "sed", "awk", "sort", "uniq", "head", "tail", "more",
            "less", "cat", "echo", "type", "notepad", "write", "wordpad"
        };

        public static readonly List<string> UtilityPatterns = new List<string>
        {
            // Common utility patterns
            "config", "setup", "install", "uninstall", "update", "updater", "helper",
            "daemon", "service", "server", "client", "agent", "monitor", "watcher",
            "manager", "console", "terminal", "shell", "prompt", "cli", "cmd",
            
            // Tool suffixes
            "tool", "util", "utility", "admin", "administrator", "settings", "options",
            "preferences", "configuration", "properties", "info", "information",
            
            // Development patterns
            "compiler", "interpreter", "debugger", "profiler", "analyzer", "linter",
            "formatter", "bundler", "packer", "builder", "runner", "executor"
        };

        /// <summary>
        /// Determines if an application is likely a tool/utility
        /// </summary>
        public static bool IsTool(string appName, string? appPath = null)
        {
            if (string.IsNullOrEmpty(appName))
                return false;

            var lowerName = appName.ToLowerInvariant();
            var lowerPath = appPath?.ToLowerInvariant() ?? string.Empty;

            // Check exact matches first
            foreach (var tool in SystemTools)
            {
                if (lowerName == tool || lowerName.StartsWith(tool + ".") || 
                    lowerName.EndsWith("." + tool))
                    return true;
                
                if (!string.IsNullOrEmpty(lowerPath) && lowerPath.Contains("\\" + tool + "."))
                    return true;
            }

            // Check patterns
            foreach (var pattern in UtilityPatterns)
            {
                if (lowerName.Contains(pattern))
                    return true;
            }

            // Check common paths that indicate tools
            if (!string.IsNullOrEmpty(lowerPath))
            {
                if (lowerPath.Contains("\\system32\\") ||
                    lowerPath.Contains("\\syswow64\\") ||
                    lowerPath.Contains("\\windowspowershell\\") ||
                    lowerPath.Contains("\\git\\") ||
                    lowerPath.Contains("\\nodejs\\") ||
                    lowerPath.Contains("\\python") ||
                    lowerPath.Contains("\\jdk") ||
                    lowerPath.Contains("\\jre") ||
                    lowerPath.Contains("\\sdk") ||
                    lowerPath.Contains("\\tools\\") ||
                    lowerPath.Contains("\\utilities\\") ||
                    lowerPath.Contains("\\bin\\") ||
                    lowerPath.Contains("\\sbin\\"))
                {
                    return true;
                }
            }

            // Check if it's a console application (no window)
            if (lowerName.EndsWith(".com") || lowerName.EndsWith(".bat") || 
                lowerName.EndsWith(".cmd") || lowerName.EndsWith(".ps1") ||
                lowerName.EndsWith(".sh") || lowerName.EndsWith(".bash"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if an application is a major/primary application
        /// </summary>
        public static bool IsMajorApp(string appName, string? appPath = null)
        {
            var lowerName = appName.ToLowerInvariant();
            
            // Major apps that should always be visible
            var majorApps = new List<string>
            {
                "chrome", "firefox", "edge", "opera", "brave", "safari",
                "outlook", "word", "excel", "powerpoint", "onenote", "teams",
                "skype", "zoom", "discord", "slack", "telegram", "whatsapp",
                "spotify", "itunes", "vlc", "windows media player",
                "photoshop", "illustrator", "premiere", "after effects",
                "visual studio", "vs code", "intellij", "eclipse", "android studio",
                "steam", "epic games", "origin", "uplay", "battle.net",
                "obs", "streamlabs", "nvidia", "amd", "logitech",
                "file explorer", "explorer", "settings", "store", "mail", "calendar"
            };

            foreach (var app in majorApps)
            {
                if (lowerName.Contains(app))
                    return true;
            }

            return false;
        }
    }
}