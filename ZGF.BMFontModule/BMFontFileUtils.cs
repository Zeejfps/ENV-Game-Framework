using System.Xml.Serialization;

namespace ZGF.BMFontModule;

public static class BMFontFileUtils
{
    public static BMFontFile DeserializeFromXmlFile ( String filename )
    {
        var deserializer = new XmlSerializer(typeof(BMFontFile));
        using var textReader = new StreamReader(filename);
        var file = deserializer.Deserialize(textReader) as BMFontFile;
        if (file == null)
            throw new Exception("Failed to load BMFontFile");
        file.Update();
        return file;
    }
}