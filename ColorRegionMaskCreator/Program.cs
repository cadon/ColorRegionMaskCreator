using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ColorRegionMaskCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine(args[0]);
            var dontCreateRegionHighlights = false;
            var argDict = new Dictionary<string, string>();

            for (var ai = 0; ai < args.Length; ai++)
            {
                var parameterName = args[ai].ToLowerInvariant();
                if (parameterName.Length > 0 && parameterName[0] == '-')
                {
                    parameterName = parameterName.Substring(1);
                }

                switch (parameterName)
                {
                    case "in":
                    case "out":
                    case "outregions":
                    case "maxwidth":
                    case "maxheight":
                    case "highlightr":
                    case "highlightg":
                    case "highlightb":
                    case "greenscreenmingreen":
                    case "greenscreenfactorglargerthanrb":
                    case "greenscreenborderfactorglargerthanrb":
                    case "enlargeoutputimage":
                    case "cropbackground":
                    case "expandbackgroundtosize":
                        if (ai + 2 > args.Length) break;
                        argDict[parameterName] = args[ai + 1];
                        break;
                    case "noregions":
                        dontCreateRegionHighlights = true;
                        break;
                    case "autostart":
                        argDict[parameterName] = "1";
                        break;
                    case "openoutfolder":
                        if (ai + 2 > args.Length) break;
                        argDict[parameterName] = args[ai + 1] == "1" ? "1" : string.Empty;
                        break;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Creates base images and according mask images from jpg or png files in the in folder.");
            Console.WriteLine("The base image can have a green screen which will be made transparent in the output.");
            Console.WriteLine("The mask file is expected to have regions that are r, g, b, gb, rg, rb; the contrast will be maximized.");
            Console.WriteLine("The according mask file needs to have the same filename with _m appended to the name before the extension, e.g. in/myImage.jpg and in/myImage_m.jpg");
            Console.WriteLine();
            Console.WriteLine("### Parameters");
            Console.WriteLine("The default input folder is \"/in\" and can be adjusted with the command line parameter \"-in [inputFolder]\".");
            Console.WriteLine("Parameter -out [outputFolder] (default \"/out\")");
            Console.WriteLine("Parameter -outRegions [outputRegionsFolder] (default \"/outRegions\")");
            Console.WriteLine("Parameter -openOutFolder: possible values 1: will open the output folder after the images are processed. Other values will not open the output folder. Omitting this parameter will ask if the folder should be opened.");
            Console.WriteLine("Parameter -autostart with no values. If stated, the app will start processing directly.");
            Console.WriteLine("Parameter -maxWidth [width in px]. Max width of the output images. Smaller images are not enlarged. Default 800");
            Console.WriteLine("Parameter -maxHeight [height in px]. Max height of the output images. Smaller images are not enlarged. Default 600");
            Console.WriteLine("Parameter -highlightR [red value]. Red channel of color for region highlighting. Default 240");
            Console.WriteLine("Parameter -highlightG [green value]. Green channel of color for region highlighting. Default 50");
            Console.WriteLine("Parameter -highlightB [blue value]. Blue channel of color for region highlighting. Default 10");
            Console.WriteLine(
                "Parameter -GreenScreenMinGreen [green value]. If there are green edges on the object from the green screen (spilling), decreasing the following values may help to reduce that. Too low values will remove too much from the object.");
            Console.WriteLine(" GreenScreenMinGreen is the min value of the green channel that a pixel can be set as green screen (if also the GreenScreenFactorGLargerThanRB condition is fulfilled). Default 50.");
            Console.WriteLine("Parameter -GreenScreenFactorGLargerThanRB [factor]. GreenScreenFactorGLargerThanRB is the factor the green channel needs to be larger than the red and the blue channel that a pixel can be set as green screen (if also the GreenScreenMinGreen condition is fulfilled)");
            Console.WriteLine(" E.g. with a a value of 2 a pixel with the rgb color (50,100,50) is considered a green screen, but the color (51,100,0) is not. Default 2.0.");
            Console.WriteLine("Parameter -GreenScreenBorderFactorGLargerThanRB [factor]. Factor the green channel needs to be larger than the red and the blue channel to remove green out of pixel and replace with transparency, used to reduce green border. Default 1.2.");
            Console.WriteLine("Parameter -EnlargeOutputImage [true|false]. If the output is smaller than the desired output size, it can be enlarged, default false");
            Console.WriteLine("Parameter -CropBackground [true|false]. The background can be cropped, default false");
            Console.WriteLine("Parameter -ExpandBackgroundToSize [true|false]. The background can be expanded (uncropped) to make the output fit the maxWidth and maxHeight ratio");
            if (!dontCreateRegionHighlights)
            {
                Console.WriteLine();
                Console.WriteLine("Also create color region images? (y/n)");
                var answer = Console.ReadLine();
                dontCreateRegionHighlights = answer?.Trim().ToLowerInvariant() != "y";
            }
            
            Console.WriteLine();
            
            var autoStart = argDict.ContainsKey("autostart");
            if (!autoStart)
            {
                Console.WriteLine("Enter to start. q to quit");
                if (Console.ReadLine()?.Trim().ToLowerInvariant() == "q") return;
            }

            if (!ImageMasks.CreateImageMasks(!dontCreateRegionHighlights, argDict))
                return;

            bool openOutFolder;
            if (argDict.TryGetValue("openoutfolder", out var openOutVal))
            {
                openOutFolder = openOutVal == "1";
            }
            else
            {
                Console.WriteLine("Open output folder? (y/n)");
                openOutFolder = Console.ReadLine()?.Trim().ToLowerInvariant() == "y";
            }

            if (openOutFolder)
            {
                if (Directory.Exists(ImageMasks.OutputFolderPath))
                    Process.Start("explorer.exe", $"\"{ImageMasks.OutputFolderPath}\"");

                if (!dontCreateRegionHighlights && Directory.Exists(ImageMasks.OutputFolderPath))
                    Process.Start("explorer.exe", $"\"{ImageMasks.OutputRegionsFolderPath}\"");
            }
        }
    }
}
