namespace ZGF.ILSpy.Core;

internal sealed class NaturalStringComparer : IComparer<string>
{
    public static readonly NaturalStringComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
            {
                var xStart = i;
                var yStart = j;
                while (i < x.Length && char.IsAsciiDigit(x[i])) i++;
                while (j < y.Length && char.IsAsciiDigit(y[j])) j++;
                var xDigits = x.AsSpan(xStart, i - xStart).TrimStart('0');
                var yDigits = y.AsSpan(yStart, j - yStart).TrimStart('0');
                if (xDigits.Length != yDigits.Length) return xDigits.Length.CompareTo(yDigits.Length);
                var digits = xDigits.SequenceCompareTo(yDigits);
                if (digits != 0) return digits;
                continue;
            }
            var c = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
            if (c != 0) return c;
            i++;
            j++;
        }
        var length = (x.Length - i).CompareTo(y.Length - j);
        return length != 0 ? length : string.CompareOrdinal(x, y);
    }
}
