namespace ZGF.WavefrontObjModule;

internal sealed class ObjectNameReader
{
    private char[] _buffer = new char[64];
    
    public ReadOnlySpan<char> Read(StreamReader textReader)
    {
        var buffer = _buffer;
        
        var o = textReader.Read();
        if (o != 'o')
            throw new Exception($"Expected 'o', found '{o}'");
        
        var space = textReader.Read();
        if (space != ' ')
            throw new Exception($"Expected ' ', found '{space}'");
        
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