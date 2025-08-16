using System.Xml.Serialization;

namespace ZGF.BMFontModule;

[Serializable]
public class FontPage
{
    public int Id
    {
        get;
        set;
    }

    [XmlAttribute ( "file" )]
    public String File
    {
        get;
        set;
    }
}