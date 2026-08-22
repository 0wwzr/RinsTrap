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

        public const int MaxDurationSeconds = 30;

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

        public static double GetOggDurationSeconds(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var reader = new BinaryReader(stream);

                long lastGranulePosition = 0;

                while (stream.Position < stream.Length - 27)
                {
                    byte[] header = reader.ReadBytes(27);

                    if (header[0] != 'O' || header[1] != 'g' || header[2] != 'g' || header[3] != 'S')
                        break;

                    int segmentCount = header[26];
                    byte[] segments = reader.ReadBytes(segmentCount);

                    int bodySize = 0;
                    foreach (byte s in segments)
                        bodySize += s;

                    if (bodySize > 0)
                        reader.ReadBytes(bodySize);

                    long granule = BitConverter.ToInt64(header, 10);
                    if (granule > lastGranulePosition)
                        lastGranulePosition = granule;
                }

                if (lastGranulePosition > 0)
                    return lastGranulePosition / 44100.0;

                return -1;
            }
            catch
            {
                return -1;
            }
        }
    }
}
