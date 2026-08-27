using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        public void TileWindows()
        {
            var instances = RunningInstances.Where(i => i.MainWindowHandle != IntPtr.Zero).ToList();
            if (!instances.Any()) return;

            int count = instances.Count;
            int cols = (int)Math.Ceiling(Math.Sqrt(count));
            int rows = (int)Math.Ceiling((double)count / cols);

            var screen = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            int w = screen.Width / cols;
            int h = screen.Height / rows;

            for (int i = 0; i < count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                int x = screen.Left + c * w;
                int y = screen.Top + r * h;
                SetWindowPos(instances[i].MainWindowHandle, IntPtr.Zero, x, y, w, h, 0x0040); // SWP_SHOWWINDOW
            }
        }

        public void CascadeWindows()
        {
            var instances = RunningInstances.Where(i => i.MainWindowHandle != IntPtr.Zero).ToList();
            if (!instances.Any()) return;

            var screen = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            int x = screen.Left + 50;
            int y = screen.Top + 50;
            int offset = 30;

            foreach (var instance in instances)
            {
                SetWindowPos(instance.MainWindowHandle, IntPtr.Zero, x, y, 800, 600, 0x0040);
                x += offset;
                y += offset;
                if (x > screen.Right - 850) x = screen.Left + 50;
                if (y > screen.Bottom - 650) y = screen.Top + 50;
            }
        }

        public void MinimizeAllWindows()
        {
            foreach (var instance in RunningInstances)
            {
                if (instance.MainWindowHandle != IntPtr.Zero)
                {
                    ShowWindow(instance.MainWindowHandle, 6); // SW_MINIMIZE
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }

    public class RunningInstance
    {
        public int ProcessId { get; set; }
        public string Name { get; set; } = "";
        public string AccountName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public string Runtime => (DateTime.Now - StartTime).ToString(@"hh\:mm\:ss");
        public IntPtr MainWindowHandle
        {
            get
            {
                try
                {
                    var process = Process.GetProcessById(ProcessId);
                    return process.MainWindowHandle;
                }
                catch
                {
                    return IntPtr.Zero;
                }
            }
        }
    }
}
