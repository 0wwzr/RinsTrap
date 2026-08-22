using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RinsTrap.Integrations
{
    public class AntiAfkService : IDisposable
    {
        private static AntiAfkService? _instance;
        public static AntiAfkService Instance => _instance ??= new AntiAfkService();

        private System.Windows.Forms.Timer? _timer;
        private readonly Random _random = new();
        private bool _disposed;

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;

        public bool IsRunning => _timer?.Enabled ?? false;

        public event EventHandler? StatusChanged;

        private AntiAfkService() { }

        public void Start()
        {
            if (_timer != null && _timer.Enabled)
                return;

            if (_timer == null)
            {
                _timer = new System.Windows.Forms.Timer();
                _timer.Tick += Timer_Tick;
            }

            _timer.Interval = App.Settings.Prop.AntiAfkIntervalSeconds * 1000;
            _timer.Enabled = true;

            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Enabled = false;
            }
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateInterval(int seconds)
        {
            if (_timer != null)
            {
                _timer.Interval = seconds * 1000;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!App.Settings.Prop.AntiAfkEnabled)
                return;

            // Check if Roblox is in foreground
            IntPtr foregroundWindow = GetForegroundWindow();
            GetWindowThreadProcessId(foregroundWindow, out int foregroundPid);

            var robloxProcesses = Utilities.GetProcessesSafe()
                .Where(p => p.ProcessName.StartsWith("Roblox", StringComparison.OrdinalIgnoreCase));

            bool robloxInForeground = robloxProcesses.Any(p => p.Id == foregroundPid);

            if (!robloxInForeground)
                return;

            if (App.Settings.Prop.AntiAfkSimulateMouse)
            {
                SimulateMouseActivity();
            }

            if (App.Settings.Prop.AntiAfkSimulateKey)
            {
                SimulateKeyActivity();
            }
        }

        private void SimulateMouseActivity()
        {
            // Move mouse slightly to simulate activity
            int deltaX = _random.Next(-5, 6);
            int deltaY = _random.Next(-5, 6);

            mouse_event(MOUSEEVENTF_MOVE, deltaX, deltaY, 0, 0);

            // Small chance to click to simulate more natural behavior
            if (_random.Next(0, 10) == 0)
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            }
        }

        private void SimulateKeyActivity()
        {
            // Send a key press to simulate activity
            Keys key = App.Settings.Prop.AntiAfkKeyToSend switch
            {
                "Space" => Keys.Space,
                "W" => Keys.W,
                "A" => Keys.A,
                "S" => Keys.S,
                "D" => Keys.D,
                "E" => Keys.E,
                "Q" => Keys.Q,
                _ => Keys.Space
            };

            SendKeys.SendWait($"{{{key}}}");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer?.Dispose();
                _timer = null;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
