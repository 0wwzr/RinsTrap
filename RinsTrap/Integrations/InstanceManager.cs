using System.Collections.ObjectModel;
using System.Diagnostics;

namespace RinsTrap.Integrations
{
    public class InstanceManager
    {
        private static InstanceManager? _instance;
        public static InstanceManager Instance => _instance ??= new InstanceManager();

        public ObservableCollection<RunningInstance> RunningInstances { get; } = new();

        public event EventHandler? InstancesChanged;

        private InstanceManager() { }

        public void RegisterInstance(int processId, string name = "", string accountName = "")
        {
            if (!RunningInstances.Any(i => i.ProcessId == processId))
            {
                RunningInstances.Add(new RunningInstance
                {
                    ProcessId = processId,
                    Name = name,
                    AccountName = accountName,
                    StartTime = DateTime.Now
                });
                InstancesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UnregisterInstance(int processId)
        {
            var instance = RunningInstances.FirstOrDefault(i => i.ProcessId == processId);
            if (instance != null)
            {
                RunningInstances.Remove(instance);
                InstancesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void RefreshInstances()
        {
            var toRemove = RunningInstances.Where(i => !Utilities.IsProcessRunning(i.ProcessId)).ToList();
            foreach (var instance in toRemove)
            {
                RunningInstances.Remove(instance);
            }
            InstancesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void KillInstance(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                process.Kill();
                UnregisterInstance(processId);
            }
            catch { }
        }

        public void KillAllInstances()
        {
            foreach (var instance in RunningInstances.ToList())
            {
                KillInstance(instance.ProcessId);
            }
        }
    }

    public class RunningInstance
    {
        public int ProcessId { get; set; }
        public string Name { get; set; } = "";
        public string AccountName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public string Runtime => (DateTime.Now - StartTime).ToString(@"hh\:mm\:ss");
    }
}
