using System.Globalization;

namespace ZGF.Gui.Localization;

public static class PluralRules
{
    public static string Select(CultureInfo culture, in PluralForms forms, long n) =>
        forms.Get(Category(culture.TwoLetterISOLanguageName, n));

    public static PluralCategory Category(string language, long n) => language switch
    {
        "fr" or "pt" => n is 0 or 1 ? PluralCategory.One : PluralCategory.Other,
        "ar" => Arabic(n),
        _ => n == 1 ? PluralCategory.One : PluralCategory.Other,
    };

    // CLDR cardinal rules for Arabic — the full six-form set.
    private static PluralCategory Arabic(long n) => n switch
    {
        0 => PluralCategory.Zero,
        1 => PluralCategory.One,
        2 => PluralCategory.Two,
        _ => (n % 100) switch
        {
            >= 3 and <= 10 => PluralCategory.Few,
            >= 11 and <= 99 => PluralCategory.Many,
            _ => PluralCategory.Other,
        },
    };
}
