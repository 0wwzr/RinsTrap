namespace RinsTrap
{
    /// <summary>
    /// Tracks the duration of a Roblox play session, persists playtime statistics,
    /// and raises notifications for session summaries and break reminders.
    /// </summary>
    public class PlaytimeTracker : IDisposable
    {
        private readonly DateTime _sessionStart = DateTime.Now;

        private readonly CancellationTokenSource _cts = new();

        private Task? _tickTask;

        private long _tickSeconds = 0;

        private bool _disposed = false;

        private long _gameUniverseId = 0;

        private long _gamePlaceId = 0;

        private string _gameName = "";

        private DateTime _gameStart;

        /// <summary>
        /// Invoked when a notification should be shown to the user
        /// </summary>
        public Action<string, string>? Notify { get; set; }

        public TimeSpan SessionDuration => DateTime.Now - _sessionStart;

        public void OnGameJoin(long universeId, long placeId, string? name)
        {
            _gameUniverseId = universeId;
            _gamePlaceId = placeId;
            _gameName = name ?? "";
            _gameStart = DateTime.Now;
        }

        public void OnGameLeave()
        {
            if (_gameUniverseId == 0)
                return;

            long seconds = (long)(DateTime.Now - _gameStart).TotalSeconds;

            if (seconds > 0)
            {
                var stats = App.PlaytimeStats.Prop;

                if (stats.Games.TryGetValue(_gameUniverseId, out var entry))
                {
                    entry.SecondsPlayed += seconds;
                    entry.SessionCount++;
                    entry.LastPlayed = DateTime.Now;

                    if (!String.IsNullOrEmpty(_gameName))
                        entry.Name = _gameName;
                }
                else
                {
                    stats.Games[_gameUniverseId] = new Models.Persistable.GamePlaytimeEntry
                    {
                        UniverseId = _gameUniverseId,
                        PlaceId = _gamePlaceId,
                        Name = _gameName,
                        SecondsPlayed = seconds,
                        SessionCount = 1,
                        LastPlayed = DateTime.Now
                    };
                }

                App.PlaytimeStats.Save();
            }

            _gameUniverseId = 0;
        }

        public void Start()
        {
            const string LOG_IDENT = "PlaytimeTracker::Start";

            App.Logger.WriteLine(LOG_IDENT, $"Starting playtime tracking (session started at {_sessionStart:O})");

            _tickTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(60), _cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    _tickSeconds += 60;

                    App.PlaytimeStats.Prop.SessionSeconds = _tickSeconds;
                    App.PlaytimeStats.Save();

                    if (App.Settings.Prop.EnablePlaytimeReminder)
                    {
                        int interval = Math.Max(15, App.Settings.Prop.PlaytimeReminderMinutes);
                        long intervalSeconds = interval * 60L;

                        if (_tickSeconds % intervalSeconds == 0)
                        {
                            Notify?.Invoke(
                                Strings.Notifications_PlaytimeReminder_Title,
                                String.Format(Strings.Notifications_PlaytimeReminder_Text, FormatDuration(_tickSeconds))
                            );
                        }
                    }
                }
            }, _cts.Token);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            const string LOG_IDENT = "PlaytimeTracker::Dispose";

            _cts.Cancel();

            try
            {
                _tickTask?.Wait();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT, ex);
            }

            long seconds = (long)SessionDuration.TotalSeconds;

            App.PlaytimeStats.Prop.SessionSeconds = 0;

            if (seconds <= 0)
                return;

            if (_gameUniverseId != 0)
            {
                long gameSeconds = (long)(DateTime.Now - _gameStart).TotalSeconds;

                if (gameSeconds > 0)
                    OnGameLeave();
                else
                    _gameUniverseId = 0;
            }

            App.PlaytimeStats.Prop.SessionCount++;
            App.PlaytimeStats.Prop.SecondsByDay[DateTime.Now.ToString("yyyy-MM-dd")] =
                App.PlaytimeStats.Prop.SecondsByDay.GetValueOrDefault(DateTime.Now.ToString("yyyy-MM-dd")) + seconds;
            App.PlaytimeStats.Save();

            App.Logger.WriteLine(LOG_IDENT, $"Session ended, recorded {seconds} seconds");

            if (App.Settings.Prop.NotifySessionEnd)
            {
                Notify?.Invoke(
                    Strings.Notifications_SessionEnd_Title,
                    String.Format(Strings.Notifications_SessionEnd_Text, FormatDuration(seconds))
                );
            }
        }

        public static string FormatDuration(long seconds)
        {
            if (seconds < 60)
                return $"{seconds}s";

            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";

            return $"{timeSpan.Minutes}m";
        }
    }
}