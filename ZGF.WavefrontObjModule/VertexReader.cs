namespace ZGF.WavefrontObjModule;

internal sealed class VertexReader
{
    private readonly char[] _buffer = new char[64];
    
    public VertexPosition ReadPosition(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        
        Span<float> values = stackalloc float[4];
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

        if (currValueIndex < 3)
            values[3] = 1.0f;

        return new VertexPosition
        {
            X = values[0],
            Y = values[1],
            Z = values[2],
            W = values[3]
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