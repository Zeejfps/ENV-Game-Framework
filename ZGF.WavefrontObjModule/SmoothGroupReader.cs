namespace ZGF.WavefrontObjModule;

internal sealed class SmoothGroupReader
{
    private readonly byte[] _buffer = new byte[64];
    
    public void Read(StreamReader textReader)
    {
        var buffer = _buffer;
        int charAsInt;
        var len = 0;
        while ((charAsInt = textReader.Read()) > 0)
        {
            if (charAsInt == '\r') continue;
            if (charAsInt == '\n')
            {
                break;
            }
        }
    }
}