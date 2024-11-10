using System;
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
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var l in lines)
                {
                    var t = l.TrimStart();
                    if (string.IsNullOrEmpty(t) || t.StartsWith(";")) continue;


                    var m = RegexSettingLine.Match(t);
                    if (!m.Success) continue;

                    ReadSetting(m.Groups[1].Value.ToLowerInvariant(), m.Groups[2].Value);
                }
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
                case "greenscreenborderfactorglargerthanrb":
                    if (double.TryParse(value, out d))
                        GreenScreenBorderFactorGLargerThanRB = d;
                    break;
                case "enlargeoutputimage":
                    EnlargeOutputImage = IsTrue(value);
                    break;
                case "cropbackground":
                    CropBackground = IsTrue(value);
                    break;
                case "expandbackgroundtosize":
                    ExpandBackgroundToSize = IsTrue(value);
                    break;
            }

            bool IsTrue(string v) => !(string.IsNullOrEmpty(v) || v == "0" || v.ToLowerInvariant() == "false");
        }

        public int MaxWidth = 256;
        public int MaxHeight = 256;
        public bool EnlargeOutputImage = false;
        public bool CropBackground = false;
        public bool ExpandBackgroundToSize = false;
        public byte HighlightColorR = 255;
        public byte HighlightColorG = 0;
        public byte HighlightColorB = 0;
        public byte GreenScreenMinGreen = 50;
        /// <summary>
        /// On borders of the foreground to the green screen remove partial green of pixels and use as alpha.
        /// </summary>
        public double GreenScreenBorderFactorGLargerThanRB = 1.2;
        public double GreenScreenFactorGLargerThanRB = 2;

        private static Regex RegexSettingLine = new Regex(@"(\w+) *= *(.*)");
    }
}
