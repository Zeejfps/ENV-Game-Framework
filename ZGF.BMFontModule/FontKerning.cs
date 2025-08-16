using System.Xml.Serialization;

namespace ZGF.BMFontModule;

[Serializable]
public class FontKerning
{
    [XmlAttribute ( "first" )]
    public Int32 First
    {
        get;
        set;
    }

    [XmlAttribute ( "second" )]
    public Int32 Second
    {
        get;
        set;
    }

    [XmlAttribute ( "amount" )]
    public Int32 Amount
    {
        get;
        set;
    }
}