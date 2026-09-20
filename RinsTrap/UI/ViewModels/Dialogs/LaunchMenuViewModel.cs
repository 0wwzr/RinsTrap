using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

using RinsTrap.Models.SettingTasks;
using RinsTrap.UI.Elements.Dialogs;

namespace RinsTrap.UI.ViewModels.Installer
{
    public class LaunchMenuViewModel : NotifyPropertyChangedViewModel
    {
        private int _trollLogoClicks;

        private const int TrollActivationClicks = 20;

        private static readonly string[] PreActivationLabels =
        {
            "Launch Totally Normal Roblox",
            "Launch Roblox, Probably",
            "Launch Completely Safe Experience",
            "Launch Roblox (Nothing Strange)",
            "Launch Regular Roblox",
            "Launch The Normal One",
            "Launch Roblox, Trust Me",
            "Launch An Ordinary Experience"
        };

        public string Version => $"v{App.Version}";

        public string LaunchRobloxLabel { get; private set; } = Strings.LaunchMenu_LaunchRoblox;

        public string? LaunchRobloxIconPath { get; private set; }

        public bool HasLaunchRobloxIcon => !String.IsNullOrEmpty(LaunchRobloxIconPath);

        public ICommand LaunchSettingsCommand => new RelayCommand(LaunchSettings);
        public ICommand LaunchRobloxCommand => new RelayCommand(LaunchRoblox);
        public ICommand LaunchRobloxStudioCommand => new RelayCommand(LaunchRobloxStudio);
        public ICommand TrollSkyboxCommand => new RelayCommand(ApplyTrollSkybox);

        public event EventHandler<NextAction>? CloseWindowRequest;

        private void ApplyTrollSkybox()
        {
            const string LOG_IDENT = "LaunchMenuViewModel::ApplyTrollSkybox";

            _trollLogoClicks++;

            if (_trollLogoClicks < TrollActivationClicks)
            {
                LaunchRobloxLabel = PreActivationLabels[Random.Shared.Next(PreActivationLabels.Length)];
                OnPropertyChanged(nameof(LaunchRobloxLabel));
                return;
            }

            _trollLogoClicks = 0;

            var candidatePaths = new[]
            {
                Path.Combine(Paths.Base, "troll"),
                Path.Combine(AppContext.BaseDirectory, "troll"),
                Path.Combine(Environment.CurrentDirectory, "troll")
            };

            string? trollFolder = candidatePaths.FirstOrDefault(Directory.Exists);
            if (string.IsNullOrEmpty(trollFolder))
            {
                MessageBox.Show("Troll folder not found. Check the archive in the project root.", "John Pork Mode", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var imageFiles = Directory.GetFiles(trollFolder)
                .Where(file => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" }
                    .Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToArray();

            if (imageFiles.Length == 0)
            {
                MessageBox.Show("No troll images were found in the troll folder.", "John Pork Mode", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var tempDir = Path.Combine(Paths.Temp, "TrollSkybox");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);

            Directory.CreateDirectory(tempDir);

            string[] faceNames = { "ft", "bk", "lf", "rt", "up", "dn" };
            for (int i = 0; i < faceNames.Length; i++)
            {
                string source = imageFiles[i % imageFiles.Length];
                string target = Path.Combine(tempDir, $"sky512_{faceNames[i]}.png");
                File.Copy(source, target, true);
            }

            var skyboxTask = new SkyboxTask
            {
                NewState = tempDir
            };

            skyboxTask.Execute();

            App.Logger.WriteLine(LOG_IDENT, $"Applied troll skybox from '{trollFolder}'");
            LaunchRobloxLabel = "Launch Totally Normal Roblox";
            LaunchRobloxIconPath = "pack://application:,,,/RinsTrap.ico";
            OnPropertyChanged(nameof(LaunchRobloxLabel));
            OnPropertyChanged(nameof(LaunchRobloxIconPath));
            OnPropertyChanged(nameof(HasLaunchRobloxIcon));
        }

        private void LaunchSettings() => CloseWindowRequest?.Invoke(this, NextAction.LaunchSettings);
        private void LaunchRoblox() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRoblox);
        private void LaunchRobloxStudio() => CloseWindowRequest?.Invoke(this, NextAction.LaunchRobloxStudio);
    }

}