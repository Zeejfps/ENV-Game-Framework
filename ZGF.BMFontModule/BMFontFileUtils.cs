using System.Xml;
using System.Xml.Serialization;

namespace ZGF.BMFontModule;

public static class BMFontFileUtils
{
    private static FontInfo ParseFontInfo(XmlElement element)
    {
        var info = new FontInfo();

        info.Face = element.GetAttribute("face");
        info.Size = int.TryParse(element.GetAttribute("size"), out var size) ? size : 0;
        info.Bold = element.GetAttribute("bold") == "1";
        info.Italic = element.GetAttribute("italic") == "1";
        info.CharSet = element.GetAttribute("charset");
        info.Unicode = element.GetAttribute("unicode") == "1";
        info.StretchHeight = int.TryParse(element.GetAttribute("stretchH"), out var stretchH) ? stretchH : 100;
        info.Smooth = element.GetAttribute("smooth") == "1";
        info.SuperSampling = int.TryParse(element.GetAttribute("aa"), out var aa) ? aa : 1;

        // Parse padding "1,1,1,1"
        var paddingAttr = element.GetAttribute("padding");
        if (!string.IsNullOrEmpty(paddingAttr))
        {
            var parts = paddingAttr.Split(',');
            if (parts.Length == 4)
            {
                info.Padding = new Padding{
                    Top = int.Parse(parts[0]),
                    Right = int.Parse(parts[1]),
                    Bottom = int.Parse(parts[2]),
                    Left = int.Parse(parts[3])
                };
            }
        }

        // Parse spacing "1,1"
        var spacingAttr = element.GetAttribute("spacing");
        if (!string.IsNullOrEmpty(spacingAttr))
        {
            var parts = spacingAttr.Split(',');
            if (parts.Length == 2)
            {
                info.Spacing = new Spacing
                {
                    Horizontal = int.Parse(parts[0]),
                    Vertical = int.Parse(parts[1])
                };
            }
        }

        return info;
    }

    public static BMFontFile DeserializeFromXmlFile(string filename)
    {
        using var textReader = new StreamReader(filename);
        var doc = new XmlDocument();
        doc.Load(textReader);

        var infoElement = doc.GetElementsByTagName("info").OfType<XmlElement>().First();
        var fontInfo = ParseFontInfo(infoElement);

        var bmFontFile = new BMFontFile
        {
            Info = fontInfo,
        };
        bmFontFile.Update();
        return bmFontFile;
    }
}