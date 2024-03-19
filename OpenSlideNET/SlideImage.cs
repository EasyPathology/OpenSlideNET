using OpenSlideNET.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using OpenSlideNET.Structs;

namespace OpenSlideNET;

/// <summary>
/// Represents the image dimensions
/// </summary>
public readonly struct ImageDimensions {
	/// <summary>
	/// The width of the image.
	/// </summary>
	public long Width { get; }

	/// <summary>
	/// The height of the image.
	/// </summary>
	public long Height { get; }

	/// <summary>
	/// Initialize a new <see cref="ImageDimensions"/> struct.
	/// </summary>
	/// <param name="width">The width of the image.</param>
	/// <param name="height">The height of the image.</param>
	public ImageDimensions(long width, long height) {
		Width = width;
		Height = height;
	}

	/// <summary>
	/// Deconstruction the struct.
	/// </summary>
	/// <param name="width">The width of the image.</param>
	/// <param name="height">The height of the image.</param>
	public void Deconstruct(out long width, out long height) {
		width = Width;
		height = Height;
	}

	/// <summary>
	/// Converts the <see cref="ImageDimensions"/> struct into a tuple of (Width, Height).
	/// </summary>
	/// <param name="dimensions">the <see cref="ImageDimensions"/> struct.</param>
	public static implicit operator (long Width, long Height)(ImageDimensions dimensions) {
		return (dimensions.Width, dimensions.Height);
	}

	/// <summary>
	/// Converts a tuple of (Width, Height) into the <see cref="ImageDimensions"/> struct.
	/// </summary>
	/// <param name="dimensions">A tuple of (Width, Height).</param>
	public static explicit operator ImageDimensions(ValueTuple<long, long> dimensions) {
		return new ImageDimensions(dimensions.Item1, dimensions.Item2);
	}
}

public interface ISlideImage : IDisposable {
	int LevelCount { get; }
	ImageDimensions Dimensions { get; }
	Color4B? BackgroundColor { get; }

	string QuickHash1 { get; }
	string QuickHash2 { get; }
	Size2D? MicronsPerPixel { get; }

	ImageDimensions GetLevelDimensions(int level);
	Size2I GetLevelTileSize(int level);
	Size2I GetLevelOverlap(int level);
	
	double GetLevelDownsample(int level);
	IReadOnlyList<string> GetAllPropertyNames();
	bool TryGetProperty(string name, out string value);
	void ReadRegion(int level, long x, long y, long width, long height, IntPtr buffer);
}

public static class SlideImage {
	public static HashSet<string> SupportedExtensions { get; } = new() {
		".ndpi", ".svs", ".tiff", ".tif", ".mrxs"
	};

	/// <summary>
	/// Open a whole slide image.
	/// This function can be expensive; avoid calling it unnecessarily. For example, a tile server should not call Open() on every tile request. Instead, it should maintain a cache of <see cref="OpenSlideImage"/> objects and reuse them when possible.
	/// </summary>
	/// <param name="slidePath">The filename to open.</param>
	/// <returns>The <see cref="OpenSlideImage"/> object.</returns>
	/// <exception cref="OpenSlideUnsupportedFormatException">The file format can not be recognized.</exception>
	/// <exception cref="OpenSlideException">The file format is recognized, but an error occurred when opening the file.</exception>
	public static ISlideImage Open([NotNull] string slidePath) {
		if (slidePath == null) {
			throw new ArgumentNullException(nameof(slidePath));
		}

		if (!SupportedExtensions.Contains(Path.GetExtension(slidePath))) {
			throw new OpenSlideUnsupportedFormatException();
		}

		// Open file using OpenSlide
		var handle = OpenSlideInterop.Open(slidePath);
		if (handle.IsInvalid) {
			throw new OpenSlideUnsupportedFormatException();
		}

		if (!ThrowHelper.TryCheckError(handle, out var errMsg)) {
			handle.Dispose();
			throw new OpenSlideException(errMsg);
		}
		return new OpenSlideImage(slidePath, handle);
	}

	public static bool TryReadQuickHash(string slidePath, [NotNullWhen(true)] out string quickHash, [NotNullWhen(false)] out Exception exception) {
		try {
			if (slidePath.EndsWith(".mds")) {
				quickHash = SlideHash.GetHash(slidePath);
				exception = null;
				return true;
			}

			using var handle = OpenSlideInterop.Open(slidePath);
			if (handle.IsInvalid) {
				quickHash = null;
				exception = new FileLoadException(slidePath);
				return false;
			}

			quickHash = OpenSlideInterop.GetPropertyValue(handle, OpenSlideInterop.OpenSlidePropertyNameQuickHash1);
			quickHash ??= SlideHash.GetHash(slidePath);
			exception = null;
			return true;
		} catch (Exception e) {
			quickHash = null;
			exception = e;
			return false;
		}
	}
}