using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeroDesk.Models
{
    public class AppGroup : INotifyPropertyChanged
    {
        private string _name = "New Group";
        private string _id = Guid.NewGuid().ToString();
        private bool _isExpanded = false;
        private bool _isEditingName = false;
        private ImageSource? _groupIcon;
        
        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }
        
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); UpdateGroupIcon(); }
        }
        
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }
        
        public bool IsEditingName
        {
            get => _isEditingName;
            set { _isEditingName = value; OnPropertyChanged(); }
        }
        
        public ImageSource? GroupIcon
        {
            get => _groupIcon;
            set { _groupIcon = value; OnPropertyChanged(); }
        }
        
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