namespace ZGF.Gui.Tests;

public static class StringExtensions
{
    public static IEnumerable<int> AsCodePoints(this string s)
    {
        for(var i = 0; i < s.Length; ++i)
        {
            yield return char.ConvertToUtf32(s, i);
            if(char.IsHighSurrogate(s, i))
                i++;
        }
    }
}