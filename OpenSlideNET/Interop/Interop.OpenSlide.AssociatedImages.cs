using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OpenSlideNET.Interop {
	public static partial class OpenSlideInterop {
		[DllImport(LibOpenSlide, EntryPoint = "openslide_get_associated_image_names", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr GetAssociatedImageNames_Internal(OpenSlideImageSafeHandle osr);

		/// <summary>
		/// Get the NULL-terminated array of associated image names. 
		/// Certain vendor-specific associated images may exist within a whole slide image. They are encoded as key-value pairs. This call provides a list of names as strings that can be used to read associated images with openslide_get_associated_image_dimensions() and openslide_read_associated_image().
		/// </summary>
		/// <param name="osr">The OpenSlide object. </param>
		/// <returns>A NULL-terminated string array of associated image names, or an empty array if an error occurred. </returns>
		public static unsafe string[] GetAssociatedImageNames(OpenSlideImageSafeHandle osr) {
			var list = new List<string>();
			var pCurrent = (IntPtr*)GetAssociatedImageNames_Internal(osr);
			while (*pCurrent != IntPtr.Zero) {
				var name = StringFromNativeUtf8(*pCurrent);
				list.Add(name);
				pCurrent++;
			}
			return list.ToArray();
		}

		[DllImport(LibOpenSlide, EntryPoint = "openslide_get_associated_image_dimensions", CallingConvention = CallingConvention.Cdecl)]
		private static extern void GetAssociatedImageDimensions_Internal(OpenSlideImageSafeHandle osr, string name, out long w, out long h);

		/// <summary>
		/// Get the dimensions of an associated image. 
		/// This function returns the width and height of an associated image associated with a whole slide image. Once the dimensions are known, use openslide_read_associated_image() to read the image.
		/// </summary>
		/// <param name="osr">The OpenSlide object. </param>
		/// <param name="name">The name of the desired associated image. Must be a valid name as given by openslide_get_associated_image_names(). </param>
		/// <param name="w">The width of the associated image, or -1 if an error occurred. </param>
		/// <param name="h">The height of the associated image, or -1 if an error occurred. </param>
		public static void GetAssociatedImageDimensions(OpenSlideImageSafeHandle osr, string name, out long w, out long h) {
			GetAssociatedImageDimensions_Internal(osr, name, out w, out h);
		}

		[DllImport(LibOpenSlide, EntryPoint = "openslide_read_associated_image", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void ReadAssociatedImage_Internal(OpenSlideImageSafeHandle osr, string name, void* dest);

		/// <summary>
		/// Copy pre-multiplied ARGB data from an associated image. 
		/// This function reads and decompresses an associated image associated with a whole slide image. dest must be a valid pointer to enough memory to hold the image, at least (width * height * 4) bytes in length. Get the width and height with openslide_get_associated_image_dimensions(). This call does nothing if an error occurred.
		/// </summary>
		/// <param name="osr">The OpenSlide object. </param>
		/// <param name="name">The name of the desired associated image. Must be a valid name as given by openslide_get_associated_image_names(). </param>
		/// <param name="dest">The destination buffer for the ARGB data. </param>
		public static unsafe void ReadAssociatedImage(OpenSlideImageSafeHandle osr, string name, void* dest) {
			ReadAssociatedImage_Internal(osr, name, dest);
		}
	}
}
