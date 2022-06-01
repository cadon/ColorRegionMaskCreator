using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ColorRegionMaskCreator
{
    internal static class ImageMasks
    {
        private const string InputFolder = "in";
        private const string OutputFolder = "outMasks";
        private const string OutputRegionsFolder = "outRegions";
        private static string _baseDirectoryPath;
        private static string _inputFolderPath;
        public static string OutputFolderPath;
        public static string OutputRegionsFolderPath;
        private static Config _config;

        internal static void CreateImageMasks(bool createRegionImages)
        {
            _baseDirectoryPath = Environment.CurrentDirectory;
            _inputFolderPath = Path.Combine(_baseDirectoryPath, InputFolder);
            OutputFolderPath = Path.Combine(_baseDirectoryPath, OutputFolder);
            OutputRegionsFolderPath = Path.Combine(_baseDirectoryPath, OutputRegionsFolder);

            _config = new Config();
            _config.Load();

            // get source files
            var files = Directory.GetFiles(_inputFolderPath);

            var imageFiles = new Dictionary<string, string>();

            // set defaultImages
            foreach (var f in files)
            {
                if (f.EndsWith(".jpg") && !f.EndsWith("_m.jpg"))
                    imageFiles.Add(Path.Combine(_inputFolderPath, f), null);
            }

            // set maskFiles
            foreach (var f in files)
            {
                if (f.EndsWith("_m.jpg"))
                {
                    var maskFilePath = Path.Combine(_inputFolderPath, f);
                    var baseImageFilePath = maskFilePath.Replace("_m.jpg", ".jpg");
                    if (imageFiles.ContainsKey(baseImageFilePath))
                        imageFiles[baseImageFilePath] = maskFilePath;
                }
            }

            InitializeLogisticFunction();

            Directory.CreateDirectory(OutputFolderPath);
            if (createRegionImages)
                Directory.CreateDirectory(OutputRegionsFolderPath);

            int count = imageFiles.Count;
            int i = 0;

            foreach (var fp in imageFiles)
            {
                CreateFiles(fp.Key, fp.Value, createRegionImages);
                int progress = ++i * 100 / count;
                Console.WriteLine($"{progress} % ({i}/{count}): {Path.GetFileNameWithoutExtension(fp.Key)}");
            }
        }

        /// <summary>
        /// Loads file and creates mask assuming there is a greenScreen.
        /// </summary>
        private static void CreateFiles(string baseImageFilePath, string colorMaskFile, bool createRegionImages)
        {
            using (Bitmap bmpBackgroundJpg = new Bitmap(baseImageFilePath))
            using (Bitmap bmpBackground = new Bitmap(bmpBackgroundJpg.Width, bmpBackgroundJpg.Height, PixelFormat.Format32bppArgb))
            using (Bitmap bmpColorMask = File.Exists(colorMaskFile) ? new Bitmap(colorMaskFile) : null)
            {
                BitmapData bmpDataJpg = bmpBackgroundJpg.LockBits(
                    new Rectangle(0, 0, bmpBackgroundJpg.Width, bmpBackgroundJpg.Height),
                    ImageLockMode.ReadWrite, bmpBackgroundJpg.PixelFormat);
                BitmapData bmpDataBackground = bmpBackground.LockBits(new Rectangle(0, 0, bmpBackground.Width, bmpBackground.Height),
                    ImageLockMode.ReadWrite, bmpBackground.PixelFormat);
                BitmapData bmpDataColorMask = bmpColorMask?.LockBits(new Rectangle(0, 0, bmpColorMask.Width, bmpColorMask.Height),
                    ImageLockMode.ReadWrite, bmpColorMask.PixelFormat);

                // count of pixels by lightness. used for contrast improvement
                var bgHistogram = new int[256];

                unsafe
                {
                    byte* scan0Jpg = (byte*)bmpDataJpg.Scan0.ToPointer();
                    byte* scan0Background = (byte*)bmpDataBackground.Scan0.ToPointer();
                    byte* scan0ColorMask = bmpDataColorMask != null ? (byte*)bmpDataColorMask.Scan0.ToPointer() : null;

                    var width = bmpDataJpg.Width;
                    var height = bmpDataJpg.Height;
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            byte* dJpg = scan0Jpg + j * bmpDataJpg.Stride + i * 3;
                            byte* dBg = scan0Background + j * bmpDataBackground.Stride + i * 4;
                            byte* dCm = scan0ColorMask != null ? (scan0ColorMask + j * bmpDataColorMask.Stride + i * 3) : null;

                            if (dJpg[1] >= _config.GreenScreenMinGreen && dJpg[2] * _config.GreenScreenFactorGLargerThanRB <= dJpg[1] && dJpg[0] * _config.GreenScreenFactorGLargerThanRB <= dJpg[1])
                            {
                                // is greenScreen, set color mask to black
                                if (dCm != null)
                                {
                                    dCm[0] = 0;
                                    dCm[1] = 0;
                                    dCm[2] = 0;
                                }
                            }
                            else
                            {
                                dBg[0] = dJpg[0];
                                byte alpha = 255;
                                // remove green glow of green screen and set transparency
                                if (dJpg[1] > dJpg[0] + 20 && dJpg[1] > dJpg[0] * 1.2 && dJpg[1] > dJpg[2] * 1.2)
                                {
                                    dBg[1] = dJpg[0];
                                    var notGreenMean = (dJpg[0] + dJpg[2]) / 2;
                                    alpha = (byte)(255 - dJpg[1] + notGreenMean);
                                }
                                else
                                {
                                    dBg[1] = dJpg[1];
                                }
                                dBg[2] = dJpg[2];
                                dBg[3] = alpha;

                                bgHistogram[(dBg[0] + dBg[1] + dBg[2]) / 3]++; // assume lightness for the histogram adjustment is just the average of rgb

                                if (dCm == null) continue;

                                const int thresholdDifferentColor = 20;
                                // color mask should only be set if there is no base color
                                bool colorDifference = Math.Abs(dJpg[0] - dCm[0]) > thresholdDifferentColor
                                                       || Math.Abs(dJpg[1] - dCm[1]) > thresholdDifferentColor
                                                       || Math.Abs(dJpg[2] - dCm[2]) > thresholdDifferentColor;

                                //if (colorDifferenceB == 0 && colorDifferenceG == 0 && colorDifferenceR == 0)
                                if (!colorDifference)
                                {
                                    dCm[0] = 0;
                                    dCm[1] = 0;
                                    dCm[2] = 0;
                                }
                                else
                                {
                                    // get the dominant colors
                                    byte max = Math.Max(Math.Max(dCm[0], dCm[1]), dCm[2]);

                                    if (max < 20)
                                    {
                                        // set to black
                                        dCm[0] = 0;
                                        dCm[1] = 0;
                                        dCm[2] = 0;
                                    }
                                    else
                                    {
                                        // the dominant color should have a value of 255
                                        double factor = 255d / max;

                                        // there are at most 2 channels active simultaneously
                                        byte min = Math.Min(Math.Min(dCm[0], dCm[1]), dCm[2]);
                                        byte threshold = (byte)(min * 1.2);

                                        dCm[0] = (byte)(dCm[0] > threshold ? factor * dCm[0] : 0);
                                        dCm[1] = (byte)(dCm[1] > threshold ? factor * dCm[1] : 0);
                                        dCm[2] = (byte)(dCm[2] > threshold ? factor * dCm[2] : 0);
                                    }
                                }
                            }
                        }
                    }

                    // adjust contrast of the background image to improve the displayed colors
                    var transformFunction = CreateTransformFunction(bgHistogram);

                    width = bmpDataBackground.Width;
                    height = bmpDataBackground.Height;
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            byte* dBg = scan0Background + j * bmpDataBackground.Stride + i * 4;
                            if (dBg[3] == 0) continue; // transparent

                            // adjust lightness of pixel
                            var lightness = (dBg[0] + dBg[1] + dBg[2]) / 3;
                            if (lightness == 0 || lightness == 255) continue;

                            var lightnessFactor = (double)transformFunction[lightness] / lightness;
                            dBg[0] = ApplyLightnessCorrection(dBg[0], lightnessFactor);
                            dBg[1] = ApplyLightnessCorrection(dBg[1], lightnessFactor);
                            dBg[2] = ApplyLightnessCorrection(dBg[2], lightnessFactor);

                            byte ApplyLightnessCorrection(byte val, double factor)
                            {
                                var result = val * factor;
                                if (result > 255) return 255;
                                if (result < 0) return 0;
                                return (byte)result;
                            }
                        }
                    }
                }

                bmpBackgroundJpg.UnlockBits(bmpDataJpg);
                bmpBackground.UnlockBits(bmpDataBackground);
                bmpColorMask?.UnlockBits(bmpDataColorMask);

                var filePathBaseImage = Path.Combine(OutputFolderPath, Path.GetFileNameWithoutExtension(baseImageFilePath) + ".png");
                var filePathMaskImage = Path.Combine(OutputFolderPath, Path.GetFileNameWithoutExtension(colorMaskFile) + ".png");

                var resizeFactor = 1d;
                if (_config.MaxWidth > 0 && _config.MaxWidth < bmpBackground.Width)
                {
                    resizeFactor = (double)_config.MaxWidth / bmpBackground.Width;
                }

                if (_config.MaxHeight > 0 && _config.MaxHeight < bmpBackground.Height)
                {
                    var resizeFactorH = (double)_config.MaxHeight / bmpBackground.Height;
                    if (resizeFactorH < resizeFactor)
                        resizeFactor = resizeFactorH;
                }

                var imageWidth = (int)(bmpBackground.Width * resizeFactor);
                var imageHeight = (int)(bmpBackground.Height * resizeFactor);

                SaveResizedBitmap(bmpBackground, filePathBaseImage, imageWidth, imageHeight);
                SaveResizedBitmap(bmpColorMask, filePathMaskImage, imageWidth, imageHeight);

                if (createRegionImages && bmpColorMask != null)
                {
                    const int colorRegionCount = 6;

                    var colors = new byte[colorRegionCount][];
                    var highlightedColor = new byte[] { _config.ColorR, _config.ColorG, _config.ColorB };
                    for (int i = 0; i < colorRegionCount; i++)
                    {
                        colors[i] = highlightedColor;
                    }
                    var enabledColorRegions = new bool[colorRegionCount];

                    for (int i = 0; i < colorRegionCount; i++)
                    {
                        for (int j = 0; j < colorRegionCount; j++)
                        {
                            enabledColorRegions[j] = i == j;
                        }

                        using (var regionImage = new Bitmap(filePathBaseImage))
                        {
                            if (CreateColorRegionImages(colors, enabledColorRegions, filePathMaskImage, regionImage))
                                SaveResizedBitmap(regionImage,
                                    Path.Combine(OutputRegionsFolderPath, Path.GetFileNameWithoutExtension(baseImageFilePath) + "_PaintRegion_" + i + ".png"),
                                    imageWidth, imageHeight);
                        }
                    }
                }
            }
        }

        private static void SaveResizedBitmap(Bitmap bmp, string filePath, int imageWidth, int imageHeight)
        {
            if (bmp == null) return;

            using (var bmpResized = new Bitmap(imageWidth, imageHeight, bmp.PixelFormat))
            using (var g = Graphics.FromImage(bmpResized))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(bmp, 0, 0, imageWidth, imageHeight);
                bmpResized.Save(filePath, ImageFormat.Png);
            }
        }

        /// <summary>
        /// Creates a transformation for lightness values to create a gaussian histogram.
        /// </summary>
        /// <param name="bgHistogram"></param>
        /// <returns></returns>
        private static int[] CreateTransformFunction(int[] bgHistogram)
        {
            var transformation = new int[256];
            int count = bgHistogram.Sum();
            int integral = 0;
            for (int i = 0; i < 256; i++)
            {
                // equalize histogram, then make histogram gaussian to increase amount of middle values (these give better colors with the mask)
                transformation[i] = _invLogisticFunction[255 * integral / count];
                //transformation[i] = 255 * integral / count;
                integral += bgHistogram[i];
            }

            return transformation;
        }

        private static int[] _invLogisticFunction;
        /// <summary>
        /// Used to create a good lightness distribution of the final image, so the applied color masks look good.
        /// </summary>
        private static void InitializeLogisticFunction()
        {
            _invLogisticFunction = new int[256];
            const int x0 = 127;
            const int k = 50;
            const int stretch = 22;
            const double l = 255 + stretch * 2;

            for (int i = 1; i < 255; i++)
            {
                // logistic function: (l / (1 + Math.Exp(-k * (i - x0))))
                var r = (int)(-k * Math.Log(l / (i + stretch) - 1) + x0);
                if (r < 0) r = 0;
                else if (r > 255) r = 255;
                _invLogisticFunction[i] = r;
            }

            _invLogisticFunction[255] = 255;
        }

        /// <summary>
        /// Applies the colors to the base image.
        /// </summary>
        private static bool CreateColorRegionImages(byte[][] rgb, bool[] enabledColorRegions, string speciesColorMaskFilePath, Bitmap bmpBaseImage)
        {
            var imageFine = false;
            using (Bitmap bmpMask = new Bitmap(speciesColorMaskFilePath))
            {
                BitmapData bmpDataBaseImage = bmpBaseImage.LockBits(
                    new Rectangle(0, 0, bmpBaseImage.Width, bmpBaseImage.Height), ImageLockMode.ReadOnly,
                    bmpBaseImage.PixelFormat);
                BitmapData bmpDataMask = bmpMask.LockBits(
                    new Rectangle(0, 0, bmpMask.Width, bmpMask.Height), ImageLockMode.ReadOnly,
                    bmpMask.PixelFormat);

                int bgBytes = bmpBaseImage.PixelFormat == PixelFormat.Format32bppArgb ? 4 : 3;
                int msBytes = bmpDataMask.PixelFormat == PixelFormat.Format32bppArgb ? 4 : 3;

                float o = 0;
                try
                {
                    unsafe
                    {
                        byte* scan0Bg = (byte*)bmpDataBaseImage.Scan0.ToPointer();
                        byte* scan0Ms = (byte*)bmpDataMask.Scan0.ToPointer();

                        var width = bmpDataBaseImage.Width;
                        var height = bmpDataBaseImage.Height;
                        var strideBaseImage = bmpDataBaseImage.Stride;
                        var strideMask = bmpDataMask.Stride;

                        for (int i = 0; i < width; i++)
                        {
                            for (int j = 0; j < height; j++)
                            {
                                byte* dBg = scan0Bg + j * strideBaseImage + i * bgBytes;
                                // continue if the pixel is transparent
                                if (dBg[3] == 0)
                                    continue;

                                byte* dMs = scan0Ms + j * strideMask + i * msBytes;

                                int r = dMs[2];
                                int g = dMs[1];
                                int b = dMs[0];
                                byte finalR = dBg[2];
                                byte finalG = dBg[1];
                                byte finalB = dBg[0];

                                for (int m = 0; m < 6; m++)
                                {
                                    if (!enabledColorRegions[m])
                                        continue;
                                    switch (m)
                                    {
                                        case 0:
                                            o = Math.Max(0, r - g - b) / 255f;
                                            break;
                                        case 1:
                                            o = Math.Max(0, g - r - b) / 255f;
                                            break;
                                        case 2:
                                            o = Math.Max(0, b - r - g) / 255f;
                                            break;
                                        case 3:
                                            o = Math.Min(g, b) / 255f;
                                            break;
                                        case 4:
                                            o = Math.Min(r, g) / 255f;
                                            break;
                                        case 5:
                                            o = Math.Min(r, b) / 255f;
                                            break;
                                    }

                                    if (o == 0)
                                        continue;
                                    // using "grain merge", e.g. see https://docs.gimp.org/en/gimp-concepts-layer-modes.html
                                    int rMix = finalR + rgb[m][0] - 128;
                                    if (rMix < 0) rMix = 0;
                                    else if (rMix > 255) rMix = 255;
                                    int gMix = finalG + rgb[m][1] - 128;
                                    if (gMix < 0) gMix = 0;
                                    else if (gMix > 255) gMix = 255;
                                    int bMix = finalB + rgb[m][2] - 128;
                                    if (bMix < 0) bMix = 0;
                                    else if (bMix > 255) bMix = 255;

                                    finalR = (byte)(o * rMix + (1 - o) * finalR);
                                    finalG = (byte)(o * gMix + (1 - o) * finalG);
                                    finalB = (byte)(o * bMix + (1 - o) * finalB);
                                }

                                // set final color
                                dBg[0] = finalB;
                                dBg[1] = finalG;
                                dBg[2] = finalR;
                            }
                        }
                        imageFine = true;
                    }
                }
                catch
                {
                    // error during drawing, maybe mask is smaller than image
                }

                bmpBaseImage.UnlockBits(bmpDataBaseImage);
                bmpMask.UnlockBits(bmpDataMask);
            }

            return imageFine;
        }
    }
}
