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

        private string MarkerPath => Path.Combine(Paths.Base, "GuiRecolor.preset");

        private string ManifestPath => Path.Combine(Paths.Base, "GuiRecolor.files");

        private string SourceDirectory => Path.Combine(AppContext.BaseDirectory, "Resources", "GuiRecolors", NewState.ToString());

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
                string path = Path.Combine(Paths.Modifications, relativePath);

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

        public override void Execute()
        {
            const string LOG_IDENT = "GuiRecolorTask::Execute";

            if (NewState == OriginalState)
                return;

            // remove the previously applied recolor first so switching presets never leaves stale files behind
            RemoveAppliedFiles();

            if (NewState == GuiRecolorType.None)
            {
                App.Logger.WriteLine(LOG_IDENT, "Removing GUI recolor preset");

                if (File.Exists(MarkerPath))
                    File.Delete(MarkerPath);
            }
            else
            {
                App.Logger.WriteLine(LOG_IDENT, $"Applying GUI recolor preset '{NewState}'");

                if (!Directory.Exists(SourceDirectory))
                {
                    App.Logger.WriteLine(LOG_IDENT, $"Preset folder '{SourceDirectory}' does not exist");
                    return;
                }

                var appliedFiles = new List<string>();

                foreach (string sourceFile in Directory.EnumerateFiles(SourceDirectory, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(SourceDirectory, sourceFile);
                    string outputPath = Path.Combine(Paths.Modifications, relativePath);
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
