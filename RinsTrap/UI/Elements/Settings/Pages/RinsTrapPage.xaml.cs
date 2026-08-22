using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RinsTrap.UI.ViewModels.Settings;

namespace RinsTrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for RinsTrapPage.xaml
    /// </summary>
    public partial class RinsTrapPage
    {
        public RinsTrapPage()
        {
            DataContext = new RinsTrapViewModel();
            InitializeComponent();
        }
    }
}
