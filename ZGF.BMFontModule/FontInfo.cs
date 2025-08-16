using System.Numerics;
using System.Xml.Serialization;

namespace ZGF.BMFontModule;

[Serializable]
public class FontInfo
{
    [XmlAttribute ( "face" )]
    public String Face
    {
        get;
        set;
    }

    [XmlAttribute ( "size" )]
    public Int32 Size
    {
        get;
        set;
    }

    [XmlAttribute ( "bold" )]
    public Int32 Bold
    {
        get;
        set;
    }

    [XmlAttribute ( "italic" )]
    public Int32 Italic
    {
        get;
        set;
    }

    [XmlAttribute ( "charset" )]
    public String CharSet
    {
        get;
        set;
    }

    [XmlAttribute ( "unicode" )]
    public Int32 Unicode
    {
        get;
        set;
    }

    [XmlAttribute ( "stretchH" )]
    public Int32 StretchHeight
    {
        get;
        set;
    }

    [XmlAttribute ( "smooth" )]
    public Int32 Smooth
    {
        get;
        set;
    }

    [XmlAttribute ( "aa" )]
    public Int32 SuperSampling
    {
        get;
        set;
    }

    [XmlAttribute ( "padding" )]
    public String Padding
    {
        get;
        set;
    }

    private Vector2 _Spacing;
    [XmlAttribute ( "spacing" )]
    public String Spacing
    {
        get
        {
            return _Spacing.X + "," + _Spacing.Y;
        }
        set
        {
            String[] spacing = value.Split ( ',' );
            _Spacing = new Vector2 ( Convert.ToInt32 ( spacing[0] ), Convert.ToInt32 ( spacing[1] ) );
        }
    }

    [XmlAttribute ( "outline" )]
    public Int32 OutLine
    {
        get;
        set;
    }
}