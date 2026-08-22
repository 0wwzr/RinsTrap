using RinsTrap.UI.ViewModels.Settings;

namespace RinsTrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for StatisticsPage.xaml
    /// </summary>
    public partial class StatisticsPage
    {
        public StatisticsPage()
        {
            DataContext = new StatisticsViewModel();
            InitializeComponent();
        }
    }
}