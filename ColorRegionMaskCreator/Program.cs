using System;
using System.Diagnostics;
using System.IO;

namespace ColorRegionMaskCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Creates base images and according mask images from jpg files in the in folder.");
            Console.WriteLine("The base image can have a greenscreen which will be made transparent in the output.");
            Console.WriteLine("The mask file is expected to have regions that are r, g, b, gb, rg, rb; the contrast will be maximized.");
            Console.WriteLine("The according mask file needs to have the same filename with _m appended to the name before the extension, e.g. in/myImage.jpg and in/myImage_m.jpg");
            Console.WriteLine();
            Console.WriteLine("Also create color region images? (y/n)");
            var answer = Console.ReadLine();
            var createColorRegionImages = answer?.Trim().ToLowerInvariant() == "y";

            ImageMasks.CreateImageMasks(createColorRegionImages);

            Console.WriteLine("Open output folder? (y/n)");
            if (Console.ReadLine()?.Trim().ToLowerInvariant() == "y")
            {
                if (Directory.Exists(ImageMasks.OutputFolderPath))
                    Process.Start("explorer.exe", $"\"{ImageMasks.OutputFolderPath}\"");

                if (createColorRegionImages && Directory.Exists(ImageMasks.OutputFolderPath))
                    Process.Start("explorer.exe", $"\"{ImageMasks.OutputRegionsFolderPath}\"");
            }
        }
    }
}
