using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace RinsTrap.Integrations
{
    public class ScreenshotService : IDisposable
    {
        private static ScreenshotService? _instance;
        public static ScreenshotService Instance => _instance ??= new ScreenshotService();

        private System.Windows.Forms.Timer? _timer;
        private readonly Random _random = new();
        private bool _disposed;

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public bool IsRunning => _timer?.Enabled ?? false;

        public event EventHandler? StatusChanged;
        public event EventHandler<string>? ScreenshotTaken;

        private ScreenshotService() { }

        public void Start()
        {
            if (_timer != null && _timer.Enabled)
                return;

            if (_timer == null)
            {
                _timer = new System.Windows.Forms.Timer();
                _timer.Tick += Timer_Tick;
            }

            _timer.Interval = App.Settings.Prop.ScreenshotIntervalSeconds * 1000;
            _timer.Enabled = true;

            // Create save directory if it doesn't exist
            string savePath = GetSavePath();
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

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
            if (!App.Settings.Prop.InstanceScreenshotsEnabled)
                return;

            CaptureAllInstances();
        }

        public void CaptureAllInstances()
        {
            var robloxProcesses = Utilities.GetProcessesSafe()
                .Where(p => p.ProcessName.StartsWith("Roblox", StringComparison.OrdinalIgnoreCase))
                .Where(p => !p.HasExited);

            foreach (var process in robloxProcesses)
            {
                try
                {
                    CaptureWindow(process);
                }
                catch { }
            }
        }

        public void CaptureWindow(Process process)
        {
            try
            {
                IntPtr hWnd = process.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                    return;

                GetWindowRect(hWnd, out RECT rect);
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (width <= 0 || height <= 0)
                    return;

                using var bitmap = new System.Drawing.Bitmap(width, height);
                using var graphics = System.Drawing.Graphics.FromImage(bitmap);
                
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height));

                string savePath = GetSavePath();
                string fileName = $"{process.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(savePath, fileName);

                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                ScreenshotTaken?.Invoke(this, filePath);
                App.Logger.WriteLine("ScreenshotService", $"Captured screenshot: {filePath}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ScreenshotService", ex);
            }
        }

        private string GetSavePath()
        {
            if (!string.IsNullOrEmpty(App.Settings.Prop.ScreenshotSavePath))
                return App.Settings.Prop.ScreenshotSavePath;

            return Path.Combine(Paths.Temp, "Screenshots");
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
