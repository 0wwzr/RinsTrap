using System.Collections.ObjectModel;
using System.Windows.Input;

using Microsoft.Win32;

using CommunityToolkit.Mvvm.Input;

namespace RinsTrap.UI.ViewModels.Settings
{
    public class IntegrationsViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand AddIntegrationCommand => new RelayCommand(AddIntegration);

        public ICommand DeleteIntegrationCommand => new RelayCommand(DeleteIntegration);

        public ICommand BrowseIntegrationLocationCommand => new RelayCommand(BrowseIntegrationLocation);

        private void AddIntegration()
        {
            CustomIntegrations.Add(new CustomIntegration()
            {
                Name = Strings.Menu_Integrations_Custom_NewIntegration
            });

            SelectedCustomIntegrationIndex = CustomIntegrations.Count - 1;

            OnPropertyChanged(nameof(SelectedCustomIntegrationIndex));
            OnPropertyChanged(nameof(IsCustomIntegrationSelected));
        }

        private void DeleteIntegration()
        {
            if (SelectedCustomIntegration is null)
                return;

            CustomIntegrations.Remove(SelectedCustomIntegration);

            if (CustomIntegrations.Count > 0)
            {
                SelectedCustomIntegrationIndex = CustomIntegrations.Count - 1;
                OnPropertyChanged(nameof(SelectedCustomIntegrationIndex));
            }

            OnPropertyChanged(nameof(IsCustomIntegrationSelected));
        }

        private void BrowseIntegrationLocation()
        {
            if (SelectedCustomIntegration is null)
                return;

            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.Menu_AllFiles}|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            SelectedCustomIntegration.Name = dialog.SafeFileName;
            SelectedCustomIntegration.Location = dialog.FileName;
            OnPropertyChanged(nameof(SelectedCustomIntegration));
        }

        public bool ActivityTrackingEnabled
        {
            get => App.Settings.Prop.EnableActivityTracking;
            set
            {
                App.Settings.Prop.EnableActivityTracking = value;

                if (!value)
                {
                    ShowServerDetailsEnabled = value;
                    DisableAppPatchEnabled = value;
                    DiscordActivityEnabled = value;
                    DiscordActivityJoinEnabled = value;

                    OnPropertyChanged(nameof(ShowServerDetailsEnabled));
                    OnPropertyChanged(nameof(DisableAppPatchEnabled));
                    OnPropertyChanged(nameof(DiscordActivityEnabled));
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                }
            }
        }

        public bool ShowServerDetailsEnabled
        {
            get => App.Settings.Prop.ShowServerDetails;
            set => App.Settings.Prop.ShowServerDetails = value;
        }

        public bool KillRobloxBackgroundEnabled
        {
            get => App.Settings.Prop.KillRobloxBackgroundProcesses;
            set => App.Settings.Prop.KillRobloxBackgroundProcesses = value;
        }

        public bool DiscordActivityEnabled
        {
            get => App.Settings.Prop.UseDiscordRichPresence;
            set
            {
                App.Settings.Prop.UseDiscordRichPresence = value;

                if (!value)
                {
                    DiscordActivityJoinEnabled = value;
                    DiscordAccountOnProfile = value;
                    DiscordServerLocationOnProfile = value;
                    DiscordPlaytimeOnProfile = value;
                    OnPropertyChanged(nameof(DiscordActivityJoinEnabled));
                    OnPropertyChanged(nameof(DiscordAccountOnProfile));
                    OnPropertyChanged(nameof(DiscordServerLocationOnProfile));
                    OnPropertyChanged(nameof(DiscordPlaytimeOnProfile));
                }
            }
        }

        public bool DiscordActivityJoinEnabled
        {
            get => !App.Settings.Prop.HideRPCButtons;
            set => App.Settings.Prop.HideRPCButtons = !value;
        }

        public bool DiscordAccountOnProfile
        {
            get => App.Settings.Prop.ShowAccountOnRichPresence;
            set => App.Settings.Prop.ShowAccountOnRichPresence = value;
        }

        public bool DiscordServerLocationOnProfile
        {
            get => App.Settings.Prop.ShowServerLocationOnRichPresence;
            set => App.Settings.Prop.ShowServerLocationOnRichPresence = value;
        }

        public bool DiscordGamePageButton
        {
            get => App.Settings.Prop.ShowGamePageButton;
            set => App.Settings.Prop.ShowGamePageButton = value;
        }

        public bool DiscordPlaytimeOnProfile
        {
            get => App.Settings.Prop.ShowPlaytimeOnRichPresence;
            set => App.Settings.Prop.ShowPlaytimeOnRichPresence = value;
        }

        public bool DisableAppPatchEnabled
        {
            get => App.Settings.Prop.UseDisableAppPatch;
            set => App.Settings.Prop.UseDisableAppPatch = value;
        }

        public bool ObsIntegrationEnabled
        {
            get => App.Settings.Prop.UseObsIntegration;
            set => App.Settings.Prop.UseObsIntegration = value;
        }

        public string ObsPassword
        {
            get => App.Settings.Prop.ObsPassword;
            set => App.Settings.Prop.ObsPassword = value;
        }

        public string ObsGameScene
        {
            get => App.Settings.Prop.ObsGameScene;
            set => App.Settings.Prop.ObsGameScene = value;
        }

        public string ObsLobbyScene
        {
            get => App.Settings.Prop.ObsLobbyScene;
            set => App.Settings.Prop.ObsLobbyScene = value;
        }

        public bool SyncDiscordToTwitch
        {
            get => App.Settings.Prop.SyncDiscordToTwitch;
            set => App.Settings.Prop.SyncDiscordToTwitch = value;
        }

        public string TwitchChannelId
        {
            get => App.Settings.Prop.TwitchChannelId;
            set => App.Settings.Prop.TwitchChannelId = value;
        }

        public bool SyncTwitchToDiscord
        {
            get => App.Settings.Prop.SyncTwitchToDiscord;
            set => App.Settings.Prop.SyncTwitchToDiscord = value;
        }

        public ObservableCollection<CustomIntegration> CustomIntegrations
        {
            get => App.Settings.Prop.CustomIntegrations;
            set => App.Settings.Prop.CustomIntegrations = value;
        }

        public CustomIntegration? SelectedCustomIntegration { get; set; }
        public int SelectedCustomIntegrationIndex { get; set; }
        public bool IsCustomIntegrationSelected => SelectedCustomIntegration is not null;
    }
}
