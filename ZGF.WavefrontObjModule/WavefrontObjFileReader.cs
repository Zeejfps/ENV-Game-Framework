namespace ZGF.WavefrontObjModule;

internal sealed class WavefrontObjFileReader
{
    
    
    public IWavefrontObjFileContents ReadFromFile(string pathToFile)
    {
        var buffer = new char[256];
        using var fileStream = File.OpenRead(pathToFile);
        using var textReader = new StreamReader(fileStream);

        int chasAsInt;
        while ((chasAsInt = textReader.Read()) > 0)
        {
            switch (chasAsInt)
            {
                case '#':
                    // Comment, skip read to end of line
                    ReadToEndOfLine(textReader);
                    break;
                case 'm':
                    var materialFile = ReadMaterial(textReader, buffer);
                    Console.WriteLine($"mat: {materialFile}");
                    break;
            
            }
        }

        return null;
    }
    
    private void ReadToEndOfLine(StreamReader textReader)
    {
        int chasAsInt;
        while ((chasAsInt = textReader.Read()) > 0)
        {
            if (chasAsInt == '\n')
            {
                return;
            }
        }
    }

    private Span<char> ReadMaterial(StreamReader textReader, Span<char> buffer)
    {
        //m - [tllib ]
        var charsRead = textReader.Read(buffer[..6]);
        if (charsRead != 6)
            throw new Exception("Malformed Obj file");
        
        int chasAsInt;
        var len = 0;
        while ((chasAsInt = textReader.Read()) > 0)
        {
            if (chasAsInt == '\n')
            {
                return buffer[..len];
            }
            
            buffer[len] = (char)chasAsInt;
            len++;
        }
        return buffer[..len];
    }
}