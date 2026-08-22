using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using RinsTrap.Integrations;
using RinsTrap.Models;

namespace RinsTrap.UI.ViewModels.Settings
{
    public class MultiInstanceViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand AddAccountCommand => new RelayCommand(AddAccount);
        public ICommand DeleteAccountCommand => new RelayCommand(DeleteAccount);
        public ICommand LaunchInstanceCommand => new RelayCommand<object>(LaunchInstance);
        public ICommand LaunchAllInstancesCommand => new RelayCommand(LaunchAllInstances);
        public ICommand KillInstanceCommand => new RelayCommand<RunningInstance>(KillInstance);
        public ICommand KillAllInstancesCommand => new RelayCommand(KillAllInstances);
        public ICommand RefreshInstancesCommand => new RelayCommand(RefreshInstances);

        public ObservableCollection<MultiInstanceAccount> Accounts
        {
            get => App.Settings.Prop.MultiInstanceAccounts;
            set => App.Settings.Prop.MultiInstanceAccounts = value;
        }

        public MultiInstanceAccount? SelectedAccount { get; set; }

        public bool IsAccountSelected => SelectedAccount is not null;

        public ObservableCollection<RunningInstance> RunningInstances => InstanceManager.Instance.RunningInstances;

        public bool AllowMultipleInstances
        {
            get => App.Settings.Prop.AllowMultipleInstances;
            set
            {
                App.Settings.Prop.AllowMultipleInstances = value;
                OnPropertyChanged(nameof(AllowMultipleInstances));
            }
        }

        public MultiInstanceViewModel()
        {
            InstanceManager.Instance.InstancesChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(RunningInstances));
            };
        }

        private void AddAccount()
        {
            var account = new MultiInstanceAccount
            {
                Name = $"Account {Accounts.Count + 1}"
            };
            Accounts.Add(account);
            SelectedAccount = account;
            OnPropertyChanged(nameof(SelectedAccount));
            OnPropertyChanged(nameof(IsAccountSelected));
        }

        private void DeleteAccount()
        {
            if (SelectedAccount is not null)
            {
                Accounts.Remove(SelectedAccount);
                SelectedAccount = null;
                OnPropertyChanged(nameof(SelectedAccount));
                OnPropertyChanged(nameof(IsAccountSelected));
            }
        }

        private void LaunchInstance(object? param)
        {
            var account = param as MultiInstanceAccount ?? SelectedAccount;
            if (account is null) return;

            // Update account settings
            App.Settings.Prop.AllowMultipleInstances = true;

            // Launch Roblox with multi-instance mode
            LaunchHandler.LaunchRoblox(Enums.LaunchMode.Player);

            // Track the instance after a short delay
            Task.Delay(2000).ContinueWith(_ =>
            {
                var robloxProcesses = Utilities.GetProcessesSafe()
                    .Where(p => p.ProcessName.StartsWith("Roblox", StringComparison.OrdinalIgnoreCase));

                foreach (var process in robloxProcesses)
                {
                    if (!InstanceManager.Instance.RunningInstances.Any(i => i.ProcessId == process.Id))
                    {
                        InstanceManager.Instance.RegisterInstance(process.Id, account.Name, account.AccountName);
                        account.ProcessId = process.Id;
                        break;
                    }
                }
            });
        }

        private void LaunchAllInstances()
        {
            var accountsToLaunch = Accounts.Where(a => a.AutoLaunch).ToList();
            if (!accountsToLaunch.Any())
            {
                accountsToLaunch = Accounts.ToList();
            }

            foreach (var account in accountsToLaunch)
            {
                Task.Delay(1000 * accountsToLaunch.IndexOf(account)).ContinueWith(_ =>
                {
                    App.Settings.Prop.AllowMultipleInstances = true;
                    App.Current.Dispatcher.Invoke(() => LaunchHandler.LaunchRoblox(Enums.LaunchMode.Player));

                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        var robloxProcesses = Utilities.GetProcessesSafe()
                            .Where(p => p.ProcessName.StartsWith("Roblox", StringComparison.OrdinalIgnoreCase));

                        foreach (var process in robloxProcesses)
                        {
                            if (!InstanceManager.Instance.RunningInstances.Any(i => i.ProcessId == process.Id))
                            {
                                InstanceManager.Instance.RegisterInstance(process.Id, account.Name, account.AccountName);
                                account.ProcessId = process.Id;
                                break;
                            }
                        }
                    });
                });
            }
        }

        private void KillInstance(RunningInstance? instance)
        {
            if (instance is not null)
            {
                InstanceManager.Instance.KillInstance(instance.ProcessId);

                // Update account ProcessId
                var account = Accounts.FirstOrDefault(a => a.ProcessId == instance.ProcessId);
                if (account is not null)
                    account.ProcessId = 0;
            }
        }

        private void KillAllInstances()
        {
            InstanceManager.Instance.KillAllInstances();

            foreach (var account in Accounts)
            {
                account.ProcessId = 0;
            }
        }

        private void RefreshInstances()
        {
            InstanceManager.Instance.RefreshInstances();
        }
    }
}
