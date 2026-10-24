using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>Choosing the interface language and how code is decompiled. The screen decompiles again
/// what it shows when the language or the options change.</summary>
public static class Preferences
{
    public static void SetLocale(this AppState w, Locale locale)
    {
        if (locale == w.Locale)
            return;
        w.Locale = locale;
    }

    public static void SetLanguage(this AppState w, DecompilationLanguage language)
    {
        if (language == w.Language)
            return;
        w.Language = language;
    }

    public static void SetLanguageVersion(this AppState w, CSharpVersion version)
    {
        var options = w.DecompilerOptions with { LanguageVersion = version };
        w.SetDecompilerOptions(options);
    }

    public static void SetDecompilerToggle(this AppState w, DecompilerToggle toggle, bool value)
    {
        var options = w.DecompilerOptions.With(toggle, value);
        w.SetDecompilerOptions(options);
    }

    public static void SetDecompilerToggles(this AppState w, IEnumerable<DecompilerToggle> toggles, bool value)
    {
        var options = w.DecompilerOptions.With(toggles, value);
        w.SetDecompilerOptions(options);
    }

    /// <summary>Turns every toggle the options panel shows back to its default, keeping the language
    /// version and how projects are exported.</summary>
    public static void ResetDecompilerToggles(this AppState w)
    {
        var options = w.DecompilerOptions.WithCodeTogglesReset();
        w.SetDecompilerOptions(options);
    }

    private static void SetDecompilerOptions(this AppState w, DecompilerOptions options)
    {
        if (options == w.DecompilerOptions)
            return;
        w.DecompilerOptions = options;
    }
}
