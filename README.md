# ColorRegionMaskCreator
Creates image masks based on a base image and a mask file where up to 6 masks are defined by r, g, b, gb, rg, rb.

The source files needs to be in the subfolder `in` and in the jpg format.
The base image can have a greenscreen which will be made transparent in the output.
The mask file is expected to have regions that are r, g, b, gb, rg, rb; the contrast will be maximized.
The according mask file needs to have the same filename with _m appended to the name before the extension, e.g. in/myImage.jpg and in/myImage_m.jpg.