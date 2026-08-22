using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace RinsTrap.UI.ViewModels.Settings
{
    public class StatisticsViewModel : NotifyPropertyChangedViewModel
    {
        public ICommand ResetStatisticsCommand => new RelayCommand(ResetStatistics);

        public ICommand OpenScreenshotsFolderCommand => new RelayCommand(OpenScreenshotsFolder);

        public ICommand OpenGameCommand => new RelayCommand<string>(url =>
        {
            if (!String.IsNullOrEmpty(url))
                Utilities.ShellExecute(url);
        });

        public int[] ReminderIntervals { get; } = { 15, 30, 45, 60, 90, 120 };

        public StatisticsViewModel()
        {
            LoadData();
        }

        public string TodayPlaytime { get; private set; } = "";

        public string WeekPlaytime { get; private set; } = "";

        public string TotalPlaytime { get; private set; } = "";

        public string SessionCount { get; private set; } = "";

        public string GamesPlayed { get; private set; } = "";

        public string AverageSession { get; private set; } = "";

        public string ActiveDays { get; private set; } = "";

        public string MostPlayedGame { get; private set; } = "";

        public string? MostPlayedGameIcon { get; private set; } = null;

        private long _mostPlayedUniverseId = 0;

        public ObservableCollection<PlaytimeDayEntry> Last7Days { get; set; } = new();

        public ObservableCollection<GamePlaytimeEntry> TopGames { get; set; } = new();

        public bool HasGames { get; private set; } = false;

        public bool HasData { get; private set; } = true;

        public bool NotifySessionEnd
        {
            get => App.Settings.Prop.NotifySessionEnd;
            set => App.Settings.Prop.NotifySessionEnd = value;
        }

        public bool EnablePlaytimeReminder
        {
            get => App.Settings.Prop.EnablePlaytimeReminder;
            set => App.Settings.Prop.EnablePlaytimeReminder = value;
        }

        public int SelectedReminderInterval
        {
            get => App.Settings.Prop.PlaytimeReminderMinutes;
            set => App.Settings.Prop.PlaytimeReminderMinutes = value;
        }

        private void LoadData()
        {
            const string LOG_IDENT = "StatisticsViewModel::LoadData";

            var stats = App.PlaytimeStats.Prop;

            string todayKey = DateTime.Now.ToString("yyyy-MM-dd");
            long todaySeconds = stats.SecondsByDay.GetValueOrDefault(todayKey);
            long totalSeconds = stats.SecondsByDay.Values.Sum();

            DateTime monday = DateTime.Now.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            long weekSeconds = 0;

            for (int i = 0; i < 7; i++)
                weekSeconds += stats.SecondsByDay.GetValueOrDefault(monday.AddDays(i).ToString("yyyy-MM-dd"));

            totalSeconds += stats.SessionSeconds;
            todaySeconds += stats.SessionSeconds;

            TodayPlaytime = PlaytimeTracker.FormatDuration(todaySeconds);
            WeekPlaytime = PlaytimeTracker.FormatDuration(weekSeconds);
            TotalPlaytime = PlaytimeTracker.FormatDuration(totalSeconds);
            SessionCount = stats.SessionCount.ToString();
            GamesPlayed = stats.Games.Count.ToString();
            ActiveDays = stats.SecondsByDay.Count(x => x.Value > 0).ToString();

            AverageSession = stats.SessionCount > 0
                ? PlaytimeTracker.FormatDuration(totalSeconds / stats.SessionCount)
                : Strings.Menu_Statistics_NotEnoughData;

            var mostPlayedGame = stats.Games.Values.OrderByDescending(x => x.SecondsPlayed).FirstOrDefault();
            _mostPlayedUniverseId = mostPlayedGame?.UniverseId ?? 0;
            MostPlayedGame = mostPlayedGame == null
                ? Strings.Menu_Statistics_NotEnoughData
                : (String.IsNullOrEmpty(mostPlayedGame.Name) ? $"Game {mostPlayedGame.UniverseId}" : mostPlayedGame.Name);

            HasData = totalSeconds > 0 || stats.SessionCount > 0;

            Last7Days.Clear();

            var days = new List<(DateTime Date, long Seconds)>();

            for (int i = 6; i >= 0; i--)
            {
                DateTime date = DateTime.Now.Date.AddDays(-i);
                days.Add((date, stats.SecondsByDay.GetValueOrDefault(date.ToString("yyyy-MM-dd"))));
            }

            long maxDaySeconds = Math.Max(days.Max(x => x.Seconds), 1);

            foreach (var day in days)
            {
                double barHeight = day.Seconds <= 0 ? 4 : Math.Min(120, Math.Max(8, 120.0 * day.Seconds / maxDaySeconds));

                Last7Days.Add(new PlaytimeDayEntry
                {
                    Label = day.Date == DateTime.Now.Date ? Strings.Menu_Statistics_Today : day.Date.ToString("ddd, MMM d"),
                    Duration = PlaytimeTracker.FormatDuration(day.Seconds),
                    Seconds = day.Seconds,
                    BarHeight = barHeight
                });
            }

            TopGames.Clear();

            foreach (var game in stats.Games.Values
                .OrderByDescending(x => x.SecondsPlayed)
                .Take(5))
            {
                TopGames.Add(new GamePlaytimeEntry
                {
                    UniverseId = game.UniverseId,
                    PlaceId = game.PlaceId,
                    Label = String.IsNullOrEmpty(game.Name) ? $"Game {game.UniverseId}" : game.Name,
                    Duration = PlaytimeTracker.FormatDuration(game.SecondsPlayed),
                    Sessions = game.SessionCount.ToString()
                });
            }

            HasGames = TopGames.Any();

            App.Logger.WriteLine(LOG_IDENT, $"Loaded statistics (today={todaySeconds}s, week={weekSeconds}s, total={totalSeconds}s, sessions={stats.SessionCount})");

            OnPropertyChanged(nameof(TodayPlaytime));
            OnPropertyChanged(nameof(WeekPlaytime));
            OnPropertyChanged(nameof(TotalPlaytime));
            OnPropertyChanged(nameof(SessionCount));
            OnPropertyChanged(nameof(GamesPlayed));
            OnPropertyChanged(nameof(AverageSession));
            OnPropertyChanged(nameof(ActiveDays));
            OnPropertyChanged(nameof(MostPlayedGame));
            OnPropertyChanged(nameof(MostPlayedGameIcon));
            OnPropertyChanged(nameof(Last7Days));
            OnPropertyChanged(nameof(TopGames));
            OnPropertyChanged(nameof(HasGames));
            OnPropertyChanged(nameof(HasData));

            LoadGameData();
        }

        private async void LoadGameData()
        {
            var entries = TopGames.Where(x => x.UniverseId != 0).ToList();

            if (!entries.Any())
                return;

            try
            {
                string universeIds = String.Join(',', entries.Select(x => x.UniverseId).Distinct());

                await UniverseDetails.FetchBulk(universeIds);

                foreach (var entry in entries)
                {
                    var details = UniverseDetails.LoadFromCache(entry.UniverseId);

                    if (details is null)
                        continue;

                    entry.IconUrl = details.Thumbnail?.ImageUrl;

                    if (!String.IsNullOrEmpty(details.Data?.Name))
                        entry.Label = details.Data.Name;

                    if (entry.UniverseId == _mostPlayedUniverseId)
                    {
                        MostPlayedGameIcon = entry.IconUrl;
                        MostPlayedGame = entry.Label;
                        OnPropertyChanged(nameof(MostPlayedGameIcon));
                        OnPropertyChanged(nameof(MostPlayedGame));
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("StatisticsViewModel::LoadGameData", ex);
            }
        }

        private void ResetStatistics()
        {
            var result = Frontend.ShowMessageBox(Strings.Menu_Statistics_ResetPrompt, MessageBoxImage.Warning, MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;

            App.PlaytimeStats.Prop = new();
            App.PlaytimeStats.Save();

            LoadData();
        }

        private void OpenScreenshotsFolder()
        {
            Directory.CreateDirectory(Paths.Screenshots);
            Process.Start("explorer.exe", Paths.Screenshots);
        }
    }

    public class PlaytimeDayEntry
    {
        public string Label { get; set; } = "";

        public string Duration { get; set; } = "";

        public long Seconds { get; set; } = 0;

        public double BarHeight { get; set; } = 0;
    }

    public class GamePlaytimeEntry : NotifyPropertyChangedViewModel
    {
        private long _universeId = 0;

        public long UniverseId
        {
            get => _universeId;
            set
            {
                _universeId = value;
                OnPropertyChanged(nameof(UniverseId));
            }
        }

        private long _placeId = 0;

        public long PlaceId
        {
            get => _placeId;
            set
            {
                _placeId = value;
                OnPropertyChanged(nameof(PlaceId));
                OnPropertyChanged(nameof(GameUrl));
            }
        }

        public string GameUrl => $"https://www.roblox.com/games/{(PlaceId != 0 ? PlaceId : UniverseId)}";

        private string _label = "";

        public string Label
        {
            get => _label;
            set
            {
                _label = value;
                OnPropertyChanged(nameof(Label));
            }
        }

        private string? _iconUrl = null;

        public string? IconUrl
        {
            get => _iconUrl;
            set
            {
                _iconUrl = value;
                OnPropertyChanged(nameof(IconUrl));
            }
        }

        private string _duration = "";

        public string Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                OnPropertyChanged(nameof(Duration));
            }
        }

        private string _sessions = "";

        public string Sessions
        {
            get => _sessions;
            set
            {
                _sessions = value;
                OnPropertyChanged(nameof(Sessions));
            }
        }
    }
}