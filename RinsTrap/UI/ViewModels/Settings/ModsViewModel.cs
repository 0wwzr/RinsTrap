using System.IO.Compression;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.Foundation;

using CommunityToolkit.Mvvm.Input;

using RinsTrap.Models.SettingTasks;
using RinsTrap.AppData;

namespace RinsTrap.UI.ViewModels.Settings
{
    public class ModsViewModel : NotifyPropertyChangedViewModel
    {
        private void OpenModsFolder() => Process.Start("explorer.exe", Paths.Modifications);

        private readonly Dictionary<string, byte[]> FontHeaders = new()
        {
            { "ttf", new byte[4] { 0x00, 0x01, 0x00, 0x00 } },
            { "otf", new byte[4] { 0x4F, 0x54, 0x54, 0x4F } },
            { "ttc", new byte[4] { 0x74, 0x74, 0x63, 0x66 } } 
        };

        private void ManageCustomFont()
        {
            if (!String.IsNullOrEmpty(TextFontTask.NewState))
            {
                TextFontTask.NewState = "";
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = $"{Strings.Menu_FontFiles}|*.ttf;*.otf;*.ttc"
                };

                if (dialog.ShowDialog() != true)
                    return;

                string type = dialog.FileName.Substring(dialog.FileName.Length-3, 3).ToLowerInvariant();

                if (!FontHeaders.ContainsKey(type) 
                    || !FontHeaders.Any(x => File.ReadAllBytes(dialog.FileName).Take(4).SequenceEqual(x.Value)))
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Misc_CustomFont_Invalid, MessageBoxImage.Error);
                    return;
                }

                TextFontTask.NewState = dialog.FileName;
            }

            OnPropertyChanged(nameof(ChooseCustomFontVisibility));
            OnPropertyChanged(nameof(DeleteCustomFontVisibility));
        }

        public ICommand OpenModsFolderCommand => new RelayCommand(OpenModsFolder);

        public Visibility ChooseCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DeleteCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ChooseDeathSoundVisibility => !String.IsNullOrEmpty(DeathSoundTask.NewState) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DeleteDeathSoundVisibility => !String.IsNullOrEmpty(DeathSoundTask.NewState) ? Visibility.Visible : Visibility.Collapsed;

        public DeathSoundTask DeathSoundTask { get; } = new();

        public ICommand ManageCustomFontCommand => new RelayCommand(ManageCustomFont);

        public ICommand ImportRecolorCommand => new RelayCommand(ImportCustomRecolor);

        public ICommand ManageDeathSoundCommand => new RelayCommand(ManageDeathSound);

        private static bool HasClientStructure(string path) =>
            Directory.Exists(Path.Combine(path, "content")) || Directory.Exists(Path.Combine(path, "ExtraContent"));

        private void ImportCustomRecolor()
        {
            var dialog = new OpenFileDialog
            {
                Filter = $"{Strings.FileTypes_ZipArchive}|*.zip"
            };

            if (dialog.ShowDialog() != true)
                return;

            string destRoot = Path.Combine(Paths.Base, "CustomRecolors");

            if (Directory.Exists(destRoot))
                Directory.Delete(destRoot, true);

            Directory.CreateDirectory(destRoot);

            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(dialog.FileName, destRoot);

                // auto-descend into a wrapper folder if needed
                if (!HasClientStructure(destRoot))
                {
                    var candidates = Directory.EnumerateDirectories(destRoot).Where(HasClientStructure).Take(2).ToList();

                    if (candidates.Count == 1)
                    {
                        string tempDir = destRoot + "_temp";
                        Directory.Move(candidates[0], tempDir);
                        Directory.Delete(destRoot, true);
                        Directory.Move(tempDir, destRoot);
                    }
                }

                if (!HasClientStructure(destRoot))
                {
                    Directory.Delete(destRoot, true);
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Presets_GuiColor_ImportInvalid, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception)
            {
                if (Directory.Exists(destRoot))
                    Directory.Delete(destRoot, true);

                Frontend.ShowMessageBox(Strings.Menu_Mods_Presets_GuiColor_ImportInvalid, MessageBoxImage.Error);
                return;
            }

            GuiRecolorTask.NewState = Enums.GuiRecolorType.Custom;
        }

        private void ManageDeathSound()
        {
            if (!String.IsNullOrEmpty(DeathSoundTask.NewState))
            {
                DeathSoundTask.NewState = "";
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = $"{Strings.Menu_AudioFiles}|*.ogg;*.mp3;*.wav;*.flac;*.wma;*.aac;*.m4a;*.aiff|All files|*.*"
                };

                if (dialog.ShowDialog() != true)
                    return;

                if (!DeathSoundTask.IsSupportedAudioFile(dialog.FileName))
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Misc_CustomDeathSound_Invalid, MessageBoxImage.Error);
                    return;
                }

                double duration = DeathSoundTask.GetAudioDurationSeconds(dialog.FileName);

                if (duration < 0)
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Misc_CustomDeathSound_Invalid, MessageBoxImage.Error);
                    return;
                }

                if (duration > DeathSoundTask.MaxDurationSeconds)
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Misc_CustomDeathSound_TooLong, MessageBoxImage.Error);
                    return;
                }

                DeathSoundTask.NewState = dialog.FileName;
            }

            OnPropertyChanged(nameof(ChooseDeathSoundVisibility));
            OnPropertyChanged(nameof(DeleteDeathSoundVisibility));
        }

        public ICommand OpenCompatSettingsCommand => new RelayCommand(OpenCompatSettings);

        public ModPresetTask OldAvatarBackgroundTask { get; } = new("OldAvatarBackground", @"ExtraContent\places\Mobile.rbxl", "OldAvatarBackground.rbxl");

        public ModPresetTask OldCharacterSoundsTask { get; } = new("OldCharacterSounds", new()
        {
            { @"content\sounds\action_footsteps_plastic.mp3", "Sounds.OldWalk.mp3"  },
            { @"content\sounds\action_jump.mp3",              "Sounds.OldJump.mp3"  },
            { @"content\sounds\action_get_up.mp3",            "Sounds.OldGetUp.mp3" },
            { @"content\sounds\action_falling.mp3",           "Sounds.Empty.mp3"    },
            { @"content\sounds\action_jump_land.mp3",         "Sounds.Empty.mp3"    },
            { @"content\sounds\action_swim.mp3",              "Sounds.Empty.mp3"    },
            { @"content\sounds\impact_water.mp3",             "Sounds.Empty.mp3"    }
        });

        public EmojiModPresetTask EmojiFontTask { get; } = new();

        public EnumModPresetTask<Enums.CursorType> CursorTypeTask { get; } = new("CursorType", new()
        {
            {
                Enums.CursorType.From2006, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2006.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2006.ArrowFarCursor.png" }
                }
            },
            {
                Enums.CursorType.From2013, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2013.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2013.ArrowFarCursor.png" }
                }
            }
        });

        public GuiRecolorTask GuiRecolorTask { get; } = new();

        public FontModPresetTask TextFontTask { get; } = new();

        public ICommand ChooseCustomCursorCommand => new RelayCommand(ChooseCustomCursor);
        public ICommand RemoveCustomCursorCommand => new RelayCommand(RemoveCustomCursor);

        public string CustomCursorArrowPath
        {
            get => App.Settings.Prop.CustomCursorArrowPath;
            set
            {
                App.Settings.Prop.CustomCursorArrowPath = value;
                OnPropertyChanged(nameof(CustomCursorArrowPath));
                OnPropertyChanged(nameof(CustomCursorVisibility));
                OnPropertyChanged(nameof(ChooseCustomCursorVisibility));
            }
        }

        public Visibility CustomCursorVisibility => String.IsNullOrEmpty(App.Settings.Prop.CustomCursorArrowPath) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility ChooseCustomCursorVisibility => String.IsNullOrEmpty(App.Settings.Prop.CustomCursorArrowPath) ? Visibility.Visible : Visibility.Collapsed;

        private void ChooseCustomCursor()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp"
            };

            if (dialog.ShowDialog() != true)
                return;

            string destDir = Path.Combine(Paths.Base, "CustomCursor");
            Directory.CreateDirectory(destDir);

            string ext = Path.GetExtension(dialog.FileName);
            string arrowDest = Path.Combine(destDir, $"ArrowCursor{ext}");
            string arrowFarDest = Path.Combine(destDir, $"ArrowFarCursor{ext}");

            File.Copy(dialog.FileName, arrowDest, true);

            string? dir = Path.GetDirectoryName(dialog.FileName);
            if (dir != null)
            {
                string farCursor = Path.Combine(dir, "ArrowFarCursor" + ext);
                if (File.Exists(farCursor))
                    File.Copy(farCursor, arrowFarDest, true);
                else
                    File.Copy(dialog.FileName, arrowFarDest, true);
            }
            else
            {
                File.Copy(dialog.FileName, arrowFarDest, true);
            }

            CustomCursorArrowPath = arrowDest;
            App.Settings.Prop.CustomCursorArrowFarPath = arrowFarDest;

            if (CursorTypeTask.NewState != Enums.CursorType.Custom)
                CursorTypeTask.NewState = Enums.CursorType.Custom;
        }

        private void RemoveCustomCursor()
        {
            string dir = Path.Combine(Paths.Base, "CustomCursor");
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);

            CustomCursorArrowPath = "";
            App.Settings.Prop.CustomCursorArrowFarPath = "";
        }

        private void OpenCompatSettings()
        {
            string path = new RobloxPlayerData().ExecutablePath;

            if (File.Exists(path))
                PInvoke.SHObjectProperties(HWND.Null, SHOP_TYPE.SHOP_FILEPATH, path, "Compatibility");
            else
                Frontend.ShowMessageBox(Strings.Common_RobloxNotInstalled, MessageBoxImage.Error);

        }
    }
}
