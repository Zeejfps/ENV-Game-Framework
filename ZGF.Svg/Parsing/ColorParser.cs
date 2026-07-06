using System.Globalization;

namespace ZGF.Svg.Parsing;

internal static class ColorParser
{
    /// <summary>
    /// Parses a paint value: #hex forms, rgb()/rgba(), named colors, none,
    /// currentColor, transparent. Returns false for anything unrecognized
    /// (including unsupported url() references) so the caller keeps the
    /// inherited value.
    /// </summary>
    public static bool TryParsePaint(ReadOnlySpan<char> s, out SvgPaintKind kind, out uint argb)
    {
        s = s.Trim();
        kind = SvgPaintKind.Color;
        argb = 0;

        if (s.IsEmpty)
            return false;

        if (s.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            kind = SvgPaintKind.None;
            return true;
        }

        if (s.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
        {
            kind = SvgPaintKind.CurrentColor;
            argb = 0xFF000000;
            return true;
        }

        return TryParseColor(s, out argb);
    }

    public static bool TryParseColor(ReadOnlySpan<char> s, out uint argb)
    {
        s = s.Trim();
        argb = 0;

        if (s.IsEmpty)
            return false;

        if (s[0] == '#')
            return TryParseHex(s[1..], out argb);

        if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            return TryParseRgbFunc(s, out argb);

        if (s.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            argb = 0;
            return true;
        }

        return NamedColors.TryGet(s, out argb);
    }

    private static bool TryParseHex(ReadOnlySpan<char> hex, out uint argb)
    {
        argb = 0;
        switch (hex.Length)
        {
            case 3:  // #rgb
            case 4:  // #rgba
            {
                Span<char> expanded = stackalloc char[hex.Length * 2];
                for (var i = 0; i < hex.Length; i++)
                {
                    expanded[i * 2] = hex[i];
                    expanded[i * 2 + 1] = hex[i];
                }
                return TryParseHex(expanded, out argb);
            }
            case 6:  // #rrggbb
            {
                if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                    return false;
                argb = 0xFF000000 | rgb;
                return true;
            }
            case 8:  // #rrggbbaa
            {
                if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgba))
                    return false;
                argb = (rgba << 24) | (rgba >> 8);
                return true;
            }
            default:
                return false;
        }
    }

    private static bool TryParseRgbFunc(ReadOnlySpan<char> s, out uint argb)
    {
        argb = 0;
        var open = s.IndexOf('(');
        var closeIdx = s.LastIndexOf(')');
        if (open < 0 || closeIdx <= open)
            return false;

        var args = s[(open + 1)..closeIdx];
        Span<Range> parts = stackalloc Range[5];
        var count = args.Split(parts, ',', StringSplitOptions.TrimEntries);
        if (count is not (3 or 4))
            return false;

        if (!TryParseChannel(args[parts[0]], out var r) ||
            !TryParseChannel(args[parts[1]], out var g) ||
            !TryParseChannel(args[parts[2]], out var b))
            return false;

        uint a = 255;
        if (count == 4)
        {
            if (!float.TryParse(args[parts[3]], NumberStyles.Float, CultureInfo.InvariantCulture, out var alpha))
                return false;
            a = (uint)Math.Clamp((int)MathF.Round(alpha * 255f), 0, 255);
        }

        argb = (a << 24) | (r << 16) | (g << 8) | b;
        return true;
    }

    private static bool TryParseChannel(ReadOnlySpan<char> s, out uint value)
    {
        value = 0;
        var isPercent = s.EndsWith("%");
        if (isPercent)
            s = s[..^1];
        if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            return false;
        if (isPercent)
            f = f * 255f / 100f;
        value = (uint)Math.Clamp((int)MathF.Round(f), 0, 255);
        return true;
    }
}
