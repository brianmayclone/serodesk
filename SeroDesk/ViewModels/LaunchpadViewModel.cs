using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using Newtonsoft.Json;
using SeroDesk.Models;
using SeroDesk.Services;
using SeroDesk.Platform;

namespace SeroDesk.ViewModels
{
    public class LaunchpadViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<AppIcon> _allApplications;
        private ObservableCollection<AppIcon> _filteredApplications;
        private ObservableCollection<object> _displayItems;
        private ObservableCollection<AppGroup> _appGroups;
        private string _searchText = string.Empty;
        private ImageBrush? _currentWallpaper;
        private int _currentPage = 0;
        private int _itemsPerPage = 35; // 7x5 grid = 35 items per page (reduced for larger icons)
        private ObservableCollection<ObservableCollection<object>> _pages;
        private LayoutConfiguration? _layoutConfig;
        private string _configPath;
        
        public ObservableCollection<AppIcon> AllApplications
        {
            get => _allApplications;
            set { _allApplications = value; OnPropertyChanged(); }
        }
        
        public ObservableCollection<AppIcon> FilteredApplications
        {
            get => _filteredApplications;
            set { _filteredApplications = value; OnPropertyChanged(); }
        }
        
        public ObservableCollection<object> DisplayItems
        {
            get => _displayItems;
            set { _displayItems = value; OnPropertyChanged(); }
        }
        
        public ObservableCollection<AppGroup> AppGroups
        {
            get => _appGroups;
            set { _appGroups = value; OnPropertyChanged(); }
        }
        
        public ImageBrush? CurrentWallpaper
        {
            get => _currentWallpaper;
            set { _currentWallpaper = value; OnPropertyChanged(); }
        }
        
        public string SearchText
        {
            get => _searchText;
            set 
            { 
                _searchText = value; 
                OnPropertyChanged();
                FilterApplications(value);
            }
        }
        
        public ObservableCollection<ObservableCollection<object>> Pages
        {
            get => _pages;
            set { _pages = value; OnPropertyChanged(); }
        }
        
        public int CurrentPage
        {
            get => _currentPage;
            set 
            { 
                _currentPage = value; 
                OnPropertyChanged(); 
                UpdateCurrentPageItems();
            }
        }
        
        public int TotalPages => _pages?.Count ?? 1;
        
        public bool HasMultiplePages => TotalPages > 1;
        
        public LaunchpadViewModel()
        {
            _allApplications = new ObservableCollection<AppIcon>();
            _filteredApplications = new ObservableCollection<AppIcon>();
            _displayItems = new ObservableCollection<object>();
            _appGroups = new ObservableCollection<AppGroup>();
            _pages = new ObservableCollection<ObservableCollection<object>>();
            
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SeroDesk", "launchpad_config.json"); // Eindeutiger Name um Konflikte zu vermeiden
            
            // Load Windows wallpaper
            LoadWallpaper();
            CleanupLegacyFiles(); // Remove old desktop_layout.json files
            LoadLayoutConfiguration();
        }
        
        public async Task LoadAllApplicationsAsync()
        {
            try
            {
                AllApplications.Clear();
                FilteredApplications.Clear();
                
                var allApps = new List<AppIcon>();
                
                // First, add some test apps to verify UI is working
                var testApps = new[]
                {
                    new AppIcon { Name = "Notepad", ExecutablePath = @"C:\Windows\System32\notepad.exe" },
                    new AppIcon { Name = "Calculator", ExecutablePath = @"C:\Windows\System32\calc.exe" },
                    new AppIcon { Name = "Paint", ExecutablePath = @"C:\Windows\System32\mspaint.exe" },
                    new AppIcon { Name = "Command Prompt", ExecutablePath = @"C:\Windows\System32\cmd.exe" }
                };
                
                foreach (var testApp in testApps)
                {
                    if (System.IO.File.Exists(testApp.ExecutablePath))
                    {
                        testApp.Id = testApp.ExecutablePath;
                        testApp.IconImage = IconExtractor.GetIconForFile(testApp.ExecutablePath, true);
                        allApps.Add(testApp);
                    }
                }
                
                // Now try to load all installed applications
                try
                {
                    var scannedApps = await ApplicationScanner.ScanInstalledApplicationsAsync();
                    foreach (var app in scannedApps)
                    {
                        if (app.Id == null) app.Id = app.ExecutablePath ?? app.Name;
                        allApps.Add(app);
                    }
                }
                catch (Exception scanEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Scanner error: {scanEx.Message}");
                }
                
                // Check if this is first run or we have a saved layout
                System.Diagnostics.Debug.WriteLine($"LoadAllApplicationsAsync: Checking layout config - IsFirstRun: {_layoutConfig?.IsFirstRun}, Config null: {_layoutConfig == null}");
                
                if (_layoutConfig?.IsFirstRun == true || _layoutConfig == null)
                {
                    System.Diagnostics.Debug.WriteLine("LoadAllApplicationsAsync: First run detected - auto-categorizing apps");
                    // Auto-categorize tools on first run
                    await AutoCategorizeApps(allApps);
                    
                    if (_layoutConfig != null)
                    {
                        _layoutConfig.IsFirstRun = false;
                        System.Diagnostics.Debug.WriteLine("LoadAllApplicationsAsync: Setting IsFirstRun = false and saving config");
                        SaveLayoutConfiguration();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("LoadAllApplicationsAsync: WARNING - _layoutConfig is null after auto-categorization!");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LoadAllApplicationsAsync: Existing layout detected - restoring saved layout");
                    // Restore saved layout
                    RestoreAppLayout(allApps);
                }
                
                // Add all non-grouped apps to FilteredApplications
                foreach (var app in AllApplications)
                {
                    FilteredApplications.Add(app);
                }
                
                UpdateDisplayItems();
                
                // Notify UI that all applications have been loaded
                OnPropertyChanged(nameof(AllApplications));
                OnPropertyChanged(nameof(AppGroups));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading apps: {ex.Message}");
            }
        }
        
        public async void LoadAllApplications()
        {
            await LoadAllApplicationsAsync();
        }
        
        private void LoadWallpaper()
        {
            CurrentWallpaper = WallpaperService.Instance.GetCurrentWallpaper();
        }
        
        public void RefreshWallpaper()
        {
            WallpaperService.Instance.ClearCache();
            LoadWallpaper();
        }
        
        // Removed unused search timer fields - search is now immediate
        
        public void FilterApplications(string searchText)
        {
            System.Diagnostics.Debug.WriteLine($"FilterApplications called with: '{searchText}'");
            // Perform search immediately for better user experience
            // No debouncing needed since we're now using proper caching
            PerformSearch(searchText);
        }
        
        private void PerformSearch(string searchText)
        {
            System.Diagnostics.Debug.WriteLine($"PerformSearch called with: '{searchText}' (IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(searchText)})");
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                System.Diagnostics.Debug.WriteLine("Empty search - showing organized view with groups");
                // Show organized view with groups
                UpdateDisplayItems();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Non-empty search '{searchText}' - showing flat search results");
                // Show flat search results - ONLY individual apps
                var previousCount = DisplayItems.Count;
                DisplayItems.Clear();
                System.Diagnostics.Debug.WriteLine($"Cleared DisplayItems (was {previousCount} items)");
                
                var searchLower = searchText.ToLowerInvariant();
                var allMatchingApps = new List<AppIcon>();
                
                System.Diagnostics.Debug.WriteLine($"Search '{searchText}': Searching in {AllApplications.Count} ungrouped apps and {AppGroups.Sum(g => g.Apps.Count)} grouped apps");
                
                // 1. Search in ungrouped apps (AllApplications contains ungrouped apps only)
                var ungroupedMatches = AllApplications
                    .Where(app => string.IsNullOrEmpty(app.GroupId) && app.Name.ToLowerInvariant().Contains(searchLower))
                    .ToList();
                allMatchingApps.AddRange(ungroupedMatches);
                System.Diagnostics.Debug.WriteLine($"Found {ungroupedMatches.Count} ungrouped apps matching '{searchText}': {string.Join(", ", ungroupedMatches.Select(a => a.Name))}");
                
                // 2. Search in ALL apps within groups - show them as individual apps
                var groupedMatches = AppGroups
                    .SelectMany(group => group.Apps)
                    .Where(app => app.Name.ToLowerInvariant().Contains(searchLower))
                    .ToList();
                allMatchingApps.AddRange(groupedMatches);
                System.Diagnostics.Debug.WriteLine($"Found {groupedMatches.Count} grouped apps matching '{searchText}': {string.Join(", ", groupedMatches.Select(a => a.Name))}");
                
                // 3. Also include apps from groups where the GROUP NAME matches
                var appsFromNamedGroups = AppGroups
                    .Where(group => group.Name.ToLowerInvariant().Contains(searchLower))
                    .SelectMany(group => group.Apps)
                    .Where(app => !app.Name.ToLowerInvariant().Contains(searchLower)) // Don't duplicate apps already found
                    .ToList();
                allMatchingApps.AddRange(appsFromNamedGroups);
                System.Diagnostics.Debug.WriteLine($"Found {appsFromNamedGroups.Count} additional apps from groups named '{searchText}': {string.Join(", ", appsFromNamedGroups.Select(a => a.Name))}");
                
                // Sort all results and add to DisplayItems
                var sortedResults = allMatchingApps
                    .Distinct() // Remove duplicates
                    .OrderBy(app => app.Name)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"Adding {sortedResults.Count} sorted results to DisplayItems");
                foreach (var app in sortedResults)
                {
                    DisplayItems.Add(app);
                    System.Diagnostics.Debug.WriteLine($"  Added: {app.Name}");
                }
                
                System.Diagnostics.Debug.WriteLine($"Search '{searchText}': Total {DisplayItems.Count} individual apps shown as results");
            }
            
            // Force UI update after search
            System.Diagnostics.Debug.WriteLine($"Triggering PropertyChanged for DisplayItems (count: {DisplayItems.Count})");
            OnPropertyChanged(nameof(DisplayItems));
        }
        
        private void UpdateDisplayItems()
        {
            // Organized view with groups and paging (no search)
            CreatePages();
            UpdateCurrentPageItems();
        }
        
        private void CreatePages()
        {
            Pages.Clear();
            
            var allItems = new List<object>();
            
            // Add groups first
            allItems.AddRange(AppGroups);
            
            // Add ungrouped apps
            allItems.AddRange(AllApplications.Where(app => string.IsNullOrEmpty(app.GroupId)));
            
            System.Diagnostics.Debug.WriteLine($"CreatePages: {allItems.Count} total items, {AllApplications.Count} apps, {AppGroups.Count} groups");
            
            // Split into pages
            for (int i = 0; i < allItems.Count; i += _itemsPerPage)
            {
                var pageItems = allItems.Skip(i).Take(_itemsPerPage).ToList();
                var page = new ObservableCollection<object>(pageItems);
                Pages.Add(page);
            }
            
            // Ensure we have at least one page
            if (Pages.Count == 0)
            {
                Pages.Add(new ObservableCollection<object>());
            }
            
            // Reset current page if necessary
            if (CurrentPage >= Pages.Count)
            {
                CurrentPage = 0;
            }
            
            System.Diagnostics.Debug.WriteLine($"CreatePages: Created {Pages.Count} pages");
            
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(HasMultiplePages));
            OnPropertyChanged(nameof(Pages));
        }
        
        private void UpdateCurrentPageItems()
        {
            DisplayItems.Clear();
            
            if (Pages.Count > CurrentPage)
            {
                foreach (var item in Pages[CurrentPage])
                {
                    DisplayItems.Add(item);
                }
            }
        }
        
        public AppGroup CreateGroup(string name)
        {
            var group = new AppGroup(name);
            AppGroups.Add(group);
            UpdateDisplayItems();
            SaveLayoutConfiguration(); // Use unified config instead of separate group storage
            return group;
        }
        
        public void AddAppToGroup(AppIcon app, AppGroup group)
        {
            // Remove from old group if any
            var oldGroup = AppGroups.FirstOrDefault(g => g.Id == app.GroupId);
            oldGroup?.RemoveApp(app);
            
            // Add to new group
            group.AddApp(app);
            UpdateDisplayItems();
            SaveLayoutConfiguration(); // Use unified config instead of separate group storage
        }
        
        public void RemoveAppFromGroup(AppIcon app)
        {
            var group = AppGroups.FirstOrDefault(g => g.Id == app.GroupId);
            group?.RemoveApp(app);
            
            // Remove empty groups
            if (group != null && group.Apps.Count == 0)
            {
                AppGroups.Remove(group);
            }
            
            UpdateDisplayItems();
            SaveLayoutConfiguration(); // Use unified config instead of separate group storage
        }
        
        public void RenameGroup(AppGroup group, string newName)
        {
            group.Name = newName;
            SaveLayoutConfiguration(); // Use unified config instead of separate group storage
        }
        
        public void AddApplication(AppIcon app)
        {
            AllApplications.Add(app);
            
            // Add to filtered if matches current search
            if (string.IsNullOrWhiteSpace(SearchText) || 
                app.Name.ToLowerInvariant().Contains(SearchText.ToLowerInvariant()))
            {
                FilteredApplications.Add(app);
            }
        }
        
        public void RemoveApplication(AppIcon app)
        {
            // Remove from group if any
            RemoveAppFromGroup(app);
            
            AllApplications.Remove(app);
            FilteredApplications.Remove(app);
            UpdateDisplayItems();
        }
        
        // Removed LoadGroupsFromStorage and SaveGroupsToStorage methods
        // All group data is now saved in the unified launchpad_config.json file
        
        public void NextPage()
        {
            if (CurrentPage < TotalPages - 1)
            {
                CurrentPage++;
            }
        }
        
        public void PreviousPage()
        {
            if (CurrentPage > 0)
            {
                CurrentPage--;
            }
        }
        
        public void GoToPage(int pageIndex)
        {
            if (pageIndex >= 0 && pageIndex < TotalPages)
            {
                CurrentPage = pageIndex;
            }
        }
        
        public void MoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || toIndex < 0 || fromIndex >= DisplayItems.Count || toIndex >= DisplayItems.Count)
                return;
                
            if (fromIndex == toIndex)
                return;
            
            var item = DisplayItems[fromIndex];
            DisplayItems.RemoveAt(fromIndex);
            DisplayItems.Insert(toIndex, item);
            
            // Update underlying collections
            UpdateUnderlyingCollections();
        }
        
        public void RefreshLayout()
        {
            // Force a layout refresh
            OnPropertyChanged(nameof(DisplayItems));
            CreatePages();
        }
        
        private void UpdateUnderlyingCollections()
        {
            // Update the order in AllApplications and AppGroups based on DisplayItems
            var newAppOrder = new List<AppIcon>();
            var newGroupOrder = new List<AppGroup>();
            
            foreach (var item in DisplayItems)
            {
                if (item is AppIcon app)
                    newAppOrder.Add(app);
                else if (item is AppGroup group)
                    newGroupOrder.Add(group);
            }
            
            // Update collections while preserving references
            AllApplications.Clear();
            AppGroups.Clear();
            
            foreach (var app in newAppOrder)
                AllApplications.Add(app);
                
            foreach (var group in newGroupOrder)
                AppGroups.Add(group);
                
            // Save to storage
            SaveLayoutConfiguration(); // Use unified config instead of separate group storage
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        // Removed old SavedGroup class - using Models.SavedGroup from LayoutConfiguration instead
        
        // New methods for auto-categorization and layout persistence
        private Task AutoCategorizeApps(List<AppIcon> apps)
        {
            System.Diagnostics.Debug.WriteLine($"AutoCategorizeApps: Starting intelligent categorization of {apps.Count} apps");
            
            // Use intelligent categorizer to create smart groups
            var categorizedApps = AppCategorizer.CategorizeApplications(apps);
            var appGroups = AppCategorizer.CreateAppGroups(categorizedApps);
            
            System.Diagnostics.Debug.WriteLine($"AutoCategorizeApps: Created {appGroups.Count} intelligent groups");
            
            // Add created groups to AppGroups collection
            foreach (var group in appGroups)
            {
                AppGroups.Add(group);
                System.Diagnostics.Debug.WriteLine($"Created group '{group.Name}' with {group.Apps.Count} apps");
            }
            
            // Add all apps to AllApplications (both grouped and ungrouped)
            foreach (var app in apps)
            {
                AllApplications.Add(app);
            }
            
            // IMPORTANT: Update DisplayItems after categorization but BEFORE saving
            UpdateDisplayItems();
            
            // Ensure we have a valid layout config before saving
            if (_layoutConfig == null)
            {
                _layoutConfig = new LayoutConfiguration();
                System.Diagnostics.Debug.WriteLine("AutoCategorizeApps: Created new _layoutConfig");
            }
            
            // Save initial layout with proper group data
            SaveLayoutConfiguration();
            
            System.Diagnostics.Debug.WriteLine($"AutoCategorizeApps: Saved layout with {AppGroups.Count} groups and {AllApplications.Count} apps");
            
            return Task.CompletedTask;
        }
        
        private void RestoreAppLayout(List<AppIcon> apps)
        {
            System.Diagnostics.Debug.WriteLine($"RestoreAppLayout: Starting with {apps.Count} apps");
            
            if (_layoutConfig == null)
            {
                System.Diagnostics.Debug.WriteLine("RestoreAppLayout: No config found, adding apps normally");
                // No config, just add apps normally
                foreach (var app in apps.OrderBy(a => a.Name))
                {
                    AllApplications.Add(app);
                }
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"RestoreAppLayout: Config found with {_layoutConfig.Groups?.Count ?? 0} groups and {_layoutConfig.AppPositions?.Count ?? 0} positions");
            
            // Create a dictionary for quick lookup
            var appDict = apps.ToDictionary(
                a => a.Id ?? a.ExecutablePath ?? a.Name,
                a => a
            );
            
            // Restore groups first
            System.Diagnostics.Debug.WriteLine($"RestoreAppLayout: Restoring {_layoutConfig.Groups?.Count ?? 0} groups");
            foreach (var savedGroup in _layoutConfig.Groups ?? new List<Models.SavedGroup>())
            {
                System.Diagnostics.Debug.WriteLine($"RestoreAppLayout: Restoring group '{savedGroup.Name}' with {savedGroup.AppIds.Count} app IDs");
                
                var group = new AppGroup(savedGroup.Name)
                {
                    Id = savedGroup.Id
                };
                
                foreach (var appId in savedGroup.AppIds)
                {
                    if (appDict.TryGetValue(appId, out var app))
                    {
                        System.Diagnostics.Debug.WriteLine($"  Found app '{app.Name}' for group '{savedGroup.Name}'");
                        group.AddApp(app);
                        app.GroupId = group.Id;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  App with ID '{appId}' not found in current apps");
                    }
                }
                
                if (group.Apps.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"  Added group '{savedGroup.Name}' with {group.Apps.Count} apps to AppGroups");
                    AppGroups.Add(group);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  Skipped empty group '{savedGroup.Name}'");
                }
            }
            
            // Restore app positions
            var positionedApps = new HashSet<string>();
            var orderedApps = new List<AppIcon>();
            
            // First add apps in their saved positions
            foreach (var savedPos in (_layoutConfig.AppPositions ?? new List<Models.SavedAppPosition>()).OrderBy(p => p.PageIndex).ThenBy(p => p.Row).ThenBy(p => p.Column))
            {
                if (appDict.TryGetValue(savedPos.AppId, out var app))
                {
                    if (string.IsNullOrEmpty(app.GroupId)) // Only add non-grouped apps
                    {
                        orderedApps.Add(app);
                        positionedApps.Add(savedPos.AppId);
                    }
                }
            }
            
            // Then add any new apps that weren't in the saved layout
            foreach (var app in apps.OrderBy(a => a.Name))
            {
                var appId = app.Id ?? app.ExecutablePath ?? app.Name;
                if (!positionedApps.Contains(appId) && string.IsNullOrEmpty(app.GroupId))
                {
                    orderedApps.Add(app);
                }
            }
            
            // Add all apps to the collection
            foreach (var app in orderedApps)
            {
                AllApplications.Add(app);
            }
        }
        
        /// <summary>
        /// Cleans up legacy desktop_layout.json files that are no longer used
        /// since the DesktopViewModel was removed.
        /// </summary>
        private void CleanupLegacyFiles()
        {
            try
            {
                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk");
                
                var legacyFiles = new[]
                {
                    Path.Combine(configDir, "desktop_layout.json"),
                    Path.Combine(configDir, "layout_config.json") // Also remove this if it exists
                };
                
                foreach (var legacyFile in legacyFiles)
                {
                    if (File.Exists(legacyFile))
                    {
                        System.Diagnostics.Debug.WriteLine($"CleanupLegacyFiles: Removing legacy file: {legacyFile}");
                        File.Delete(legacyFile);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CleanupLegacyFiles: Error cleaning up legacy files: {ex.Message}");
            }
        }
        
        private void LoadLayoutConfiguration()
        {
            try
            {
                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk");
                
                System.Diagnostics.Debug.WriteLine($"LoadLayoutConfiguration: Config directory: {configDir}");
                System.Diagnostics.Debug.WriteLine($"LoadLayoutConfiguration: Config file path: {_configPath}");
                
                Directory.CreateDirectory(configDir);
                
                if (File.Exists(_configPath))
                {
                    System.Diagnostics.Debug.WriteLine("LoadLayoutConfiguration: Config file exists, loading...");
                    var json = File.ReadAllText(_configPath);
                    System.Diagnostics.Debug.WriteLine($"LoadLayoutConfiguration: Config JSON length: {json.Length}");
                    
                    _layoutConfig = JsonConvert.DeserializeObject<LayoutConfiguration>(json);
                    System.Diagnostics.Debug.WriteLine($"LoadLayoutConfiguration: Loaded config - IsFirstRun: {_layoutConfig?.IsFirstRun}, Groups: {_layoutConfig?.Groups?.Count ?? 0}, Positions: {_layoutConfig?.AppPositions?.Count ?? 0}");
                    
                    if (_layoutConfig?.Groups != null)
                    {
                        foreach (var group in _layoutConfig.Groups)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Loaded group '{group.Name}' with {group.AppIds.Count} apps");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LoadLayoutConfiguration: Config file does not exist, creating default for first run");
                    // Create default config for first run
                    _layoutConfig = new LayoutConfiguration
                    {
                        IsFirstRun = true
                    };
                    SaveLayoutConfiguration();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading layout config: {ex.Message}");
                _layoutConfig = new LayoutConfiguration { IsFirstRun = true };
            }
        }
        
        private void SaveLayoutConfiguration()
        {
            try
            {
                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk");
                
                Directory.CreateDirectory(configDir);
                
                if (_layoutConfig == null)
                {
                    _layoutConfig = new LayoutConfiguration();
                }
                
                // Update config with current state
                _layoutConfig.LastModified = DateTime.Now;
                
                // Save groups - use AppGroups collection directly, not DisplayItems
                _layoutConfig.Groups = AppGroups.Select(group => new Models.SavedGroup
                {
                    Id = group.Id,
                    Name = group.Name,
                    AppIds = group.Apps.Select(app => app.Id ?? app.ExecutablePath ?? app.Name).ToList()
                }).ToList();
                
                System.Diagnostics.Debug.WriteLine($"SaveLayoutConfiguration: Saving {_layoutConfig.Groups.Count} groups");
                foreach (var group in _layoutConfig.Groups)
                {
                    System.Diagnostics.Debug.WriteLine($"  Group '{group.Name}' with {group.AppIds.Count} apps: {string.Join(", ", group.AppIds)}");
                }
                
                // Save app positions - use a combination of DisplayItems and AllApplications
                _layoutConfig.AppPositions = new List<Models.SavedAppPosition>();
                int index = 0;
                
                // If DisplayItems is populated (normal save), use it
                if (DisplayItems.Count > 0)
                {
                    foreach (var item in DisplayItems)
                    {
                        if (item is AppIcon app)
                        {
                            var position = new Models.SavedAppPosition
                            {
                                AppId = app.Id ?? app.ExecutablePath ?? app.Name,
                                AppPath = app.ExecutablePath ?? string.Empty,
                                PageIndex = index / _itemsPerPage,
                                Row = (index % _itemsPerPage) / 7,
                                Column = index % 7,
                                GroupId = app.GroupId
                            };
                            _layoutConfig.AppPositions.Add(position);
                            index++;
                        }
                        else if (item is AppGroup)
                        {
                            // Groups are saved separately
                            index++;
                        }
                    }
                }
                else
                {
                    // If DisplayItems is empty (first run), save from AllApplications
                    System.Diagnostics.Debug.WriteLine("DisplayItems empty, saving positions from AllApplications");
                    foreach (var app in AllApplications.Where(a => string.IsNullOrEmpty(a.GroupId)))
                    {
                        var position = new Models.SavedAppPosition
                        {
                            AppId = app.Id ?? app.ExecutablePath ?? app.Name,
                            AppPath = app.ExecutablePath ?? string.Empty,
                            PageIndex = index / _itemsPerPage,
                            Row = (index % _itemsPerPage) / 7,
                            Column = index % 7,
                            GroupId = app.GroupId
                        };
                        _layoutConfig.AppPositions.Add(position);
                        index++;
                    }
                    
                    // Add group positions
                    foreach (var group in AppGroups)
                    {
                        index++; // Groups take up one position each
                    }
                }
                
                var json = JsonConvert.SerializeObject(_layoutConfig, Formatting.Indented);
                File.WriteAllText(_configPath, json);
                
                System.Diagnostics.Debug.WriteLine($"Saved layout config: {_layoutConfig.AppPositions.Count} positions, {_layoutConfig.Groups.Count} groups");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving layout config: {ex.Message}");
            }
        }
        
        public void SaveCurrentLayout()
        {
            SaveLayoutConfiguration();
        }
        
        public void ResetLayout()
        {
            _layoutConfig = new LayoutConfiguration
            {
                IsFirstRun = true
            };
            SaveLayoutConfiguration();
            
            // Reload apps to trigger auto-categorization
            _ = LoadAllApplicationsAsync();
        }
        
        private string? GetRootInstallFolder(string executablePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(executablePath);
                if (string.IsNullOrEmpty(directory))
                    return null;
                
                // Look for common install root patterns
                var pathParts = directory.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                
                // Check for Program Files patterns
                if (pathParts.Length >= 3)
                {
                    for (int i = 0; i < pathParts.Length - 1; i++)
                    {
                        if (pathParts[i].Equals("Program Files", StringComparison.OrdinalIgnoreCase) ||
                            pathParts[i].Equals("Program Files (x86)", StringComparison.OrdinalIgnoreCase))
                        {
                            // Return the next folder as the root install folder
                            if (i + 1 < pathParts.Length)
                            {
                                var rootPath = string.Join(Path.DirectorySeparatorChar.ToString(), 
                                    pathParts.Take(i + 2));
                                return Path.GetPathRoot(executablePath) + rootPath;
                            }
                        }
                    }
                }
                
                // Check for other common install locations
                var commonRoots = new[] { "Microsoft Visual Studio", "JetBrains", "Google", "Mozilla", "Adobe" };
                foreach (var root in commonRoots)
                {
                    var rootIndex = Array.FindIndex(pathParts, p => 
                        p.Contains(root, StringComparison.OrdinalIgnoreCase));
                    if (rootIndex >= 0 && rootIndex < pathParts.Length - 1)
                    {
                        var rootPath = string.Join(Path.DirectorySeparatorChar.ToString(), 
                            pathParts.Take(rootIndex + 1));
                        return Path.GetPathRoot(executablePath) + rootPath;
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        private string GetFriendlyGroupName(string folderName, List<AppIcon> apps)
        {
            // Create friendly names for common software suites
            var friendlyNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Microsoft Visual Studio", "Visual Studio" },
                { "Visual Studio", "Visual Studio" },
                { "JetBrains", "JetBrains" },
                { "Google", "Google Apps" },
                { "Mozilla", "Mozilla" },
                { "Adobe", "Adobe Creative Suite" },
                { "Microsoft Office", "Microsoft Office" },
                { "Office", "Microsoft Office" }
            };
            
            // Check if folder name matches any friendly names
            foreach (var friendly in friendlyNames)
            {
                if (folderName.Contains(friendly.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return friendly.Value;
                }
            }
            
            // If no friendly name found, try to detect from app names
            var appNames = apps.Select(a => a.Name).ToList();
            
            // Check for Visual Studio pattern
            if (appNames.Any(name => name.Contains("Visual Studio", StringComparison.OrdinalIgnoreCase)))
            {
                return "Visual Studio";
            }
            
            // Check for Office pattern
            if (appNames.Any(name => name.Contains("Word", StringComparison.OrdinalIgnoreCase) ||
                                   name.Contains("Excel", StringComparison.OrdinalIgnoreCase) ||
                                   name.Contains("PowerPoint", StringComparison.OrdinalIgnoreCase)))
            {
                return "Microsoft Office";
            }
            
            // Check for Adobe pattern
            if (appNames.Any(name => name.Contains("Photoshop", StringComparison.OrdinalIgnoreCase) ||
                                   name.Contains("Illustrator", StringComparison.OrdinalIgnoreCase) ||
                                   name.Contains("Premiere", StringComparison.OrdinalIgnoreCase)))
            {
                return "Adobe Creative Suite";
            }
            
            // Default: use folder name
            return folderName;
        }
    }
}