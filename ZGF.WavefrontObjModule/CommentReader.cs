namespace ZGF.WavefrontObjModule;

internal sealed class CommentReader
{
    public void Read(StreamReader textReader)
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
}