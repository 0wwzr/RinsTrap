using System.Collections.ObjectModel;

namespace RinsTrap.Models.Persistable
{
    public class Settings
    {
        // bootstrapper configuration
        public BootstrapperStyle BootstrapperStyle { get; set; } = BootstrapperStyle.FluentDialog;
        public BootstrapperIcon BootstrapperIcon { get; set; } = BootstrapperIcon.IconRinsTrap;
        public string BootstrapperTitle { get; set; } = App.ProjectName;
        public string BootstrapperIconCustomLocation { get; set; } = "";
        public string BootstrapperImagePath { get; set; } = "";
        public string CustomCursorArrowPath { get; set; } = "";
        public string CustomCursorArrowFarPath { get; set; } = "";
        public Theme Theme { get; set; } = Theme.Default;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DeveloperMode { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
        public bool ConfirmLaunches { get; set; } = false;
        public string Locale { get; set; } = "nil";
        public bool UseFastFlagManager { get; set; } = true;
        public bool WPFSoftwareRender { get; set; } = false;
        public bool EnableAnalytics { get; set; } = true;
        public bool BackgroundUpdatesEnabled { get; set; } = false;
        public bool DebugDisableVersionPackageCleanup { get; set; } = false;
        public string? SelectedCustomTheme { get; set; } = null;
        public WebEnvironment WebEnvironment { get; set; } = WebEnvironment.Production;

        // integration configuration
        public bool EnableActivityTracking { get; set; } = true;
        public bool UseDiscordRichPresence { get; set; } = true;
        public bool HideRPCButtons { get; set; } = true;
        public bool ShowAccountOnRichPresence { get; set; } = false;
        public bool ShowServerDetails { get; set; } = false;
        public bool ShowServerLocationOnRichPresence { get; set; } = false;
        public bool ShowGamePageButton { get; set; } = true;
        public bool ShowPlaytimeOnRichPresence { get; set; } = true;
        public bool KillRobloxBackgroundProcesses { get; set; } = false;
        public bool StreamerMode { get; set; } = false;
        public ObservableCollection<CustomIntegration> CustomIntegrations { get; set; } = new();

        // mod preset configuration
        public bool UseDisableAppPatch { get; set; } = false;

        // playtime configuration
        public bool NotifySessionEnd { get; set; } = true;
        public bool EnablePlaytimeReminder { get; set; } = false;
        public int PlaytimeReminderMinutes { get; set; } = 60;

        // update configuration
        public bool ShowWhatsNew { get; set; } = true;
        public string WhatsNewLastSeenVersion { get; set; } = "";
    }
}
