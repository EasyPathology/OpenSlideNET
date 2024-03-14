using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenSlideNET.Interop
{
    /// <summary>
    /// The interop helper for OpenSlide.
    /// </summary>
    public static partial class OpenSlideInterop
    {
#if LINUX
        private const string LibOpenSlide = "libopenslide.so.1";
        private const string LibRelativePath = @"runtimes\linux-x64\native\";
#elif OSX
        private const string LibOpenSlide = "libopenslide.1.dylib";
        private const string LibRelativePath = @"runtimes\osx-x64\native\";
#else
        private const string LibOpenSlide = "libopenslide-1.dll";
        private const string LibRelativePath = @"runtimes\win-x64\native\";
#endif

		static OpenSlideInterop() {
			NativeLibrary.SetDllImportResolver(typeof(OpenSlideInterop).Assembly, ImportResolver);
		}

		private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
            return NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, LibRelativePath, libraryName));
		}

		/// <summary>
		/// The name of the property containing a slide's comment, if any. 
		/// </summary>
		public const string OpenSlidePropertyNameComment = "openslide.comment";

        /// <summary>
        /// The name of the property containing a slide's comment, if any. 
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameComment =>
            OpenSlidePropertyNameComment.Select(static x => (byte)x).Append((byte)0).ToArray();
        
        /// <summary>
        /// The name of the property containing an identification of the vendor. 
        /// </summary>
        public const string OpenSlidePropertyNameVendor = "openslide.vendor";

        /// <summary>
        /// The name of the property containing an identification of the vendor. 
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameVendor =>
            OpenSlidePropertyNameVendor.Select(static x => (byte)x).Append((byte)0).ToArray();

        /// <summary>
        /// The name of the property containing the "quickhash-1" sum. 
        /// </summary>
        public const string OpenSlidePropertyNameQuickHash1 = "openslide.quickhash-1";

        /// <summary>
        /// The name of the property containing the "quickhash-1" sum. 
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameQuickHash1 =>
            OpenSlidePropertyNameQuickHash1.Select(static x => (byte)x).Append((byte)0).ToArray();

        /// <summary>
        /// The name of the property containing a slide's background color, if any.
        /// </summary>
        public const string OpenSlidePropertyNameBackgroundColor = "openslide.background-color";

        /// <summary>
        /// The name of the property containing a slide's background color, if any.
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameBackgroundColor => 
            OpenSlidePropertyNameBackgroundColor.Select(static x => (byte)x).Append((byte)0).ToArray();
    

        /// <summary>
        /// The name of the property containing a slide's objective power, if known. 
        /// </summary>
        public const string OpenSlidePropertyNameObjectivePower = "openslide.objective-power";

        /// <summary>
        /// The name of the property containing a slide's objective power, if known. 
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameObjectivePower => 
            OpenSlidePropertyNameObjectivePower.Select(static x => (byte)x).Append((byte)0).ToArray();


        /// <summary>
        /// The name of the property containing the number of microns per pixel in the X dimension of level 0, if known.
        /// </summary>
        public const string OpenSlidePropertyNameMPPX = "openslide.mpp-x";

        /// <summary>
        /// The name of the property containing the number of microns per pixel in the X dimension of level 0, if known.
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameMPPX => 
            OpenSlidePropertyNameMPPX.Select(static x => (byte)x).Append((byte)0).ToArray();

        /// <summary>
        /// The name of the property containing the number of microns per pixel in the Y dimension of level 0, if known.
        /// </summary>
        public const string OpenSlidePropertyNameMPPY = "openslide.mpp-y";

        /// <summary>
        /// The name of the property containing the number of microns per pixel in the Y dimension of level 0, if known.
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameMPPY => 
            OpenSlidePropertyNameMPPY.Select(static x => (byte)x).Append((byte)0).ToArray();

        /// <summary>
        /// The name of the property containing the X coordinate of the rectangle bounding the non-empty region of the slide, if available. 
        /// </summary>
        public const string OpenSlidePropertyNameBoundsX = "openslide.bounds-x";

        /// <summary>
        /// The name of the property containing the X coordinate of the rectangle bounding the non-empty region of the slide, if available. 
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameBoundsX => 
            OpenSlidePropertyNameBoundsX.Select(static x => (byte)x).Append((byte)0).ToArray();

        /// <summary>
        /// The name of the property containing the Y coordinate of the rectangle bounding the non-empty region of the slide, if available. 
        /// </summary>
        public const string OpenSlidePropertyNameBoundsY = "openslide.bounds-y";

        /// <summary>
        /// The name of the property containing the Y coordinate of the rectangle bounding the non-empty region of the slide, if available. 
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameBoundsY => 
            OpenSlidePropertyNameBoundsY.Select(static x => (byte)x).Append((byte)0).ToArray();

        /// <summary>
        /// The name of the property containing the width of the rectangle bounding the non-empty region of the slide, if available. 
        /// </summary>
        public const string OpenSlidePropertyNameBoundsWidth = "openslide.bounds-width";

        /// <summary>
        /// The name of the property containing the width of the rectangle bounding the non-empty region of the slide, if available. 
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameBoundsWidth =>
            OpenSlidePropertyNameBoundsWidth.Select(static x => (byte)x).Append((byte)0).ToArray();

        /// <summary>
        /// The name of the property containing the height of the rectangle bounding the non-empty region of the slide, if available.
        /// </summary>
        public const string OpenSlidePropertyNameBoundsHeight = "openslide.bounds-height";

        /// <summary>
        /// The name of the property containing the height of the rectangle bounding the non-empty region of the slide, if available.
        /// </summary>
        public static ReadOnlySpan<byte> Utf8OpenSlidePropertyNameBoundsHeight => 
            OpenSlidePropertyNameBoundsHeight.Select(static x => (byte)x).Append((byte)0).ToArray();

        
        [DllImport(LibOpenSlide, EntryPoint = "openslide_get_version", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetVersionInternal();

        /// <summary>
        /// Get the version of the OpenSlide library.
        /// </summary>
        public static string GetVersion() => StringFromNativeUtf8(GetVersionInternal())!;
    }
}
