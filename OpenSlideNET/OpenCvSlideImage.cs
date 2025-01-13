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
    public string FullPath { get; }

    private readonly Mat mat;

    public OpenCvSlideImage(string filePath)
    {
        QuickHash1 = SlideHash.GetHash(filePath);
        QuickHash2 = SlideHash.GetHash2(filePath);
        FullPath = filePath;
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
        if (width <= 0 || height <= 0) return;

        using var targetMat = Mat.FromPixelData((int)height, (int)width, MatType.CV_8UC4, buffer);
        targetMat.SetTo(Scalar.Black);

        var (actualX, actualY) = (Math.Clamp((int)x, 0, mat.Width), Math.Clamp((int)y, 0, mat.Height));
        var (deltaX, deltaY) = (actualX - (int)x, actualY - (int)y);
        var actualWidth = Math.Clamp((int)(width - deltaX), 0, mat.Width - actualX);
        var actualHeight = Math.Clamp((int)(height - deltaY), 0, mat.Height - actualY);
        if (actualWidth <= 0 || actualHeight <= 0) return;

        using var sourceMat = new Mat(mat, new Rect(actualX, actualY, actualWidth, actualHeight));
        using var targetRoi = new Mat(targetMat, new Rect(deltaX, deltaY, actualWidth, actualHeight));
        sourceMat.CopyTo(targetRoi);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        mat.Dispose();
    }
}