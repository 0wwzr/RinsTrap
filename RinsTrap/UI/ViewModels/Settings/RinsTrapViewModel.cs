using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;

namespace RinsTrap.UI.ViewModels.Settings
{
    public class RinsTrapViewModel : NotifyPropertyChangedViewModel
    {
        public WebEnvironment[] WebEnvironments => Enum.GetValues<WebEnvironment>();

        public bool UpdateCheckingEnabled
        {
            get => App.Settings.Prop.CheckForUpdates;
            set => App.Settings.Prop.CheckForUpdates = value;
        }

        public bool ShowWhatsNew
        {
            get => App.Settings.Prop.ShowWhatsNew;
            set => App.Settings.Prop.ShowWhatsNew = value;
        }

        public ICommand ViewChangelogCommand => new RelayCommand(ViewChangelog);
        public ICommand CheckForUpdatesCommand => new AsyncRelayCommand(CheckForUpdatesAsync);

        private string _updateStatus = "";
        public string UpdateStatus
        {
            get => _updateStatus;
            set
            {
                _updateStatus = value;
                OnPropertyChanged(nameof(UpdateStatus));
                OnPropertyChanged(nameof(UpdateStatusVisibility));
            }
        }

        public Visibility UpdateStatusVisibility => String.IsNullOrEmpty(UpdateStatus) ? Visibility.Collapsed : Visibility.Visible;

        private bool _isCheckingForUpdates;
        public bool IsCheckingForUpdates
        {
            get => _isCheckingForUpdates;
            set
            {
                _isCheckingForUpdates = value;
                OnPropertyChanged(nameof(IsCheckingForUpdates));
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            if (IsCheckingForUpdates)
                return;

            IsCheckingForUpdates = true;
            UpdateStatus = "Checking for updates...";

            try
            {
                var release = await UpdateService.CheckForUpdateAsync();

                if (release is null)
                {
                    UpdateStatus = "You're up to date!";
                    return;
                }

                var result = MessageBox.Show(
                    $"Update {release.TagName} is available.\n\nDo you want to download and install it now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    UpdateStatus = "Downloading update...";
                    bool success = await UpdateService.DownloadAndApplyUpdateAsync(release);

                    if (!success)
                    {
                        UpdateStatus = "Update failed. Please try again later.";
                    }
                }
                else
                {
                    UpdateStatus = "";
                }
            }
            catch (Exception ex)
            {
                UpdateStatus = "Failed to check for updates.";
                App.Logger.WriteLine("RinsTrapViewModel", "Update check failed");
                App.Logger.WriteException("RinsTrapViewModel", ex);
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        private void ViewChangelog()
        {
            var dialog = new UI.Elements.Dialogs.WhatsNewDialog(false)
            {
                Owner = Application.Current.MainWindow
            };

            dialog.ShowDialog();
        }

        public bool AnalyticsEnabled
        {
            get => App.Settings.Prop.EnableAnalytics;
            set => App.Settings.Prop.EnableAnalytics = value;
        }

        public WebEnvironment WebEnvironment
        {
            get => App.Settings.Prop.WebEnvironment;
            set => App.Settings.Prop.WebEnvironment = value;
        }

        public Visibility WebEnvironmentVisibility => App.Settings.Prop.DeveloperMode ? Visibility.Visible : Visibility.Collapsed;

        public bool ShouldExportConfig { get; set; } = true;

        public bool ShouldExportLogs { get; set; } = true;

        public ICommand ExportDataCommand => new RelayCommand(ExportData);

        private void ExportData()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");

            var dialog = new SaveFileDialog 
            { 
                FileName = $"RinsTrap-export-{timestamp}.zip",
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip" 
            };

            if (dialog.ShowDialog() != true)
                return;

            using var memStream = new MemoryStream();
            using var zipStream = new ZipOutputStream(memStream);

            if (ShouldExportConfig)
            {
                var files = new List<string>()
                {
                    App.Settings.FileLocation,
                    App.State.FileLocation,
                    App.FastFlags.FileLocation
                };

                AddFilesToZipStream(zipStream, files, "Config/");
            }

            if (ShouldExportLogs && Directory.Exists(Paths.Logs))
            {
                var files = Directory.GetFiles(Paths.Logs)
                    .Where(x => !x.Equals(App.Logger.FileLocation, StringComparison.OrdinalIgnoreCase));

                AddFilesToZipStream(zipStream, files, "Logs/");
            }

            zipStream.CloseEntry();
            zipStream.Finish();
            memStream.Position = 0;

            using var outputStream = File.OpenWrite(dialog.FileName);
            memStream.CopyTo(outputStream);

            Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
        }

        private void AddFilesToZipStream(ZipOutputStream zipStream, IEnumerable<string> files, string directory)
        {
            const string LOG_IDENT = "RinsTrapViewModel::AddFilesToZipStream";

            foreach (string file in files)
            {
                if (!File.Exists(file))
                    continue;

                try
                {
                    using FileStream fileStream = File.OpenRead(file);

                    var entry = new ZipEntry(directory + Path.GetFileName(file));
                    entry.DateTime = DateTime.Now;

                    zipStream.PutNextEntry(entry);

                    fileStream.CopyTo(zipStream);
                }
                catch (IOException ex)
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Failed to open '{file}'");
                    App.Logger.WriteException(LOG_IDENT, ex);
                }
            }
        }
    }
}
