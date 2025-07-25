﻿namespace ZGF.Gui.Tests;

public sealed class TextMeasurer : ITextMeasurer
{
    private readonly BitmapFont _bitmapFont;

    public TextMeasurer(BitmapFont bitmapFont)
    {
        _bitmapFont = bitmapFont;
    }

    public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style)
    {
        var totalWidth = 0f;
        foreach (var codePoint in text.EnumerateCodePoints())
        {
            if (!_bitmapFont.TryGetGlyphInfo(codePoint, out var glyphInfo))
                continue;
            
            totalWidth += glyphInfo.XAdvance;
        }
        return totalWidth;
    }

    public float MeasureTextHeight(ReadOnlySpan<char> text, TextStyle style)
    {
        return _bitmapFont.FontMetrics.Common.LineHeight;
    }
}