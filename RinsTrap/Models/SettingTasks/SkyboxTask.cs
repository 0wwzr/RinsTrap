using RinsTrap.Models.SettingTasks.Base;
using RinsTrap.Utility;

namespace RinsTrap.Models.SettingTasks
{
    public class SkyboxTask : StringBaseTask
    {
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };

        public SkyboxTask() : base("ModPreset", "Skybox")
        {
            if (IsApplied())
                OriginalState = FindSourceFolder() ?? "";
        }

        private string MarkerPath => Path.Combine(Paths.Base, "Skybox.source");

        private string PresetMarkerPath => Path.Combine(Paths.Base, "Skybox.preset");

        public bool IsApplied()
        {
            return File.Exists(MarkerPath) && File.Exists(SkyboxConverter.GetOutputPath("ft"));
        }

        private string? FindSourceFolder()
        {
            if (File.Exists(MarkerPath))
                return File.ReadAllText(MarkerPath).Trim();

            return null;
        }

        public IReadOnlyList<string> GetMissingFaces(string folder)
        {
            var found = FindFaceFiles(folder);

            return SkyboxConverter.FaceFiles.Keys.Where(face => !found.ContainsKey(face)).ToList();
        }

        public override void Execute()
        {
            const string LOG_IDENT = "SkyboxTask::Execute";

            if (NewState == OriginalState)
                return;

            if (String.IsNullOrEmpty(NewState))
            {
                App.Logger.WriteLine(LOG_IDENT, "Removing custom skybox");

                foreach (var face in SkyboxConverter.FaceFiles.Keys)
                {
                    string path = SkyboxConverter.GetOutputPath(face);

                    if (File.Exists(path))
                    {
                        Filesystem.AssertReadOnly(path);
                        File.Delete(path);
                    }
                }

                string markerPath = MarkerPath;

                if (File.Exists(markerPath))
                    File.Delete(markerPath);
            }
            else
            {
                App.Logger.WriteLine(LOG_IDENT, $"Applying custom skybox from '{NewState}'");

                var missing = GetMissingFaces(NewState);

                if (missing.Any())
                    throw new Exception(Strings.Skybox_Errors_MissingFaces.Replace("{faces}", String.Join(", ", missing)));

                foreach (var face in SkyboxConverter.FaceFiles.Keys)
                {
                    string sourcePath = FindFaceFiles(NewState)[face];
                    string outputPath = SkyboxConverter.GetOutputPath(face);

                    Filesystem.AssertReadOnly(outputPath);
                    SkyboxConverter.Convert(sourcePath, outputPath);
                }

                File.WriteAllText(MarkerPath, NewState);

                // custom skyboxes and color presets are mutually exclusive
                if (File.Exists(PresetMarkerPath))
                    File.Delete(PresetMarkerPath);
            }

            OriginalState = NewState;
        }

        private static Dictionary<string, string> FindFaceFiles(string folder)
        {
            var result = new Dictionary<string, string>();

            if (!Directory.Exists(folder))
                return result;

            var candidates = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(x => ImageExtensions.Contains(Path.GetExtension(x).ToLowerInvariant()));

            foreach (var file in candidates)
            {
                string name = Path.GetFileNameWithoutExtension(file);

                foreach (var face in SkyboxConverter.FaceFiles.Keys)
                {
                    if (name.EndsWith(face, StringComparison.OrdinalIgnoreCase)
                        || name.Equals(face, StringComparison.OrdinalIgnoreCase)
                        || name.StartsWith("sky512_" + face, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!result.ContainsKey(face))
                            result[face] = file;

                        break;
                    }
                }
            }

            return result;
        }
    }
}