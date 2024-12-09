using System.Xml.Serialization;

namespace OpenSlideNET;

[XmlRoot(ElementName = "Image", Namespace = "http://schemas.microsoft.com/deepzoom/2008")]
public class DziImage
{
    [XmlAttribute(AttributeName = "Format")]
    public required string Format { get; set; }

    [XmlAttribute(AttributeName = "Overlap")]
    public int Overlap { get; set; }

    [XmlAttribute(AttributeName = "TileSize")]
    public required int TileSize { get; set; }

    [XmlElement(ElementName = "Size")]
    public required DziSize Size { get; set; }

    /// <summary>
    /// 全文件的SHA256值
    /// </summary>
    [XmlElement(ElementName = "EasyPathology.QuickHash2")]
    public string? QuickHash2 { get; set; }

    /// <summary>
    /// X, Y方向每个像素的实际长度（单位：微米）
    /// </summary>
    [XmlElement(ElementName = "EasyPathology.MicronsPerPixel")]
    public DziSize? MicronsPerPixel { get; set; }

    /// <summary>
    /// 背景颜色，格式为#RRGGBB
    /// </summary>
    [XmlElement(ElementName = "EasyPathology.BackgroundColor")]
    public string? BackgroundColor { get; set; }
}

[XmlRoot(ElementName = "Size")]
public class DziSize
{
    [XmlAttribute(AttributeName = "Width")]
    public required int Width { get; set; }
    
    [XmlAttribute(AttributeName = "Height")]
    public required int Height { get; set; }
}