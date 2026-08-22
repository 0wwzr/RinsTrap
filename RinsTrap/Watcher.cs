using System.Windows;

using RinsTrap.AppData;
using RinsTrap.Integrations;
using RinsTrap.Models;

namespace RinsTrap
{
    public class Watcher : IDisposable
    {
        private readonly InterProcessLock _lock = new("Watcher");

        private readonly WatcherData? _watcherData;
        
        private readonly NotifyIconWrapper? _notifyIcon;

        public readonly ActivityWatcher? ActivityWatcher;

        public readonly DiscordRichPresence? RichPresence;

        public readonly PlaytimeTracker? PlaytimeTracker;

        private readonly ObsIntegration? _obsIntegration;

        public int ProcessId => _watcherData?.ProcessId ?? 0;

        public Watcher()
        {
            const string LOG_IDENT = "Watcher";

            if (!_lock.IsAcquired)
            {
                App.Logger.WriteLine(LOG_IDENT, "Watcher instance already exists");
                return;
            }

            string? watcherDataArg = App.LaunchSettings.WatcherFlag.Data;

            if (String.IsNullOrEmpty(watcherDataArg))
            {
#if DEBUG
                string path = new RobloxPlayerData().ExecutablePath;
                if (!File.Exists(path))
                    throw new ApplicationException("Roblox player is not been installed");

                using var gameClientProcess = Process.Start(path);

                _watcherData = new() { ProcessId = gameClientProcess.Id };
#else
                throw new Exception("Watcher data not specified");
#endif
            }
            else
            {
                _watcherData = JsonSerializer.Deserialize<WatcherData>(Encoding.UTF8.GetString(Convert.FromBase64String(watcherDataArg)));
            }

            if (_watcherData is null)
                throw new Exception("Watcher data is invalid");

            if (App.Settings.Prop.EnableActivityTracking)
            {
                ActivityWatcher = new(_watcherData.LogFile);

                if (App.Settings.Prop.UseDisableAppPatch)
                {
                    ActivityWatcher.OnAppClose += delegate
                    {
                        App.Logger.WriteLine(LOG_IDENT, "Received desktop app exit, closing Roblox");
                        using var process = Process.GetProcessById(_watcherData.ProcessId);
                        process.CloseMainWindow();
                    };
                }

                if (App.Settings.Prop.UseDiscordRichPresence)
                    RichPresence = new(ActivityWatcher);

                if (App.Settings.Prop.UseObsIntegration)
                {
                    _obsIntegration = new ObsIntegration();
                    _obsIntegration.Connect();
                }
            }

            if (!App.LaunchSettings.TestModeFlag.Active)
            {
                PlaytimeTracker = new PlaytimeTracker
                {
                    Notify = (title, message) =>
                    {
                        try
                        {
                            if (_notifyIcon is null)
                                return;

                            Application.Current.Dispatcher.Invoke(() => _notifyIcon.ShowAlert(title, message, 5, null));
                        }
                        catch (Exception ex)
                        {
                            App.Logger.WriteException(LOG_IDENT, ex);
                        }
                    }
                };

                if (ActivityWatcher is not null)
                {
                    ActivityWatcher.OnGameJoin += (_, _) =>
                    {
                        var data = ActivityWatcher.Data;

                        PlaytimeTracker.OnGameJoin(
                            data.UniverseId,
                            data.PlaceId,
                            data.UniverseDetails?.Data.Name
                        );

                        _obsIntegration?.SwitchToGameScene();
                    };

                    ActivityWatcher.OnGameLeave += (_, _) =>
                    {
                        PlaytimeTracker.OnGameLeave();
                        _obsIntegration?.SwitchToLobbyScene();
                    };
                }
            }

            _notifyIcon = new(this);
        }

        public void KillRobloxProcess() => CloseProcess(_watcherData!.ProcessId, true);

        public void CloseProcess(int pid, bool force = false)
        {
            const string LOG_IDENT = "Watcher::CloseProcess";

            try
            {
                using var process = Process.GetProcessById(pid);

                App.Logger.WriteLine(LOG_IDENT, $"Killing process '{process.ProcessName}' (pid={pid}, force={force})");

                if (process.HasExited)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"PID {pid} has already exited");
                    return;
                }

                if (force)
                    process.Kill();
                else
                    process.CloseMainWindow();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, $"PID {pid} could not be closed");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public async Task Run()
        {
            if (!_lock.IsAcquired || _watcherData is null)
                return;

            ActivityWatcher?.Start();
            PlaytimeTracker?.Start();

            while (Utilities.GetProcessesSafe().Any(x => x.Id == _watcherData.ProcessId))
                await Task.Delay(1000);

            if (App.Settings.Prop.KillRobloxBackgroundProcesses)
            {
                foreach (var process in Utilities.GetProcessesSafe().Where(x => x.ProcessName.StartsWith("Roblox", StringComparison.OrdinalIgnoreCase)))
                    CloseProcess(process.Id, true);
            }

            if (_watcherData.AutoclosePids is not null)
            {
                foreach (int pid in _watcherData.AutoclosePids)
                    CloseProcess(pid);
            }

            if (App.LaunchSettings.TestModeFlag.Active)
                Process.Start(Paths.Process, "-settings -testmode");
        }

        public void Dispose()
        {
            App.Logger.WriteLine("Watcher::Dispose", "Disposing Watcher");

            PlaytimeTracker?.Dispose();
            _notifyIcon?.Dispose();
            RichPresence?.Dispose();
            _obsIntegration?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
