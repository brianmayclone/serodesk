using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
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
        private int _itemsPerPage = 48; // 8x6 grid = 48 items per page
        private ObservableCollection<ObservableCollection<object>> _pages;
        
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
            
            // Load Windows wallpaper
            LoadWallpaper();
        }
        
        public async Task LoadAllApplicationsAsync()
        {
            try
            {
                AllApplications.Clear();
                FilteredApplications.Clear();
                
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
                        testApp.IconImage = IconExtractor.GetIconForFile(testApp.ExecutablePath, true);
                        AllApplications.Add(testApp);
                        FilteredApplications.Add(testApp);
                    }
                }
                
                // Now try to load all installed applications
                try
                {
                    var apps = await ApplicationScanner.ScanInstalledApplicationsAsync();
                    
                    foreach (var app in apps)
                    {
                        AllApplications.Add(app);
                        FilteredApplications.Add(app);
                    }
                }
                catch (Exception scanEx)
                {
                    System.Windows.MessageBox.Show($"Scanner error: {scanEx.Message}", "Scanner Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                
                // Load saved groups and organize apps
                await LoadGroupsFromStorage();
                UpdateDisplayItems();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading apps: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        
        public void FilterApplications(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Show organized view with groups
                UpdateDisplayItems();
            }
            else
            {
                // Show flat search results
                DisplayItems.Clear();
                
                // Search in apps and groups
                var filteredApps = AllApplications.Where(app => 
                    app.Name.ToLowerInvariant().Contains(searchText.ToLowerInvariant()))
                    .OrderBy(app => app.Name);
                
                var filteredGroups = AppGroups.Where(group =>
                    group.Name.ToLowerInvariant().Contains(searchText.ToLowerInvariant()) ||
                    group.Apps.Any(app => app.Name.ToLowerInvariant().Contains(searchText.ToLowerInvariant())));
                
                foreach (var group in filteredGroups)
                {
                    DisplayItems.Add(group);
                }
                
                foreach (var app in filteredApps.Where(app => string.IsNullOrEmpty(app.GroupId)))
                {
                    DisplayItems.Add(app);
                }
            }
        }
        
        private void UpdateDisplayItems()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Organized view with paging
                CreatePages();
                UpdateCurrentPageItems();
            }
            else
            {
                // Search view - show all matching items on one page
                DisplayItems.Clear();
                
                var filteredGroups = AppGroups.Where(group =>
                    group.Name.ToLowerInvariant().Contains(SearchText.ToLowerInvariant()) ||
                    group.Apps.Any(app => app.Name.ToLowerInvariant().Contains(SearchText.ToLowerInvariant())));
                
                var filteredApps = AllApplications.Where(app => 
                    app.Name.ToLowerInvariant().Contains(SearchText.ToLowerInvariant()) &&
                    string.IsNullOrEmpty(app.GroupId));
                
                foreach (var group in filteredGroups)
                {
                    DisplayItems.Add(group);
                }
                
                foreach (var app in filteredApps)
                {
                    DisplayItems.Add(app);
                }
            }
        }
        
        private void CreatePages()
        {
            Pages.Clear();
            
            var allItems = new List<object>();
            
            // Add groups first
            allItems.AddRange(AppGroups);
            
            // Add ungrouped apps
            allItems.AddRange(AllApplications.Where(app => string.IsNullOrEmpty(app.GroupId)));
            
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
            
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(HasMultiplePages));
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
            SaveGroupsToStorage();
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
            SaveGroupsToStorage();
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
            SaveGroupsToStorage();
        }
        
        public void RenameGroup(AppGroup group, string newName)
        {
            group.Name = newName;
            SaveGroupsToStorage();
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
        
        private async Task LoadGroupsFromStorage()
        {
            try
            {
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk", "app_groups.json");
                
                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    var savedGroups = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SavedGroup>>(json);
                    
                    if (savedGroups != null)
                    {
                        foreach (var saved in savedGroups)
                        {
                            var group = new AppGroup(saved.Name) { Id = saved.Id };
                            
                            // Add apps to group
                            foreach (var appId in saved.AppIds)
                            {
                                var app = AllApplications.FirstOrDefault(a => a.Id == appId);
                                if (app != null)
                                {
                                    group.AddApp(app);
                                }
                            }
                            
                            if (group.Apps.Count > 0)
                            {
                                AppGroups.Add(group);
                            }
                        }
                    }
                }
            }
            catch { }
        }
        
        private async void SaveGroupsToStorage()
        {
            try
            {
                var configDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SeroDesk");
                
                Directory.CreateDirectory(configDir);
                
                var savedGroups = AppGroups.Select(group => new SavedGroup
                {
                    Id = group.Id,
                    Name = group.Name,
                    AppIds = group.Apps.Select(app => app.Id).ToList()
                }).ToList();
                
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(savedGroups, 
                    Newtonsoft.Json.Formatting.Indented);
                
                await File.WriteAllTextAsync(Path.Combine(configDir, "app_groups.json"), json);
            }
            catch { }
        }
        
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
            SaveGroupsToStorage();
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private class SavedGroup
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public List<string> AppIds { get; set; } = new List<string>();
        }
    }
}