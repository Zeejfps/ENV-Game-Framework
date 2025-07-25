namespace ZGF.WavefrontObjModule;

internal sealed class ObjectNameReader
{
    private char[] _buffer = new char[64];
    
    public ReadOnlySpan<char> Read(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n') break;
            
            buffer[len] = (char)charAsInt;
            len++;
        }

        return buffer.AsSpan(0, len);
    }
}