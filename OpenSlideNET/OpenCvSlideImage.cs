using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EasyPathology.Abstractions.DataTypes;
using OpenCvSharp;

namespace OpenSlideNET;

public class OpenCvSlideImage : ISlideImage
{
    public static string[] SupportedExtensions => [".jpg", ".jpeg", ".png", ".bmp", ".webp"];

    public int LevelCount => 1;

    public ImageDimensions Dimensions => new(mat.Width, mat.Height);

    public Color4B? BackgroundColor => null;

    public string QuickHash1 { get; }

    public string QuickHash2 { get; }

    public Size2D? MicronsPerPixel => null;

    private readonly Mat mat;

    public OpenCvSlideImage(string filePath)
    {
        QuickHash1 = SlideHash.GetHash(filePath);
        QuickHash2 = SlideHash.GetHash2(filePath);
        mat = Cv2.ImRead(filePath);
        Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2BGRA);
    }

    public ImageDimensions GetLevelDimensions(int level) => Dimensions;

    public Size2I GetLevelTileSize(int level) => new(mat.Width, mat.Height);

    public Size2I GetLevelOverlap(int level) => new(1, 1);

    public double GetLevelDownsample(int level) => 1d;

    public IReadOnlyList<string> GetAllPropertyNames() => Array.Empty<string>();

    public bool TryGetProperty(string name, [NotNullWhen(true)] out string? value)
    {
        value = null;
        return false;
    }

    public void ReadRegion(int level, long x, long y, long width, long height, IntPtr buffer)
    {
        var roi = new Rect((int)x, (int)y, (int)width, (int)height);
        using var roiMat = new Mat(mat, roi);
        var step = roiMat.Step();
        for (var row = 0; row < roiMat.Rows; row++)
        {
            unsafe
            {
                Buffer.MemoryCopy(roiMat.DataPointer + row * step, (byte*)buffer + row * width * 4, width * 4, width * 4);
            }
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        mat.Dispose();
    }
}