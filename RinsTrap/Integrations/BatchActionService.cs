using System.Diagnostics;

namespace RinsTrap.Integrations
{
    public class BatchActionService
    {
        private static BatchActionService? _instance;
        public static BatchActionService Instance => _instance ??= new BatchActionService();

        private BatchActionService() { }

        public void KillSelectedInstances(List<int> processIds)
        {
            foreach (var pid in processIds)
            {
                try
                {
                    var process = Process.GetProcessById(pid);
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                        InstanceLogger.Instance.LogInfo(pid, "Instance", "Killed via batch action");
                    }
                }
                catch (Exception ex)
                {
                    InstanceLogger.Instance.LogError(pid, "Instance", $"Failed to kill: {ex.Message}");
                }
            }
        }

        public void LaunchSelectedAccounts(List<Models.MultiInstanceAccount> accounts)
        {
            foreach (var account in accounts)
            {
                try
                {
                    // Launch with account settings
                    LaunchHandler.LaunchRoblox(Enums.LaunchMode.Player);
                    InstanceLogger.Instance.LogInfo(0, account.Name, "Launched via batch action");
                    
                    // Wait between launches to avoid conflicts
                    Task.Delay(2000).Wait();
                }
                catch (Exception ex)
                {
                    InstanceLogger.Instance.LogError(0, account.Name, $"Launch failed: {ex.Message}");
                }
            }
        }

        public void SendKeyboardToAll(List<int> processIds, string key)
        {
            foreach (var pid in processIds)
            {
                try
                {
                    // This would need P/Invoke for SetForegroundWindow and SendKeys
                    // For now, just log the action
                    InstanceLogger.Instance.LogInfo(pid, "Instance", $"Batch keyboard: {key}");
                }
                catch (Exception ex)
                {
                    InstanceLogger.Instance.LogError(pid, "Instance", $"Keyboard send failed: {ex.Message}");
                }
            }
        }

        public void RestartAllInstances()
        {
            var instances = InstanceManager.Instance.RunningInstances.ToList();
            
            // Kill all
            foreach (var instance in instances)
            {
                try
                {
                    var process = Process.GetProcessById(instance.ProcessId);
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                }
                catch { }
            }

            // Wait a bit
            Task.Delay(3000).Wait();

            // Relaunch all auto-launch accounts
            var accounts = App.Settings.Prop.MultiInstanceAccounts
                .Where(a => a.AutoLaunch)
                .ToList();

            foreach (var account in accounts)
            {
                try
                {
                    LaunchHandler.LaunchRoblox(Enums.LaunchMode.Player);
                    Task.Delay(2000).Wait();
                }
                catch { }
            }

            InstanceLogger.Instance.LogInfo(0, "All", "Batch restart initiated");
        }
    }
}
