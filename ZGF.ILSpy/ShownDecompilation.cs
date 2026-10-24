using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The language and options the tabs in front were last brought up to date with.</summary>
public sealed class ShownDecompilation
{
    private DecompilationLanguage? _language;
    private DecompilerOptions? _options;

    /// <summary>Whether <paramref name="language"/> or <paramref name="options"/> differ from what the tabs in
    /// front were last brought up to date with; records them when they do.</summary>
    public bool TakeChange(DecompilationLanguage language, DecompilerOptions options)
    {
        if (!HasChanged(language, options))
            return false;
        _language = language;
        _options = options;
        return true;
    }

    private bool HasChanged(DecompilationLanguage language, DecompilerOptions options)
    {
        if (language != _language)
            return true;
        return !ReferenceEquals(options, _options);
    }
}
