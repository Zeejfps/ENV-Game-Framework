using ZGF.Gui.Tests;

namespace GitGui;

/// <summary>
/// Mirrors an <see cref="IScrollableContent"/>'s scroll state into stand-alone scroll bars
/// and routes user-driven scroll-bar changes back into the content. The horizontal scroll
/// bar is optional for callers that only need a vertical axis (e.g. plain text panes).
/// </summary>
internal sealed class ScrollSyncController : KeyboardMouseController, IDisposable
{
    private readonly IScrollableContent _content;
    private readonly VerticalScrollBarView _vScrollBar;
    private readonly HorizontalScrollBarView? _hScrollBar;

    public ScrollSyncController(
        IScrollableContent content,
        VerticalScrollBarView vScrollBar,
        HorizontalScrollBarView? hScrollBar = null)
    {
        _content = content;
        _vScrollBar = vScrollBar;
        _hScrollBar = hScrollBar;

        _content.VerticalScrollPositionChanged += OnContentVerticalScroll;
        _vScrollBar.ScrollPositionChanged += _content.SetVerticalNormalizedScrollPosition;

        if (_hScrollBar != null)
        {
            _content.HorizontalScrollPositionChanged += OnContentHorizontalScroll;
            _hScrollBar.ScrollPositionChanged += _content.SetHorizontalNormalizedScrollPosition;
        }
    }

    public void Dispose()
    {
        _content.VerticalScrollPositionChanged -= OnContentVerticalScroll;
        _vScrollBar.ScrollPositionChanged -= _content.SetVerticalNormalizedScrollPosition;

        if (_hScrollBar != null)
        {
            _content.HorizontalScrollPositionChanged -= OnContentHorizontalScroll;
            _hScrollBar.ScrollPositionChanged -= _content.SetHorizontalNormalizedScrollPosition;
        }
    }

    private void OnContentVerticalScroll(float normalized)
        => ScrollBarSync.ApplyVertical(_vScrollBar, _content.VerticalScale, normalized);

    private void OnContentHorizontalScroll(float normalized)
        => ScrollBarSync.ApplyHorizontal(_hScrollBar!, _content.HorizontalScale, normalized);
}
