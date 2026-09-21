using RinsTrap.UI.ViewModels.Settings;

namespace RinsTrap.UI.Elements.Settings.Pages
{
    public partial class DiscordPage
    {
        public DiscordPage()
        {
            DataContext = new IntegrationsViewModel();
            InitializeComponent();
        }
    }
}
