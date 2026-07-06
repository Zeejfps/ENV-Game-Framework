using System.Numerics;

namespace ZGF.Svg.Parsing;

/// <summary>
/// Parses the SVG transform-list grammar: matrix, translate, scale, rotate,
/// skewX, skewY, applied left-to-right per the spec.
/// </summary>
internal static class TransformParser
{
    public static Matrix3x2 Parse(ReadOnlySpan<char> s)
    {
        var result = Matrix3x2.Identity;
        var i = 0;
        Span<float> args = stackalloc float[6];

        while (i < s.Length)
        {
            while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ','))
                i++;
            if (i >= s.Length)
                break;

            var nameStart = i;
            while (i < s.Length && (char.IsAsciiLetter(s[i])))
                i++;
            var name = s[nameStart..i];

            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
            if (i >= s.Length || s[i] != '(')
                break;
            i++;

            var argEnd = s[i..].IndexOf(')');
            if (argEnd < 0)
                break;
            var argSpan = s.Slice(i, argEnd);
            i += argEnd + 1;

            var argCount = 0;
            var reader = new PathDataReader(argSpan);
            while (argCount < 6 && reader.TryReadNumber(out var v))
                args[argCount++] = v;

            var m = ToMatrix(name, args, argCount);
            if (m is null)
                continue;

            // SVG applies transform-list items left-to-right: leftmost is outermost.
            // With row-vector Matrix3x2 convention (p * M), that means prepending.
            result = m.Value * result;
        }

        return result;
    }

    private static Matrix3x2? ToMatrix(ReadOnlySpan<char> name, ReadOnlySpan<float> a, int count)
    {
        const float degToRad = MathF.PI / 180f;

        if (name.Equals("matrix", StringComparison.OrdinalIgnoreCase) && count == 6)
            return new Matrix3x2(a[0], a[1], a[2], a[3], a[4], a[5]);

        if (name.Equals("translate", StringComparison.OrdinalIgnoreCase) && count >= 1)
            return Matrix3x2.CreateTranslation(a[0], count >= 2 ? a[1] : 0f);

        if (name.Equals("scale", StringComparison.OrdinalIgnoreCase) && count >= 1)
            return Matrix3x2.CreateScale(a[0], count >= 2 ? a[1] : a[0]);

        if (name.Equals("rotate", StringComparison.OrdinalIgnoreCase))
        {
            switch (count)
            {
                case 1:
                    return Matrix3x2.CreateRotation(a[0] * degToRad);
                case 3:
                    return Matrix3x2.CreateRotation(a[0] * degToRad, new Vector2(a[1], a[2]));
            }
        }

        if (name.Equals("skewX", StringComparison.OrdinalIgnoreCase) && count == 1)
            return new Matrix3x2(1f, 0f, MathF.Tan(a[0] * degToRad), 1f, 0f, 0f);

        if (name.Equals("skewY", StringComparison.OrdinalIgnoreCase) && count == 1)
            return new Matrix3x2(1f, MathF.Tan(a[0] * degToRad), 0f, 1f, 0f, 0f);

        return null;
    }
}
