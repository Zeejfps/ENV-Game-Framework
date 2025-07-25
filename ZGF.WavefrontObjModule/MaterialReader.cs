namespace ZGF.WavefrontObjModule;

internal sealed class MaterialReader
{
    private readonly char[] _buffer = new char[64];
    
    public ReadOnlySpan<char> Read(StreamReader textReader)
    {
        var buffer = _buffer;
        //[mtllib ]
        int charAsInt;
        var len = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n')
            {
                return buffer[..len];
            }
            
            buffer[len] = (char)charAsInt;
            len++;
        }
        return buffer.AsSpan(0, len);
    }
}