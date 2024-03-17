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
        TiffPageDeleter.NdpiDeleteMacro(path, Path.ChangeExtension(path, ".g.ndpi"));
    }
}