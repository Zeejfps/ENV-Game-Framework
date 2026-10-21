using System.Globalization;

namespace ZGF.Fonts;

public readonly record struct SystemFontFile(string Path, int FaceIndex);

/// <summary>
/// The OS fonts to register with <see cref="FreeTypeFontBackend.RegisterFallbackFont"/> so text the
/// bundled fonts don't cover — CJK, Arabic, symbols — draws instead of showing as boxes, without
/// shipping a font for it.
/// </summary>
public static class SystemFallbackFonts
{
    /// <summary>The fallbacks present on this machine, in the order to register them. The chain is
    /// first-cover-wins, so symbols come first, and the CJK family of
    /// <paramref name="culture"/> (the current UI culture by default) comes before the others, since
    /// they share Han ideographs but draw them in regional forms.</summary>
    public static IReadOnlyList<SystemFontFile> Find(CultureInfo? culture = null)
    {
        var result = new List<SystemFontFile>();
        foreach (var group in Groups(culture ?? CultureInfo.CurrentUICulture))
        {
            foreach (var font in group)
            {
                if (!File.Exists(font.Path)) continue;
                if (!result.Contains(font)) result.Add(font);
                break;
            }
        }
        return result;
    }

    private enum Cjk
    {
        Japanese,
        SimplifiedChinese,
        TraditionalChinese,
        Korean,
    }

    private static IEnumerable<SystemFontFile[]> Groups(CultureInfo culture)
    {
        foreach (var group in Symbols())
            yield return group;
        foreach (var family in CjkOrder(culture))
            yield return CjkCandidates(family);
        yield return Arabic();
    }

    private static IEnumerable<Cjk> CjkOrder(CultureInfo culture)
    {
        var preferred = Preferred(culture);
        if (preferred is { } first)
            yield return first;
        foreach (var family in Enum.GetValues<Cjk>())
        {
            if (family != preferred)
                yield return family;
        }
    }

    private static Cjk? Preferred(CultureInfo culture)
    {
        var name = culture.Name;
        return culture.TwoLetterISOLanguageName switch
        {
            "ja" => Cjk.Japanese,
            "ko" => Cjk.Korean,
            "zh" when name.Contains("Hant") || name.EndsWith("-TW") || name.EndsWith("-HK") || name.EndsWith("-MO") => Cjk.TraditionalChinese,
            "zh" => Cjk.SimplifiedChinese,
            _ => null,
        };
    }

    private static string WindowsFonts => Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    private static SystemFontFile Windows(string file, int face = 0) => new(Path.Combine(WindowsFonts, file), face);

    private static IEnumerable<SystemFontFile[]> Symbols()
    {
        if (OperatingSystem.IsMacOS())
        {
            yield return
            [
                new("/System/Library/Fonts/Menlo.ttc", 0),
                new("/System/Library/Fonts/Supplemental/Arial Unicode.ttf", 0),
            ];
            yield return
            [
                new("/System/Library/Fonts/Supplemental/STIXTwoMath.otf", 0),
                new("/System/Library/Fonts/Apple Symbols.ttf", 0),
            ];
        }
        else if (OperatingSystem.IsWindows())
        {
            yield return [Windows("seguisym.ttf"), Windows("arialuni.ttf"), Windows("consola.ttf")];
            yield return [Windows("cambria.ttc", 1), Windows("seguisym.ttf")];
        }
        else
        {
            yield return
            [
                new("/usr/share/fonts/truetype/noto/NotoSansSymbols2-Regular.ttf", 0),
                new("/usr/share/fonts/opentype/noto/NotoSansSymbols2-Regular.ttf", 0),
                new("/usr/share/fonts/google-noto/NotoSansSymbols2-Regular.ttf", 0),
                new("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 0),
            ];
            yield return
            [
                new("/usr/share/fonts/truetype/noto/NotoSansSymbols-Regular.ttf", 0),
                new("/usr/share/fonts/opentype/noto/NotoSansSymbols-Regular.ttf", 0),
                new("/usr/share/fonts/google-noto/NotoSansSymbols-Regular.ttf", 0),
                new("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 0),
            ];
        }
    }

    private static SystemFontFile[] CjkCandidates(Cjk family)
    {
        if (OperatingSystem.IsMacOS())
        {
            return family switch
            {
                Cjk.Japanese => [new("/System/Library/Fonts/ヒラギノ角ゴシック W3.ttc", 0), new("/System/Library/Fonts/Hiragino Sans GB.ttc", 0)],
                Cjk.SimplifiedChinese => [new("/System/Library/Fonts/PingFang.ttc", 0), new("/System/Library/Fonts/STHeiti Medium.ttc", 0)],
                Cjk.TraditionalChinese => [new("/System/Library/Fonts/PingFang.ttc", 0), new("/System/Library/Fonts/STHeiti Medium.ttc", 0)],
                _ => [new("/System/Library/Fonts/AppleSDGothicNeo.ttc", 0)],
            };
        }
        if (OperatingSystem.IsWindows())
        {
            return family switch
            {
                Cjk.Japanese => [Windows("YuGothR.ttc"), Windows("meiryo.ttc"), Windows("msgothic.ttc")],
                Cjk.SimplifiedChinese => [Windows("msyh.ttc"), Windows("simsun.ttc")],
                Cjk.TraditionalChinese => [Windows("msjh.ttc"), Windows("mingliu.ttc")],
                _ => [Windows("malgun.ttf"), Windows("gulim.ttc")],
            };
        }
        return
        [
            new("/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc", 0),
            new("/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc", 0),
            new("/usr/share/fonts/google-noto-cjk/NotoSansCJK-Regular.ttc", 0),
            new("/usr/share/fonts/opentype/noto/NotoSansCJKjp-Regular.otf", 0),
            new("/usr/share/fonts/truetype/droid/DroidSansFallbackFull.ttf", 0),
        ];
    }

    private static SystemFontFile[] Arabic()
    {
        if (OperatingSystem.IsMacOS())
        {
            return
            [
                new("/System/Library/Fonts/Supplemental/GeezaPro.ttc", 0),
                new("/Library/Fonts/GeezaPro.ttc", 0),
                new("/System/Library/Fonts/Supplemental/Arial.ttf", 0),
            ];
        }
        if (OperatingSystem.IsWindows())
            return [Windows("segoeui.ttf"), Windows("tahoma.ttf"), Windows("arial.ttf")];
        return
        [
            new("/usr/share/fonts/truetype/noto/NotoNaskhArabic-Regular.ttf", 0),
            new("/usr/share/fonts/opentype/noto/NotoNaskhArabic-Regular.ttf", 0),
            new("/usr/share/fonts/google-noto/NotoNaskhArabic-Regular.ttf", 0),
            new("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 0),
        ];
    }
}
