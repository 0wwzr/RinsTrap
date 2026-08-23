using System.Diagnostics;

namespace RinsTrap.Integrations
{
    public class AutoRelaunchService : IDisposable
    {
        private static AutoRelaunchService? _instance;
        public static AutoRelaunchService Instance => _instance ??= new AutoRelaunchService();

        private System.Windows.Forms.Timer? _checkTimer;
        private readonly Dictionary<int, DateTime> _crashTimestamps = new();
        private readonly HashSet<int> _relaunchingProcesses = new();
        private bool _disposed;

        public bool IsRunning => _checkTimer?.Enabled ?? false;
        public event EventHandler? StatusChanged;

        private AutoRelaunchService() { }

        public void Start()
        {
            if (_checkTimer != null && _checkTimer.Enabled)
                return;

            if (_checkTimer == null)
            {
                _checkTimer = new System.Windows.Forms.Timer();
                _checkTimer.Tick += CheckTimer_Tick;
            }

            _checkTimer.Interval = 5000; // Check every 5 seconds
            _checkTimer.Enabled = true;

            StatusChanged?.Invoke(this, EventArgs.Empty);
            App.Logger.WriteLine("AutoRelaunchService", "Auto-relaunch service started");
        }

        public void Stop()
        {
            if (_checkTimer != null)
            {
                _checkTimer.Enabled = false;
            }

            _crashTimestamps.Clear();
            _relaunchingProcesses.Clear();

            StatusChanged?.Invoke(this, EventArgs.Empty);
            App.Logger.WriteLine("AutoRelaunchService", "Auto-relaunch service stopped");
        }

        private void CheckTimer_Tick(object? sender, EventArgs e)
        {
            if (!App.Settings.Prop.AutoRelaunchEnabled)
                return;

            CheckAndRelaunch();
        }

        public void CheckAndRelaunch()
        {
            var runningInstances = InstanceManager.Instance.RunningInstances.ToList();

            foreach (var instance in runningInstances)
            {
                try
                {
                    // Check if process is still running
                    if (Utilities.IsProcessRunning(instance.ProcessId))
                        continue;

                    // Process has exited
                    if (_crashTimestamps.ContainsKey(instance.ProcessId))
                        continue; // Already handled

                    _crashTimestamps[instance.ProcessId] = DateTime.Now;

                    // Find the account
                    var account = App.Settings.Prop.MultiInstanceAccounts
                        .FirstOrDefault(a => a.ProcessId == instance.ProcessId);

                    if (account == null)
                        continue;

                    InstanceLogger.Instance.LogError(
                        instance.ProcessId, 
                        instance.Name, 
                        "Instance crashed or was closed unexpectedly");

                    // Schedule relaunch
                    ScheduleRelaunch(account, instance);
                }
                catch { }
            }

            // Clean up old crash timestamps
            var oldKeys = _crashTimestamps
                .Where(kvp => (DateTime.Now - kvp.Value).TotalMinutes > 5)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldKeys)
            {
                _crashTimestamps.Remove(key);
            }
        }

        private void ScheduleRelaunch(MultiInstanceAccount account, RunningInstance instance)
        {
            if (_relaunchingProcesses.Contains(account.ProcessId))
                return;

            _relaunchingProcesses.Add(account.ProcessId);

            int delaySeconds = App.Settings.Prop.AutoRelaunchDelaySeconds;

            InstanceLogger.Instance.LogInfo(
                instance.ProcessId,
                instance.Name,
                $"Auto-relaunching in {delaySeconds} seconds...");

            Task.Delay(delaySeconds * 1000).ContinueWith(_ =>
            {
                try
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        // Clear the old process ID
                        account.ProcessId = 0;

                        // Launch new instance
                        LaunchHandler.LaunchRoblox(Enums.LaunchMode.Player);

                        InstanceLogger.Instance.LogInfo(
                            0,
                            instance.Name,
                            "Auto-relaunch initiated");
                    });
                }
                catch (Exception ex)
                {
                    InstanceLogger.Instance.LogError(
                        0,
                        instance.Name,
                        $"Auto-relaunch failed: {ex.Message}");
                }
                finally
                {
                    _relaunchingProcesses.Remove(account.ProcessId);
                }
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _checkTimer?.Dispose();
                _checkTimer = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
