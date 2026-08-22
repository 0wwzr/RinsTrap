using System.Diagnostics;
using System.Windows;

using RinsTrap.Models.APIs;

namespace RinsTrap.Utility
{
    public static class UpdateService
    {
        public static async Task<GithubRelease?> CheckForUpdateAsync()
        {
            try
            {
                var release = await App.GetLatestRelease();

                if (release is null || release.TagName is null)
                    return null;

                var comparison = Utilities.CompareVersions(App.Version, release.TagName);

                if (comparison == VersionComparison.Equal || comparison == VersionComparison.GreaterThan)
                    return null;

                return release;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> DownloadAndApplyUpdateAsync(GithubRelease release, Action<string>? progressCallback = null)
        {
            const string LOG_IDENT = "UpdateService::DownloadAndApplyUpdate";

            try
            {
                var asset = release.Assets?
                    .Where(x => x.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Name.Contains(App.ProjectName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (asset is null)
                {
                    App.Logger.WriteLine(LOG_IDENT, "No executable asset found in release");
                    return false;
                }

                string downloadDir = Path.Combine(Paths.Temp, "Updates");
                Directory.CreateDirectory(downloadDir);

                string downloadPath = Path.Combine(downloadDir, asset.Name);

                if (!File.Exists(downloadPath))
                {
                    progressCallback?.Invoke($"Downloading {release.TagName}...");

                    using var response = await App.HttpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    await using var contentStream = await response.Content.ReadAsStreamAsync();
                    await using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await contentStream.CopyToAsync(fileStream);
                }

                progressCallback?.Invoke("Starting update...");

                App.Settings.Save();

                ProcessStartInfo startInfo = new()
                {
                    FileName = downloadPath,
                    Arguments = "-upgrade"
                };

                Process.Start(startInfo);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });

                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "Failed to download or apply update");
                App.Logger.WriteException(LOG_IDENT, ex);
                return false;
            }
        }
    }
}
