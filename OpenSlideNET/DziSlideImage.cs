using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization;
using EasyPathology.Abstractions.DataTypes;
using OpenCvSharp;
using OpenSlideNET.Extensions;

namespace OpenSlideNET;

public class DziSlideImage : ISlideImage
{
    public static string[] SupportedExtensions => [".dzi"];

    protected readonly string tileBasePath;
    protected readonly Image image;

    internal protected DziSlideImage(
        string dziPath,
        string quickHash,
        Size2D? micronsPerPixel = null,
        Color4B? backgroundColor = null)
    {
        try
        {
            using var fs = File.OpenRead(dziPath);
            using var xml = XmlReader.Create(fs);
            var serializer = new XmlSerializer(typeof(Image));
            image = ((Image?)serializer.Deserialize(xml)).NotNull();
        }
        catch (Exception e)
        {
            throw new FormatException(e.Message, e);
        }

        QuickHash2 = quickHash;
        tileBasePath = Path.GetDirectoryName(dziPath).NotNull();
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
        x /= image.TileSize;

        y = (int)(y / ds);
        if (y != 0) y += image.Overlap;
        y /= image.TileSize;

        if (width > image.TileSize + (x == 0 ? image.Overlap : 2 * image.Overlap) ||
            height > image.TileSize + (y == 0 ? image.Overlap : 2 * image.Overlap))
        {
            throw new NotSupportedException();
        }

        ReadRegion(
            $"output_files/{LevelCount - level}/{x}_{y}.{image.Format}",
            width,
            height,
            buffer);
    }

    protected virtual void ReadRegion(string relativeTilePath, long width, long height, IntPtr buffer)
    {
        var tilePath = Path.Combine(tileBasePath, relativeTilePath);
        if (!File.Exists(tilePath))
        {
            unsafe
            {
                Unsafe.InitBlock(buffer.ToPointer(), 0, (uint)(width * height * 4));
            }
        }

        using var mat = Cv2.ImRead(tilePath, ImreadModes.Unchanged);
        if (mat.Type() == MatType.CV_8UC3)
        {
            Cv2.CvtColor(mat, mat, ColorConversionCodes.RGB2RGBA);
        }
        unsafe
        {
            Buffer.MemoryCopy(mat.DataPointer,
                buffer.ToPointer(),
                width * height * 4,
                mat.Total() * mat.ElemSize());
        }
    }

    public Size2I GetLevelTileSize(int level)
    {
        // dzi每层的tile大小都是一样的
        return new Size2I(image.TileSize, image.TileSize);
    }

    [XmlRoot(ElementName = "Image", Namespace = "http://schemas.microsoft.com/deepzoom/2008")]
    public class Image
    {
        [XmlAttribute(AttributeName = "Format")]
        public required string Format { get; set; }

        [XmlAttribute(AttributeName = "Overlap")]
        public int Overlap { get; set; }

        [XmlAttribute(AttributeName = "TileSize")]
        public required int TileSize { get; set; }

        [XmlElement(ElementName = "Size")]
        public required Size Size { get; set; }
    }

    [XmlRoot(ElementName = "Size")]
    public class Size
    {
        [XmlAttribute(AttributeName = "Height")]
        public required int Height { get; set; }

        [XmlAttribute(AttributeName = "Width")]
        public required int Width { get; set; }
    }

    public virtual void Dispose() { }
}