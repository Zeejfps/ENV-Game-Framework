namespace ZGF.ILSpy;

/// <summary>The id of a theme read as <typeparamref name="TFile"/>: the name of its file without the
/// extension, compared ignoring case. A color theme's id is no icon theme's.</summary>
public sealed record ThemeId<TFile> where TFile : IThemeFile<TFile>
{
    private static readonly char[] NotInFileName = [..Path.GetInvalidFileNameChars(), '/', '\\'];

    private ThemeId(string text)
    {
        Text = text;
    }

    public string Text { get; }

    /// <summary>The id <paramref name="text"/> is, or null when it names no file in a themes folder: blank,
    /// <c>.</c>, <c>..</c>, or holding a character a file name cannot.</summary>
    public static ThemeId<TFile>? Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        if (text is "." or "..")
            return null;
        if (text.IndexOfAny(NotInFileName) >= 0)
            return null;
        return new ThemeId<TFile>(text);
    }

    /// <summary>The id <paramref name="text"/>, which the app names itself and must be one.</summary>
    public static ThemeId<TFile> Of(string text)
    {
        var id = Parse(text);
        if (id == null)
            throw new ArgumentException($"'{text}' is no theme id.", nameof(text));
        return id;
    }

    public bool Equals(ThemeId<TFile>? other)
    {
        if (other is null)
            return false;
        return string.Equals(Text, other.Text, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Text);

    public override string ToString() => Text;
}
