using System.Windows;
using System.Windows.Input;
using RinsTrap.Integrations;
using CommunityToolkit.Mvvm.Input;

namespace RinsTrap.UI.ViewModels.ContextMenu
{
    internal class ServerInformationViewModel : NotifyPropertyChangedViewModel
    {
        private readonly ActivityWatcher _activityWatcher;

        public string InstanceId => _activityWatcher.Data.JobId;

        public string ServerType => _activityWatcher.Data.ServerType.ToTranslatedString();

        public string AccountName { get; private set; } = Strings.Common_Loading;

        public string AccountAvatarUrl { get; private set; } = "";

        public string GameName { get; private set; } = Strings.Common_Loading;

        public string PlayerCount { get; private set; } = Strings.Common_Loading;

        public string ServerLocation { get; private set; } = Strings.Common_Loading;

        public string ServerUptime => FormatElapsed(_activityWatcher.Data.TimeJoined);

        public string CurrentPlayerPlaytime => FormatElapsed(_activityWatcher.Data.TimeJoined);

        public Visibility ServerLocationVisibility => App.Settings.Prop.ShowServerDetails ? Visibility.Visible : Visibility.Collapsed;

        public ICommand CopyInstanceIdCommand => new RelayCommand(CopyInstanceId);

        public ICommand CopyInviteLinkCommand => new RelayCommand(CopyInviteLink);

        public ServerInformationViewModel(Watcher watcher)
        {
            _activityWatcher = watcher.ActivityWatcher!;

            if (ServerLocationVisibility == Visibility.Visible)
                QueryServerLocation();

            _ = LoadRobloxDetails();
        }

        private async Task LoadRobloxDetails()
        {
            try
            {
                var activity = _activityWatcher.Data;

                if (activity.UserId != 0)
                {
                    var userDetails = await UserDetails.Fetch(activity.UserId);
                    AccountName = $"{userDetails.Data.DisplayName} (@{userDetails.Data.Name})";
                    AccountAvatarUrl = userDetails.Thumbnail.ImageUrl ?? "";
                }

                if (activity.UniverseId != 0)
                {
                    await UniverseDetails.FetchSingle(activity.UniverseId);
                    var universeDetails = UniverseDetails.LoadFromCache(activity.UniverseId);

                    if (universeDetails is not null)
                    {
                        GameName = universeDetails.Data.Name;
                        PlayerCount = $"{universeDetails.Data.Playing:N0} playing";
                    }
                }

                OnPropertyChanged(nameof(AccountName));
                OnPropertyChanged(nameof(AccountAvatarUrl));
                OnPropertyChanged(nameof(GameName));
                OnPropertyChanged(nameof(PlayerCount));
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ServerInformationViewModel::LoadRobloxDetails", ex);
            }
        }

        public async void QueryServerLocation()
        {
            string? location = await _activityWatcher.Data.QueryServerLocation();

            if (String.IsNullOrEmpty(location))
                ServerLocation = Strings.Common_NotAvailable;
            else
                ServerLocation = location;

            OnPropertyChanged(nameof(ServerLocation));
        }

        private void CopyInstanceId() => Clipboard.SetDataObject(InstanceId);

        private void CopyInviteLink() => Clipboard.SetDataObject(_activityWatcher.Data.GetInviteDeeplink());

        private static string FormatElapsed(DateTime start)
        {
            TimeSpan elapsed = DateTime.Now - start;
            if (elapsed.TotalSeconds < 0)
                return "0m";

            if (elapsed.TotalDays >= 1)
                return $"{(int)elapsed.TotalDays}d {elapsed.Hours}h {elapsed.Minutes}m";

            if (elapsed.TotalHours >= 1)
                return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";

            return $"{Math.Max(0, elapsed.Minutes)}m";
        }
    }
}
