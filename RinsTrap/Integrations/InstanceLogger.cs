using System.Collections.ObjectModel;

namespace RinsTrap.Integrations
{
    public class InstanceLogger
    {
        private static InstanceLogger? _instance;
        public static InstanceLogger Instance => _instance ??= new InstanceLogger();

        public ObservableCollection<LogEntry> LogEntries { get; } = new();

        public event EventHandler<LogEntry>? NewLogEntry;

        private InstanceLogger() { }

        public void Log(int processId, string instanceName, LogType type, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                ProcessId = processId,
                InstanceName = instanceName,
                Type = type,
                Message = message
            };

            App.Current.Dispatcher.Invoke(() =>
            {
                LogEntries.Insert(0, entry);

                // Keep only last 1000 entries
                while (LogEntries.Count > 1000)
                {
                    LogEntries.RemoveAt(LogEntries.Count - 1);
                }
            });

            NewLogEntry?.Invoke(this, entry);

            // Also log to file
            App.Logger.WriteLine($"InstanceLogger[{instanceName}]", $"[{type}] {message}");
        }

        public void LogGameJoin(int processId, string instanceName, string gameId)
        {
            Log(processId, instanceName, LogType.GameJoin, $"Joined game: {gameId}");
        }

        public void LogGameLeave(int processId, string instanceName)
        {
            Log(processId, instanceName, LogType.GameLeave, "Left game");
        }

        public void LogError(int processId, string instanceName, string error)
        {
            Log(processId, instanceName, LogType.Error, $"Error: {error}");
        }

        public void LogInfo(int processId, string instanceName, string message)
        {
            Log(processId, instanceName, LogType.Info, message);
        }

        public void LogScreenshot(int processId, string instanceName, string path)
        {
            Log(processId, instanceName, LogType.Screenshot, $"Screenshot saved: {Path.GetFileName(path)}");
        }

        public void Clear()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                LogEntries.Clear();
            });
        }

        public void ExportLogs(string filePath)
        {
            var lines = LogEntries.Select(e => 
                $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] [{e.Type}] [{e.InstanceName} ({e.ProcessId})] {e.Message}");

            File.WriteAllLines(filePath, lines);
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public int ProcessId { get; set; }
        public string InstanceName { get; set; } = "";
        public LogType Type { get; set; }
        public string Message { get; set; } = "";
    }

    public enum LogType
    {
        Info,
        GameJoin,
        GameLeave,
        Error,
        Screenshot,
        Warning
    }
}
