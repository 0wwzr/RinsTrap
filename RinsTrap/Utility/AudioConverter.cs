using System.Buffers.Binary;

using NAudio.Wave;

namespace RinsTrap.Utility
{
    public static class AudioConverter
    {
        private const int TARGET_SAMPLE_RATE = 44100;
        private const int BLOCK_SIZE = 512;
        private static readonly float[] SineWindow = GenerateSineWindow(BLOCK_SIZE);

        public static void ConvertToOgg(string inputPath, string outputPath)
        {
            float[] pcm;
            int channels;

            using (var reader = new AudioFileReader(inputPath))
            {
                channels = Math.Min(reader.WaveFormat.Channels, 2);
                var outFormat = new WaveFormat(TARGET_SAMPLE_RATE, 16, channels);
                using var resampler = new MediaFoundationResampler(reader, outFormat) { ResamplerQuality = 60 };

                using var ms = new MemoryStream();
                byte[] buf = new byte[4096];
                int read;
                while ((read = resampler.Read(buf, 0, buf.Length)) > 0)
                    ms.Write(buf, 0, read);

                byte[] allBytes = ms.ToArray();
                int sampleCount = allBytes.Length / (2 * channels);
                pcm = new float[sampleCount * channels];

                for (int i = 0; i < allBytes.Length; i += 2)
                {
                    short s = BinaryPrimitives.ReadInt16LittleEndian(allBytes.AsSpan(i));
                    pcm[i / 2] = s / 32768f;
                }
            }

            int serial = Random.Shared.Next();
            int totalSamples = pcm.Length / channels;
            int blockCount = (int)Math.Ceiling((double)totalSamples / BLOCK_SIZE);

            using var output = File.Create(outputPath);

            WriteOggPage(output, BuildIdHeader(channels), serial, 0, false);
            WriteOggPage(output, BuildCommentHeader(), serial, 0, false);
            WriteOggPage(output, BuildSetupHeader(channels), serial, 0, false);

            int granule = 0;
            for (int b = 0; b < blockCount; b++)
            {
                int offset = b * BLOCK_SIZE * channels;
                int remaining = Math.Min(BLOCK_SIZE, totalSamples - b * BLOCK_SIZE);

                float[] leftBlock = new float[BLOCK_SIZE];
                float[] rightBlock = channels == 2 ? new float[BLOCK_SIZE] : Array.Empty<float>();

                for (int s = 0; s < BLOCK_SIZE; s++)
                {
                    int src = offset + s * channels;
                    if (s < remaining && src < pcm.Length)
                    {
                        leftBlock[s] = pcm[src] * SineWindow[s];
                        if (channels == 2 && src + 1 < pcm.Length)
                            rightBlock[s] = pcm[src + 1] * SineWindow[s];
                    }
                }

                byte[] packet = BuildAudioPacket(leftBlock, rightBlock, channels);

                granule += remaining;
                WriteOggPage(output, packet, serial, granule, b == blockCount - 1);
            }
        }

        private static byte[] BuildAudioPacket(float[] left, float[] right, int channels)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write((byte)0x00);

            WriteQuantizedCoefficients(bw, left);
            if (channels == 2)
                WriteQuantizedCoefficients(bw, right);

            bw.Flush();
            return ms.ToArray();
        }

        private static void WriteQuantizedCoefficients(BinaryWriter bw, float[] coeffs)
        {
            int n = coeffs.Length;
            int bitsPerSample = 8;
            int bytesToWrite = (n * bitsPerSample + 7) / 8;
            byte[] packed = new byte[bytesToWrite];
            int bitIdx = 0;

            for (int i = 0; i < n; i++)
            {
                int val = (int)Math.Round(coeffs[i] * 1000);
                val = Math.Clamp(val, -128, 127);

                for (int b = bitsPerSample - 1; b >= 0; b--)
                {
                    int bytePos = bitIdx / 8;
                    int bitPos = 7 - (bitIdx % 8);
                    if (bytePos < packed.Length && ((val >> b) & 1) == 1)
                        packed[bytePos] |= (byte)(1 << bitPos);
                    bitIdx++;
                }
            }

            bw.Write(packed);
        }

        private static byte[] BuildIdHeader(int channels)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write((byte)0x01);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("vorbis"));

            WriteLE32(bw, 0);
            bw.Write((byte)channels);
            WriteLE32(bw, TARGET_SAMPLE_RATE);
            WriteLE32(bw, 0);
            WriteLE32(bw, TARGET_SAMPLE_RATE);
            WriteLE32(bw, 0);
            bw.Write((byte)ilog2(BLOCK_SIZE));
            bw.Write((byte)ilog2(BLOCK_SIZE));
            bw.Write((byte)1);

            bw.Flush();
            return ms.ToArray();
        }

        private static byte[] BuildCommentHeader()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write((byte)0x03);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("vorbis"));

            byte[] vendor = System.Text.Encoding.UTF8.GetBytes("RinsTrap");
            WriteLE32(bw, vendor.Length);
            bw.Write(vendor);
            WriteLE32(bw, 0);
            bw.Write((byte)1);

            bw.Flush();
            return ms.ToArray();
        }

        private static byte[] BuildSetupHeader(int channels)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write((byte)0x05);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("vorbis"));

            var w = new BitWriter(bw);

            w.Write(0x0001, 16);

            w.Write(0, 24);
            w.Write(0, 24);
            w.Write(1, 16);

            w.Write(1, 4);
            w.Write(0, 4);

            w.Write(1, 1);
            w.Write(0, 24);
            w.Write(2, 16);
            w.Write(1, 1);
            for (int i = 0; i < 2; i++)
                w.Write(0, 8);

            w.Write(0, 6);
            w.Write(1, 6);

            w.Write(0, 16);
            w.Write(0, 16);
            w.Write(0, 8);
            w.Write(1, 1);
            w.Write(0, 1);
            w.Write(0, 1);
            w.Write(0, 1);
            w.Write(0, 1);
            w.Write(0, 2);
            w.Write(0, 16);
            w.Write(0, 16);
            w.Write(0, 8);
            w.Write(0, 1);
            w.Write(0, 1);
            for (int i = 0; i < 2; i++)
            {
                w.Write(0, 1);
                w.Write(0, 8);
                w.Write(0, 8);
            }

            w.Write(0, 2);

            w.Write(0, 16);
            w.Write(0, 16);
            w.Write(0, 8);
            for (int i = 0; i < channels; i++)
                w.Write(i, 8);
            w.Write(0, 2);
            w.Write(0, 1);

            w.Write(1, 6);
            w.Write(0, 16);
            w.Write(0, 16);
            w.Write(0, 1);
            w.Write(0, 3);
            w.Write(1, 8);

            w.Flush();

            bw.Flush();
            return ms.ToArray();
        }

        private static int ilog2(int v)
        {
            int r = 0;
            v >>= 1;
            while (v > 0) { r++; v >>= 1; }
            return r;
        }

        private static void WriteLE32(BinaryWriter bw, int v)
        {
            bw.Write((byte)(v & 0xFF));
            bw.Write((byte)((v >> 8) & 0xFF));
            bw.Write((byte)((v >> 16) & 0xFF));
            bw.Write((byte)((v >> 24) & 0xFF));
        }

        private static float[] GenerateSineWindow(int size)
        {
            float[] w = new float[size];
            for (int i = 0; i < size; i++)
                w[i] = (float)Math.Sin(Math.PI * i / size);
            return w;
        }

        private static void WriteOggPage(Stream output, byte[] body, int serial, int granule, bool last)
        {
            int segs = body.Length == 0 ? 0 : (int)Math.Ceiling(body.Length / 255.0);
            if (segs == 0) segs = 1;

            byte[] segTable = new byte[segs];
            int rem = body.Length;
            for (int i = 0; i < segs - 1; i++) { segTable[i] = 255; rem -= 255; }
            segTable[segs - 1] = (byte)rem;

            int hdrLen = 27 + segs;
            byte[] page = new byte[hdrLen + body.Length];

            page[0] = (byte)'O'; page[1] = (byte)'g'; page[2] = (byte)'g'; page[3] = (byte)'S';
            page[4] = 0;
            page[5] = (byte)(last ? 0x04 : 0x00);
            BinaryPrimitives.WriteInt64LittleEndian(page.AsSpan(6), granule);
            BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(14), serial);
            BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(18), 0);
            BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(22), 0);
            page[26] = (byte)segs;
            Array.Copy(segTable, 0, page, 27, segs);
            Array.Copy(body, 0, page, hdrLen, body.Length);

            uint crc = ComputeOggCrc(page);
            BinaryPrimitives.WriteUInt32LittleEndian(page.AsSpan(22), crc);

            output.Write(page, 0, page.Length);
        }

        private static uint ComputeOggCrc(byte[] data)
        {
            uint crc = 0;
            foreach (byte b in data)
            {
                crc ^= (uint)b << 24;
                for (int i = 0; i < 8; i++)
                    crc = (crc & 0x80000000) != 0 ? (crc << 1) ^ 0x04C11DB7 : crc << 1;
            }
            return crc;
        }

        private class BitWriter
        {
            private readonly BinaryWriter _bw;
            private int _buf;
            private int _count;

            public BitWriter(BinaryWriter bw) => _bw = bw;

            public void Write(int value, int bits)
            {
                _buf |= (value << _count);
                _count += bits;
                while (_count >= 8)
                {
                    _bw.Write((byte)(_buf & 0xFF));
                    _buf >>= 8;
                    _count -= 8;
                }
            }

            public void Flush()
            {
                if (_count > 0)
                {
                    _bw.Write((byte)(_buf & 0xFF));
                    _buf = 0;
                    _count = 0;
                }
            }
        }
    }
}
