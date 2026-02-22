# ColorRegionMaskCreator
Creates image masks based on a base image and a mask file where up to 6 masks are defined by r, g, b, gb, rg, rb.

The source files needs to be in the subfolder `in` and in the jpg format.
The base image can have a greenscreen which will be made transparent in the output.
The mask file is expected to have regions that have one of these channels maximized: r, g, b, gb, rg, rb; the contrast will be maximized.
The according mask file needs to have the same filename with _m appended to the name before the extension, e.g. in/myImage.jpg and in/myImage_m.jpg.

### Manual
Create two images: one background image with the base colors and a green screen background, the second one with color regions highlighted by a color. Follow the following steps for that:
* [more detailed walkthrough](https://github.com/cadon/ARKStatsExtractor/wiki/Creating-New-Images)
* ASA mod [Green world](https://www.curseforge.com/ark-survival-ascended/mods/greenworld) can help
* [Video tutorial by shen](https://www.youtube.com/watch?v=oZTGQ4A11uA) - the result of this tutorial is a bit different since it's a different use case but most of the steps can be used

1. in Singleplayer enter in the console `LeaveMeAlone | Fly | gamma 2 | r.ShadowQuality 0 | r.EyeAdaptationQuality 0 | r.bloomquality 0` so creatures won't hunt you and glowing effects and shadows do not overlap
2. `playersonly` so creatures won't walk away and press the backspace key on the keyboard to hide the interface
3. spawn in creature in the green screen area, e.g `spawndino "Blueprint'/Game/PrimalEarth/Dinos/Dodo/Dodo_Character_BP.Dodo_Character_BP'" 500 0 0 120`
4. looking at creature and `forcetame`
5. ride creature to good location for the images (or whistle if you can't ride them)
6. often disable `Looking at player` option looks better on the images (look at creature, hold E - options)
7. `fly` to have a better angle for the image. `settimeofday 8:0` (or other times, until the lighting looks good)
8. check if change the colors of the creature can be changed when looking at it (when pausing do not move to keep the exact same angle), e.g. by using `SetTargetDinoColor 0 1`
9. wait for a good posture. `slomo 0.1` (or other values) can help to slow down the movement to catch the right posture
10. `slomo 0` to freeze the image.
11. set all colors to white (color id 18, `SetTargetDinoColor 0 18 | SetTargetDinoColor 1 18 | SetTargetDinoColor 2 18 | SetTargetDinoColor 3 18 | SetTargetDinoColor 4 18 | SetTargetDinoColor 5 18`) and make a screenshot, this should be named like `Ankylo.jpg`
12. set the colors to their region color (`SetTargetDinoColor 0 1 | SetTargetDinoColor 1 3 | SetTargetDinoColor 2 2 | SetTargetDinoColor 3 5 | SetTargetDinoColor 4 4 | SetTargetDinoColor 5 6`), the image looks like the right one with the green background, the creature is very colorful, this file should be named like the first image, but with `_m` suffixed, e.g. `Ankylo_m.jpg`
13. for the cropping an application like gimp can be used by loading both images as a layer, crop both, so they're aligned pixelperfect, then save each as jpg in a folder named `in`
14. run the script `ColorRegionMaskCreator.exe`
