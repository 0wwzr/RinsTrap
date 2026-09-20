using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace RinsTrap.UI.ViewModels.Settings
{
    public class StatisticsViewModel : NotifyPropertyChangedViewModel
    {
        private DateTime? _fromDate = DateTime.Today.AddDays(-6);
        private DateTime? _toDate = DateTime.Today;

        public StatisticsViewModel()
        {
            LoadData();
        }

        public ICommand RefreshCommand => new RelayCommand(LoadData);

        public ICommand ApplyDateRangeCommand => new RelayCommand(LoadData);

        public ICommand OpenScreenshotsFolderCommand => new RelayCommand(OpenScreenshotsFolder);

        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (_fromDate == value)
                    return;

                _fromDate = value;
                OnPropertyChanged(nameof(FromDate));
            }
        }

        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (_toDate == value)
                    return;

                _toDate = value;
                OnPropertyChanged(nameof(ToDate));
            }
        }

        public string TodayPlaytime { get; private set; } = "0m";

        public string SelectedRangePlaytime { get; private set; } = "0m";

        public string TotalPlaytime { get; private set; } = "0m";

        public string SessionCount { get; private set; } = "0";

        public string GamesPlayed { get; private set; } = "0";

        public string ActiveDays { get; private set; } = "0";

        public string AverageSession { get; private set; } = "0m";

        public string RangeSummary { get; private set; } = "";

        public ObservableCollection<PlaytimeDayEntry> Days { get; } = new();

        public ObservableCollection<GameStatisticsEntry> Games { get; } = new();

        public bool HasData { get; private set; }

        public bool HasGames => Games.Count > 0;

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

        public int[] ReminderIntervals { get; } = { 15, 30, 45, 60, 90, 120 };

        private void LoadData()
        {
            DateTime today = DateTime.Today;
            DateTime from = (FromDate ?? today.AddDays(-6)).Date;
            DateTime to = (ToDate ?? today).Date;

            if (from > to)
            {
                (from, to) = (to, from);
                FromDate = from;
                ToDate = to;
            }

            var stats = App.PlaytimeStats.Prop;
            long selectedSeconds = 0;
            int activeDays = 0;
            var dailySeconds = new Dictionary<DateTime, long>();

            foreach (var pair in stats.SecondsByDay)
            {
                if (!DateTime.TryParseExact(pair.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    continue;

                if (date < from || date > to)
                    continue;

                dailySeconds[date] = pair.Value;
                selectedSeconds += pair.Value;
                if (pair.Value > 0)
                    activeDays++;
            }

            long todaySeconds = GetSeconds(stats, today);
            long totalSeconds = stats.SecondsByDay.Values.Sum();
            int sessions = stats.SessionCount;
            if (sessions == 0 && totalSeconds > 0)
                sessions = 1;

            Days.Clear();
            DateTime displayFrom = to.AddDays(-30);
            if (displayFrom < from)
                displayFrom = from;

            for (DateTime date = displayFrom; date <= to; date = date.AddDays(1))
            {
                long seconds = dailySeconds.GetValueOrDefault(date);
                Days.Add(new PlaytimeDayEntry
                {
                    DateLabel = date.ToString("ddd, MMM d", CultureInfo.InvariantCulture),
                    Duration = PlaytimeTracker.FormatDuration(seconds),
                    Seconds = seconds
                });
            }

            Games.Clear();
            foreach (var game in stats.Games.Values
                .Where(game => IsGameInRange(game, from, to))
                .OrderByDescending(game => game.SecondsPlayed))
            {
                Games.Add(new GameStatisticsEntry
                {
                    Name = String.IsNullOrWhiteSpace(game.Name) ? $"Game {game.UniverseId}" : game.Name,
                    Duration = PlaytimeTracker.FormatDuration(game.SecondsPlayed),
                    Sessions = game.SessionCount.ToString(CultureInfo.InvariantCulture),
                    LastPlayed = game.LastPlayed == default ? "Unknown" : game.LastPlayed.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)
                });
            }

            TodayPlaytime = PlaytimeTracker.FormatDuration(todaySeconds);
            SelectedRangePlaytime = PlaytimeTracker.FormatDuration(selectedSeconds);
            TotalPlaytime = PlaytimeTracker.FormatDuration(totalSeconds);
            SessionCount = sessions.ToString(CultureInfo.InvariantCulture);
            GamesPlayed = Games.Count.ToString(CultureInfo.InvariantCulture);
            ActiveDays = activeDays.ToString(CultureInfo.InvariantCulture);
            AverageSession = sessions > 0 ? PlaytimeTracker.FormatDuration(selectedSeconds / sessions) : "0m";
            RangeSummary = $"Showing {from:MMM d, yyyy} - {to:MMM d, yyyy}";
            HasData = selectedSeconds > 0 || sessions > 0 || Games.Count > 0;

            OnPropertyChanged(nameof(TodayPlaytime));
            OnPropertyChanged(nameof(SelectedRangePlaytime));
            OnPropertyChanged(nameof(TotalPlaytime));
            OnPropertyChanged(nameof(SessionCount));
            OnPropertyChanged(nameof(GamesPlayed));
            OnPropertyChanged(nameof(ActiveDays));
            OnPropertyChanged(nameof(AverageSession));
            OnPropertyChanged(nameof(RangeSummary));
            OnPropertyChanged(nameof(Days));
            OnPropertyChanged(nameof(Games));
            OnPropertyChanged(nameof(HasGames));
            OnPropertyChanged(nameof(HasData));
        }

        private static bool IsGameInRange(Models.Persistable.GamePlaytimeEntry game, DateTime from, DateTime to)
        {
            if (game.FirstPlayed == default && game.LastPlayed == default)
                return true;

            DateTime first = game.FirstPlayed == default ? game.LastPlayed.Date : game.FirstPlayed.Date;
            DateTime last = game.LastPlayed == default ? game.FirstPlayed.Date : game.LastPlayed.Date;
            return first <= to && last >= from;
        }

        private static long GetSeconds(Models.Persistable.PlaytimeStats stats, DateTime date)
        {
            return stats.SecondsByDay.GetValueOrDefault(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        private static void OpenScreenshotsFolder()
        {
            Directory.CreateDirectory(Paths.Screenshots);
            Process.Start("explorer.exe", Paths.Screenshots);
        }
    }

    public class PlaytimeDayEntry
    {
        public string DateLabel { get; set; } = "";

        public string Duration { get; set; } = "";

        public long Seconds { get; set; }
    }

    public class GameStatisticsEntry
    {
        public string Name { get; set; } = "";

        public string Duration { get; set; } = "";

        public string Sessions { get; set; } = "";

        public string LastPlayed { get; set; } = "";
    }
}
