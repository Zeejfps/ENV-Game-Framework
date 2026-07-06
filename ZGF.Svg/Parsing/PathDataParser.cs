using System.Globalization;
using System.Numerics;
using ZGF.Svg.Scene;

namespace ZGF.Svg.Parsing;

/// <summary>
/// Parses the SVG path-data grammar ("d" attribute) into normalized segments:
/// only Move/Line/Cubic/Close survive. Relative commands become absolute,
/// H/V become L, S/T reflect into C/Q, Q elevates to C, arcs become cubic runs.
/// Lenient like browsers: malformed data keeps the segments parsed so far.
/// </summary>
internal static class PathDataParser
{
    public static void Parse(ReadOnlySpan<char> d, List<PathSegment> output)
    {
        var reader = new PathDataReader(d);
        var current = Vector2.Zero;
        var subpathStart = Vector2.Zero;
        var prevCubicControl = Vector2.Zero;
        var prevQuadControl = Vector2.Zero;
        var prevWasCubic = false;
        var prevWasQuad = false;
        var hasOpenSubpath = false;

        while (reader.TryReadCommand(out var cmd))
        {
            var relative = char.IsLower(cmd);
            switch (char.ToUpperInvariant(cmd))
            {
                case 'M':
                {
                    if (!reader.TryReadPoint(relative, current, out var p))
                        return;
                    output.Add(PathSegment.MoveTo(p));
                    current = p;
                    subpathStart = p;
                    hasOpenSubpath = true;
                    // Extra coordinate pairs after a moveto are implicit linetos.
                    while (reader.NextIsNumber())
                    {
                        if (!reader.TryReadPoint(relative, current, out p))
                            return;
                        output.Add(PathSegment.LineTo(p));
                        current = p;
                    }
                    prevWasCubic = false;
                    prevWasQuad = false;
                    break;
                }
                case 'L':
                {
                    do
                    {
                        if (!reader.TryReadPoint(relative, current, out var p))
                            return;
                        output.Add(PathSegment.LineTo(p));
                        current = p;
                    } while (reader.NextIsNumber());
                    prevWasCubic = false;
                    prevWasQuad = false;
                    break;
                }
                case 'H':
                {
                    do
                    {
                        if (!reader.TryReadNumber(out var x))
                            return;
                        if (relative)
                            x += current.X;
                        current = current with { X = x };
                        output.Add(PathSegment.LineTo(current));
                    } while (reader.NextIsNumber());
                    prevWasCubic = false;
                    prevWasQuad = false;
                    break;
                }
                case 'V':
                {
                    do
                    {
                        if (!reader.TryReadNumber(out var y))
                            return;
                        if (relative)
                            y += current.Y;
                        current = current with { Y = y };
                        output.Add(PathSegment.LineTo(current));
                    } while (reader.NextIsNumber());
                    prevWasCubic = false;
                    prevWasQuad = false;
                    break;
                }
                case 'C':
                {
                    do
                    {
                        if (!reader.TryReadPoint(relative, current, out var c1) ||
                            !reader.TryReadPoint(relative, current, out var c2) ||
                            !reader.TryReadPoint(relative, current, out var p))
                            return;
                        output.Add(PathSegment.CubicTo(c1, c2, p));
                        prevCubicControl = c2;
                        current = p;
                    } while (reader.NextIsNumber());
                    prevWasCubic = true;
                    prevWasQuad = false;
                    break;
                }
                case 'S':
                {
                    do
                    {
                        var c1 = prevWasCubic ? current * 2f - prevCubicControl : current;
                        if (!reader.TryReadPoint(relative, current, out var c2) ||
                            !reader.TryReadPoint(relative, current, out var p))
                            return;
                        output.Add(PathSegment.CubicTo(c1, c2, p));
                        prevCubicControl = c2;
                        current = p;
                        prevWasCubic = true;
                    } while (reader.NextIsNumber());
                    prevWasQuad = false;
                    break;
                }
                case 'Q':
                {
                    do
                    {
                        if (!reader.TryReadPoint(relative, current, out var q) ||
                            !reader.TryReadPoint(relative, current, out var p))
                            return;
                        AddQuadAsCubic(output, current, q, p);
                        prevQuadControl = q;
                        current = p;
                    } while (reader.NextIsNumber());
                    prevWasQuad = true;
                    prevWasCubic = false;
                    break;
                }
                case 'T':
                {
                    do
                    {
                        var q = prevWasQuad ? current * 2f - prevQuadControl : current;
                        if (!reader.TryReadPoint(relative, current, out var p))
                            return;
                        AddQuadAsCubic(output, current, q, p);
                        prevQuadControl = q;
                        current = p;
                        prevWasQuad = true;
                    } while (reader.NextIsNumber());
                    prevWasCubic = false;
                    break;
                }
                case 'A':
                {
                    do
                    {
                        if (!reader.TryReadNumber(out var rx) ||
                            !reader.TryReadNumber(out var ry) ||
                            !reader.TryReadNumber(out var rotation) ||
                            !reader.TryReadFlag(out var largeArc) ||
                            !reader.TryReadFlag(out var sweep) ||
                            !reader.TryReadPoint(relative, current, out var p))
                            return;
                        ArcConverter.ArcToCubics(output, current, rx, ry, rotation, largeArc, sweep, p);
                        current = p;
                    } while (reader.NextIsNumber());
                    prevWasCubic = false;
                    prevWasQuad = false;
                    break;
                }
                case 'Z':
                {
                    if (hasOpenSubpath)
                    {
                        output.Add(PathSegment.ClosePath());
                        current = subpathStart;
                    }
                    prevWasCubic = false;
                    prevWasQuad = false;
                    break;
                }
                default:
                    return;
            }
        }
    }

    /// <summary>Exact degree elevation of a quadratic to a cubic.</summary>
    private static void AddQuadAsCubic(List<PathSegment> output, Vector2 from, Vector2 q, Vector2 to)
    {
        const float twoThirds = 2f / 3f;
        var c1 = from + (q - from) * twoThirds;
        var c2 = to + (q - to) * twoThirds;
        output.Add(PathSegment.CubicTo(c1, c2, to));
    }
}

/// <summary>Allocation-free cursor over path data.</summary>
internal ref struct PathDataReader
{
    private readonly ReadOnlySpan<char> _s;
    private int _i;

    public PathDataReader(ReadOnlySpan<char> s)
    {
        _s = s;
        _i = 0;
    }

    public bool TryReadCommand(out char cmd)
    {
        SkipSeparators();
        if (_i < _s.Length && IsCommand(_s[_i]))
        {
            cmd = _s[_i++];
            return true;
        }
        cmd = default;
        return false;
    }

    public bool NextIsNumber()
    {
        SkipSeparators();
        if (_i >= _s.Length)
            return false;
        var c = _s[_i];
        return c is (>= '0' and <= '9') or '.' or '-' or '+';
    }

    public bool TryReadNumber(out float value)
    {
        SkipSeparators();
        var start = _i;
        if (_i < _s.Length && _s[_i] is '+' or '-')
            _i++;
        while (_i < _s.Length && _s[_i] is >= '0' and <= '9')
            _i++;
        if (_i < _s.Length && _s[_i] == '.')
        {
            _i++;
            while (_i < _s.Length && _s[_i] is >= '0' and <= '9')
                _i++;
        }
        if (_i < _s.Length && _s[_i] is 'e' or 'E')
        {
            var expStart = _i;
            _i++;
            if (_i < _s.Length && _s[_i] is '+' or '-')
                _i++;
            if (_i < _s.Length && _s[_i] is >= '0' and <= '9')
            {
                while (_i < _s.Length && _s[_i] is >= '0' and <= '9')
                    _i++;
            }
            else
            {
                // Not an exponent after all (e.g. path data can't contain 'e' otherwise,
                // but stay lenient and back off).
                _i = expStart;
            }
        }
        return float.TryParse(_s[start.._i], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    public bool TryReadPoint(bool relative, Vector2 current, out Vector2 p)
    {
        if (TryReadNumber(out var x) && TryReadNumber(out var y))
        {
            p = relative ? new Vector2(current.X + x, current.Y + y) : new Vector2(x, y);
            return true;
        }
        p = default;
        return false;
    }

    /// <summary>Arc flags are single characters and may be packed ("110" = 1,1,0).</summary>
    public bool TryReadFlag(out bool value)
    {
        SkipSeparators();
        if (_i < _s.Length && _s[_i] is '0' or '1')
        {
            value = _s[_i++] == '1';
            return true;
        }
        value = default;
        return false;
    }

    private void SkipSeparators()
    {
        while (_i < _s.Length && (_s[_i] == ',' || char.IsWhiteSpace(_s[_i])))
            _i++;
    }

    private static bool IsCommand(char c) => c is
        'M' or 'm' or 'L' or 'l' or 'H' or 'h' or 'V' or 'v' or
        'C' or 'c' or 'S' or 's' or 'Q' or 'q' or 'T' or 't' or
        'A' or 'a' or 'Z' or 'z';
}
