using System.Drawing;
using System.Drawing.Imaging;

using RinsTrap.Enums;

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

        public static void Convert(string sourcePath, string outputPath)
        {
            const string LOG_IDENT = "SkyboxConverter::Convert";

            App.Logger.WriteLine(LOG_IDENT, $"Converting '{sourcePath}' to '{outputPath}'");

            using var bitmap = new Bitmap(sourcePath);
            using var resized = new Bitmap(bitmap, new Size(FaceSize, FaceSize));

            ConvertBitmap(resized, outputPath);
        }

        public static void GeneratePreset(SkyboxColor color, string face, string outputPath)
        {
            const string LOG_IDENT = "SkyboxConverter::GeneratePreset";

            App.Logger.WriteLine(LOG_IDENT, $"Generating preset skybox face '{face}' ({color}) to '{outputPath}'");

            (Color top, Color horizon, Color ground) = GetPresetColors(color);

            using var gradient = new Bitmap(FaceSize, FaceSize, PixelFormat.Format32bppArgb);

            for (int y = 0; y < FaceSize; y++)
            {
                Color rowColor;

                if (face == "up")
                {
                    // slightly darker at the very top of the sky
                    rowColor = Lerp(top, horizon, 0.08f + (y / (float)FaceSize) * 0.12f);
                }
                else if (face == "dn")
                {
                    rowColor = ground;
                }
                else
                {
                    // vertical gradient from zenith at the top to horizon at the bottom
                    float t = y / (float)(FaceSize - 1);
                    rowColor = Lerp(top, horizon, t);
                }

                for (int x = 0; x < FaceSize; x++)
                    gradient.SetPixel(x, y, rowColor);
            }

            ConvertBitmap(gradient, outputPath);
        }

        private static (Color, Color, Color) GetPresetColors(SkyboxColor color)
        {
            return color switch
            {
                SkyboxColor.Day       => (Color.FromArgb(0x1E, 0x90, 0xFF), Color.FromArgb(0x87, 0xCE, 0xEB), Color.FromArgb(0x22, 0x8B, 0x22)),
                SkyboxColor.Sunset    => (Color.FromArgb(0xFF, 0x45, 0x00), Color.FromArgb(0xFF, 0xDA, 0xB9), Color.FromArgb(0x8B, 0x45, 0x13)),
                SkyboxColor.Night     => (Color.FromArgb(0x0B, 0x10, 0x26), Color.FromArgb(0x1C, 0x2A, 0x4A), Color.FromArgb(0x10, 0x14, 0x18)),
                SkyboxColor.Dawn      => (Color.FromArgb(0xFF, 0x7F, 0x50), Color.FromArgb(0xFF, 0xE4, 0xE1), Color.FromArgb(0x6B, 0x42, 0x26)),
                SkyboxColor.Dusk      => (Color.FromArgb(0x2E, 0x08, 0x54), Color.FromArgb(0xFF, 0x8C, 0x69), Color.FromArgb(0x3B, 0x2F, 0x2F)),
                SkyboxColor.Emerald   => (Color.FromArgb(0x00, 0xA8, 0x6B), Color.FromArgb(0xB2, 0xFF, 0xE0), Color.FromArgb(0x2E, 0x8B, 0x57)),
                SkyboxColor.Ocean     => (Color.FromArgb(0x00, 0x69, 0x94), Color.FromArgb(0x7E, 0xC8, 0xE3), Color.FromArgb(0x1E, 0x4D, 0x5B)),
                SkyboxColor.Lavender  => (Color.FromArgb(0x96, 0x7B, 0xB6), Color.FromArgb(0xE6, 0xE6, 0xFA), Color.FromArgb(0x6A, 0x5A, 0xCD)),
                SkyboxColor.Candy     => (Color.FromArgb(0xFF, 0x69, 0xB4), Color.FromArgb(0xFF, 0xF0, 0xF5), Color.FromArgb(0xDB, 0x70, 0x93)),
                SkyboxColor.Golden    => (Color.FromArgb(0xDA, 0xA5, 0x20), Color.FromArgb(0xFF, 0xF8, 0xDC), Color.FromArgb(0xB8, 0x86, 0x0B)),
                SkyboxColor.Crimson   => (Color.FromArgb(0xDC, 0x14, 0x3C), Color.FromArgb(0xFF, 0xC0, 0xCB), Color.FromArgb(0x8B, 0x00, 0x00)),
                SkyboxColor.Midnight  => (Color.FromArgb(0x00, 0x00, 0x00), Color.FromArgb(0x19, 0x19, 0x70), Color.FromArgb(0x00, 0x00, 0x00)),
                _                     => (Color.FromArgb(0x1E, 0x90, 0xFF), Color.FromArgb(0x87, 0xCE, 0xEB), Color.FromArgb(0x22, 0x8B, 0x22)),
            };
        }

        private static Color Lerp(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);

            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t)
            );
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