using System.Xml.Serialization;
using ZGF.BMFontModule;

namespace BmFont;

public static class FontLoader
{
    public static FontFile Load ( String filename )
    {
        XmlSerializer deserializer = new XmlSerializer ( typeof ( FontFile ) );
        TextReader textReader = new StreamReader ( filename );
        FontFile file = ( FontFile ) deserializer.Deserialize ( textReader );
        textReader.Close ( );
        file.Update();
        return file;
    }
}