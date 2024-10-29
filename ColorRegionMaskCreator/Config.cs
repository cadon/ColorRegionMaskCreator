using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ColorRegionMaskCreator
{
    public class Config
    {
        /// <summary>
        /// Load config file. Command line arguments will overrule the config file values.
        /// </summary>
        public bool Load(Dictionary<string, string> args)
        {
            const string filePath = "config.ini";
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            var lines = File.ReadAllLines(filePath);
            foreach (var l in lines)
            {
                var t = l.TrimStart();
                if (string.IsNullOrEmpty(t) || t.StartsWith(";")) continue;


                var m = RegexSettingLine.Match(t);
                if (!m.Success) continue;

                ReadSetting(m.Groups[1].Value.ToLowerInvariant(), m.Groups[2].Value);
            }

            // use command line options, these will overrule the config file values
            foreach (var arg in args)
                ReadSetting(arg.Key, arg.Value);

            return true;
        }

        private void ReadSetting(string parameterName, string value)
        {
            switch (parameterName)
            {
                case "maxwidth":
                    if (int.TryParse(value, out var i))
                        MaxWidth = i;
                    break;
                case "maxheight":
                    if (int.TryParse(value, out i))
                        MaxHeight = i;
                    break;
                case "highlightr":
                    if (byte.TryParse(value, out var b))
                        HighlightColorR = b;
                    break;
                case "highlightg":
                    if (byte.TryParse(value, out b))
                        HighlightColorG = b;
                    break;
                case "highlightb":
                    if (byte.TryParse(value, out b))
                        HighlightColorB = b;
                    break;
                case "greenscreenmingreen":
                    if (byte.TryParse(value, out b))
                        GreenScreenMinGreen = b;
                    break;
                case "greenscreenfactorglargerthanrb":
                    if (double.TryParse(value, out var d))
                        GreenScreenFactorGLargerThanRB = d;
                    break;
                case "greenscreengreenlargerthanrb":
                    if (byte.TryParse(value, out b))
                        GreenScreenGreenLargerThanRB = b;
                    break;
            }
        }

        public int MaxWidth = 256;
        public int MaxHeight = 256;
        public byte HighlightColorR = 255;
        public byte HighlightColorG = 0;
        public byte HighlightColorB = 0;
        public byte GreenScreenMinGreen = 50;
        public byte GreenScreenGreenLargerThanRB = 50;
        public double GreenScreenFactorGLargerThanRB = 2;

        private static Regex RegexSettingLine = new Regex(@"(\w+) *= *(.*)");
    }
}
