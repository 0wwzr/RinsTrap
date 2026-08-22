using System.Windows;
using System.Windows.Controls;

using RinsTrap.Integrations;

namespace RinsTrap.UI.Elements.Settings.Pages
{
    public partial class MultiInstancePage : Wpf.Ui.Controls.UiPage
    {
        public MultiInstancePage()
        {
            DataContext = new ViewModels.Settings.MultiInstanceViewModel();
            InitializeComponent();
        }

        private void KillInstance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is int processId)
            {
                InstanceManager.Instance.KillInstance(processId);

                var viewModel = DataContext as ViewModels.Settings.MultiInstanceViewModel;
                var account = viewModel?.Accounts.FirstOrDefault(a => a.ProcessId == processId);
                if (account is not null)
                    account.ProcessId = 0;
            }
        }
    }
}
