using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyPathology.Abstractions.DataTypes;
using EasyPathology.Abstractions.Extensions;
using OpenSlideNET.Interop;

namespace OpenSlideNET;

/// <summary>
/// Represents the image dimensions
/// </summary>
public readonly struct ImageDimensions
{
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
    public ImageDimensions(long width, long height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Deconstruction the struct.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    public void Deconstruct(out long width, out long height)
    {
        width = Width;
        height = Height;
    }

    /// <summary>
    /// Converts the <see cref="ImageDimensions"/> struct into a tuple of (Width, Height).
    /// </summary>
    /// <param name="dimensions">the <see cref="ImageDimensions"/> struct.</param>
    public static implicit operator (long Width, long Height)(ImageDimensions dimensions)
    {
        return (dimensions.Width, dimensions.Height);
    }

    /// <summary>
    /// Converts a tuple of (Width, Height) into the <see cref="ImageDimensions"/> struct.
    /// </summary>
    /// <param name="dimensions">A tuple of (Width, Height).</param>
    public static explicit operator ImageDimensions(ValueTuple<long, long> dimensions)
    {
        return new ImageDimensions(dimensions.Item1, dimensions.Item2);
    }
}

public interface ISlideImage : IDisposable
{
    int LevelCount { get; }
    ImageDimensions Dimensions { get; }
    Color4B? BackgroundColor { get; }

    string QuickHash1 { get; }
    string QuickHash2 { get; }
    Size2D? MicronsPerPixel { get; }
    string FullPath { get; }

    ImageDimensions GetLevelDimensions(int level);
    Size2I GetLevelTileSize(int level);
    Size2I GetLevelOverlap(int level);

    double GetLevelDownsample(int level);
    IReadOnlyList<string> GetAllPropertyNames();
    bool TryGetProperty(string name, [NotNullWhen(true)] out string? value);
    void ReadRegion(int level, long x, long y, long width, long height, IntPtr buffer);
}

public static class SlideImage
{
    public static IEnumerable<string> SupportedExtensions =>
        OpenSlideImage.SupportedExtensions
            .Concat(OpenCvSlideImage.SupportedExtensions)
            .Concat(DziSlideImage.SupportedExtensions);

    /// <summary>
    /// Open a whole slide image.
    /// This function can be expensive; avoid calling it unnecessarily. For example, a tile server should not call Open() on every tile request. Instead, it should maintain a cache of <see cref="OpenSlideImage"/> objects and reuse them when possible.
    /// </summary>
    /// <param name="slidePath">The filename to open.</param>
    /// <returns>The <see cref="OpenSlideImage"/> object.</returns>
    /// <exception cref="OpenSlideUnsupportedFormatException">The file format can not be recognized.</exception>
    /// <exception cref="OpenSlideException">The file format is recognized, but an error occurred when opening the file.</exception>
    public static ISlideImage Open(string slidePath)
    {
        ArgumentNullException.ThrowIfNull(slidePath);

        var extension = Path.GetExtension(slidePath);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            if (OpenSlideImage.SupportedExtensions.Contains(extension))
            {
                return OpenOpenSlideImage(slidePath);
            }

            if (DziSlideImage.SupportedExtensions.Contains(extension))
            {
                return OpenDziSlideImage(slidePath);
            }

            if (OpenCvSlideImage.SupportedExtensions.Contains(extension))
            {
                return OpenOpenCvSlideImage(slidePath);
            }
        }

        if (OpenSlideImage.DetectFormat(slidePath) != null)
        {
            return OpenOpenSlideImage(slidePath);
        }

        do
        {
            using var fs = File.OpenRead(slidePath);
            if (fs.Length < 16) break;
            var buffer = new byte[16];
            if (fs.Read(buffer, 0, 16) != 16) break;

            if (IsXml(Encoding.ASCII.GetString(buffer)) ||
                IsXml(Encoding.UTF8.GetString(buffer)) ||
                IsXml(Encoding.Unicode.GetString(buffer)))
            {
                return OpenDziSlideImage(slidePath);
            }
        }
        while (false);

        try
        {
            return OpenOpenCvSlideImage(slidePath);
        }
        catch
        {
            throw new OpenSlideUnsupportedFormatException();
        }

        static OpenSlideImage OpenOpenSlideImage(string path)
        {
            // Open file using OpenSlide
            var handle = OpenSlideInterop.Open(path);
            if (handle.IsInvalid)
            {
                throw new OpenSlideUnsupportedFormatException();
            }

            if (!ThrowHelper.TryCheckError(handle, out var errMsg))
            {
                handle.Dispose();
                throw new OpenSlideException(errMsg);
            }

            return new OpenSlideImage(path, handle);
        }

        static DziSlideImage OpenDziSlideImage(string path)
        {
            return new DziSlideImage(path,
                Path.Combine(Path.GetDirectoryName(path).NotNull(), "output_files"),
                SlideHash.GetHash2(path));
        }

        static OpenCvSlideImage OpenOpenCvSlideImage(string path)
        {
            return new OpenCvSlideImage(path);
        }

        static bool IsXml(string text)
        {
            return text[0] == '<' && text.All(c => c >= 32 && c <= 126);
        }
    }

    public static async ValueTask<(string quickHash, string quickHash2)> ReadQuickHashAsync(string slidePath)
    {
        var extension = Path.GetExtension(slidePath).ToLower();

        if (OpenSlideImage.SupportedExtensions.Contains(extension))
        {
            using var handle = OpenSlideInterop.Open(slidePath);
            if (handle.IsInvalid)
            {
                throw new OpenSlideUnsupportedFormatException("Invalid file format");
            }

            if (!ThrowHelper.TryCheckError(handle, out var errMsg))
            {
                throw new OpenSlideException(errMsg);
            }

            return (
                OpenSlideInterop.GetPropertyValue(handle, OpenSlideInterop.OpenSlidePropertyNameQuickHash1) ??
                await SlideHash.GetHashAsync(slidePath),
                await SlideHash.GetHash2Async(slidePath)
            );
        }

        if (OpenCvSlideImage.SupportedExtensions.Contains(extension))
        {
            return (
                await SlideHash.GetHashAsync(slidePath),
                await SlideHash.GetHash2Async(slidePath)
            );
        }

        if (DziSlideImage.SupportedExtensions.Contains(extension))
        {
            return (
                await SlideHash.GetHashAsync(slidePath),
                await SlideHash.GetHash2Async(slidePath)
            );
        }

        throw new OpenSlideUnsupportedFormatException();
    }
}