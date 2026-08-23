using System.Runtime.InteropServices;

namespace RinsTrap.Integrations
{
    public class DiscordRichPresenceService : IDisposable
    {
        private static DiscordRichPresenceService? _instance;
        public static DiscordRichPresenceService Instance => _instance ??= new DiscordRichPresenceService();

        private bool _initialized;
        private bool _disposed;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        // Discord SDK function pointers
        private delegate int DiscordInitialize(string applicationId, ref DiscordEventHandlers handlers, bool autoRegister, string steamId);
        private delegate void DiscordShutdown();
        private delegate void DiscordUpdatePresence(ref DiscordRichPresence presence);
        private delegate void DiscordRunCallbacks();

        private DiscordInitialize? _initialize;
        private DiscordShutdown? _shutdown;
        private DiscordUpdatePresence? _updatePresence;
        private DiscordRunCallbacks? _runCallbacks;

        private System.Windows.Forms.Timer? _callbackTimer;
        private IntPtr _discordModule;

        public bool IsInitialized => _initialized;
        public event EventHandler? StatusChanged;

        [StructLayout(LayoutKind.Sequential)]
        private struct DiscordEventHandlers
        {
            public IntPtr ready;
            public IntPtr disconnected;
            public IntPtr errored;
            public IntPtr joinGame;
            public IntPtr joinRequest;
            public IntPtr spectateGame;
            public IntPtr joinAsked;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DiscordRichPresence
        {
            public string state;
            public string details;
            public long startTimestamp;
            public long endTimestamp;
            public string largeImageKey;
            public string largeImageText;
            public string smallImageKey;
            public string smallImageText;
            public string partyId;
            public int partySize;
            public int partyMax;
            public string matchSecret;
            public string joinSecret;
            public string spectateSecret;
            public byte instance;
        }

        private DiscordRichPresenceService() { }

        public bool Initialize(string applicationId)
        {
            if (_initialized)
                return true;

            try
            {
                // Try to load discord_game_sdk.dll
                _discordModule = LoadLibrary("discord_game_sdk.dll");
                if (_discordModule == IntPtr.Zero)
                {
                    App.Logger.WriteLine("DiscordRichPresence", "Discord SDK not found - Rich Presence unavailable");
                    return false;
                }

                // Get function pointers
                var initPtr = GetProcAddress(_discordModule, "Discord_Initialize");
                var shutdownPtr = GetProcAddress(_discordModule, "Discord_Shutdown");
                var updatePtr = GetProcAddress(_discordModule, "Discord_UpdatePresence");
                var runPtr = GetProcAddress(_discordModule, "Discord_RunCallbacks");

                if (initPtr == IntPtr.Zero || shutdownPtr == IntPtr.Zero || 
                    updatePtr == IntPtr.Zero || runPtr == IntPtr.Zero)
                {
                    App.Logger.WriteLine("DiscordRichPresence", "Discord SDK functions not found");
                    return false;
                }

                _initialize = Marshal.GetDelegateForFunctionPointer<DiscordInitialize>(initPtr);
                _shutdown = Marshal.GetDelegateForFunctionPointer<DiscordShutdown>(shutdownPtr);
                _updatePresence = Marshal.GetDelegateForFunctionPointer<DiscordUpdatePresence>(updatePtr);
                _runCallbacks = Marshal.GetDelegateForFunctionPointer<DiscordRunCallbacks>(runPtr);

                // Initialize Discord
                var handlers = new DiscordEventHandlers();
                int result = _initialize(applicationId, ref handlers, false, null);

                if (result != 0)
                {
                    App.Logger.WriteLine("DiscordRichPresence", $"Discord initialization failed: {result}");
                    return false;
                }

                // Start callback timer
                _callbackTimer = new System.Windows.Forms.Timer();
                _callbackTimer.Interval = 1000;
                _callbackTimer.Tick += (_, _) => _runCallbacks?.Invoke();
                _callbackTimer.Enabled = true;

                _initialized = true;
                StatusChanged?.Invoke(this, EventArgs.Empty);
                App.Logger.WriteLine("DiscordRichPresence", "Discord Rich Presence initialized");
                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("DiscordRichPresence", ex);
                return false;
            }
        }

        public void UpdatePresence(string state, string details, int instanceCount, int maxInstances)
        {
            if (!_initialized || _updatePresence == null)
                return;

            try
            {
                var presence = new DiscordRichPresence
                {
                    state = state,
                    details = details,
                    largeImageKey = "rinsTrap_logo",
                    largeImageText = "RinsTrap",
                    smallImageKey = instanceCount > 0 ? "roblox" : "idle",
                    smallImageText = $"{instanceCount}/{maxInstances} instances",
                    partySize = instanceCount,
                    partyMax = maxInstances
                };

                _updatePresence(ref presence);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("DiscordRichPresence", ex);
            }
        }

        public void UpdateInstanceCount(int count)
        {
            string state = count switch
            {
                0 => "Idle",
                1 => "Running 1 instance",
                _ => $"Running {count} instances"
            };

            UpdatePresence(state, "Multi-Instance Manager", count, 10);
        }

        public void ClearPresence()
        {
            if (!_initialized || _updatePresence == null)
                return;

            try
            {
                var presence = new DiscordRichPresence
                {
                    state = "",
                    details = ""
                };
                _updatePresence(ref presence);
            }
            catch { }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _callbackTimer?.Dispose();
                _callbackTimer = null;

                if (_initialized)
                {
                    _shutdown?.Invoke();
                    _initialized = false;
                }

                if (_discordModule != IntPtr.Zero)
                {
                    FreeLibrary(_discordModule);
                    _discordModule = IntPtr.Zero;
                }

                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
    }
}
