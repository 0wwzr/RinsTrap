using System.Text.Json.Serialization;

namespace RinsTrap.Models
{
    public class MultiInstanceAccount
    {
        public string Name { get; set; } = "Account";

        public bool AutoLaunch { get; set; } = false;

        public string LaunchArgs { get; set; } = "";

        [JsonIgnore]
        public int ProcessId { get; set; } = 0;

        [JsonIgnore]
        public bool IsRunning => ProcessId > 0 && Utilities.IsProcessRunning(ProcessId);

        [JsonIgnore]
        public string Status => IsRunning ? $"Running (PID {ProcessId})" : "Not running";
    }
}
