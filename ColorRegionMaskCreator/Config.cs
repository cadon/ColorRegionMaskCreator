using System.IO;
using System.Text.RegularExpressions;

namespace ColorRegionMaskCreator
{
    public class Config
    {
        public bool Load()
        {
            const string filePath = "config.ini";
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            var lines = File.ReadAllLines(filePath);
            foreach (var l in lines)
            {
                var t = l.TrimStart();
                if (string.IsNullOrEmpty(t) || t.StartsWith(";")) continue;

                ReadSetting(t);
            }

            return true;
        }

        private void ReadSetting(string line)
        {
            var m = RegexSettingLine.Match(line);
            if (!m.Success) return;

            switch (m.Groups[1].Value.ToLowerInvariant())
            {
                case "maxwidth":
                    if (int.TryParse(m.Groups[2].Value, out var i))
                        MaxWidth = i;
                    break;
                case "maxheight":
                    if (int.TryParse(m.Groups[2].Value, out i))
                        MaxHeight = i;
                    break;
                case "colorr":
                    if (byte.TryParse(m.Groups[2].Value, out var b))
                        ColorR = b;
                    break;
                case "colorg":
                    if (byte.TryParse(m.Groups[2].Value, out b))
                        ColorG = b;
                    break;
                case "colorb":
                    if (byte.TryParse(m.Groups[2].Value, out b))
                        ColorB = b;
                    break;
                case "greenscreenmingreen":
                    if (byte.TryParse(m.Groups[2].Value, out b))
                        GreenScreenMinGreen = b;
                    break;
                case "greenscreenfactorglargerthanrb":
                    if (double.TryParse(m.Groups[2].Value, out var d))
                        GreenScreenFactorGLargerThanRB = d;
                    break;
                case "greenscreengreenlargerthanrb":
                    if (byte.TryParse(m.Groups[2].Value, out b))
                        GreenScreenGreenLargerThanRB = b;
                    break;
            }
        }

        public int MaxWidth = 256;
        public int MaxHeight = 256;
        public byte ColorR = 255;
        public byte ColorG = 0;
        public byte ColorB = 0;
        public byte GreenScreenMinGreen = 50;
        public byte GreenScreenGreenLargerThanRB = 50;
        public double GreenScreenFactorGLargerThanRB = 2;

        private static Regex RegexSettingLine = new Regex(@"(\w+) *= *(.*)");
    }
}
