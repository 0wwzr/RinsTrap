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

        public ICommand ApplyCustomRangeCommand => new RelayCommand(ApplyCustomRange);

        public ICommand OpenGameCommand => new RelayCommand<string>(url =>
        {
            if (!String.IsNullOrEmpty(url))
                Utilities.ShellExecute(url);
        });

        public int[] ReminderIntervals { get; } = { 15, 30, 45, 60, 90, 120 };

        public ObservableCollection<string> RangeOptions { get; } = new()
        {
            "Last 7 Days",
            "Last 30 Days",
            "Last 90 Days",
            "This Month",
            "All Time",
            "Custom"
        };

        public ObservableCollection<RangePresetOption> QuickRangeOptions { get; } = new()
        {
            new("7D", "7D"),
            new("30D", "30D"),
            new("90D", "90D"),
            new("YTD", "YTD"),
            new("All", "All"),
            new("Custom", "Custom")
        };

        private string _selectedRangePreset = "7D";

        public string SelectedRangePreset
        {
            get => _selectedRangePreset;
            set
            {
                if (_selectedRangePreset == value)
                    return;

                _selectedRangePreset = value;
                OnPropertyChanged(nameof(SelectedRangePreset));
                OnPropertyChanged(nameof(IsCustomRange));
                OnPropertyChanged(nameof(RangeSummary));
                LoadData();
            }
        }

        public bool IsCustomRange => SelectedRangePreset == "Custom";

        public string RangeSummary => GetRangeSummary();

        public string BestDayLabel { get; private set; } = "—";

        public string BestDayDuration { get; private set; } = "—";

        public string ActiveStreakLabel { get; private set; } = "—";

        public string TopGameInsight { get; private set; } = "—";

        private DateTime? _customFromDate = DateTime.Today.AddDays(-6);

        public DateTime? CustomFromDate
        {
            get => _customFromDate;
            set
            {
                if (_customFromDate == value)
                    return;

                _customFromDate = value;
                OnPropertyChanged(nameof(CustomFromDate));
                OnPropertyChanged(nameof(RangeSummary));
            }
        }

        private DateTime? _customToDate = DateTime.Today;

        public DateTime? CustomToDate
        {
            get => _customToDate;
            set
            {
                if (_customToDate == value)
                    return;

                _customToDate = value;
                OnPropertyChanged(nameof(CustomToDate));
                OnPropertyChanged(nameof(RangeSummary));
            }
        }

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

        private (DateTime Start, DateTime End) GetSelectedRange()
        {
            DateTime today = DateTime.Today;
            DateTime end = today;
            DateTime start = today.AddDays(-6);

            switch (SelectedRangePreset)
            {
                case "30D":
                case "Last 30 Days":
                    start = today.AddDays(-29);
                    break;
                case "90D":
                case "Last 90 Days":
                    start = today.AddDays(-89);
                    break;
                case "This Month":
                    start = new DateTime(today.Year, today.Month, 1);
                    break;
                case "YTD":
                case "Year to Date":
                    start = new DateTime(today.Year, 1, 1);
                    break;
                case "All":
                case "All Time":
                    start = DateTime.MinValue;
                    end = today;
                    break;
                case "Custom":
                    start = (CustomFromDate ?? today.AddDays(-6)).Date;
                    end = (CustomToDate ?? today).Date;
                    if (start > end)
                    {
                        DateTime swap = start;
                        start = end;
                        end = swap;
                    }
                    break;
                case "7D":
                case "Last 7 Days":
                default:
                    start = today.AddDays(-6);
                    break;
            }

            return (start, end);
        }

        private static string GetRangePresetLabel(string value)
        {
            return value switch
            {
                "7D" => "Last 7 Days",
                "30D" => "Last 30 Days",
                "90D" => "Last 90 Days",
                "YTD" => "Year to Date",
                "All" => "All Time",
                "Custom" => "Custom",
                _ => value
            };
        }

        private string GetRangeSummary()
        {
            var range = GetSelectedRange();

            if (SelectedRangePreset != "Custom")
            {
                return $"Showing {range.Start:MMM d, yyyy} - {range.End:MMM d, yyyy}";
            }

            string fromText = (CustomFromDate ?? range.Start).ToString("MMM d, yyyy");
            string toText = (CustomToDate ?? range.End).ToString("MMM d, yyyy");
            return $"Custom range: {fromText} - {toText}";
        }

        private void LoadData()
        {
            const string LOG_IDENT = "StatisticsViewModel::LoadData";

            var stats = App.PlaytimeStats.Prop;
            var range = GetSelectedRange();

            var selectedDates = new List<DateTime>();
            DateTime date = range.Start.Date;
            while (date <= range.End.Date)
            {
                selectedDates.Add(date);
                date = date.AddDays(1);
            }

            long selectedSeconds = selectedDates.Sum(d => stats.SecondsByDay.GetValueOrDefault(d.ToString("yyyy-MM-dd")));
            long totalSeconds = selectedSeconds;
            long todaySeconds = selectedDates.Contains(DateTime.Today)
                ? stats.SecondsByDay.GetValueOrDefault(DateTime.Today.ToString("yyyy-MM-dd"))
                : 0;

            DateTime weekStart = DateTime.Today.AddDays(-6);
            long weekSeconds = selectedDates
                .Where(d => d >= weekStart.Date && d <= DateTime.Today)
                .Sum(d => stats.SecondsByDay.GetValueOrDefault(d.ToString("yyyy-MM-dd")));

            var gamesInRange = stats.Games.Values
                .Where(g =>
                    (g.FirstPlayed.Date >= range.Start.Date && g.FirstPlayed.Date <= range.End.Date) ||
                    (g.LastPlayed.Date >= range.Start.Date && g.LastPlayed.Date <= range.End.Date))
                .ToList();

            int selectedSessions = gamesInRange.Sum(g => g.SessionCount);
            int selectedGames = gamesInRange.Count;

            if (selectedSessions <= 0 && selectedSeconds > 0)
                selectedSessions = 1;

            TodayPlaytime = PlaytimeTracker.FormatDuration(todaySeconds);
            WeekPlaytime = PlaytimeTracker.FormatDuration(weekSeconds);
            TotalPlaytime = PlaytimeTracker.FormatDuration(totalSeconds);
            SessionCount = selectedSessions.ToString();
            GamesPlayed = selectedGames.ToString();
            ActiveDays = selectedDates.Count(x => stats.SecondsByDay.GetValueOrDefault(x.ToString("yyyy-MM-dd")) > 0).ToString();

            var bestDay = selectedDates
                .Select(day => new { Day = day, Seconds = stats.SecondsByDay.GetValueOrDefault(day.ToString("yyyy-MM-dd")) })
                .OrderByDescending(x => x.Seconds)
                .FirstOrDefault();

            BestDayLabel = bestDay is not null && bestDay.Seconds > 0 ? bestDay.Day.ToString("ddd, MMM d") : "No active day";
            BestDayDuration = bestDay is not null && bestDay.Seconds > 0 ? PlaytimeTracker.FormatDuration(bestDay.Seconds) : "0m";

            int activeStreak = 0;
            DateTime streakDate = DateTime.Today;
            while (stats.SecondsByDay.GetValueOrDefault(streakDate.ToString("yyyy-MM-dd")) > 0)
            {
                activeStreak++;
                streakDate = streakDate.AddDays(-1);
            }
            ActiveStreakLabel = activeStreak > 0 ? $"{activeStreak} day streak" : "No streak";

            long averageSeconds = selectedSeconds > 0 && selectedSessions > 0 ? selectedSeconds / selectedSessions : 0;
            AverageSession = selectedSeconds > 0 && selectedSessions > 0
                ? PlaytimeTracker.FormatDuration(averageSeconds)
                : Strings.Menu_Statistics_NotEnoughData;

            var mostPlayedGame = gamesInRange
                .OrderByDescending(x => x.SecondsPlayed)
                .FirstOrDefault() ?? stats.Games.Values
                    .OrderByDescending(x => x.SecondsPlayed)
                    .FirstOrDefault();

            _mostPlayedUniverseId = mostPlayedGame?.UniverseId ?? 0;
            MostPlayedGame = mostPlayedGame == null
                ? Strings.Menu_Statistics_NotEnoughData
                : ResolveGameDisplayName(mostPlayedGame.Name, mostPlayedGame.UniverseId);
            TopGameInsight = mostPlayedGame == null ? "No favorite game" : $"{MostPlayedGame} • {PlaytimeTracker.FormatDuration(mostPlayedGame.SecondsPlayed)}";

            HasData = selectedSeconds > 0 || selectedSessions > 0 || selectedGames > 0;

            Last7Days.Clear();

            var days = new List<(DateTime Date, long Seconds)>();
            DateTime chartDate = range.End.Date;
            for (int i = Math.Max(0, (range.End.Date - range.Start.Date).Days); i >= 0; i--)
            {
                DateTime day = range.End.Date.AddDays(-i);
                days.Add((day, stats.SecondsByDay.GetValueOrDefault(day.ToString("yyyy-MM-dd"))));
            }

            long maxDaySeconds = Math.Max(days.Max(x => x.Seconds), 1);

            foreach (var day in days)
            {
                double barHeight = day.Seconds <= 0 ? 4 : Math.Min(120, Math.Max(8, 120.0 * day.Seconds / maxDaySeconds));

                Last7Days.Add(new PlaytimeDayEntry
                {
                    Label = day.Date == DateTime.Today ? Strings.Menu_Statistics_Today : day.Date.ToString("ddd, MMM d"),
                    Duration = PlaytimeTracker.FormatDuration(day.Seconds),
                    Seconds = day.Seconds,
                    BarHeight = barHeight
                });
            }

            TopGames.Clear();

            foreach (var game in gamesInRange
                .OrderByDescending(x => x.SecondsPlayed)
                .Take(5))
            {
                TopGames.Add(new GamePlaytimeEntry
                {
                    UniverseId = game.UniverseId,
                    PlaceId = game.PlaceId,
                    Label = ResolveGameDisplayName(game.Name, game.UniverseId),
                    Duration = PlaytimeTracker.FormatDuration(game.SecondsPlayed),
                    Sessions = game.SessionCount.ToString()
                });
            }

            HasGames = TopGames.Any();

            App.Logger.WriteLine(LOG_IDENT, $"Loaded statistics (range={range.Start:yyyy-MM-dd}..{range.End:yyyy-MM-dd}, selectedSeconds={selectedSeconds}s, total={totalSeconds}s, sessions={selectedSessions})");

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
            OnPropertyChanged(nameof(RangeSummary));
            OnPropertyChanged(nameof(BestDayLabel));
            OnPropertyChanged(nameof(BestDayDuration));
            OnPropertyChanged(nameof(ActiveStreakLabel));
            OnPropertyChanged(nameof(TopGameInsight));

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
                    {
                        entry.Label = details.Data.Name;

                        if (entry.UniverseId == _mostPlayedUniverseId)
                        {
                            MostPlayedGame = entry.Label;
                            TopGameInsight = $"{MostPlayedGame} • {PlaytimeTracker.FormatDuration(App.PlaytimeStats.Prop.Games.GetValueOrDefault(entry.UniverseId)?.SecondsPlayed ?? 0)}";
                            OnPropertyChanged(nameof(MostPlayedGame));
                            OnPropertyChanged(nameof(TopGameInsight));
                        }
                    }

                    if (entry.UniverseId == _mostPlayedUniverseId)
                    {
                        MostPlayedGameIcon = entry.IconUrl;
                        OnPropertyChanged(nameof(MostPlayedGameIcon));
                    }
                }
            }
            catch (Exception ex)
            {
                    App.Logger.WriteException("StatisticsViewModel::LoadGameData", ex);
            }
        }

        private static string ResolveGameDisplayName(string? rawName, long universeId)
        {
            if (!String.IsNullOrWhiteSpace(rawName))
                return rawName.Trim();

            return universeId != 0 ? $"Game {universeId}" : "Unknown game";
        }

        private void ApplyCustomRange()
        {
            if (SelectedRangePreset != "Custom")
                return;

            DateTime today = DateTime.Today;
            DateTime start = (CustomFromDate ?? today.AddDays(-6)).Date;
            DateTime end = (CustomToDate ?? today).Date;

            if (start > end)
            {
                DateTime swap = start;
                start = end;
                end = swap;
                CustomFromDate = start;
                CustomToDate = end;
            }

            LoadData();
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

    public class RangePresetOption
    {
        public string Label { get; set; }

        public string Value { get; set; }

        public RangePresetOption(string label, string value)
        {
            Label = label;
            Value = value;
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