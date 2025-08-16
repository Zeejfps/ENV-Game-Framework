using System.Xml.Serialization;

namespace ZGF.BMFontModule;

public class FontInfo
{
    public string Face
    {
        get;
        set;
    }

    public Int32 Size
    {
        get;
        set;
    }

    public bool Bold
    {
        get;
        set;
    }

    public bool Italic
    {
        get;
        set;
    }

    public string CharSet
    {
        get;
        set;
    }

    public bool Unicode
    {
        get;
        set;
    }

    public Int32 StretchHeight
    {
        get;
        set;
    }

    public bool Smooth
    {
        get;
        set;
    }

    public Int32 SuperSampling
    {
        get;
        set;
    }

    public Padding Padding
    {
        get;
        set;
    }

    public Spacing Spacing
    {
        get;
        set;
    }

    [XmlAttribute ( "outline" )]
    public Int32 OutLine
    {
        get;
        set;
    }
}