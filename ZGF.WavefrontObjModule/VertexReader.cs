namespace ZGF.WavefrontObjModule;

internal sealed class VertexReader
{
    private readonly char[] _buffer = new char[64];
    
    public VertexNormal ReadNormal(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        
        Span<float> values = stackalloc float[3];
        var currValueIndex = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == ' ')
            {
                var floatValue = float.Parse(_buffer.AsSpan(0, len));
                values[currValueIndex] = floatValue;
                ++currValueIndex;
                len = 0;
                continue;
            }
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;
            
            buffer[len] = (char)charAsInt;
            len++;
        }

        return new VertexNormal
        {
            X = values[0],
            Y = values[1],
            Z = values[2],
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
}