using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

using RinsTrap.UI.Elements.Dialogs;

namespace RinsTrap.UI.ViewModels.Installer
{
    public class LaunchMenuViewModel
    {
        public string Version => $"v{App.Version}";

        public ICommand LaunchSettingsCommand => new RelayCommand(LaunchSettings);
        public ICommand LaunchRobloxCommand => new RelayCommand(LaunchRoblox);
        public ICommand LaunchRobloxStudioCommand => new RelayCommand(LaunchRobloxStudio);
        public ICommand CheckForUpdatesCommand => new RelayCommand(CheckForUpdates);

        public event EventHandler<NextAction>? CloseWindowRequest;

        private async void CheckForUpdates()
        {
            var loadingWindow = new LoadingWindow("Checking for updates...");
            loadingWindow.Show();
            loadingWindow.Owner = Application.Current.MainWindow;

            try
            {
                var (hasUpdate, releaseInfo) = await CheckGitHubForUpdates();
                loadingWindow.Close();

                if (!hasUpdate)
                {
                    MessageBox.Show("You are on the latest version!", "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = MessageBox.Show(
                    $"A new version is available!\n\nCurrent: {Version}\nLatest: v{releaseInfo.TagName}\n\n{releaseInfo.Body?.Substring(0, Math.Min(500, releaseInfo.Body.Length))}...\n\nDo you want to download and install the update now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await DownloadAndInstallUpdate(releaseInfo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check for updates:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<(bool HasUpdate, GitHubRelease Release)> CheckGitHubForUpdates()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "RinsTrap-Updater");
            
            var response = await client.GetStringAsync("https://api.github.com/repos/0wwzr/RinsTrap/releases/latest");
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);

            if (release == null) return (false, null);

            var currentVersion = App.Version;
            var latestVersion = ParseVersion(release.TagName.TrimStart('v'));
            var current = ParseVersion(currentVersion);

            return (latestVersion > current, release);
        }

        private Version ParseVersion(string version)
        {
            try
            {
                return new Version(version);
            }
            catch
            {
                return new Version(0, 0, 0);
            }
        }

        private async Task DownloadAndInstallUpdate(GitHubRelease release)
        {
            var loadingWindow = new LoadingWindow("Downloading update...");
            loadingWindow.Show();
            loadingWindow.Owner = Application.Current.MainWindow;

            try
            {
                var asset = release.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe") || a.Name.EndsWith(".msi"));
                if (asset == null)
                {
                    MessageBox.Show("No suitable installer found in release.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "RinsTrap-Updater");
                
                var installerPath = Path.Combine(Path.GetTempPath(), $"RinsTrap_Installer_{DateTime.Now:yyyyMMdd_HHmmss}.exe");
                
                using (var stream = await client.GetStreamAsync(asset.BrowserDownloadUrl))
                using (var fileStream = File.Create(installerPath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                var result = MessageBox.Show(
                    "Update downloaded successfully!\n\nThe installer will now launch. RinsTrap will close and the installer will take over.\n\nDo you want to proceed?",
                    "Ready to Install",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = installerPath,
                        UseShellExecute = true
                    });
                    
                    App.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to download/install update:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchSettings() => CloseWindowRequest?.Invoke(this, NextAction.LaunchSettings);
        private void LaunchRoblox() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRoblox);
        private void LaunchRobloxStudio() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRobloxStudio);
    }

    public class GitHubRelease
    {
        public string TagName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Body { get; set; } = "";
        public List<GitHubAsset> Assets { get; set; } = new();
        public DateTime PublishedAt { get; set; }
    }

    public class GitHubAsset
    {
        public string Name { get; set; } = "";
        public string BrowserDownloadUrl { get; set; } = "";
        public long Size { get; set; }
    }
}