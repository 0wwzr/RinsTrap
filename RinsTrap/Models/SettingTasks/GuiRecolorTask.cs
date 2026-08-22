using RinsTrap.Enums;
using RinsTrap.Models.SettingTasks.Base;
using RinsTrap.Utility;

namespace RinsTrap.Models.SettingTasks
{
    public class GuiRecolorTask : EnumBaseTask<GuiRecolorType>
    {
        public GuiRecolorTask() : base("ModPreset", "GuiRecolor")
        {
            if (File.Exists(MarkerPath) && Enum.TryParse(File.ReadAllText(MarkerPath).Trim(), out GuiRecolorType color))
                OriginalState = color;
        }

        private string MarkerPath => Path.Combine(RecolorsRoot, "GuiRecolor.preset");

        private string ManifestPath => Path.Combine(RecolorsRoot, "GuiRecolor.files");

        private string RecolorsRoot => Path.Combine(AppContext.BaseDirectory, "Resources", "GuiRecolors");

        private string GetPresetSourceDir(GuiRecolorType preset) => preset switch
        {
            GuiRecolorType.Purple => Path.Combine(RecolorsRoot, "purple roblox gui", "Purple"),
            GuiRecolorType.Rainbow => Path.Combine(RecolorsRoot, "rainbow roblox gui", "rainbow roblox gui"),
            GuiRecolorType.Synthwave => Path.Combine(RecolorsRoot, "synthwave roblox gui", "synthwave"),
            GuiRecolorType.Yellow => Path.Combine(RecolorsRoot, "yellow roblox gui"),
            GuiRecolorType.DeepBlue => Path.Combine(RecolorsRoot, "deep blue roblox gui"),
            GuiRecolorType.Red => Path.Combine(RecolorsRoot, "red roblox gui"),
            _ => ""
        };

        private List<string> ReadManifest()
        {
            if (!File.Exists(ManifestPath))
                return new List<string>();

            return File.ReadAllLines(ManifestPath)
                .Where(line => !String.IsNullOrWhiteSpace(line))
                .ToList();
        }

        private void RemoveAppliedFiles()
        {
            foreach (string relativePath in ReadManifest())
            {
                string path = Path.Combine(RecolorsRoot, relativePath);

                try
                {
                    if (File.Exists(path))
                    {
                        Filesystem.AssertReadOnly(path);
                        File.Delete(path);
                    }
                }
                catch (Exception e)
                {
                    App.Logger.WriteLine("GuiRecolorTask", $"Failed to remove '{path}': {e.Message}");
                }
            }

            if (File.Exists(ManifestPath))
                File.Delete(ManifestPath);
        }

        private void CleanupEmptyDirs()
        {
            foreach (string dir in Directory.EnumerateDirectories(RecolorsRoot, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir);
                }
                catch { }
            }
        }

        public override void Execute()
        {
            const string LOG_IDENT = "GuiRecolorTask::Execute";

            if (NewState == OriginalState)
                return;

            RemoveAppliedFiles();

            if (NewState == GuiRecolorType.None)
            {
                App.Logger.WriteLine(LOG_IDENT, "Removing GUI recolor preset");

                if (File.Exists(MarkerPath))
                    File.Delete(MarkerPath);

                CleanupEmptyDirs();
            }
            else
            {
                App.Logger.WriteLine(LOG_IDENT, $"Applying GUI recolor preset '{NewState}'");

                string sourceDir = GetPresetSourceDir(NewState);

                if (!Directory.Exists(sourceDir))
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Preset folder '{sourceDir}' does not exist");
                    return;
                }

                var appliedFiles = new List<string>();

                foreach (string sourceFile in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                    string outputPath = Path.Combine(RecolorsRoot, relativePath);
                    string? outputDirectory = Path.GetDirectoryName(outputPath);

                    if (!String.IsNullOrEmpty(outputDirectory))
                        Directory.CreateDirectory(outputDirectory);

                    Filesystem.AssertReadOnly(outputPath);
                    File.Copy(sourceFile, outputPath, true);
                    appliedFiles.Add(relativePath);
                }

                File.WriteAllLines(ManifestPath, appliedFiles);
                File.WriteAllText(MarkerPath, NewState.ToString());
            }

            OriginalState = NewState;
        }
    }
}
