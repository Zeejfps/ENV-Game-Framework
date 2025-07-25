namespace ZGF.WavefrontObjModule;

internal sealed class VertexReader
{
    private readonly char[] _buffer = new char[64];
    
    public VertexPosition ReadPosition(StreamReader textReader)
    {
        var x = ReadFloat(textReader);
        var y = ReadFloat(textReader);
        var z = ReadFloat(textReader);

        if (!TryReadFloat(textReader, out var w))
            w = 1f;
        
        return new VertexPosition
        {
            X = x,
            Y = y,
            Z = z,
            W = w
        };
    }
    
    public VertexNormal ReadNormal(StreamReader textReader)
    {
        var x = ReadFloat(textReader);
        var y = ReadFloat(textReader);
        var z = ReadFloat(textReader);
        
        return new VertexNormal
        {
            X = x,
            Y = y,
            Z = z,
        };
    }
    
    public VertexTextureCoord ReadTextureCoords(StreamReader textReader)
    {
        var u = ReadFloat(textReader);
        var v = ReadFloat(textReader);
        
        return new VertexTextureCoord
        {
            U = u,
            V = v,
        };
    }

    private float ReadFloat(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == ' ') break;
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;
            
            buffer[len] = (char)charAsInt;
            len++;
        }
        
        return float.Parse(buffer.AsSpan(0, len));
    }

    private bool TryReadFloat(StreamReader textReader, out float value)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == ' ') break;
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;
            
            buffer[len] = (char)charAsInt;
            len++;
        }

        if (len == 0)
        {
            value = 0;
            return false;
        }
        
        return float.TryParse(buffer.AsSpan(0, len), out value);
    }
}