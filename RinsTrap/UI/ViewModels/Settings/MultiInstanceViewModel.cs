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
        public ICommand LaunchInstanceCommand => new RelayCommand(LaunchInstance);
        public ICommand LaunchAllInstancesCommand => new RelayCommand(LaunchAllInstances);
        public ICommand LaunchSelectedAccountsCommand => new RelayCommand(LaunchSelectedAccounts);
        public ICommand KillInstanceCommand => new RelayCommand<RunningInstance>(KillInstance);
        public ICommand KillAllInstancesCommand => new RelayCommand(KillAllInstances);
        public ICommand RefreshInstancesCommand => new RelayCommand(RefreshInstances);
        public ICommand ToggleAntiAfkCommand => new RelayCommand(ToggleAntiAfk);
        public ICommand SelectAllAccountsCommand => new RelayCommand(SelectAllAccounts);
        public ICommand DeleteSelectedAccountsCommand => new RelayCommand(DeleteSelectedAccounts);

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

        // Anti-AFK properties
        public bool AntiAfkEnabled
        {
            get => App.Settings.Prop.AntiAfkEnabled;
            set
            {
                App.Settings.Prop.AntiAfkEnabled = value;
                OnPropertyChanged(nameof(AntiAfkEnabled));
                OnPropertyChanged(nameof(AntiAfkStatusText));
            }
        }

        public int AntiAfkIntervalSeconds
        {
            get => App.Settings.Prop.AntiAfkIntervalSeconds;
            set
            {
                App.Settings.Prop.AntiAfkIntervalSeconds = value;
                AntiAfkService.Instance.UpdateInterval(value);
                OnPropertyChanged(nameof(AntiAfkIntervalSeconds));
                OnPropertyChanged(nameof(AntiAfkIntervalText));
            }
        }

        public bool AntiAfkSimulateMouse
        {
            get => App.Settings.Prop.AntiAfkSimulateMouse;
            set
            {
                App.Settings.Prop.AntiAfkSimulateMouse = value;
                OnPropertyChanged(nameof(AntiAfkSimulateMouse));
            }
        }

        public bool AntiAfkSimulateKey
        {
            get => App.Settings.Prop.AntiAfkSimulateKey;
            set
            {
                App.Settings.Prop.AntiAfkSimulateKey = value;
                OnPropertyChanged(nameof(AntiAfkSimulateKey));
            }
        }

        public string AntiAfkKeyToSend
        {
            get => App.Settings.Prop.AntiAfkKeyToSend;
            set
            {
                App.Settings.Prop.AntiAfkKeyToSend = value;
                OnPropertyChanged(nameof(AntiAfkKeyToSend));
            }
        }

        public string AntiAfkStatusText => AntiAfkEnabled ? "Active" : "Inactive";

        public string AntiAfkIntervalText => $"Every {AntiAfkIntervalSeconds / 60}m {AntiAfkIntervalSeconds % 60}s";

        public List<string> AntiAfkKeys { get; } = new() { "Space", "W", "A", "S", "D", "E", "Q" };

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

        private void LaunchInstance()
        {
            if (SelectedAccount is null) return;

            // Enable multi-instance mode
            App.Settings.Prop.AllowMultipleInstances = true;
            AllowMultipleInstances = true;

            // Launch Roblox without closing RinsTrap
            Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Use the existing launch handler
                        LaunchHandler.LaunchRoblox(Enums.LaunchMode.Player);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine("MultiInstanceViewModel", $"Failed to launch: {ex.Message}");
                    }
                });
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
                SelectedAccount = account;
                LaunchInstance();
                Task.Delay(2000).Wait(); // Wait between launches
            }
        }

        private void LaunchSelectedAccounts()
        {
            // Launch all accounts that are auto-launch enabled
            LaunchAllInstances();
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

        private void ToggleAntiAfk()
        {
            if (AntiAfkService.Instance.IsRunning)
            {
                AntiAfkService.Instance.Stop();
            }
            else
            {
                AntiAfkService.Instance.Start();
            }
            OnPropertyChanged(nameof(AntiAfkStatusText));
        }

        private void SelectAllAccounts()
        {
            // This would need UI support - for now just select last account
            if (Accounts.Any())
                SelectedAccount = Accounts.Last();
        }

        private void DeleteSelectedAccounts()
        {
            // Delete all accounts
            var accountsToRemove = Accounts.ToList();
            foreach (var account in accountsToRemove)
            {
                if (account.IsRunning)
                {
                    InstanceManager.Instance.KillInstance(account.ProcessId);
                }
                Accounts.Remove(account);
            }
            SelectedAccount = null;
            OnPropertyChanged(nameof(SelectedAccount));
            OnPropertyChanged(nameof(IsAccountSelected));
        }
    }
}
