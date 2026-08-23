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

        private void SelectAllInstances_Click(object sender, RoutedEventArgs e)
        {
            InstancesDataGrid.SelectAll();
        }

        private void KillSelectedInstances_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ViewModels.Settings.MultiInstanceViewModel;
            if (viewModel is null) return;

            foreach (var item in InstancesDataGrid.SelectedItems)
            {
                if (item is RunningInstance instance)
                {
                    InstanceManager.Instance.KillInstance(instance.ProcessId);

                    var account = viewModel.Accounts.FirstOrDefault(a => a.ProcessId == instance.ProcessId);
                    if (account is not null)
                        account.ProcessId = 0;
                }
            }
        }
    }
}
