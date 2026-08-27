using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Diagnostics;

using CommunityToolkit.Mvvm.Input;

using RinsTrap.Integrations;
using RinsTrap.Models;
using RinsTrap.Models.Persistable;
using RinsTrap;

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
        public ICommand ToggleScreenshotsCommand => new RelayCommand(ToggleScreenshots);
        public ICommand CaptureNowCommand => new RelayCommand(CaptureNow);
        public ICommand ClearLogsCommand => new RelayCommand(ClearLogs);
        public ICommand ExportLogsCommand => new RelayCommand(ExportLogs);
        public ICommand BrowseScreenshotPathCommand => new RelayCommand(BrowseScreenshotPath);
        public ICommand ToggleAutoRelaunchCommand => new RelayCommand(ToggleAutoRelaunch);
        public ICommand ToggleWebDashboardCommand => new RelayCommand(ToggleWebDashboard);
        public ICommand RestartAllInstancesCommand => new RelayCommand(RestartAllInstances);
        public ICommand ToggleAntiDetectionCommand => new RelayCommand(ToggleAntiDetection);

        // New commands
        public ICommand TileWindowsCommand => new RelayCommand(TileWindows);
        public ICommand CascadeWindowsCommand => new RelayCommand(CascadeWindows);
        public ICommand MinimizeAllWindowsCommand => new RelayCommand(MinimizeAllWindows);
        public ICommand CreateInstanceGroupCommand => new RelayCommand(CreateInstanceGroup);
        public ICommand ManageInstanceGroupsCommand => new RelayCommand(ManageInstanceGroups);
        public ICommand SaveAsTemplateCommand => new RelayCommand(SaveAsTemplate);
        public ICommand ManageTemplatesCommand => new RelayCommand(ManageTemplates);
        public ICommand AddScheduleCommand => new RelayCommand(AddSchedule);
        public ICommand ViewSchedulesCommand => new RelayCommand(ViewSchedules);
        public ICommand ConfigureHotkeysCommand => new RelayCommand(ConfigureHotkeys);

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

        // Screenshot properties
        public bool ScreenshotsEnabled
        {
            get => App.Settings.Prop.InstanceScreenshotsEnabled;
            set
            {
                App.Settings.Prop.InstanceScreenshotsEnabled = value;
                OnPropertyChanged(nameof(ScreenshotsEnabled));
                OnPropertyChanged(nameof(ScreenshotStatusText));
            }
        }

        public int ScreenshotIntervalSeconds
        {
            get => App.Settings.Prop.ScreenshotIntervalSeconds;
            set
            {
                App.Settings.Prop.ScreenshotIntervalSeconds = value;
                ScreenshotService.Instance.UpdateInterval(value);
                OnPropertyChanged(nameof(ScreenshotIntervalSeconds));
                OnPropertyChanged(nameof(ScreenshotIntervalText));
            }
        }

        public string ScreenshotSavePath
        {
            get => App.Settings.Prop.ScreenshotSavePath;
            set
            {
                App.Settings.Prop.ScreenshotSavePath = value;
                OnPropertyChanged(nameof(ScreenshotSavePath));
            }
        }

        public string ScreenshotStatusText => ScreenshotsEnabled ? "Active" : "Inactive";

        public string ScreenshotIntervalText => $"Every {ScreenshotIntervalSeconds / 60}m {ScreenshotIntervalSeconds % 60}s";

        public List<string> ScreenshotIntervals { get; } = new() 
        { 
            "10s", "30s", "1m", "5m", "10m", "30m", "1h" 
        };

        // Logging properties
        public bool LoggingEnabled
        {
            get => App.Settings.Prop.InstanceLoggingEnabled;
            set
            {
                App.Settings.Prop.InstanceLoggingEnabled = value;
                OnPropertyChanged(nameof(LoggingEnabled));
            }
        }

        public ObservableCollection<LogEntry> LogEntries => InstanceLogger.Instance.LogEntries;

        // Auto-Relaunch properties
        public bool AutoRelaunchEnabled
        {
            get => App.Settings.Prop.AutoRelaunchEnabled;
            set
            {
                App.Settings.Prop.AutoRelaunchEnabled = value;
                OnPropertyChanged(nameof(AutoRelaunchEnabled));
                OnPropertyChanged(nameof(AutoRelaunchStatusText));
            }
        }

        public int AutoRelaunchDelaySeconds
        {
            get => App.Settings.Prop.AutoRelaunchDelaySeconds;
            set
            {
                App.Settings.Prop.AutoRelaunchDelaySeconds = value;
                OnPropertyChanged(nameof(AutoRelaunchDelaySeconds));
                OnPropertyChanged(nameof(AutoRelaunchDelayText));
            }
        }

        public string AutoRelaunchStatusText => AutoRelaunchEnabled ? "Active" : "Inactive";
        public string AutoRelaunchDelayText => $"Delay: {AutoRelaunchDelaySeconds}s";

        // Web Dashboard properties
        public bool WebDashboardEnabled
        {
            get => App.Settings.Prop.WebDashboardEnabled;
            set
            {
                App.Settings.Prop.WebDashboardEnabled = value;
                OnPropertyChanged(nameof(WebDashboardEnabled));
                OnPropertyChanged(nameof(WebDashboardStatusText));
                OnPropertyChanged(nameof(WebDashboardUrl));
            }
        }

        public int WebDashboardPort
        {
            get => App.Settings.Prop.WebDashboardPort;
            set
            {
                App.Settings.Prop.WebDashboardPort = value;
                OnPropertyChanged(nameof(WebDashboardPort));
                OnPropertyChanged(nameof(WebDashboardUrl));
            }
        }

        public string WebDashboardStatusText => WebDashboardEnabled ? "Running" : "Stopped";
        public string WebDashboardUrl => $"http://localhost:{WebDashboardPort}";

        // Anti-Detection properties
        public bool AntiDetectionEnabled
        {
            get => App.Settings.Prop.AntiDetectionEnabled;
            set
            {
                App.Settings.Prop.AntiDetectionEnabled = value;
                OnPropertyChanged(nameof(AntiDetectionEnabled));
                OnPropertyChanged(nameof(AntiDetectionStatusText));
            }
        }

        public bool RandomizeUserId
        {
            get => App.Settings.Prop.RandomizeUserId;
            set
            {
                App.Settings.Prop.RandomizeUserId = value;
                OnPropertyChanged(nameof(RandomizeUserId));
            }
        }

        public bool RandomizeSessionId
        {
            get => App.Settings.Prop.RandomizeSessionId;
            set
            {
                App.Settings.Prop.RandomizeSessionId = value;
                OnPropertyChanged(nameof(RandomizeSessionId));
            }
        }

        public bool RandomizeClientVersion
        {
            get => App.Settings.Prop.RandomizeClientVersion;
            set
            {
                App.Settings.Prop.RandomizeClientVersion = value;
                OnPropertyChanged(nameof(RandomizeClientVersion));
            }
        }

        public bool SpoofHardwareId
        {
            get => App.Settings.Prop.SpoofHardwareId;
            set
            {
                App.Settings.Prop.SpoofHardwareId = value;
                OnPropertyChanged(nameof(SpoofHardwareId));
            }
        }

        public string AntiDetectionStatusText => AntiDetectionEnabled ? "Active" : "Inactive";

        // Anti-AFK Extended properties
        public string AntiAfkMode
        {
            get => App.Settings.Prop.AntiAfkMode;
            set
            {
                App.Settings.Prop.AntiAfkMode = value;
                OnPropertyChanged(nameof(AntiAfkMode));
            }
        }

        public List<string> AntiAfkModes { get; } = new() { "Conservative", "Normal", "Aggressive" };

        public bool AntiAfkRandomizeInterval
        {
            get => App.Settings.Prop.AntiAfkRandomizeInterval;
            set
            {
                App.Settings.Prop.AntiAfkRandomizeInterval = value;
                OnPropertyChanged(nameof(AntiAfkRandomizeInterval));
            }
        }

        public string AntiAfkMousePattern
        {
            get => App.Settings.Prop.AntiAfkMousePattern;
            set
            {
                App.Settings.Prop.AntiAfkMousePattern = value;
                OnPropertyChanged(nameof(AntiAfkMousePattern));
            }
        }

        public List<string> AntiAfkMousePatterns { get; } = new() { "Jitter", "Circle", "Random", "Linear" };

        public int AntiAfkMouseDistance
        {
            get => App.Settings.Prop.AntiAfkMouseDistance;
            set
            {
                App.Settings.Prop.AntiAfkMouseDistance = value;
                OnPropertyChanged(nameof(AntiAfkMouseDistance));
            }
        }

        public bool AntiAfkRandomKey
        {
            get => App.Settings.Prop.AntiAfkRandomKey;
            set
            {
                App.Settings.Prop.AntiAfkRandomKey = value;
                OnPropertyChanged(nameof(AntiAfkRandomKey));
            }
        }

        public bool AntiAfkSimulateClick
        {
            get => App.Settings.Prop.AntiAfkSimulateClick;
            set
            {
                App.Settings.Prop.AntiAfkSimulateClick = value;
                OnPropertyChanged(nameof(AntiAfkSimulateClick));
            }
        }

        public bool AntiAfkOnlyWhenFocused
        {
            get => App.Settings.Prop.AntiAfkOnlyWhenFocused;
            set
            {
                App.Settings.Prop.AntiAfkOnlyWhenFocused = value;
                OnPropertyChanged(nameof(AntiAfkOnlyWhenFocused));
            }
        }

        public bool AntiAfkPauseDuringChat
        {
            get => App.Settings.Prop.AntiAfkPauseDuringChat;
            set
            {
                App.Settings.Prop.AntiAfkPauseDuringChat = value;
                OnPropertyChanged(nameof(AntiAfkPauseDuringChat));
            }
        }

        // Screenshot Extended properties
        public string ScreenshotFormat
        {
            get => App.Settings.Prop.ScreenshotFormat;
            set
            {
                App.Settings.Prop.ScreenshotFormat = value;
                OnPropertyChanged(nameof(ScreenshotFormat));
            }
        }

        public List<string> ScreenshotFormats { get; } = new() { "PNG", "JPG", "BMP" };

        public int ScreenshotQuality
        {
            get => App.Settings.Prop.ScreenshotQuality;
            set
            {
                App.Settings.Prop.ScreenshotQuality = value;
                OnPropertyChanged(nameof(ScreenshotQuality));
            }
        }

        public bool ScreenshotOnlyActive
        {
            get => App.Settings.Prop.ScreenshotOnlyActive;
            set
            {
                App.Settings.Prop.ScreenshotOnlyActive = value;
                OnPropertyChanged(nameof(ScreenshotOnlyActive));
            }
        }

        public bool ScreenshotTimestampFilename
        {
            get => App.Settings.Prop.ScreenshotTimestampFilename;
            set
            {
                App.Settings.Prop.ScreenshotTimestampFilename = value;
                OnPropertyChanged(nameof(ScreenshotTimestampFilename));
            }
        }

        // Window Management properties
        public bool AutoArrangeWindows
        {
            get => App.Settings.Prop.AutoArrangeWindows;
            set
            {
                App.Settings.Prop.AutoArrangeWindows = value;
                OnPropertyChanged(nameof(AutoArrangeWindows));
            }
        }

        public string ArrangeLayout
        {
            get => App.Settings.Prop.ArrangeLayout;
            set
            {
                App.Settings.Prop.ArrangeLayout = value;
                OnPropertyChanged(nameof(ArrangeLayout));
            }
        }

        public List<string> ArrangeLayouts { get; } = new() { "Grid", "Horizontal", "Vertical", "Cascade" };

        public int WindowGap
        {
            get => App.Settings.Prop.WindowGap;
            set
            {
                App.Settings.Prop.WindowGap = value;
                OnPropertyChanged(nameof(WindowGap));
            }
        }

        public bool RememberWindowPositions
        {
            get => App.Settings.Prop.RememberWindowPositions;
            set
            {
                App.Settings.Prop.RememberWindowPositions = value;
                OnPropertyChanged(nameof(RememberWindowPositions));
            }
        }

        // Auto-Relaunch Extended properties
        public int AutoRelaunchMaxRetries
        {
            get => App.Settings.Prop.AutoRelaunchMaxRetries;
            set
            {
                App.Settings.Prop.AutoRelaunchMaxRetries = value;
                OnPropertyChanged(nameof(AutoRelaunchMaxRetries));
            }
        }

        public bool AutoRelaunchOnCrashOnly
        {
            get => App.Settings.Prop.AutoRelaunchOnCrashOnly;
            set
            {
                App.Settings.Prop.AutoRelaunchOnCrashOnly = value;
                OnPropertyChanged(nameof(AutoRelaunchOnCrashOnly));
            }
        }

        public bool AutoRelaunchNotify
        {
            get => App.Settings.Prop.AutoRelaunchNotify;
            set
            {
                App.Settings.Prop.AutoRelaunchNotify = value;
                OnPropertyChanged(nameof(AutoRelaunchNotify));
            }
        }

        // Instance Groups
        public bool InstanceGroupsEnabled
        {
            get => App.Settings.Prop.InstanceGroupsEnabled;
            set
            {
                App.Settings.Prop.InstanceGroupsEnabled = value;
                OnPropertyChanged(nameof(InstanceGroupsEnabled));
            }
        }

        // Resource Limits
        public bool ResourceLimitsEnabled
        {
            get => App.Settings.Prop.ResourceLimitsEnabled;
            set
            {
                App.Settings.Prop.ResourceLimitsEnabled = value;
                OnPropertyChanged(nameof(ResourceLimitsEnabled));
            }
        }

        public int CpuLimitPercent
        {
            get => App.Settings.Prop.CpuLimitPercent;
            set
            {
                App.Settings.Prop.CpuLimitPercent = value;
                OnPropertyChanged(nameof(CpuLimitPercent));
            }
        }

        public int MemoryLimitMB
        {
            get => App.Settings.Prop.MemoryLimitMB;
            set
            {
                App.Settings.Prop.MemoryLimitMB = value;
                OnPropertyChanged(nameof(MemoryLimitMB));
            }
        }

        public string ProcessPriority
        {
            get => App.Settings.Prop.ProcessPriority;
            set
            {
                App.Settings.Prop.ProcessPriority = value;
                OnPropertyChanged(nameof(ProcessPriority));
            }
        }

        public List<string> ProcessPriorities { get; } = new() { "Idle", "Below Normal", "Normal", "Above Normal", "High", "Realtime" };

        public bool CpuAffinityEnabled
        {
            get => App.Settings.Prop.CpuAffinityEnabled;
            set
            {
                App.Settings.Prop.CpuAffinityEnabled = value;
                OnPropertyChanged(nameof(CpuAffinityEnabled));
            }
        }

        // Instance Templates
        public bool TemplatesEnabled
        {
            get => App.Settings.Prop.TemplatesEnabled;
            set
            {
                App.Settings.Prop.TemplatesEnabled = value;
                OnPropertyChanged(nameof(TemplatesEnabled));
            }
        }

        // Scheduler
        public bool SchedulerEnabled
        {
            get => App.Settings.Prop.SchedulerEnabled;
            set
            {
                App.Settings.Prop.SchedulerEnabled = value;
                OnPropertyChanged(nameof(SchedulerEnabled));
            }
        }

        // Hotkeys
        public bool HotkeysEnabled
        {
            get => App.Settings.Prop.HotkeysEnabled;
            set
            {
                App.Settings.Prop.HotkeysEnabled = value;
                OnPropertyChanged(nameof(HotkeysEnabled));
            }
        }

        // Advanced
        public bool FpsLimiterEnabled
        {
            get => App.Settings.Prop.FpsLimiterEnabled;
            set
            {
                App.Settings.Prop.FpsLimiterEnabled = value;
                OnPropertyChanged(nameof(FpsLimiterEnabled));
            }
        }

        public int FpsLimit
        {
            get => App.Settings.Prop.FpsLimit;
            set
            {
                App.Settings.Prop.FpsLimit = value;
                OnPropertyChanged(nameof(FpsLimit));
            }
        }

        public bool NetworkThrottleEnabled
        {
            get => App.Settings.Prop.NetworkThrottleEnabled;
            set
            {
                App.Settings.Prop.NetworkThrottleEnabled = value;
                OnPropertyChanged(nameof(NetworkThrottleEnabled));
            }
        }

        public int NetworkThrottleKBps
        {
            get => App.Settings.Prop.NetworkThrottleKBps;
            set
            {
                App.Settings.Prop.NetworkThrottleKBps = value;
                OnPropertyChanged(nameof(NetworkThrottleKBps));
            }
        }

        public int AutoLaunchStaggerSeconds
        {
            get => App.Settings.Prop.AutoLaunchStaggerSeconds;
            set
            {
                App.Settings.Prop.AutoLaunchStaggerSeconds = value;
                OnPropertyChanged(nameof(AutoLaunchStaggerSeconds));
            }
        }

        public bool MinimizeToTrayOnLaunch
        {
            get => App.Settings.Prop.MinimizeToTrayOnLaunch;
            set
            {
                App.Settings.Prop.MinimizeToTrayOnLaunch = value;
                OnPropertyChanged(nameof(MinimizeToTrayOnLaunch));
            }
        }

        public bool HealthMonitoringEnabled
        {
            get => App.Settings.Prop.HealthMonitoringEnabled;
            set
            {
                App.Settings.Prop.HealthMonitoringEnabled = value;
                OnPropertyChanged(nameof(HealthMonitoringEnabled));
            }
        }

        public bool AutoKillHungInstances
        {
            get => App.Settings.Prop.AutoKillHungInstances;
            set
            {
                App.Settings.Prop.AutoKillHungInstances = value;
                OnPropertyChanged(nameof(AutoKillHungInstances));
            }
        }

        public int HungTimeoutSeconds
        {
            get => App.Settings.Prop.HungTimeoutSeconds;
            set
            {
                App.Settings.Prop.HungTimeoutSeconds = value;
                OnPropertyChanged(nameof(HungTimeoutSeconds));
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

        private void LaunchInstance()
        {
            if (SelectedAccount is null) return;

            // Enable multi-instance mode
            App.Settings.Prop.AllowMultipleInstances = true;
            AllowMultipleInstances = true;

            App.Logger.WriteLine("MultiInstanceViewModel", $"Launching account: {SelectedAccount.Name}");

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

        private void ToggleScreenshots()
        {
            if (ScreenshotService.Instance.IsRunning)
            {
                ScreenshotService.Instance.Stop();
                ScreenshotsEnabled = false;
            }
            else
            {
                ScreenshotsEnabled = true;
                ScreenshotService.Instance.Start();
            }
            OnPropertyChanged(nameof(ScreenshotStatusText));
        }

        private void CaptureNow()
        {
            ScreenshotService.Instance.CaptureAllInstances();
        }

        private void BrowseScreenshotPath()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Screenshot Save Location",
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrEmpty(ScreenshotSavePath) && Directory.Exists(ScreenshotSavePath))
            {
                dialog.SelectedPath = ScreenshotSavePath;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ScreenshotSavePath = dialog.SelectedPath;
            }
        }

        private void ClearLogs()
        {
            InstanceLogger.Instance.Clear();
        }

        private void ExportLogs()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Logs",
                Filter = "Text files (*.txt)|*.txt|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = $"RinsTrap_InstanceLogs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                InstanceLogger.Instance.ExportLogs(dialog.FileName);
            }
        }

        private void ToggleAutoRelaunch()
        {
            if (AutoRelaunchService.Instance.IsRunning)
            {
                AutoRelaunchService.Instance.Stop();
                AutoRelaunchEnabled = false;
            }
            else
            {
                AutoRelaunchEnabled = true;
                AutoRelaunchService.Instance.Start();
            }
            OnPropertyChanged(nameof(AutoRelaunchStatusText));
        }

        private async void ToggleWebDashboard()
        {
            if (WebDashboardService.Instance.IsRunning)
            {
                WebDashboardService.Instance.Stop();
                WebDashboardEnabled = false;
            }
            else
            {
                WebDashboardEnabled = true;
                await WebDashboardService.Instance.StartAsync();
            }
            OnPropertyChanged(nameof(WebDashboardStatusText));
            OnPropertyChanged(nameof(WebDashboardUrl));
        }

        private void RestartAllInstances()
        {
            BatchActionService.Instance.RestartAllInstances();
        }

        private void ToggleAntiDetection()
        {
            AntiDetectionEnabled = !AntiDetectionEnabled;
            OnPropertyChanged(nameof(AntiDetectionStatusText));

            if (AntiDetectionEnabled)
            {
                AntiDetectionService.Instance.ClearCache();
            }
        }

        // New command implementations
        private void TileWindows()
        {
            InstanceManager.Instance.TileWindows();
        }

        private void CascadeWindows()
        {
            InstanceManager.Instance.CascadeWindows();
        }

        private void MinimizeAllWindows()
        {
            InstanceManager.Instance.MinimizeAllWindows();
        }

        private void CreateInstanceGroup()
        {
            // TODO: Show dialog to create instance group
            App.Logger.WriteLine("MultiInstanceViewModel", "Create Instance Group requested");
        }

        private void ManageInstanceGroups()
        {
            // TODO: Show instance groups management dialog
            App.Logger.WriteLine("MultiInstanceViewModel", "Manage Instance Groups requested");
        }

        private void SaveAsTemplate()
        {
            // TODO: Show dialog to save current configuration as template
            App.Logger.WriteLine("MultiInstanceViewModel", "Save as Template requested");
        }

        private void ManageTemplates()
        {
            // TODO: Show templates management dialog
            App.Logger.WriteLine("MultiInstanceViewModel", "Manage Templates requested");
        }

        private void AddSchedule()
        {
            // TODO: Show dialog to add scheduled launch
            App.Logger.WriteLine("MultiInstanceViewModel", "Add Schedule requested");
        }

        private void ViewSchedules()
        {
            // TODO: Show schedules management dialog
            App.Logger.WriteLine("MultiInstanceViewModel", "View Schedules requested");
        }

        private void ConfigureHotkeys()
        {
            // TODO: Show hotkeys configuration dialog
            App.Logger.WriteLine("MultiInstanceViewModel", "Configure Hotkeys requested");
        }
    }
}
