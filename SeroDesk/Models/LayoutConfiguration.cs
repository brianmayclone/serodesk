using System;
using System.Collections.Generic;

namespace SeroDesk.Models
{
    /// <summary>
    /// Represents the saved layout configuration for the LaunchPad
    /// </summary>
    public class LayoutConfiguration
    {
        public int Version { get; set; } = 1;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public List<SavedAppPosition> AppPositions { get; set; } = new List<SavedAppPosition>();
        public List<SavedGroup> Groups { get; set; } = new List<SavedGroup>();
        public bool IsFirstRun { get; set; } = true;
        public List<string> ToolPatterns { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a saved app position in the grid
    /// </summary>
    public class SavedAppPosition
    {
        public string AppId { get; set; } = string.Empty;
        public string AppPath { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public string? GroupId { get; set; }
    }

    /// <summary>
    /// Represents a saved group
    /// </summary>
    public class SavedGroup
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> AppIds { get; set; } = new List<string>();
        public int PageIndex { get; set; }
        public int Row { get; set; }
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