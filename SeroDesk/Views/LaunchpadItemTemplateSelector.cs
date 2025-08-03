using System.Windows;
using System.Windows.Controls;
using SeroDesk.Models;

namespace SeroDesk.Views
{
    public class LaunchpadItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? GroupTemplate { get; set; }
        public DataTemplate? AppTemplate { get; set; }
        
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                AppGroup => GroupTemplate,
                AppIcon => AppTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}