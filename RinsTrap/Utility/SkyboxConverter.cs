using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Numerics;

namespace RinsTrap.Utility
{
    public static class SkyboxConverter
    {
        private const string SKYBOX_DIR = "PlatformContent\\pc\\textures\\sky";

        public const int FaceSize = 1024;

        public static IReadOnlyDictionary<string, string> FaceFiles { get; } = new Dictionary<string, string>
        {
            { "ft", "sky512_ft.tex" },
            { "bk", "sky512_bk.tex" },
            { "lf", "sky512_lf.tex" },
            { "rt", "sky512_rt.tex" },
            { "up", "sky512_up.tex" },
            { "dn", "sky512_dn.tex" },
        };

        public static string GetOutputPath(string face) => Path.Combine(Paths.Modifications, SKYBOX_DIR, FaceFiles[face]);

        public static bool IsApplied(string face) => File.Exists(GetOutputPath(face));

        public static int GetMipCount() => 1 + (int)Math.Log2(FaceSize); // 11 for 1024

        public static void CreateCubemapFromImage(string sourcePath, string outputDirectory)
        {
            const string LOG_IDENT = "SkyboxConverter::CreateCubemapFromImage";

            App.Logger.WriteLine(LOG_IDENT, $"Generating cubemap from '{sourcePath}' into '{outputDirectory}'");

            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);

            Directory.CreateDirectory(outputDirectory);

            using var source = new Bitmap(sourcePath);

            foreach (var face in FaceFiles.Keys)
            {
                using var faceBitmap = new Bitmap(FaceSize, FaceSize, PixelFormat.Format32bppArgb);

                for (int y = 0; y < FaceSize; y++)
                {
                    for (int x = 0; x < FaceSize; x++)
                    {
                        double nx = ((2.0 * x) / (FaceSize - 1)) - 1.0;
                        double ny = ((2.0 * y) / (FaceSize - 1)) - 1.0;

                        var direction = GetFaceDirection(face, nx, ny);
                        var color = SampleEquirectangular(source, direction);
                        faceBitmap.SetPixel(x, y, color);
                    }
                }

                string faceFile = Path.Combine(outputDirectory, $"sky512_{face}.png");
                faceBitmap.Save(faceFile, ImageFormat.Png);
            }
        }

        public static void Convert(string sourcePath, string outputPath)
        {
            const string LOG_IDENT = "SkyboxConverter::Convert";

            App.Logger.WriteLine(LOG_IDENT, $"Converting '{sourcePath}' to '{outputPath}'");

            using var bitmap = new Bitmap(sourcePath);
            using var resized = FitToFace(bitmap);

            ConvertBitmap(resized, outputPath);
        }

        private static Bitmap FitToFace(Bitmap source)
        {
            var result = new Bitmap(FaceSize, FaceSize, PixelFormat.Format32bppArgb);
            result.SetResolution(source.HorizontalResolution, source.VerticalResolution);

            float scale = Math.Max((float)FaceSize / source.Width, (float)FaceSize / source.Height);
            int scaledWidth = Math.Max(FaceSize, (int)Math.Ceiling(source.Width * scale));
            int scaledHeight = Math.Max(FaceSize, (int)Math.Ceiling(source.Height * scale));
            int offsetX = (FaceSize - scaledWidth) / 2;
            int offsetY = (FaceSize - scaledHeight) / 2;

            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.Clear(Color.Black);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight));
            }

            return result;
        }

        private static Color SampleEquirectangular(Bitmap source, Vector3 direction)
        {
            double yaw = Math.Atan2(direction.Z, direction.X);
            double pitch = Math.Asin(direction.Y);

            double u = (yaw + Math.PI) / (Math.PI * 2.0);
            double v = (pitch + Math.PI / 2.0) / Math.PI;

            int x = (int)Math.Clamp(u * (source.Width - 1), 0, source.Width - 1);
            int y = (int)Math.Clamp((1.0 - v) * (source.Height - 1), 0, source.Height - 1);

            return source.GetPixel(x, y);
        }

        private static Vector3 GetFaceDirection(string face, double nx, double ny)
        {
            switch (face)
            {
                case "ft":
                    return Normalize(new Vector3((float)nx, (float)-ny, -1.0f));
                case "bk":
                    return Normalize(new Vector3((float)-nx, (float)-ny, 1.0f));
                case "lf":
                    return Normalize(new Vector3(-1.0f, (float)-ny, (float)nx));
                case "rt":
                    return Normalize(new Vector3(1.0f, (float)-ny, (float)-nx));
                case "up":
                    return Normalize(new Vector3((float)nx, 1.0f, (float)-ny));
                case "dn":
                    return Normalize(new Vector3((float)nx, -1.0f, (float)ny));
                default:
                    return new Vector3(0f, 0f, 1f);
            }
        }

        private static Vector3 Normalize(Vector3 vector)
        {
            double length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
            if (length < 1e-8)
                return new Vector3(0f, 0f, 1f);

            return new Vector3((float)(vector.X / length), (float)(vector.Y / length), (float)(vector.Z / length));
        }

        private static void ConvertBitmap(Bitmap bitmap, string outputPath)
        {
            var rect = new Rectangle(0, 0, FaceSize, FaceSize);
            var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte[] rgba = new byte[FaceSize * FaceSize * 4];
            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, rgba, 0, rgba.Length);

            bitmap.UnlockBits(bmpData);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var stream = File.Create(outputPath);
            using var writer = new BinaryWriter(stream);

            int mipCount = GetMipCount();
            WriteHeader(writer, mipCount);

            int w = FaceSize;
            int h = FaceSize;
            byte[] pixels = rgba;

            while (w >= 1 && h >= 1)
            {
                writer.Write(EncodeDxt1(pixels, w, h));

                if (w == 1 && h == 1)
                    break;

                pixels = Downsample(pixels, w, h);
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
            }
        }

        private static void WriteHeader(BinaryWriter writer, int mipCount)
        {
            writer.Write(new byte[] { (byte)'D', (byte)'D', (byte)'S', (byte)' ' });

            uint linearSize = (uint)((FaceSize / 4) * (FaceSize / 4) * 8);

            writer.Write(124u);                        // dwSize
            writer.Write(0x000A1007u);                 // dwFlags
            writer.Write((uint)FaceSize);              // dwHeight
            writer.Write((uint)FaceSize);              // dwWidth
            writer.Write(linearSize);                  // dwPitchOrLinearSize
            writer.Write(0u);                          // dwDepth
            writer.Write((uint)mipCount);              // dwMipMapCount
            writer.Write(new byte[44]);                // dwReserved1

            writer.Write(32u);                         // ddsPixelFormat.dwSize
            writer.Write(0x4u);                        // ddsPixelFormat.dwFlags = DDPF_FOURCC
            writer.Write(0x31545844u);                 // ddsPixelFormat.dwFourCC = 'DXT1'
            writer.Write(0u);                          // ddsPixelFormat.dwRGBBitCount
            writer.Write(0u);                          // ddsPixelFormat.dwRBitMask
            writer.Write(0u);                          // ddsPixelFormat.dwGBitMask
            writer.Write(0u);                          // ddsPixelFormat.dwBBitMask
            writer.Write(0u);                          // ddsPixelFormat.dwABitMask

            writer.Write(0x00401008u);                 // dwCaps = DDSCAPS_TEXTURE | DDSCAPS_MIPMAP | DDSCAPS_COMPLEX
            writer.Write(0u);                          // dwCaps2
            writer.Write(0u);                          // dwCaps3
            writer.Write(0u);                          // dwCaps4
            writer.Write(0u);                          // dwReserved2
        }

        private static byte[] Downsample(byte[] rgba, int srcW, int srcH)
        {
            int dstW = Math.Max(1, srcW / 2);
            int dstH = Math.Max(1, srcH / 2);

            byte[] output = new byte[dstW * dstH * 4];

            for (int y = 0; y < dstH; y++)
            {
                for (int x = 0; x < dstW; x++)
                {
                    int r = 0, g = 0, b = 0, a = 0;

                    for (int dy = 0; dy < 2; dy++)
                    {
                        for (int dx = 0; dx < 2; dx++)
                        {
                            int sx = Math.Min(srcW - 1, x * 2 + dx);
                            int sy = Math.Min(srcH - 1, y * 2 + dy);

                            int idx = (sy * srcW + sx) * 4;
                            r += rgba[idx];
                            g += rgba[idx + 1];
                            b += rgba[idx + 2];
                            a += rgba[idx + 3];
                        }
                    }

                    int o = (y * dstW + x) * 4;
                    output[o] = (byte)(r / 4);
                    output[o + 1] = (byte)(g / 4);
                    output[o + 2] = (byte)(b / 4);
                    output[o + 3] = (byte)(a / 4);
                }
            }

            return output;
        }

        private static byte[] EncodeDxt1(byte[] rgba, int width, int height)
        {
            int blocksX = Math.Max(1, width / 4);
            int blocksY = Math.Max(1, height / 4);

            byte[] output = new byte[blocksX * blocksY * 8];
            int outIdx = 0;

            for (int by = 0; by < blocksY; by++)
            {
                for (int bx = 0; bx < blocksX; bx++)
                {
                    // gather the 16 pixels in this block
                    byte[] r = new byte[16];
                    byte[] g = new byte[16];
                    byte[] b = new byte[16];

                    int p = 0;

                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            int sx = Math.Min(width - 1, bx * 4 + x);
                            int sy = Math.Min(height - 1, by * 4 + y);

                            int idx = (sy * width + sx) * 4;
                            r[p] = rgba[idx];
                            g[p] = rgba[idx + 1];
                            b[p] = rgba[idx + 2];
                            p++;
                        }
                    }

                    // find the two endpoint colors using the principal axis method
                    // (project colors onto the axis of maximum variance, use min/max as endpoints)

                    int ar = 0, ag = 0, ab = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        ar += r[i];
                        ag += g[i];
                        ab += b[i];
                    }

                    float mr = ar / 16f;
                    float mg = ag / 16f;
                    float mb = ab / 16f;

                    // covariance matrix
                    float c00 = 0, c01 = 0, c02 = 0, c11 = 0, c12 = 0, c22 = 0;

                    for (int i = 0; i < 16; i++)
                    {
                        float dr = r[i] - mr;
                        float dg = g[i] - mg;
                        float db = b[i] - mb;

                        c00 += dr * dr;
                        c01 += dr * dg;
                        c02 += dr * db;
                        c11 += dg * dg;
                        c12 += dg * db;
                        c22 += db * db;
                    }

                    // power iteration to find the dominant eigenvector
                    float px = 1f, py = 0f, pz = 0f;

                    for (int iter = 0; iter < 8; iter++)
                    {
                        float nx = c00 * px + c01 * py + c02 * pz;
                        float ny = c01 * px + c11 * py + c12 * pz;
                        float nz = c02 * px + c12 * py + c22 * pz;

                        float len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);

                        if (len < 1e-8f)
                            break;

                        px = nx / len;
                        py = ny / len;
                        pz = nz / len;
                    }

                    // project colors onto the axis and find min/max
                    float minT = float.MaxValue;
                    float maxT = float.MinValue;

                    for (int i = 0; i < 16; i++)
                    {
                        float t = (r[i] - mr) * px + (g[i] - mg) * py + (b[i] - mb) * pz;

                        if (t < minT)
                            minT = t;

                        if (t > maxT)
                            maxT = t;
                    }

                    // endpoints on the axis
                    byte c0r, c0g, c0b, c1r, c1g, c1b;

                    if (maxT - minT < 1e-8f)
                    {
                        // degenerate block - just use the average color
                        c0r = c1r = (byte)Math.Round(mr);
                        c0g = c1g = (byte)Math.Round(mg);
                        c0b = c1b = (byte)Math.Round(mb);
                    }
                    else
                    {
                        c0r = ClampByte((int)Math.Round(mr + px * minT));
                        c0g = ClampByte((int)Math.Round(mg + py * minT));
                        c0b = ClampByte((int)Math.Round(mb + pz * minT));

                        c1r = ClampByte((int)Math.Round(mr + px * maxT));
                        c1g = ClampByte((int)Math.Round(mg + py * maxT));
                        c1b = ClampByte((int)Math.Round(mb + pz * maxT));
                    }

                    ushort color0 = RgbTo565(c0r, c0g, c0b);
                    ushort color1 = RgbTo565(c1r, c1g, c1b);

                    // if color0 > color1, swap to use 3-color mode; otherwise we keep 4-color mode
                    if (color0 > color1)
                    {
                        (color0, color1) = (color1, color0);

                        (c0r, c1r) = (c1r, c0r);
                        (c0g, c1g) = (c1g, c0g);
                        (c0b, c1b) = (c1b, c0b);
                    }

                    // build the 4-color palette
                    int pr0 = c0r, pg0 = c0g, pb0 = c0b;
                    int pr1 = c1r, pg1 = c1g, pb1 = c1b;
                    int pr2 = (2 * c0r + c1r) / 3, pg2 = (2 * c0g + c1g) / 3, pb2 = (2 * c0b + c1b) / 3;
                    int pr3 = (c0r + 2 * c1r) / 3, pg3 = (c0g + 2 * c1g) / 3, pb3 = (c0b + 2 * c1b) / 3;

                    // assign indices
                    uint indices = 0;

                    for (int i = 0; i < 16; i++)
                    {
                        int ri = r[i];
                        int gi = g[i];
                        int bi = b[i];

                        int best = 0;
                        int bestDist = int.MaxValue;

                        int d = DistanceSq(ri, gi, bi, pr0, pg0, pb0);
                        if (d < bestDist) { bestDist = d; best = 0; }

                        d = DistanceSq(ri, gi, bi, pr1, pg1, pb1);
                        if (d < bestDist) { bestDist = d; best = 1; }

                        d = DistanceSq(ri, gi, bi, pr2, pg2, pb2);
                        if (d < bestDist) { bestDist = d; best = 2; }

                        d = DistanceSq(ri, gi, bi, pr3, pg3, pb3);
                        if (d < bestDist) { bestDist = d; best = 3; }

                        indices |= (uint)(best << (i * 2));
                    }

                    output[outIdx++] = (byte)(color0 & 0xFF);
                    output[outIdx++] = (byte)(color0 >> 8);
                    output[outIdx++] = (byte)(color1 & 0xFF);
                    output[outIdx++] = (byte)(color1 >> 8);

                    output[outIdx++] = (byte)(indices & 0xFF);
                    output[outIdx++] = (byte)((indices >> 8) & 0xFF);
                    output[outIdx++] = (byte)((indices >> 16) & 0xFF);
                    output[outIdx++] = (byte)((indices >> 24) & 0xFF);
                }
            }

            return output;
        }

        private static int DistanceSq(int r1, int g1, int b1, int r2, int g2, int b2)
        {
            int dr = r1 - r2;
            int dg = g1 - g2;
            int db = b1 - b2;

            return dr * dr + dg * dg + db * db;
        }

        private static byte ClampByte(int value) => (byte)Math.Clamp(value, 0, 255);

        private static ushort RgbTo565(byte r, byte g, byte b)
        {
            return (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
        }
    }
}