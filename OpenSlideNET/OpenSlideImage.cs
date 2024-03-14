using OpenSlideNET.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using OpenSlideNET.Structs;

namespace OpenSlideNET;

/// <summary>
/// A user-friendly wrapper class that operates on OpenSlide image.
/// </summary>
public sealed class OpenSlideImage : ISlideImage
{
	/// <summary>
	/// Gets the OpenSlide library version.
	/// </summary>
	public static string LibraryVersion => OpenSlideInterop.GetVersion();

	private          OpenSlideImageSafeHandle handle;
	private readonly bool                     leaveOpen;
	private readonly string                   slidePath;

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenSlideImage"/> class with the specified <see cref="OpenSlideImageSafeHandle"/>.
	/// </summary>
	/// <param name="slidePath"></param>
	/// <param name="handle"></param>
	/// <param name="leaveOpen"></param>
	internal OpenSlideImage(string slidePath, OpenSlideImageSafeHandle handle, bool leaveOpen = false)
	{
		if (handle == null || handle.IsInvalid) throw new ArgumentNullException(nameof(handle));

		this.handle    = handle;
		this.leaveOpen = leaveOpen;
		this.slidePath = slidePath;

		QuickHash1 = this[OpenSlideInterop.OpenSlidePropertyNameQuickHash1] ?? SlideHash.GetHash(slidePath);

		var mppX = GetMicronsPerPixel('X');
		var mppY = GetMicronsPerPixel('Y');
		if (mppX == null && mppY == null) MicronsPerPixel = null;
		else MicronsPerPixel                              = new Size2D(mppX ?? mppY!.Value, mppY ?? mppX!.Value);

		if (TryGetProperty("openslide.background-color", out var bgc) &&
		    Color4B.TryParse(bgc, out var color)) BackgroundColor = color;
		else BackgroundColor                                      = null;
	}

	/// <summary>
	/// Gets the OpenSlideImageSafeHandle for this object.
	/// </summary>
	public OpenSlideImageSafeHandle SafeHandle => handle ?? throw new ObjectDisposedException(nameof(OpenSlideImage));

	/// <summary>
	/// Return a string describing the format vendor of the specified file. This string is also accessible via the PROPERTY_NAME_VENDOR property.
	/// If the file is not recognized, return null.
	/// </summary>
	/// <param name="filename">the file to examine</param>
	/// <returns>the format vendor of the specified file.</returns>
	public static string DetectFormat(string filename) => OpenSlideInterop.DetectVendor(filename);

	/// <summary>
	/// The number of levels in the slide. Levels are numbered from 0 (highest resolution) to level_count - 1 (lowest resolution).
	/// </summary>
	public int LevelCount
	{
		get
		{
			EnsureNotDisposed();

			var result = OpenSlideInterop.GetLevelCount(handle);
			if (result == -1) ThrowHelper.CheckAndThrowError(handle);
			return result;
		}
	}

	private ImageDimensions? dimensionsCache;

	private void EnsureDimensionsCached()
	{
		if (dimensionsCache != null) return;
		OpenSlideInterop.GetLevel0Dimensions(handle, out var w, out var h);
		if (w == -1 || h == -1) ThrowHelper.CheckAndThrowError(handle);
		dimensionsCache = new ImageDimensions(w, h);
	}

	/// <summary>
	/// A (width, height) tuple for level 0 of the slide.
	/// </summary>
	/// <exception cref="System.InvalidOperationException"></exception>
	public ImageDimensions Dimensions
	{
		get
		{
			EnsureNotDisposed();
			EnsureDimensionsCached();

			return dimensionsCache!.Value;
		}
	}

	/// <summary>
	/// Width of the level 0 image of the slide.
	/// </summary>
	/// <exception cref="System.InvalidOperationException"></exception>
	public long Width
	{
		get
		{
			EnsureNotDisposed();
			EnsureDimensionsCached();

			return dimensionsCache!.Value.Width;
		}
	}

	/// <summary>
	/// Height of the level 0 image of the slide.
	/// </summary>
	/// <exception cref="System.InvalidOperationException"></exception>
	public long Height
	{
		get
		{
			EnsureNotDisposed();
			EnsureDimensionsCached();

			return dimensionsCache!.Value.Height;
		}
	}

	public Color4B? BackgroundColor { get; private set; }

	/// <summary>
	/// Get a (width, height) tuple for level k of the slide.
	/// </summary>
	/// <param name="level">the k level</param>
	/// <returns>A (width, height) tuple for level k of the slide.</returns>
	/// <exception cref="OpenSlideException">An error occurred when calling reading the slide or the <see cref="OpenSlideImage"/> was already in the error state.</exception>
	public ImageDimensions GetLevelDimensions(int level)
	{
		EnsureNotDisposed();

		OpenSlideInterop.GetLevelDimensions(handle, level, out var w, out var h);
		if (w == -1 || h == -1) ThrowHelper.CheckAndThrowError(handle);
		return new ImageDimensions(w, h);
	}

	private const int AdditionalOverlap = 2;

	public Size2I GetLevelOverlap(int level)
	{
		return new Size2I(AdditionalOverlap, AdditionalOverlap);
	}

	/// <summary>
	/// Get the downsample factor for level k of the slide.
	/// </summary>
	/// <param name="level">the k level</param>
	/// <returns>The downsample factor for level k of the slide.</returns>
	/// <exception cref="OpenSlideException">An error occurred when calling reading the slide or the <see cref="OpenSlideImage"/> was already in the error state.</exception>
	public double GetLevelDownsample(int level)
	{
		EnsureNotDisposed();

		var result = OpenSlideInterop.GetLevelDownsample(handle, level);
		if (result < 0d)
		{
			ThrowHelper.CheckAndThrowError(handle);
		}

		return result;
	}

	/// <summary>
	/// Gets the metadata about the slide.
	/// </summary>
	/// <param name="name">The metadata key name.</param>
	/// <returns>A string containing the metadata value or NULL if there is no such metadata.</returns>
	/// <exception cref="OpenSlideException">An error occurred when calling reading the slide or the <see cref="OpenSlideImage"/> was already in the error state.</exception>
	[IndexerName("Property")]
	public string? this[string name]
	{
		get
		{
			EnsureNotDisposed();

			var value = OpenSlideInterop.GetPropertyValue(handle, name);
			ThrowHelper.CheckAndThrowError(handle);
			return value;
		}
	}

	/// <summary>
	/// Gets the comment of the slide.
	/// </summary>
	public string? Comment => this[OpenSlideInterop.OpenSlidePropertyNameComment];

	/// <summary>
	/// Gets the vendor of the slide.
	/// </summary>
	public string? Vendor => this[OpenSlideInterop.OpenSlidePropertyNameVendor];

	/// <summary>
	/// Gets the quick hash of the slide.
	/// </summary>
	public string QuickHash1 { get; }

	/// <summary>
	/// Gets the quick hash of the slide.
	/// </summary>
	public string QuickHash2 => quickHash2 ??= SlideHash.GetHash2(slidePath);

	private string? quickHash2;

	/// <summary>
	/// Get microns per pixel in the left to right direction.
	/// </summary>
	public Size2D? MicronsPerPixel { get; }

	/// <summary>
	/// The X coordinate of the rectangle bounding the non-empty region of the slide, if available.
	/// </summary>
	public long? BoundsX =>
		TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameBoundsX, out var value) &&
		long.TryParse(value, out var result)
			? result
			: null;

	/// <summary>
	/// The Y coordinate of the rectangle bounding the non-empty region of the slide, if available.
	/// </summary>
	public long? BoundsY =>
		TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameBoundsY, out var value) &&
		long.TryParse(value, out var result)
			? result
			: null;

	/// <summary>
	/// The width of the rectangle bounding the non-empty region of the slide, if available.
	/// </summary>
	public long? BoundsWidth =>
		TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameBoundsWidth, out var value) &&
		long.TryParse(value, out var result)
			? result
			: null;

	/// <summary>
	/// The height of the rectangle bounding the non-empty region of the slide, if available.
	/// </summary>
	public long? BoundsHeight =>
		TryGetProperty(OpenSlideInterop.OpenSlidePropertyNameBoundsHeight, out var value) &&
		long.TryParse(value, out var result)
			? result
			: null;

	private double? GetMicronsPerPixel(char dimension)
	{
		if (TryGetProperty($"openslide.mpp-{dimension - 'A' + 'a'}", out var value) &&
		    double.TryParse(value, out var result))
			return result;

		if (!TryGetProperty("tiff.ResolutionUnit", out var u)) return null;
		var unit = u.ToLower() switch
		{
			"centimeter" => 10000d,
			"millimeter" => 1000d,
			"inch"       => 25400d,
			_            => -1d
		};
		if (unit > 0 && TryGetProperty($"tiff.{dimension}Resolution", out var resolution) &&
		    double.TryParse(resolution, out var lengthUnitPixel))
			return unit / lengthUnitPixel;

		return null;
	}

	/// <summary>
	/// Get the array of property names. 
	/// </summary>
	/// <returns>The array of property names</returns>
	public IReadOnlyList<string> GetAllPropertyNames()
	{
		EnsureNotDisposed();

		var properties = OpenSlideInterop.GetPropertyNames(handle);
		ThrowHelper.CheckAndThrowError(handle);
		return properties;
	}

	/// <summary>
	/// Gets the property value.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="value">The value of the property.</param>
	/// <returns>True if the property of the specified name exists. Otherwise, false.</returns>
	public bool TryGetProperty(string name, [NotNullWhen(true)] out string? value)
	{
		EnsureNotDisposed();

		value = OpenSlideInterop.GetPropertyValue(handle, name);
		return value != null;
	}

	/// <summary>
	/// Get the array of names of associated images. 
	/// </summary>
	/// <returns>The array of names of associated images.</returns>
	public IReadOnlyCollection<string> GetAllAssociatedImageNames()
	{
		EnsureNotDisposed();

		var associatedImages = OpenSlideInterop.GetAssociatedImageNames(handle);
		ThrowHelper.CheckAndThrowError(handle);
		return associatedImages;
	}

	/// <summary>
	/// Gets the dimensions of the associated image.
	/// </summary>
	/// <param name="name">The name of the associated image.</param>
	/// <param name="dimensions">The dimensions of the associated image.</param>
	/// <returns>True if the associated image of the specified name exists. Otherwise, false.</returns>
	public bool TryGetAssociatedImageDimensions(string name, out ImageDimensions dimensions)
	{
		EnsureNotDisposed();

		OpenSlideInterop.GetAssociatedImageDimensions(handle, name, out var w, out var h);
		if (w != -1 && h != -1)
		{
			dimensions = new ImageDimensions(w, h);
			return true;
		}

		dimensions = default;
		return false;
	}

	/// <summary>
	/// Copy pre-multiplied BGRA data from an associated image.
	/// </summary>
	/// <param name="name">The name of the associated image.</param>
	/// <param name="dimensions">The dimensions of the associated image.</param>
	/// <returns>The pixel data of the associated image.</returns>
	public unsafe byte[] ReadAssociatedImage(string name, out ImageDimensions dimensions)
	{
		EnsureNotDisposed();

		if (!TryGetAssociatedImageDimensions(name, out dimensions))
		{
			throw new KeyNotFoundException();
		}

		var data = new byte[dimensions.Width * dimensions.Height * 4];
		if (data.Length > 0)
		{
			fixed (void* pdata = &data[0])
			{
				ReadAssociatedImageInternal(name, pdata);
			}
		}

		return data;
	}

	/// <summary>
	/// Copy pre-multiplied BGRA data from an associated image.
	/// </summary>
	/// <param name="name">The name of the associated image.</param>
	/// <param name="buffer">The destination buffer to hold the pixel data. Should be at least (width * height * 4) bytes in length</param>
	public unsafe void ReadAssociatedImage(string name, Span<byte> buffer)
	{
		EnsureNotDisposed();

		if (!TryGetAssociatedImageDimensions(name, out var dimensions)) throw new KeyNotFoundException();
		if (buffer.Length < 4 * dimensions.Width * dimensions.Height)
			throw new ArgumentException("Destination is too small.");
		fixed (void* pdata = buffer) ReadAssociatedImageInternal(name, pdata);
	}

	/// <summary>
	/// Copy pre-multiplied BGRA data from an associated image.
	/// </summary>
	/// <param name="name">The name of the associated image.</param>
	/// <param name="buffer">The destination buffer to hold the pixel data.</param>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public unsafe void ReadAssociatedImage(string name, ref byte buffer)
	{
		EnsureNotDisposed();
		fixed (void* pdata = &buffer) ReadAssociatedImageInternal(name, pdata);
	}

	/// <summary>
	/// Copy pre-multiplied BGRA data from an associated image.
	/// </summary>
	/// <param name="name">The name of the associated image.</param>
	/// <param name="buffer">The destination buffer to hold the pixel data.</param>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public unsafe void ReadAssociatedImage(string name, IntPtr buffer)
	{
		EnsureNotDisposed();
		ReadAssociatedImageInternal(name, (void*)buffer);
	}

	private unsafe void ReadAssociatedImageInternal(string name, void* pointer)
	{
		OpenSlideInterop.ReadAssociatedImage(handle, name, pointer);
		ThrowHelper.CheckAndThrowError(handle);
	}

	/// <summary>
	/// Copy pre-multiplied BGRA data from a whole slide image.
	/// </summary>
	/// <param name="level">The desired level.</param>
	/// <param name="x">The top left x-coordinate, in the level 0 reference frame.</param>
	/// <param name="y">The top left y-coordinate, in the level 0 reference frame.</param>
	/// <param name="width">The width of the region. Must be non-negative.</param>
	/// <param name="height">The height of the region. Must be non-negative.</param>
	/// <returns>The pixel data of this region.</returns>
	public unsafe byte[] ReadRegion(int level, long x, long y, long width, long height)
	{
		EnsureNotDisposed();

		if (width  < 0) throw new ArgumentOutOfRangeException(nameof(width));
		if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

		var data = new byte[width * height * 4];
		if (data.Length > 0)
			fixed (void* pdata = &data[0])
				ReadRegion(level, x, y, width, height, pdata);

		return data;
	}

	/// <summary>
	/// Copy pre-multiplied BGRA data from a whole slide image.
	/// </summary>
	/// <param name="level">The desired level.</param>
	/// <param name="x">The top left x-coordinate, in the level 0 reference frame.</param>
	/// <param name="y">The top left y-coordinate, in the level 0 reference frame.</param>
	/// <param name="width">The width of the region. Must be non-negative.</param>
	/// <param name="height">The height of the region. Must be non-negative.</param>
	/// <param name="buffer">The destination buffer for the BGRA data.</param>
	public unsafe void ReadRegion(int level, long x, long y, long width, long height, Span<byte> buffer)
	{
		EnsureNotDisposed();
		if (buffer.Length < 4 * width * height) throw new ArgumentException("Destination is too small.");
		fixed (void* pdata = buffer) ReadRegion(level, x, y, width, height, pdata);
	}

	/// <summary>
	/// Copy pre-multiplied BGRA data from a whole slide image.
	/// </summary>
	/// <param name="level">The desired level.</param>
	/// <param name="x">The top left x-coordinate, in the level 0 reference frame.</param>
	/// <param name="y">The top left y-coordinate, in the level 0 reference frame.</param>
	/// <param name="width">The width of the region. Must be non-negative.</param>
	/// <param name="height">The height of the region. Must be non-negative.</param>
	/// <param name="buffer">The destination buffer for the BGRA data.</param>
	public unsafe void ReadRegion(int level, long x, long y, long width, long height, ref byte buffer)
	{
		EnsureNotDisposed();
		fixed (void* pdata = &buffer) ReadRegion(level, x, y, width, height, pdata);
	}

	/// <summary>
	/// Copy pre-multiplied BGRA data from a whole slide image.
	/// </summary>
	/// <param name="level">The desired level.</param>
	/// <param name="x">The top left x-coordinate, in the level 0 reference frame.</param>
	/// <param name="y">The top left y-coordinate, in the level 0 reference frame.</param>
	/// <param name="width">The width of the region. Must be non-negative.</param>
	/// <param name="height">The height of the region. Must be non-negative.</param>
	/// <param name="buffer">The destination buffer for the BGRA data.</param>
	public unsafe void ReadRegion(int level, long x, long y, long width, long height, IntPtr buffer)
	{
		EnsureNotDisposed();
		ReadRegion(level, x, y, width, height, (void*)buffer);
	}

	public Size2I GetLevelTileSize(int level)
	{
		if (!TryGetProperty($"openslide.level[{level}].tile-width", out var w) ||
		    !int.TryParse(w, out var width)) width = 0;

		if (!TryGetProperty($"openslide.level[{level}].tile-height", out var h) ||
		    !int.TryParse(h, out var height)) height = 0;

		return new Size2I(width, height);
	}

	public unsafe void ReadRegion(int level, long x, long y, long width, long height, void* pointer)
	{
		OpenSlideInterop.ReadRegion(handle, pointer, x, y, level, width, height);
		ThrowHelper.CheckAndThrowError(handle);
	}

	/// <summary>
	/// Get the best level to use for displaying the given downsample.
	/// </summary>
	/// <param name="downsample">The downsample factor.</param>
	/// <returns>The level identifier, or -1 if an error occurred.</returns>
	public int GetBestLevelForDownsample(double downsample)
	{
		EnsureNotDisposed();

		return OpenSlideInterop.GetBestLevelForDownsample(handle, downsample);
	}

	#region IDisposable Support

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnsureNotDisposed()
	{
		if (handle is null)
		{
			ThrowObjectDisposedException();
		}
	}

	private static void ThrowObjectDisposedException() => throw new ObjectDisposedException(nameof(OpenSlideImage));

	/// <summary>
	/// Dispose the <see cref="OpenSlideImage"/> object.
	/// </summary>
	public void Dispose()
	{
		var tempChange = Interlocked.Exchange(ref handle!, null);
		if (tempChange is not null && !leaveOpen)
		{
			tempChange.Dispose();
		}
	}

	#endregion
}