using RinsTrap.Enums;
using RinsTrap.Models.SettingTasks.Base;
using RinsTrap.Utility;

namespace RinsTrap.Models.SettingTasks
{
    public class SkyboxPresetTask : EnumBaseTask<SkyboxColor>
    {
        private const string CUSTOM_MARKER = "Skybox.source";

        public SkyboxPresetTask() : base("ModPreset", "SkyboxPreset")
        {
            if (File.Exists(MarkerPath) && Enum.TryParse(File.ReadAllText(MarkerPath).Trim(), out SkyboxColor color))
                OriginalState = color;
        }

        private string MarkerPath => Path.Combine(Paths.Base, "Skybox.preset");

        public override void Execute()
        {
            const string LOG_IDENT = "SkyboxPresetTask::Execute";

            if (NewState == OriginalState)
                return;

            if (NewState == SkyboxColor.None)
            {
                App.Logger.WriteLine(LOG_IDENT, "Removing skybox color preset");

                foreach (var face in SkyboxConverter.FaceFiles.Keys)
                {
                    string path = SkyboxConverter.GetOutputPath(face);

                    if (File.Exists(path))
                    {
                        Filesystem.AssertReadOnly(path);
                        File.Delete(path);
                    }
                }

                if (File.Exists(MarkerPath))
                    File.Delete(MarkerPath);
            }
            else
            {
                App.Logger.WriteLine(LOG_IDENT, $"Applying skybox color preset '{NewState}'");

                // presets and custom skyboxes are mutually exclusive
                string customMarkerPath = Path.Combine(Paths.Base, CUSTOM_MARKER);
                if (File.Exists(customMarkerPath))
                    File.Delete(customMarkerPath);

                foreach (var face in SkyboxConverter.FaceFiles.Keys)
                {
                    string outputPath = SkyboxConverter.GetOutputPath(face);

                    Filesystem.AssertReadOnly(outputPath);
                    SkyboxConverter.GeneratePreset(NewState, face, outputPath);
                }

                File.WriteAllText(MarkerPath, NewState.ToString());
            }

            OriginalState = NewState;
        }
    }
}
