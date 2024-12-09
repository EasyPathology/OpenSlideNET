using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using EasyPathology.Abstractions.DataTypes;
using EasyPathology.Abstractions.Extensions;
using OpenCvSharp;

namespace OpenSlideNET;

public class DziSlideImage : ISlideImage
{
    public static string[] SupportedExtensions => [".dzi"];

    protected readonly string tileBasePath;
    protected readonly DziImage image;

    internal protected DziSlideImage(
        string dziPath,
        string tileBasePath,
        string quickHash,
        Size2D? micronsPerPixel = null,
        Color4B? backgroundColor = null)
    {
        try
        {
            using var fs = File.OpenRead(dziPath);
            using var xml = XmlReader.Create(fs);
            var serializer = new XmlSerializer(typeof(DziImage));
            image = serializer.Deserialize(xml).NotNull<DziImage>();
        } 
        catch (Exception e)
        {
            throw new FormatException(e.Message, e);
        }

        this.tileBasePath = tileBasePath;
        QuickHash2 = quickHash;
        Dimensions = new ImageDimensions(image.Size.Width, image.Size.Height);
        var levelDimensionsList = new List<ImageDimensions>();
        var levelWidth = image.Size.Width;
        var levelHeight = image.Size.Height;
        while (true)
        {
            levelDimensionsList.Add(new ImageDimensions(levelWidth, levelHeight));
            Debug.WriteLine($"{levelDimensionsList.Count - 1}: {levelWidth}x{levelHeight}");
            levelWidth /= 2;
            levelHeight /= 2;
            if (levelWidth == 0 && levelHeight == 0)
            {
                break;
            }

            if (levelWidth == 0) levelWidth = 1;
            if (levelHeight == 0) levelHeight = 1;
        }
        levelDimensions = levelDimensionsList.ToArray();

        MicronsPerPixel = micronsPerPixel;
        BackgroundColor = backgroundColor;
    }

    public int LevelCount => levelDimensions.Length;

    public ImageDimensions Dimensions { get; }

    public Color4B? BackgroundColor { get; }

    public string QuickHash1 => QuickHash2;

    public string QuickHash2 { get; }

    public Size2D? MicronsPerPixel { get; }
    public string FullPath => tileBasePath;

    private readonly ImageDimensions[] levelDimensions;

    public ImageDimensions GetLevelDimensions(int level)
    {
        return levelDimensions[level];
    }

    public Size2I GetLevelOverlap(int level)
    {
        return new Size2I(image.Overlap, image.Overlap);
    }

    public double GetLevelDownsample(int level)
    {
        return Math.Pow(2, level);
    }

    public IReadOnlyList<string> GetAllPropertyNames() => Array.Empty<string>();

    public bool TryGetProperty(string name, out string value)
    {
        throw new NotSupportedException();
    }

    public void ReadRegion(int level, long x, long y, long width, long height, IntPtr buffer)
    {
        var ds = GetLevelDownsample(level);

        x = (int)(x / ds);
        if (x != 0) x += image.Overlap; // 除了最左边的tile，其他tile都有overlap
        var xEnd = x + width;
        y = (int)(y / ds);
        if (y != 0) y += image.Overlap;
        var yEnd = y + height;

        using var mat = Mat.FromPixelData((int)height, (int)width, MatType.CV_8UC4, buffer);
        for (var yy = y; yy < yEnd - 2 * image.Overlap; yy += image.TileSize)
        {
            var regionHeight = Math.Min(image.TileSize + (yy == 0 ? image.Overlap : 2 * image.Overlap), yEnd - yy);
            for (var xx = x; xx < xEnd - 2 * image.Overlap; xx += image.TileSize)
            {
                var regionWidth = Math.Min(image.TileSize + (xx == 0 ? image.Overlap : 2 * image.Overlap), xEnd - xx);
                using var roi = mat[new Rect((int)(xx - x), (int)(yy - y), (int)regionWidth, (int)regionHeight)];
                ReadRegion($"{LevelCount - level}/{xx / image.TileSize}_{yy / image.TileSize}.{image.Format}", roi);
            }
        }
    }

    protected virtual void ReadRegion(string relativeTilePath, Mat roi)
    {
        var tilePath = Path.Combine(tileBasePath, relativeTilePath);
        if (!File.Exists(tilePath))
        {
            roi.SetTo(new Scalar(0, 0, 0, 0));
            return;
        }

        using var tileImage = new Mat(tilePath, ImreadModes.Unchanged);
        if (tileImage.Empty())
        {
            roi.SetTo(new Scalar(0, 0, 0, 0));
            return;
        }

        if (tileImage.Channels() == 3)
        {
            Cv2.CvtColor(tileImage, tileImage, ColorConversionCodes.BGR2BGRA);
        }

        if (tileImage.Size() != roi.Size())
        {
            var width = Math.Min(tileImage.Width, roi.Width);
            var height = Math.Min(tileImage.Height, roi.Height);
            var rect = new Rect(0, 0, width, height);
            using var croppedTileImage = new Mat(tileImage, rect);
            croppedTileImage.CopyTo(roi);
        } 
        else
        {
            tileImage.CopyTo(roi);
        }
    }

    public Size2I GetLevelTileSize(int level)
    {
        // dzi每层的tile大小都是一样的
        return new Size2I(image.TileSize, image.TileSize);
    }

    public virtual void Dispose() { }
}
