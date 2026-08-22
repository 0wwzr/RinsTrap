using RinsTrap.Models.SettingTasks.Base;
using RinsTrap.Utility;

namespace RinsTrap.Models.SettingTasks
{
    public class DeathSoundTask : StringBaseTask
    {
        public const string TargetRelativePath = @"content\sounds\ouch.ogg";

        public static string Target => Path.Combine(Paths.Modifications, TargetRelativePath);

        public const int MaxDurationSeconds = 30;

        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ogg", ".mp3", ".wav", ".flac", ".wma", ".aac", ".m4a", ".aiff"
        };

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

                    string ext = Path.GetExtension(NewState).ToLowerInvariant();

                    if (ext == ".ogg")
                    {
                        File.Copy(NewState, Target, true);
                    }
                    else
                    {
                        string tempWav = Path.Combine(Paths.Temp, "death_sound_convert.wav");

                        using (var reader = new NAudio.Wave.AudioFileReader(NewState))
                        {
                            int channels = Math.Min(reader.WaveFormat.Channels, 2);
                            var outFormat = new NAudio.Wave.WaveFormat(44100, 16, channels);
                            using var resampler = new NAudio.Wave.MediaFoundationResampler(reader, outFormat) { ResamplerQuality = 60 };

                            NAudio.Wave.WaveFileWriter.CreateWaveFile(tempWav, resampler);
                        }

                        AudioConverter.ConvertToOgg(tempWav, Target);

                        if (File.Exists(tempWav))
                            File.Delete(tempWav);
                    }
                }
            }
            else if (File.Exists(Target))
            {
                Filesystem.AssertReadOnly(Target);
                File.Delete(Target);
            }

            OriginalState = NewState;
        }

        public static bool IsSupportedAudioFile(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            return SupportedExtensions.Contains(ext);
        }

        public static double GetAudioDurationSeconds(string filePath)
        {
            try
            {
                string ext = Path.GetExtension(filePath).ToLowerInvariant();

                if (ext == ".ogg")
                    return GetOggDurationSeconds(filePath);

                using var reader = new NAudio.Wave.AudioFileReader(filePath);
                return reader.TotalTime.TotalSeconds;
            }
            catch
            {
                return -1;
            }
        }

        private static double GetOggDurationSeconds(string filePath)
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
