using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

/// <summary>
/// Virtualized diff body. Owns scroll state on both axes; draws only the rows that intersect
/// the viewport in <see cref="OnDrawSelf"/>. Emits normalized scroll-position and scale
/// updates so an external scrollbar sync controller can drive the scrollbars.
/// </summary>
internal sealed class DiffContentView : View, IScrollableContent
{
    private const float GlyphColumnWidth = 18f;
    private const float BannerPaddingX = 8f;
    private const float HunkHeaderGap = 12f;
    private const float ScrollWheelStep = 60f;
    private const float AssumedFontSize = 13f;
    // Fallback mono advance ratio if the canvas isn't available yet to measure a glyph.
    private const float FallbackMonoAdvanceRatio = 0.6f;

    // Shared style instances. TextStyle is a class so DrawTextInputs holds a reference; we
    // mutate the few that need per-row recoloring (banner/glyph/line text in the row body)
    // on the UI thread between draw calls, so there's no aliasing concern.
    private static readonly TextStyle MonoMetricsStyle = new()
    {
        FontFamily = DiffOptions.MonoFontFamily,
        FontSize = AssumedFontSize,
    };
    private static readonly TextStyle MonoStartStyle = new()
    {
        FontFamily = DiffOptions.MonoFontFamily,
        FontSize = AssumedFontSize,
        VerticalAlignment = TextAlignment.Center,
    };
    private static readonly TextStyle MonoEndStyle = new()
    {
        FontFamily = DiffOptions.MonoFontFamily,
        FontSize = AssumedFontSize,
        HorizontalAlignment = TextAlignment.End,
        VerticalAlignment = TextAlignment.Center,
    };
    private static readonly TextStyle MonoCenterStyle = new()
    {
        FontFamily = DiffOptions.MonoFontFamily,
        FontSize = AssumedFontSize,
        HorizontalAlignment = TextAlignment.Center,
        VerticalAlignment = TextAlignment.Center,
    };
    private static readonly TextStyle PlaceholderStyle = new()
    {
        HorizontalAlignment = TextAlignment.Center,
        VerticalAlignment = TextAlignment.Center,
    };

    public event Action<float>? VerticalScrollPositionChanged;
    public event Action<float>? HorizontalScrollPositionChanged;

    public float VerticalScale { get; private set; } = 1f;
    public float HorizontalScale { get; private set; } = 1f;

    private DiffViewModel _viewModel = new DiffViewModel.Placeholder("Select a file to view diff.");
    private readonly List<DiffRow> _rows = new();
    private int _maxRowChars;
    private float _gutterWidth;
    private float _lineHeight;
    private float _monoAdvance;
    private bool _metricsResolved;

    private float _scrollY;
    private float _scrollX;
    private float _lastNormalizedY;
    private float _lastNormalizedX;
    private float _lastVerticalScale = 1f;
    private float _lastHorizontalScale = 1f;

    public DiffContentView()
    {
        this.UseController(_ => new DiffContentViewController(this));
    }

    public void SetViewModel(DiffViewModel vm)
    {
        _viewModel = vm;
        _rows.Clear();
        _maxRowChars = 0;
        _scrollY = 0;
        _scrollX = 0;
        // Metrics depend only on font, not content, but content width depends on metrics;
        // a fresh model forces a recompute on next draw.
        _metricsResolved = false;

        if (vm is DiffViewModel.Loaded loaded)
            FlattenRows(loaded.Result);

        SetDirty();
    }

    private void FlattenRows(DiffResult r)
    {
        if (r.ErrorMessage != null) return;
        if (r.IsBinary) return;
        if (r.Hunks.Count == 0 && !r.IsModeOnly && r.OldPath == null) return;

        if (r.OldPath != null)
            AddBanner($"Renamed: {r.OldPath} → {r.Path}");
        if (r.IsModeOnly)
            AddBanner($"Mode: {FormatMode(r.OldMode)} → {FormatMode(r.NewMode)}");

        int maxOld = 0, maxNew = 0, totalLines = 0;
        foreach (var h in r.Hunks)
        {
            foreach (var l in h.Lines)
            {
                if (l.OldLineNumber is int o && o > maxOld) maxOld = o;
                if (l.NewLineNumber is int n && n > maxNew) maxNew = n;
            }
            totalLines += h.Lines.Count;
        }
        // Gutter widths picked from max digit count, same heuristic as the old code.
        var digits = Math.Max(1, Math.Max(DigitCount(maxOld), DigitCount(maxNew)));
        _gutterWidth = digits * AssumedFontSize * FallbackMonoAdvanceRatio + 8f;

        foreach (var h in r.Hunks)
        {
            var range = $"@@ -{h.OldStart},{h.OldLines} +{h.NewStart},{h.NewLines} @@";
            _rows.Add(new DiffRow.HunkSeparator(range, string.IsNullOrEmpty(h.Header) ? null : h.Header));
            // Charge a representative width that includes both the range and any context
            // header (worst case: they don't overlap in the layout).
            var sepChars = range.Length + (h.Header?.Length ?? 0) + 2;
            if (sepChars > _maxRowChars) _maxRowChars = sepChars;

            foreach (var l in h.Lines)
            {
                var text = ExpandTabs(l.Text);
                var row = new DiffRow.Line(
                    l.Kind,
                    l.OldLineNumber?.ToString() ?? string.Empty,
                    l.NewLineNumber?.ToString() ?? string.Empty,
                    text,
                    text.Length);
                _rows.Add(row);
                if (text.Length > _maxRowChars) _maxRowChars = text.Length;
            }
        }

        if (r.Truncated)
            AddBanner($"Diff truncated — only the first {totalLines} lines are shown.");
    }

    private void AddBanner(string text)
    {
        _rows.Add(new DiffRow.Banner(text));
        if (text.Length > _maxRowChars) _maxRowChars = text.Length;
    }

    public void ScrollVerticalBy(float delta)
    {
        ApplyScrollDelta(delta, 0);
    }

    public void SetVerticalNormalizedScrollPosition(float normalized)
    {
        var range = ContentHeight() - Position.Height;
        if (range <= 0) { _scrollY = 0; }
        else { _scrollY = Math.Clamp(normalized, 0f, 1f) * range; }
        SetDirty();
    }

    public void SetHorizontalNormalizedScrollPosition(float normalized)
    {
        var range = ContentWidth() - Position.Width;
        if (range <= 0) { _scrollX = 0; }
        else { _scrollX = Math.Clamp(normalized, 0f, 1f) * range; }
        SetDirty();
    }

    private void ApplyScrollDelta(float dy, float dx)
    {
        var prevY = _scrollY;
        var prevX = _scrollX;
        _scrollY += dy;
        _scrollX += dx;
        ClampScroll();
        if (Math.Abs(_scrollY - prevY) > 0.0001f || Math.Abs(_scrollX - prevX) > 0.0001f)
            SetDirty();
    }

    private float ContentHeight()
    {
        if (_lineHeight <= 0) return 0f;
        return _rows.Count * _lineHeight;
    }

    private float ContentWidth()
    {
        // Always at least the viewport: short diffs shouldn't leave dead space on the right
        // where the colored row backgrounds would visibly stop short of the edge.
        var natural = ComputeNaturalContentWidth();
        return Math.Max(Position.Width, natural);
    }

    private float ComputeNaturalContentWidth()
    {
        if (_monoAdvance <= 0) return 0f;
        // Worst case across row kinds: line rows go gutter|gutter|glyph|text; banner rows
        // are flush-left with horizontal padding. Take the max of both formulas.
        var lineWidth = _gutterWidth + _gutterWidth + GlyphColumnWidth + _maxRowChars * _monoAdvance + BannerPaddingX;
        var bannerWidth = BannerPaddingX * 2 + _maxRowChars * _monoAdvance;
        return Math.Max(lineWidth, bannerWidth);
    }

    private void ClampScroll()
    {
        _scrollY = ScrollMath.ClampScroll(_scrollY, ContentHeight(), Position.Height);
        _scrollX = ScrollMath.ClampScroll(_scrollX, ContentWidth(), Position.Width);
    }

    private void EnsureMetrics(ICanvas c)
    {
        if (_metricsResolved) return;
        _lineHeight = c.MeasureTextLineHeight(MonoMetricsStyle);
        // One real measurement of a representative glyph is more honest than the 0.6 ratio
        // heuristic; falls back to the heuristic if the canvas reports nothing usable.
        var measured = c.MeasureTextWidth("0", MonoMetricsStyle);
        _monoAdvance = measured > 0 ? measured : AssumedFontSize * FallbackMonoAdvanceRatio;
        // Recompute gutter width from the real advance so it lines up with actual digits.
        var digitsTotal = Math.Max(1, GutterDigitCount());
        _gutterWidth = digitsTotal * _monoAdvance + 8f;
        _metricsResolved = true;
    }

    private int GutterDigitCount()
    {
        int maxDigits = 1;
        foreach (var row in _rows)
        {
            if (row is DiffRow.Line l)
            {
                if (l.OldNumber.Length > maxDigits) maxDigits = l.OldNumber.Length;
                if (l.NewNumber.Length > maxDigits) maxDigits = l.NewNumber.Length;
            }
        }
        return maxDigits;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        var pos = Position;
        var z = GetDrawZIndex();

        c.DrawRect(new DrawRectInputs
        {
            Position = pos,
            Style = SolidBgStyle(CommitsPalette.Background),
            ZIndex = z,
        });

        switch (_viewModel)
        {
            case DiffViewModel.Placeholder p:
                DrawPlaceholder(c, pos, p.Text, CommitsPalette.Placeholder, z + 1);
                NotifyScrollChanged(viewportFits: true);
                return;
            case DiffViewModel.Loaded loaded when loaded.Result.ErrorMessage != null:
                DrawPlaceholder(c, pos, loaded.Result.ErrorMessage, CommitsPalette.WarningText, z + 1);
                NotifyScrollChanged(viewportFits: true);
                return;
            case DiffViewModel.Loaded loaded when loaded.Result.IsBinary:
                DrawPlaceholder(c, pos, "Binary file not shown", CommitsPalette.Placeholder, z + 1);
                NotifyScrollChanged(viewportFits: true);
                return;
            case DiffViewModel.Loaded when _rows.Count == 0:
                DrawPlaceholder(c, pos, "No textual changes", CommitsPalette.Placeholder, z + 1);
                NotifyScrollChanged(viewportFits: true);
                return;
        }

        EnsureMetrics(c);
        ClampScroll();
        NotifyScrollChanged(viewportFits: false);

        c.PushClip(pos);

        var rowWidth = ContentWidth();
        var rowLeft = pos.Left - _scrollX;

        // One row of slack on each side absorbs partial-row overlap at the viewport edges.
        var firstVisible = Math.Max(0, (int)(_scrollY / _lineHeight) - 1);
        var lastVisible = Math.Min(_rows.Count - 1,
            (int)((_scrollY + pos.Height) / _lineHeight) + 1);

        for (var i = firstVisible; i <= lastVisible; i++)
        {
            var rowTop = pos.Top + _scrollY - i * _lineHeight;
            var rowBottom = rowTop - _lineHeight;
            switch (_rows[i])
            {
                case DiffRow.Banner b:
                    DrawBannerRow(c, b, rowLeft, rowBottom, rowWidth, z + 1);
                    break;
                case DiffRow.HunkSeparator s:
                    DrawHunkSeparatorRow(c, s, rowLeft, rowBottom, rowWidth, z + 1);
                    break;
                case DiffRow.Line l:
                    DrawLineRow(c, l, rowLeft, rowBottom, rowWidth, z + 1);
                    break;
            }
        }

        c.PopClip();
    }

    private void DrawBannerRow(ICanvas c, DiffRow.Banner b, float left, float bottom, float width, int z)
    {
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(left, bottom, width, _lineHeight),
            Style = SolidBgStyle(DiffPalette.BannerBg),
            ZIndex = z,
        });
        DrawMonoText(c, b.Text, left + BannerPaddingX, bottom,
            width - BannerPaddingX * 2, DiffPalette.BannerText, TextAlignment.Start, z + 1);
    }

    private void DrawHunkSeparatorRow(ICanvas c, DiffRow.HunkSeparator s, float left, float bottom, float width, int z)
    {
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(left, bottom, width, _lineHeight),
            Style = SolidBgStyle(DiffPalette.HunkSeparatorBg),
            ZIndex = z,
        });

        var rangeWidth = s.Range.Length * _monoAdvance;
        var textX = left + BannerPaddingX;
        DrawMonoText(c, s.Range, textX, bottom, rangeWidth,
            DiffPalette.HunkSeparatorRangeText, TextAlignment.Start, z + 1);

        if (s.Header != null)
        {
            var headerX = textX + rangeWidth + HunkHeaderGap;
            var headerWidth = Math.Max(0f, left + width - BannerPaddingX - headerX);
            if (headerWidth > 0)
                DrawMonoText(c, s.Header, headerX, bottom, headerWidth,
                    DiffPalette.HunkSeparatorContextText, TextAlignment.Start, z + 1);
        }
    }

    private void DrawLineRow(ICanvas c, DiffRow.Line l, float left, float bottom, float width, int z)
    {
        var (glyph, glyphColor) = l.Kind switch
        {
            DiffLineKind.Added => ("+", DiffPalette.LineAddedGlyphText),
            DiffLineKind.Removed => ("-", DiffPalette.LineRemovedGlyphText),
            _ => (" ", DiffPalette.LineContextGlyphText),
        };
        var bg = l.Kind switch
        {
            DiffLineKind.Added => DiffPalette.LineAddedBg,
            DiffLineKind.Removed => DiffPalette.LineRemovedBg,
            _ => CommitsPalette.Background,
        };

        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(left, bottom, width, _lineHeight),
            Style = SolidBgStyle(bg),
            ZIndex = z,
        });

        var x = left;
        DrawMonoText(c, l.OldNumber, x, bottom, _gutterWidth,
            DiffPalette.LineNumberText, TextAlignment.End, z + 1);
        x += _gutterWidth + 4f;
        DrawMonoText(c, l.NewNumber, x, bottom, _gutterWidth,
            DiffPalette.LineNumberText, TextAlignment.End, z + 1);
        x += _gutterWidth + 4f;
        DrawMonoText(c, glyph, x, bottom, GlyphColumnWidth, glyphColor, TextAlignment.Center, z + 1);
        x += GlyphColumnWidth + 4f;

        var textWidth = Math.Max(0f, left + width - x);
        DrawMonoText(c, l.Text, x, bottom, textWidth,
            DiffPalette.LineText, TextAlignment.Start, z + 1);
    }

    private void DrawMonoText(
        ICanvas c, string text, float left, float bottom, float width,
        uint color, TextAlignment alignment, int z)
    {
        if (width <= 0 || string.IsNullOrEmpty(text)) return;
        var style = alignment switch
        {
            TextAlignment.End => MonoEndStyle,
            TextAlignment.Center => MonoCenterStyle,
            _ => MonoStartStyle,
        };
        style.TextColor = color;
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF(left, bottom, width, _lineHeight),
            Text = text,
            Style = style,
            ZIndex = z,
        });
    }

    private void DrawPlaceholder(ICanvas c, RectF pos, string text, uint color, int z)
    {
        PlaceholderStyle.TextColor = color;
        c.DrawText(new DrawTextInputs
        {
            Position = pos,
            Text = text,
            Style = PlaceholderStyle,
            ZIndex = z,
        });
    }

    private void NotifyScrollChanged(bool viewportFits)
    {
        float normalizedY, normalizedX, vScale, hScale;
        if (viewportFits)
        {
            normalizedY = 0f;
            normalizedX = 0f;
            vScale = 1f;
            hScale = 1f;
        }
        else
        {
            var contentH = ContentHeight();
            var contentW = ContentWidth();
            var vph = Position.Height;
            var vpw = Position.Width;

            if (contentH <= vph || vph <= 0)
            {
                vScale = 1f;
                normalizedY = 0f;
            }
            else
            {
                vScale = vph / contentH;
                var range = contentH - vph;
                normalizedY = Math.Clamp(_scrollY / range, 0f, 1f);
            }

            if (contentW <= vpw || vpw <= 0)
            {
                hScale = 1f;
                normalizedX = 0f;
            }
            else
            {
                hScale = vpw / contentW;
                var range = contentW - vpw;
                normalizedX = Math.Clamp(_scrollX / range, 0f, 1f);
            }
        }

        VerticalScale = vScale;
        HorizontalScale = hScale;

        // Dedup against the last published value — otherwise we'd retrigger scrollbar
        // layout every frame, even when nothing actually changed.
        if (Math.Abs(vScale - _lastVerticalScale) > 0.0001f ||
            Math.Abs(normalizedY - _lastNormalizedY) > 0.0001f)
        {
            _lastVerticalScale = vScale;
            _lastNormalizedY = normalizedY;
            VerticalScrollPositionChanged?.Invoke(normalizedY);
        }
        if (Math.Abs(hScale - _lastHorizontalScale) > 0.0001f ||
            Math.Abs(normalizedX - _lastNormalizedX) > 0.0001f)
        {
            _lastHorizontalScale = hScale;
            _lastNormalizedX = normalizedX;
            HorizontalScrollPositionChanged?.Invoke(normalizedX);
        }
    }

    private static RectStyle SolidBgStyle(uint color) => new() { BackgroundColor = color };

    internal void OnWheel(float deltaY)
    {
        ApplyScrollDelta(-deltaY * ScrollWheelStep, 0);
    }

    private static int DigitCount(int n)
    {
        if (n <= 0) return 1;
        var d = 0;
        while (n > 0) { d++; n /= 10; }
        return d;
    }

    private static string ExpandTabs(string s)
    {
        if (s.IndexOf('\t') < 0) return s;
        return s.Replace("\t", TabReplacement);
    }

    private static readonly string TabReplacement = new(' ', DiffOptions.TabWidth);

    private static string FormatMode(int? mode)
        => mode is int m ? Convert.ToString(m, 8).PadLeft(6, '0') : "-";
}