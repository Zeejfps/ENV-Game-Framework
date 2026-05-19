using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal static class ScrollBarStyles
{
    public static VerticalScrollBarView CreateVertical()
    {
        var bar = new VerticalScrollBarView
        {
            TrackBackgroundColor = CommitsPalette.ScrollTrackBg,
            TrackBorderColor = new BorderColorStyle
            {
                Left = CommitsPalette.ScrollTrackBorder,
                Top = CommitsPalette.ScrollTrackBorder,
                Right = CommitsPalette.ScrollTrackBorder,
                Bottom = CommitsPalette.ScrollTrackBorder,
            },
            TrackBorderSize = new BorderSizeStyle { Left = 1 },
        };
        StyleThumb(bar.Thumb);
        bar.UseController(_ => new VerticalScrollBarViewController(bar));
        return bar;
    }

    public static HorizontalScrollBarView CreateHorizontal()
    {
        var bar = new HorizontalScrollBarView
        {
            TrackBackgroundColor = CommitsPalette.ScrollTrackBg,
            TrackBorderColor = new BorderColorStyle
            {
                Left = CommitsPalette.ScrollTrackBorder,
                Top = CommitsPalette.ScrollTrackBorder,
                Right = CommitsPalette.ScrollTrackBorder,
                Bottom = CommitsPalette.ScrollTrackBorder,
            },
            TrackBorderSize = new BorderSizeStyle { Top = 1 },
        };
        StyleThumb(bar.Thumb);
        bar.UseController(_ => new HorizontalScrollBarViewController(bar));
        return bar;
    }

    private static void StyleThumb(VerticalScrollBarThumbView thumb)
    {
        thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        thumb.BorderColor = ThumbBorder();
        thumb.BorderSize = BorderSizeStyle.All(1);
    }

    private static void StyleThumb(HorizontalScrollBarThumbView thumb)
    {
        thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        thumb.BorderColor = ThumbBorder();
        thumb.BorderSize = BorderSizeStyle.All(1);
    }

    private static BorderColorStyle ThumbBorder() => new()
    {
        Left = CommitsPalette.ScrollThumbBorder,
        Top = CommitsPalette.ScrollThumbBorder,
        Right = CommitsPalette.ScrollThumbBorder,
        Bottom = CommitsPalette.ScrollThumbBorder,
    };
}
