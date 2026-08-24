using System.Security.Cryptography;
using System.Text;

namespace RinsTrap.Integrations
{
    public class AntiDetectionService
    {
        private static AntiDetectionService? _instance;
        public static AntiDetectionService Instance => _instance ??= new AntiDetectionService();

        private readonly Random _random = new();

        private AntiDetectionService() { }

        public bool IsEnabled
        {
            get => App.Settings.Prop.AntiDetectionEnabled;
            set => App.Settings.Prop.AntiDetectionEnabled = value;
        }

        public Dictionary<string, string> GenerateLaunchParameters()
        {
            var parameters = new Dictionary<string, string>();

            if (!IsEnabled)
                return parameters;

            if (App.Settings.Prop.RandomizeUserId)
                parameters["userId"] = GenerateRandomUserId().ToString();

            if (App.Settings.Prop.RandomizeSessionId)
                parameters["sessionId"] = GenerateRandomSessionId();

            if (App.Settings.Prop.RandomizeClientVersion)
                parameters["clientVersion"] = GenerateRandomClientVersion();

            if (App.Settings.Prop.SpoofHardwareId)
                parameters["hardwareId"] = GenerateRandomHardwareId();

            parameters["gameId"] = GenerateRandomGameId().ToString();
            parameters["placeId"] = GenerateRandomPlaceId().ToString();
            parameters["sessionStartTime"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            App.Logger.WriteLine("AntiDetection", $"Generated {parameters.Count} randomized parameters");
            return parameters;
        }

        public string GenerateCommandLineArgs()
        {
            if (!IsEnabled)
                return string.Empty;

            var args = new StringBuilder();

            args.Append($"-t {_random.Next(1000000, 9999999)} ");
            args.Append($"-s {GenerateRandomHex(16)} ");

            if (_random.Next(0, 2) == 0)
                args.Append("-e ");

            if (_random.Next(0, 10) == 0)
                args.Append("--no-console ");

            int[] fpsCaps = { 60, 120, 144, 165, 240 };
            args.Append($"--fps-cap {fpsCaps[_random.Next(fpsCaps.Length)]} ");

            return args.ToString().Trim();
        }

        public string GetDataFolder()
        {
            if (!IsEnabled || !App.Settings.Prop.SpoofHardwareId)
                return string.Empty;

            string baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string robloxPath = Path.Combine(baseFolder, "Roblox");

            string uniqueId = GenerateRandomHex(8);
            string dataFolder = Path.Combine(robloxPath, $"Versions_{uniqueId}");

            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            return dataFolder;
        }

        public Dictionary<string, string> GenerateCookies()
        {
            var cookies = new Dictionary<string, string>();

            if (!IsEnabled)
                return cookies;

            cookies["RBXSessionTracker"] = GenerateRandomHex(32);
            cookies["RBXEventTracker"] = $"{GenerateRandomHex(8)}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            return cookies;
        }

        public Dictionary<string, string> GetRandomizedHeaders()
        {
            var headers = new Dictionary<string, string>();

            if (!IsEnabled)
                return headers;

            string[] userAgents =
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0"
            };

            headers["User-Agent"] = userAgents[_random.Next(userAgents.Length)];

            string[] languages = { "en-US,en;q=0.9", "en-GB,en;q=0.9", "de-DE,de;q=0.9,en-US;q=0.8", "fr-FR,fr;q=0.9,en-US;q=0.8" };
            headers["Accept-Language"] = languages[_random.Next(languages.Length)];

            return headers;
        }

        public void ClearCache()
        {
            App.Logger.WriteLine("AntiDetection", "Parameter cache cleared");
        }

        public string GenerateRandomFingerprint()
        {
            return $"FP_{GenerateRandomHex(16)}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        private long GenerateRandomUserId() => _random.Next(100000000, 999999999);

        private string GenerateRandomSessionId() => GenerateRandomHex(32);

        private string GenerateRandomClientVersion()
        {
            int major = _random.Next(0, 1);
            int minor = _random.Next(500, 600);
            int patch = _random.Next(0, 100);
            int build = _random.Next(1000, 9999);
            return $"{major}.{minor}.{patch}.{build}";
        }

        private string GenerateRandomHardwareId() => GenerateRandomHex(40);

        private long GenerateRandomGameId()
        {
            byte[] bytes = new byte[8];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return BitConverter.ToInt64(bytes, 0) & 0x7FFFFFFFFFFFFFFF;
        }

        private int GenerateRandomPlaceId() => _random.Next(10000000, 99999999);

        private string GenerateRandomHex(int length)
        {
            byte[] bytes = new byte[length / 2];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
