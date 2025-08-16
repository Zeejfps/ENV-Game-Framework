using System.Xml;

namespace ZGF.BMFontModule;

public static class BMFontFileUtils
{
    private static FontInfo ParseFontInfo(XmlElement? element)
    {
        var info = new FontInfo();
        if (element == null)
            return info;

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

    private static FontCommon ParseFontCommon(XmlElement? element)
    {
        var common = new FontCommon();
        if (element == null)
            return common;

        common.LineHeight = int.TryParse(element.GetAttribute("lineHeight"), out var lineHeight) ? lineHeight : 0;
        common.Base = int.TryParse(element.GetAttribute("base"), out var baseVal) ? baseVal : 0;
        common.ScaleW = int.TryParse(element.GetAttribute("scaleW"), out var scaleW) ? scaleW : 0;
        common.ScaleH = int.TryParse(element.GetAttribute("scaleH"), out var scaleH) ? scaleH : 0;
        common.Pages = int.TryParse(element.GetAttribute("pages"), out var pages) ? pages : 0;
        common.Packed = element.GetAttribute("packed") == "1";

        return common;
    }

    private static List<FontPage> ParseFontPages(XmlElement? element)
    {
        var pages = new List<FontPage>();
        if (element == null)
            return pages;

        foreach (XmlElement pageElement in element.GetElementsByTagName("page"))
        {
            var page = new FontPage
            {
                Id = int.TryParse(pageElement.GetAttribute("id"), out var id) ? id : 0,
                File = pageElement.GetAttribute("file")
            };

            pages.Add(page);
        }

        return pages;
    }

    public static BMFontFile DeserializeFromXmlFile(string filename)
    {
        using var textReader = new StreamReader(filename);
        var doc = new XmlDocument();
        doc.Load(textReader);

        var infoElement = doc.GetElementsByTagName("info")
            .OfType<XmlElement>()
            .FirstOrDefault();
        var fontInfo = ParseFontInfo(infoElement);

        var commonElement = doc.GetElementsByTagName("common")
            .OfType<XmlElement>()
            .FirstOrDefault();
        var fontCommon = ParseFontCommon(commonElement);

        var pagesElement = doc.GetElementsByTagName("pages")
            .OfType<XmlElement>()
            .FirstOrDefault();
        var pages = ParseFontPages(pagesElement);

        var bmFontFile = new BMFontFile
        {
            Info = fontInfo,
            Common = fontCommon,
            Pages = pages,
        };
        bmFontFile.Update();
        return bmFontFile;
    }
}