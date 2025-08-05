using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeroDesk.Models
{
    /// <summary>
    /// Represents a collection of related applications grouped together in the LaunchPad interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// AppGroup provides iOS-style folder functionality, allowing users to organize related
    /// applications into named collections. Groups can be created automatically through
    /// intelligent categorization or manually by user interaction.
    /// </para>
    /// <para>
    /// Key features of AppGroup:
    /// <list type="bullet">
    /// <item>Dynamic group icons based on contained applications</item>
    /// <item>Expandable interface for viewing group contents</item>
    /// <item>Inline name editing capabilities</item>
    /// <item>Automatic icon generation and visual representation</item>
    /// <item>Persistent storage of group structure and membership</item>
    /// </list>
    /// </para>
    /// <para>
    /// Groups are displayed as single icons in the LaunchPad with a badge showing the number
    /// of contained applications. When tapped or clicked, they expand to show all member applications.
    /// </para>
    /// </remarks>
    public class AppGroup : INotifyPropertyChanged
    {
        /// <summary>
        /// The display name of the group.
        /// </summary>
        private string _name = "New Group";
        
        /// <summary>
        /// The unique identifier for this group.
        /// </summary>
        private string _id = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Indicates whether the group is currently expanded to show its contents.
        /// </summary>
        private bool _isExpanded = false;
        
        /// <summary>
        /// Indicates whether the group name is currently being edited.
        /// </summary>
        private bool _isEditingName = false;
        
        /// <summary>
        /// The icon image displayed for this group.
        /// </summary>
        private ImageSource? _groupIcon;
        
        /// <summary>
        /// Gets or sets the unique identifier for this application group.
        /// </summary>
        /// <value>
        /// A unique string identifier used for persistence and internal references.
        /// </value>
        /// <remarks>
        /// This ID is used in configuration files to maintain group structure across sessions.
        /// </remarks>
        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the display name of the application group.
        /// </summary>
        /// <value>
        /// The user-visible name shown below the group icon in the LaunchPad.
        /// </value>
        /// <remarks>
        /// Changing the name automatically triggers an update of the group icon to reflect
        /// any category-specific visual styling.
        /// </remarks>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); UpdateGroupIcon(); }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the group is currently expanded to show its contents.
        /// </summary>
        /// <value>
        /// True if the group is expanded and showing individual applications; otherwise, false.
        /// </value>
        /// <remarks>
        /// When expanded, the group shows all contained applications in a grid layout.
        /// When collapsed, it appears as a single icon with a badge indicating the number of apps.
        /// </remarks>
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the group name is currently being edited.
        /// </summary>
        /// <value>
        /// True if the name is in edit mode; otherwise, false.
        /// </value>
        /// <remarks>
        /// This property enables inline editing functionality where users can directly
        /// modify the group name by clicking on it.
        /// </remarks>
        public bool IsEditingName
        {
            get => _isEditingName;
            set { _isEditingName = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets or sets the icon image displayed for this group.
        /// </summary>
        /// <value>
        /// An ImageSource representing the group's visual icon, or null if no icon is set.
        /// </value>
        /// <remarks>
        /// The group icon is typically auto-generated based on the group's category or
        /// the most prominent application within the group.
        /// </remarks>
        public ImageSource? GroupIcon
        {
            get => _groupIcon;
            set { _groupIcon = value; OnPropertyChanged(); }
        }
        
        /// <summary>
        /// Gets the collection of applications contained within this group.
        /// </summary>
        /// <value>
        /// An observable collection of AppIcon instances representing the group's member applications.
        /// </value>
        /// <remarks>
        /// This collection automatically notifies the UI when applications are added or removed,
        /// enabling dynamic updates to group badges and displays.
        /// </remarks>
        public ObservableCollection<AppIcon> Apps { get; } = new();
        
        public AppGroup()
        {
            Apps.CollectionChanged += (s, e) => UpdateGroupIcon();
        }
        
        public AppGroup(string name) : this()
        {
            Name = name;
        }
        
        public void AddApp(AppIcon app)
        {
            app.GroupId = Id;
            app.GroupIndex = Apps.Count;
            Apps.Add(app);
            UpdateGroupIcon();
        }
        
        public void RemoveApp(AppIcon app)
        {
            app.GroupId = null;
            app.GroupIndex = -1;
            Apps.Remove(app);
            UpdateGroupIcon();
        }
        
        public void MoveApp(AppIcon app, int newIndex)
        {
            var oldIndex = Apps.IndexOf(app);
            if (oldIndex >= 0 && newIndex >= 0 && newIndex < Apps.Count)
            {
                Apps.Move(oldIndex, newIndex);
                
                // Update indices
                for (int i = 0; i < Apps.Count; i++)
                {
                    Apps[i].GroupIndex = i;
                }
            }
        }
        
        private void UpdateGroupIcon()
        {
            // Create a composite icon from the first 4 apps in the group
            if (Apps.Count == 0)
            {
                GroupIcon = CreateDefaultGroupIcon();
            }
            else if (Apps.Count == 1)
            {
                GroupIcon = Apps[0].IconImage;
            }
            else
            {
                GroupIcon = CreateCompositeIcon();
            }
        }
        
        private ImageSource CreateDefaultGroupIcon()
        {
            // Create a default folder-like icon
            var visual = new System.Windows.Shapes.Rectangle
            {
                Width = 64,
                Height = 64,
                Fill = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)),
                RadiusX = 12,
                RadiusY = 12
            };
            
            var renderTarget = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            visual.Measure(new System.Windows.Size(64, 64));
            visual.Arrange(new System.Windows.Rect(0, 0, 64, 64));
            renderTarget.Render(visual);
            renderTarget.Freeze();
            
            return renderTarget;
        }
        
        private ImageSource CreateCompositeIcon()
        {
            var canvas = new System.Windows.Controls.Canvas
            {
                Width = 64,
                Height = 64,
                Background = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF))
            };
            
            // Add up to 4 app icons in a 2x2 grid
            var appsToShow = Apps.Take(4).ToList();
            var iconSize = appsToShow.Count <= 2 ? 28 : 20;
            var spacing = appsToShow.Count <= 2 ? 8 : 4;
            
            for (int i = 0; i < appsToShow.Count; i++)
            {
                if (appsToShow[i].IconImage != null)
                {
                    var image = new System.Windows.Controls.Image
                    {
                        Source = appsToShow[i].IconImage,
                        Width = iconSize,
                        Height = iconSize
                    };
                    
                    var row = i / 2;
                    var col = i % 2;
                    
                    System.Windows.Controls.Canvas.SetLeft(image, spacing + col * (iconSize + spacing));
                    System.Windows.Controls.Canvas.SetTop(image, spacing + row * (iconSize + spacing));
                    
                    canvas.Children.Add(image);
                }
            }
            
            var renderTarget = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            canvas.Measure(new System.Windows.Size(64, 64));
            canvas.Arrange(new System.Windows.Rect(0, 0, 64, 64));
            renderTarget.Render(canvas);
            renderTarget.Freeze();
            
            return renderTarget;
        }
        
        public bool CanAcceptDrop(AppIcon app)
        {
            return app.GroupId != Id; // Can't drop on same group
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}