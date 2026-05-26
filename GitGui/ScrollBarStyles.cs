using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal static class ScrollBarStyles
{
    public static VerticalScrollBarView CreateVertical()
    {
        var bar = new VerticalScrollBarView
        {
            TrackBorderSize = new BorderSizeStyle { Left = 1 },
        };
        ConfigureThumb(bar.Thumb);
        bar.UseController(_ => new VerticalScrollBarViewController(bar));
        bar.BindToTheme(t =>
        {
            var c = t.Commits;
            bar.TrackBackgroundColor = c.ScrollTrackBg;
            bar.TrackBorderColor = new BorderColorStyle
            {
                Left = c.ScrollTrackBorder,
                Top = c.ScrollTrackBorder,
                Right = c.ScrollTrackBorder,
                Bottom = c.ScrollTrackBorder,
            };
            bar.Thumb.IdleBackgroundColor = c.ScrollThumbBg;
            bar.Thumb.HoveredBackgroundColor = c.ScrollThumbHoverBg;
            bar.Thumb.BorderColor = ThumbBorderFromTokens(c.ScrollThumbBorder);
        });
        return bar;
    }

    public static HorizontalScrollBarView CreateHorizontal()
    {
        var bar = new HorizontalScrollBarView
        {
            TrackBorderSize = new BorderSizeStyle { Top = 1 },
        };
        ConfigureThumb(bar.Thumb);
        bar.UseController(_ => new HorizontalScrollBarViewController(bar));
        bar.BindToTheme(t =>
        {
            var c = t.Commits;
            bar.TrackBackgroundColor = c.ScrollTrackBg;
            bar.TrackBorderColor = new BorderColorStyle
            {
                Left = c.ScrollTrackBorder,
                Top = c.ScrollTrackBorder,
                Right = c.ScrollTrackBorder,
                Bottom = c.ScrollTrackBorder,
            };
            bar.Thumb.IdleBackgroundColor = c.ScrollThumbBg;
            bar.Thumb.HoveredBackgroundColor = c.ScrollThumbHoverBg;
            bar.Thumb.BorderColor = ThumbBorderFromTokens(c.ScrollThumbBorder);
        });
        return bar;
    }

    private static void ConfigureThumb(VerticalScrollBarThumbView thumb)
    {
        thumb.BorderSize = BorderSizeStyle.All(1);
    }

    private static void ConfigureThumb(HorizontalScrollBarThumbView thumb)
    {
        thumb.BorderSize = BorderSizeStyle.All(1);
    }

    private static BorderColorStyle ThumbBorderFromTokens(uint border) => new()
    {
        Left = border,
        Top = border,
        Right = border,
        Bottom = border,
    };
}
