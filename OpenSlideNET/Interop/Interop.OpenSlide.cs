using System;
using System.Runtime.InteropServices;
using EasyPathology.Abstractions.Utils;

namespace OpenSlideNET.Interop;

/// <summary>
/// The interop helper for OpenSlide.
/// </summary>
public static partial class OpenSlideInterop
{
    private const string LibOpenSlide = "libopenslide-1";
    

    static OpenSlideInterop()
    {
        NativeLibrary.SetDllImportResolver(typeof(OpenSlideInterop).Assembly, LibraryResolver.Default);
    }

    /// <summary>
    /// The name of the property containing a slide's comment, if any. 
    /// </summary>
    public const string OpenSlidePropertyNameComment = "openslide.comment";

    /// <summary>
    /// The name of the property containing an identification of the vendor. 
    /// </summary>
    public const string OpenSlidePropertyNameVendor = "openslide.vendor";

    /// <summary>
    /// The name of the property containing the "quickhash-1" sum. 
    /// </summary>
    public const string OpenSlidePropertyNameQuickHash1 = "openslide.quickhash-1";

    /// <summary>
    /// The name of the property containing a slide's background color, if any.
    /// </summary>
    public const string OpenSlidePropertyNameBackgroundColor = "openslide.background-color";
        
    /// <summary>
    /// The name of the property containing a slide's objective power, if known. 
    /// </summary>
    public const string OpenSlidePropertyNameObjectivePower = "openslide.objective-power";

    /// <summary>
    /// The name of the property containing the number of microns per pixel in the X dimension of level 0, if known.
    /// </summary>
    public const string OpenSlidePropertyNameMppX = "openslide.mpp-x";

    /// <summary>
    /// The name of the property containing the number of microns per pixel in the Y dimension of level 0, if known.
    /// </summary>
    public const string OpenSlidePropertyNameMppY = "openslide.mpp-y";

    /// <summary>
    /// The name of the property containing the X coordinate of the rectangle bounding the non-empty region of the slide, if available. 
    /// </summary>
    public const string OpenSlidePropertyNameBoundsX = "openslide.bounds-x";

    /// <summary>
    /// The name of the property containing the Y coordinate of the rectangle bounding the non-empty region of the slide, if available. 
    /// </summary>
    public const string OpenSlidePropertyNameBoundsY = "openslide.bounds-y";

    /// <summary>
    /// The name of the property containing the width of the rectangle bounding the non-empty region of the slide, if available. 
    /// </summary>
    public const string OpenSlidePropertyNameBoundsWidth = "openslide.bounds-width";

    /// <summary>
    /// The name of the property containing the height of the rectangle bounding the non-empty region of the slide, if available.
    /// </summary>
    public const string OpenSlidePropertyNameBoundsHeight = "openslide.bounds-height";


    [DllImport(LibOpenSlide, EntryPoint = "openslide_get_version", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetVersionInternal();

    /// <summary>
    /// Get the version of the OpenSlide library.
    /// </summary>
    public static string? GetVersion()
    {
        return StringFromNativeUtf8(GetVersionInternal());
    }
}