using System.Diagnostics;

namespace OpenSlideNET.Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var path  = @"F:\Shared\Files\66735_0-SK-0.ndpi";
        var image = (SlideImage.Open(path) as OpenSlideImage)!;
        foreach (var imageName in image.GetAllAssociatedImageNames())
        {
            var associatedImage = image.ReadAssociatedImage(imageName, out var dimensions);
            Debugger.Break();
        }
    }
}