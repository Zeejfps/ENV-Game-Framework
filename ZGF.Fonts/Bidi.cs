using System.Globalization;

namespace ZGF.Fonts;

/// <summary>
/// Base paragraph direction. <see cref="Auto"/> derives the base level from the first strong
/// directional character (the Unicode "first-strong" heuristic).
/// </summary>
public enum BidiDirection
{
    Auto,
    Ltr,
    Rtl,
}

/// <summary>
/// A pragmatic subset of the Unicode Bidirectional Algorithm (UAX #9): paragraph-level
/// resolution, the weak (W1–W7), neutral (N1/N2) and implicit (I1/I2) rules, the L1 reset of
/// trailing whitespace, and the L2 reordering. Explicit embeddings/overrides/isolates are
/// treated as boundary-neutral (they don't appear in app UI strings) and bracket pairing (N0)
/// is omitted. Enough to lay out Arabic/Hebrew, including numbers and Latin embedded in an RTL
/// line. Operates on a single already-newline-split line.
/// </summary>
public static class Bidi
{
    private enum Bc : byte { L, R, AL, EN, ES, ET, AN, CS, NSM, BN, B, S, WS, ON }

    /// <summary>True if the text contains any strong right-to-left character (R or AL).</summary>
    public static bool ContainsRtl(ReadOnlySpan<char> text)
    {
        var i = 0;
        while (i < text.Length)
        {
            var cp = CodePointAt(text, i, out var len);
            var c = Classify(cp);
            if (c is Bc.R or Bc.AL)
                return true;
            i += len;
        }
        return false;
    }

    /// <summary>
    /// Resolves the embedding level of every UTF-16 unit in <paramref name="text"/>. The two
    /// units of a surrogate pair share a level. <paramref name="paragraphLevel"/> is 0 for an
    /// LTR base and 1 for an RTL base.
    /// </summary>
    public static byte[] ResolveLevels(ReadOnlySpan<char> text, BidiDirection baseDir, out byte paragraphLevel)
    {
        var n = text.Length;
        var levels = new byte[n];

        // Decompose into code-point elements.
        var cls = new Bc[n];
        var orig = new Bc[n];
        var start = new int[n];
        var elen = new int[n];
        var m = 0;
        for (var i = 0; i < n;)
        {
            var cp = CodePointAt(text, i, out var l);
            var c = Classify(cp);
            cls[m] = c;
            orig[m] = c;
            start[m] = i;
            elen[m] = l;
            m++;
            i += l;
        }

        // P2/P3: base paragraph level.
        if (baseDir == BidiDirection.Ltr) paragraphLevel = 0;
        else if (baseDir == BidiDirection.Rtl) paragraphLevel = 1;
        else
        {
            paragraphLevel = 0;
            for (var k = 0; k < m; k++)
            {
                if (cls[k] == Bc.L) { paragraphLevel = 0; break; }
                if (cls[k] is Bc.R or Bc.AL) { paragraphLevel = 1; break; }
            }
        }

        if (m == 0) return levels;

        // Strong type at the start/end of the (single) run sequence.
        var sos = (paragraphLevel & 1) == 1 ? Bc.R : Bc.L;

        // ---- W rules ----
        // W1: NSM takes the type of the previous character (sos at the start).
        {
            var prev = sos;
            for (var k = 0; k < m; k++)
            {
                if (cls[k] == Bc.NSM) cls[k] = prev;
                prev = cls[k];
            }
        }
        // W2: EN becomes AN if the last strong type is AL.
        {
            var strong = sos;
            for (var k = 0; k < m; k++)
            {
                var c = cls[k];
                if (c is Bc.L or Bc.R or Bc.AL) strong = c;
                else if (c == Bc.EN && strong == Bc.AL) cls[k] = Bc.AN;
            }
        }
        // W3: AL becomes R.
        for (var k = 0; k < m; k++)
            if (cls[k] == Bc.AL) cls[k] = Bc.R;
        // W4: a single ES between two EN, or a single CS between two like numbers, joins them.
        for (var k = 1; k < m - 1; k++)
        {
            var c = cls[k];
            if (c == Bc.ES && cls[k - 1] == Bc.EN && cls[k + 1] == Bc.EN) cls[k] = Bc.EN;
            else if (c == Bc.CS && cls[k - 1] == Bc.EN && cls[k + 1] == Bc.EN) cls[k] = Bc.EN;
            else if (c == Bc.CS && cls[k - 1] == Bc.AN && cls[k + 1] == Bc.AN) cls[k] = Bc.AN;
        }
        // W5: a run of ET adjacent to EN becomes EN.
        for (var k = 0; k < m;)
        {
            if (cls[k] == Bc.ET)
            {
                var j = k;
                while (j < m && cls[j] == Bc.ET) j++;
                var adj = (k > 0 && cls[k - 1] == Bc.EN) || (j < m && cls[j] == Bc.EN);
                if (adj)
                    for (var t = k; t < j; t++) cls[t] = Bc.EN;
                k = j;
            }
            else k++;
        }
        // W6: remaining separators/terminators become ON.
        for (var k = 0; k < m; k++)
            if (cls[k] is Bc.ES or Bc.ET or Bc.CS) cls[k] = Bc.ON;
        // W7: EN becomes L if the last strong type is L.
        {
            var strong = sos;
            for (var k = 0; k < m; k++)
            {
                var c = cls[k];
                if (c is Bc.L or Bc.R) strong = c;
                else if (c == Bc.EN && strong == Bc.L) cls[k] = Bc.L;
            }
        }

        // ---- N rules (N0 bracket pairing omitted) ----
        for (var k = 0; k < m;)
        {
            if (IsNeutral(cls[k]))
            {
                var j = k;
                while (j < m && IsNeutral(cls[j])) j++;
                var left = k > 0 ? DirOf(cls[k - 1]) : sos;
                var right = j < m ? DirOf(cls[j]) : sos;
                // N1: neutrals between matching strong directions take that direction.
                // N2: otherwise the embedding (paragraph) direction.
                var resolved = left == right ? left : ((paragraphLevel & 1) == 1 ? Bc.R : Bc.L);
                for (var t = k; t < j; t++) cls[t] = resolved;
                k = j;
            }
            else k++;
        }

        // ---- I rules: implicit levels ----
        var lev = new byte[m];
        for (var k = 0; k < m; k++)
        {
            if ((paragraphLevel & 1) == 0)
                lev[k] = cls[k] switch
                {
                    Bc.R => (byte)(paragraphLevel + 1),
                    Bc.AN or Bc.EN => (byte)(paragraphLevel + 2),
                    _ => paragraphLevel,
                };
            else
                lev[k] = cls[k] switch
                {
                    Bc.L or Bc.EN or Bc.AN => (byte)(paragraphLevel + 1),
                    _ => paragraphLevel,
                };
        }

        // ---- L1: reset segment/paragraph separators and trailing whitespace to the base level.
        // Uses original types per spec.
        var reset = true; // end-of-line counts as trailing
        for (var k = m - 1; k >= 0; k--)
        {
            var o = orig[k];
            if (o is Bc.B or Bc.S)
            {
                lev[k] = paragraphLevel;
                reset = true;
            }
            else if (o is Bc.WS or Bc.BN)
            {
                if (reset) lev[k] = paragraphLevel;
            }
            else reset = false;
        }

        // Expand element levels to per-UTF-16-unit levels.
        for (var k = 0; k < m; k++)
        {
            var L = lev[k];
            var e = start[k] + elen[k];
            for (var c = start[k]; c < e; c++) levels[c] = L;
        }

        return levels;
    }

    /// <summary>
    /// Computes the visual (left-to-right) order of directional runs from their embedding levels
    /// (UAX #9 rule L2). <paramref name="order"/> is filled with run indices in visual order; it
    /// must be at least as long as <paramref name="runLevels"/>. Glyph order *within* an RTL run
    /// is handled by the shaper, so reordering is applied at run granularity only.
    /// </summary>
    public static void ComputeVisualOrder(ReadOnlySpan<byte> runLevels, Span<int> order)
    {
        var n = runLevels.Length;
        for (var i = 0; i < n; i++) order[i] = i;
        if (n <= 1) return;

        byte max = 0;
        var minOdd = byte.MaxValue;
        for (var i = 0; i < n; i++)
        {
            var l = runLevels[i];
            if (l > max) max = l;
            if ((l & 1) == 1 && l < minOdd) minOdd = l;
        }
        if (minOdd == byte.MaxValue) return; // all even: visual order == logical order

        for (int lvl = max; lvl >= minOdd; lvl--)
        {
            var pos = 0;
            while (pos < n)
            {
                if (runLevels[order[pos]] >= lvl)
                {
                    var s = pos;
                    while (pos < n && runLevels[order[pos]] >= lvl) pos++;
                    for (int a = s, b = pos - 1; a < b; a++, b--)
                        (order[a], order[b]) = (order[b], order[a]);
                }
                else pos++;
            }
        }
    }

    private static bool IsNeutral(Bc c) => c is Bc.B or Bc.S or Bc.WS or Bc.ON or Bc.BN;

    // For N rules: collapse a strong-ish type to a pure direction. EN/AN count as R.
    private static Bc DirOf(Bc c) => c == Bc.L ? Bc.L : Bc.R;

    private static int CodePointAt(ReadOnlySpan<char> text, int i, out int len)
    {
        var c = text[i];
        if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
        {
            len = 2;
            return char.ConvertToUtf32(c, text[i + 1]);
        }

        len = 1;
        return c;
    }

    private static Bc Classify(int cp)
    {
        if (cp < 0x80) return ClassifyAscii(cp);

        switch (cp)
        {
            case 0x00A0: return Bc.CS;                       // no-break space
            case 0x00A2: case 0x00A3: case 0x00A4: case 0x00A5: return Bc.ET; // ¢£¤¥
            case 0x00B0: return Bc.ET;                       // degree sign
            case 0x060C: return Bc.CS;                       // Arabic comma
            case 0x066B: case 0x066C: return Bc.AN;          // Arabic decimal / thousands separator
            case 0x20AC: return Bc.ET;                       // euro
            case 0x2030: return Bc.ET;                       // per mille
        }

        if (cp >= 0x06F0 && cp <= 0x06F9) return Bc.EN;      // extended Arabic-Indic digits
        if (cp >= 0x0660 && cp <= 0x0669) return Bc.AN;      // Arabic-Indic digits

        // Marks and format/control characters classify by category regardless of script, so
        // Arabic harakat and Hebrew points become NSM rather than strong letters.
        var cat = CharUnicodeInfo.GetUnicodeCategory(cp);
        if (cat is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark) return Bc.NSM;
        if (cat is UnicodeCategory.Format or UnicodeCategory.Control) return Bc.BN;

        if (TryRtlStrong(cp, out var rtl)) return rtl;

        return cat switch
        {
            UnicodeCategory.SpaceSeparator => Bc.WS,
            UnicodeCategory.LineSeparator => Bc.WS,
            UnicodeCategory.ParagraphSeparator => Bc.B,
            UnicodeCategory.DecimalDigitNumber => Bc.EN,
            UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter => Bc.L,
            _ => Bc.ON,
        };
    }

    private static Bc ClassifyAscii(int cp)
    {
        if (cp >= '0' && cp <= '9') return Bc.EN;
        if ((cp >= 'A' && cp <= 'Z') || (cp >= 'a' && cp <= 'z')) return Bc.L;
        switch (cp)
        {
            case ' ': return Bc.WS;
            case '\t': case 0x0B: case 0x1F: return Bc.S;
            case '\n': case 0x0D: case 0x1C: case 0x1D: case 0x1E: return Bc.B;
            case '+': case '-': return Bc.ES;
            case '#': case '$': case '%': return Bc.ET;
            case ',': case '.': case ':': return Bc.CS;
        }
        if (cp < 0x20 || cp == 0x7F) return Bc.BN;
        return Bc.ON;
    }

    // Returns the strong right-to-left class (R or AL) of a code point, or false if it isn't a
    // strong RTL letter. AL is reserved for the Arabic-script family — it additionally drives
    // European-number handling in W2/W7 — and every other RTL script is the generic R. R is the
    // safe default: it lays a script out right-to-left correctly; only Arabic-style number
    // adjacency differs, so refining a script from R to AL is a one-line table edit.
    private static bool TryRtlStrong(int cp, out Bc cls)
    {
        foreach (var (start, end, c) in RtlRanges)
        {
            if (cp < start) break; // sorted, non-overlapping: no later range can contain cp
            if (cp <= end) { cls = c; return true; }
        }

        cls = Bc.ON;
        return false;
    }

    // Strong right-to-left scripts, ordered by start code point. Marks/format characters inside
    // these blocks are reclassified as NSM/BN by the UnicodeCategory check that runs *before* this
    // table (so Arabic harakat and Hebrew points stay weak, not strong letters). Covers the living
    // RTL scripts; a purely historic block (Phoenician, Kharoshthi, Old Turkic, …) is one row away.
    // Per-block classes follow the Unicode bidi-class property.
    private static readonly (int Start, int End, Bc Class)[] RtlRanges =
    {
        (0x0590, 0x05FF, Bc.R),    // Hebrew
        (0x0600, 0x06FF, Bc.AL),   // Arabic
        (0x0700, 0x074F, Bc.AL),   // Syriac
        (0x0750, 0x077F, Bc.AL),   // Arabic Supplement
        (0x0780, 0x07BF, Bc.AL),   // Thaana
        (0x07C0, 0x07FF, Bc.R),    // N'Ko
        (0x0800, 0x083F, Bc.R),    // Samaritan
        (0x0840, 0x085F, Bc.R),    // Mandaic
        (0x0860, 0x086F, Bc.AL),   // Syriac Supplement
        (0x0870, 0x089F, Bc.AL),   // Arabic Extended-B
        (0x08A0, 0x08FF, Bc.AL),   // Arabic Extended-A
        (0xFB1D, 0xFB4F, Bc.R),    // Hebrew Presentation Forms
        (0xFB50, 0xFDFF, Bc.AL),   // Arabic Presentation Forms-A
        (0xFE70, 0xFEFF, Bc.AL),   // Arabic Presentation Forms-B
        (0x10D00, 0x10D3F, Bc.AL), // Hanifi Rohingya
        (0x10EC0, 0x10EFF, Bc.AL), // Arabic Extended-C
        (0x1E800, 0x1E8DF, Bc.R),  // Mende Kikakui
        (0x1E900, 0x1E95F, Bc.R),  // Adlam
        (0x1EE00, 0x1EEFF, Bc.AL), // Arabic Mathematical Alphabetic Symbols
    };
}
