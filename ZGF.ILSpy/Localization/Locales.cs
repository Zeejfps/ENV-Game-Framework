using System.Globalization;

namespace ZGF.ILSpy.Localization;

public static class Locales
{
    public static IReadOnlyList<(Locale Locale, string Endonym)> All { get; } =
    [
        (Locale.En, "English"),
        (Locale.De, "Deutsch"),
        (Locale.Es, "Español"),
        (Locale.Fr, "Français"),
        (Locale.PtBr, "Português (Brasil)"),
        (Locale.Ru, "Русский"),
        (Locale.Ja, "日本語"),
        (Locale.Ko, "한국어"),
        (Locale.ZhHans, "简体中文"),
    ];

    /// <summary>The locale for <paramref name="culture"/>'s language, or English when there is none.</summary>
    public static Locale For(CultureInfo culture) => culture.TwoLetterISOLanguageName switch
    {
        "de" => Locale.De,
        "es" => Locale.Es,
        "fr" => Locale.Fr,
        "ja" => Locale.Ja,
        "ko" => Locale.Ko,
        "pt" => Locale.PtBr,
        "ru" => Locale.Ru,
        "zh" => Locale.ZhHans,
        _ => Locale.En,
    };
}
