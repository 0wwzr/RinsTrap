using RinsTrap.Models.SettingTasks.Base;
using RinsTrap.Utility;

namespace RinsTrap.Models.SettingTasks
{
    /// <summary>
    /// Applies a user-chosen audio file as the death sound.
    /// </summary>
    public class DeathSoundTask : StringBaseTask
    {
        public const string TargetRelativePath = @"content\sounds\ouch.ogg";

        public static string Target => Path.Combine(Paths.Modifications, TargetRelativePath);

        public DeathSoundTask() : base("ModPreset", "DeathSound")
        {
            if (File.Exists(Target))
                OriginalState = Target;
        }

        public override void Execute()
        {
            if (!String.IsNullOrEmpty(NewState))
            {
                if (String.Compare(NewState, Target, StringComparison.InvariantCultureIgnoreCase) != 0 && File.Exists(NewState))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Target)!);

                    Filesystem.AssertReadOnly(Target);
                    File.Copy(NewState, Target, true);
                }
            }
            else if (File.Exists(Target))
            {
                Filesystem.AssertReadOnly(Target);
                File.Delete(Target);
            }

            OriginalState = NewState;
        }
    }
}
