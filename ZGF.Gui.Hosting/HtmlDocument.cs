namespace ZGF.Gui.Hosting;

internal static class HtmlDocument
{
    private const string CharsetDeclaration = "<meta charset=\"utf-8\">";

    /// <summary><paramref name="fragment"/> declared as UTF-8, so an app pasting it doesn't guess
    /// the encoding.</summary>
    public static string WithCharset(string fragment) => CharsetDeclaration + fragment;
}
