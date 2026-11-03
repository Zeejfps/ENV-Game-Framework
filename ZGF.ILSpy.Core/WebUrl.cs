namespace ZGF.ILSpy.Core;

/// <summary>An absolute HTTP or HTTPS URL, as it was given, trimmed.</summary>
public sealed record WebUrl
{
    private WebUrl(Uri uri, string text)
    {
        Uri = uri;
        Text = text;
    }

    public Uri Uri { get; }

    public string Text { get; }

    /// <summary>The URL <paramref name="text"/> holds; null unless it is an absolute HTTP or HTTPS URL.</summary>
    public static WebUrl? Parse(string? text)
    {
        if (text == null)
            return null;
        var trimmed = text.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return null;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return null;
        return new WebUrl(uri, trimmed);
    }

    public override string ToString() => Text;
}
