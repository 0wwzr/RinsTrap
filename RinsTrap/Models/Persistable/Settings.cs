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

        // Multi-instance
        public bool AllowMultipleInstances { get; set; } = false;
        public ObservableCollection<MultiInstanceAccount> MultiInstanceAccounts { get; set; } = new();
        public bool ShowInstanceMonitor { get; set; } = true;

        // Anti-AFK
        public bool AntiAfkEnabled { get; set; } = false;
        public int AntiAfkIntervalSeconds { get; set; } = 300;
        public bool AntiAfkSimulateMouse { get; set; } = true;
        public bool AntiAfkSimulateKey { get; set; } = false;
        public string AntiAfkKeyToSend { get; set; } = "Space";
        public string AntiAfkMode { get; set; } = "Normal";
        public bool AntiAfkRandomizeInterval { get; set; } = true;
        public string AntiAfkMousePattern { get; set; } = "Jitter";
        public int AntiAfkMouseDistance { get; set; } = 50;
        public bool AntiAfkRandomKey { get; set; } = false;
        public bool AntiAfkSimulateClick { get; set; } = false;
        public bool AntiAfkOnlyWhenFocused { get; set; } = true;
        public bool AntiAfkPauseDuringChat { get; set; } = true;

        // Instance Screenshots
        public bool InstanceScreenshotsEnabled { get; set; } = false;
        public int ScreenshotIntervalSeconds { get; set; } = 60;
        public string ScreenshotSavePath { get; set; } = "";
        public string ScreenshotFormat { get; set; } = "PNG";
        public int ScreenshotQuality { get; set; } = 90;
        public bool ScreenshotOnlyActive { get; set; } = true;
        public bool ScreenshotTimestampFilename { get; set; } = true;

        // Instance Logging
        public bool InstanceLoggingEnabled { get; set; } = true;

        // Auto-Relaunch
        public bool AutoRelaunchEnabled { get; set; } = false;
        public int AutoRelaunchDelaySeconds { get; set; } = 5;
        public int AutoRelaunchMaxRetries { get; set; } = 3;
        public bool AutoRelaunchOnCrashOnly { get; set; } = true;
        public bool AutoRelaunchNotify { get; set; } = true;

        // Discord Rich Presence
        public bool DiscordRichPresenceEnabled { get; set; } = false;
        public string DiscordAppId { get; set; } = "";

        // Web Dashboard
        public bool WebDashboardEnabled { get; set; } = false;
        public int WebDashboardPort { get; set; } = 8080;

        // Anti-Detection
        public bool AntiDetectionEnabled { get; set; } = false;
        public bool RandomizeUserId { get; set; } = true;
        public bool RandomizeSessionId { get; set; } = true;
        public bool RandomizeClientVersion { get; set; } = false;
        public bool SpoofHardwareId { get; set; } = false;

        // Window Management
        public bool AutoArrangeWindows { get; set; } = false;
        public string ArrangeLayout { get; set; } = "Grid";
        public int WindowGap { get; set; } = 10;
        public bool RememberWindowPositions { get; set; } = true;

        // Instance Groups
        public bool InstanceGroupsEnabled { get; set; } = false;

        // Resource Limits
        public bool ResourceLimitsEnabled { get; set; } = false;
        public int CpuLimitPercent { get; set; } = 0;
        public int MemoryLimitMB { get; set; } = 0;
        public string ProcessPriority { get; set; } = "Normal";
        public bool CpuAffinityEnabled { get; set; } = false;

        // Instance Templates
        public bool TemplatesEnabled { get; set; } = false;

        // Scheduler
        public bool SchedulerEnabled { get; set; } = false;

        // Hotkeys
        public bool HotkeysEnabled { get; set; } = false;

        // Advanced
        public bool FpsLimiterEnabled { get; set; } = false;
        public int FpsLimit { get; set; } = 60;
        public bool NetworkThrottleEnabled { get; set; } = false;
        public int NetworkThrottleKBps { get; set; } = 0;
        public int AutoLaunchStaggerSeconds { get; set; } = 2;
        public bool MinimizeToTrayOnLaunch { get; set; } = false;
        public bool HealthMonitoringEnabled { get; set; } = false;
        public bool AutoKillHungInstances { get; set; } = false;
        public int HungTimeoutSeconds { get; set; } = 30;

        // OBS integration
        public bool UseObsIntegration { get; set; } = false;
        public string ObsPassword { get; set; } = "";
        public string ObsGameScene { get; set; } = "Game";
        public string ObsLobbyScene { get; set; } = "Lobby";

        // Status sync
        public bool SyncDiscordToTwitch { get; set; } = false;
        public string TwitchChannelId { get; set; } = "";
        public bool SyncTwitchToDiscord { get; set; } = false;

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
