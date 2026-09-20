using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace RinsTrap.UI.ViewModels.Settings
{
    public class StatisticsViewModel : NotifyPropertyChangedViewModel
    {
        public StatisticsViewModel()
        {
            LoadData();
        }

        public ICommand RefreshCommand => new RelayCommand(LoadData);

        public ICommand OpenScreenshotsFolderCommand => new RelayCommand(OpenScreenshotsFolder);

        public string TodayPlaytime { get; private set; } = "0m";

        public string WeekPlaytime { get; private set; } = "0m";

        public string TotalPlaytime { get; private set; } = "0m";

        public string SessionCount { get; private set; } = "0";

        public string GamesPlayed { get; private set; } = "0";

        public string ActiveDays { get; private set; } = "0";

        public string MostPlayedGame { get; private set; } = "No recorded games";

        public string AverageSession { get; private set; } = "0m";

        public string RangeSummary { get; private set; } = "Last 7 days";

        public ObservableCollection<PlaytimeDayEntry> Last7Days { get; } = new();

        public bool HasData { get; private set; }

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
            var stats = App.PlaytimeStats.Prop;
            DateTime today = DateTime.Today;
            long todaySeconds = GetSeconds(stats, today);
            long weekSeconds = 0;
            long totalSeconds = stats.SecondsByDay.Values.Sum();
            int activeDays = 0;

            Last7Days.Clear();
            for (int offset = 6; offset >= 0; offset--)
            {
                DateTime day = today.AddDays(-offset);
                long seconds = GetSeconds(stats, day);
                weekSeconds += seconds;

                if (seconds > 0)
                    activeDays++;

                Last7Days.Add(new PlaytimeDayEntry
                {
                    Label = day == today ? "Today" : day.ToString("ddd", CultureInfo.InvariantCulture),
                    Duration = PlaytimeTracker.FormatDuration(seconds),
                    Seconds = seconds
                });
            }

            var games = stats.Games.Values
                .OrderByDescending(game => game.SecondsPlayed)
                .ToList();
            int sessions = stats.SessionCount;

            if (sessions == 0 && totalSeconds > 0)
                sessions = 1;

            TodayPlaytime = PlaytimeTracker.FormatDuration(todaySeconds);
            WeekPlaytime = PlaytimeTracker.FormatDuration(weekSeconds);
            TotalPlaytime = PlaytimeTracker.FormatDuration(totalSeconds);
            SessionCount = sessions.ToString(CultureInfo.InvariantCulture);
            GamesPlayed = games.Count.ToString(CultureInfo.InvariantCulture);
            ActiveDays = activeDays.ToString(CultureInfo.InvariantCulture);
            AverageSession = sessions > 0 ? PlaytimeTracker.FormatDuration(totalSeconds / sessions) : "0m";
            MostPlayedGame = games.FirstOrDefault()?.Name ?? "No recorded games";
            HasData = totalSeconds > 0 || sessions > 0 || games.Count > 0;

            OnPropertyChanged(nameof(TodayPlaytime));
            OnPropertyChanged(nameof(WeekPlaytime));
            OnPropertyChanged(nameof(TotalPlaytime));
            OnPropertyChanged(nameof(SessionCount));
            OnPropertyChanged(nameof(GamesPlayed));
            OnPropertyChanged(nameof(ActiveDays));
            OnPropertyChanged(nameof(AverageSession));
            OnPropertyChanged(nameof(MostPlayedGame));
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(Last7Days));
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
        public string Label { get; set; } = "";

        public string Duration { get; set; } = "";

        public long Seconds { get; set; }
    }
}
