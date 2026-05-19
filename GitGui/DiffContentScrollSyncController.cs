using ZGF.Gui.Tests;

namespace GitGui;

internal sealed class DiffContentScrollSyncController : KeyboardMouseController, IDisposable
{
    private readonly DiffContentView _content;
    private readonly VerticalScrollBarView _vScrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;

    public DiffContentScrollSyncController(
        DiffContentView content,
        VerticalScrollBarView vScrollBar,
        HorizontalScrollBarView hScrollBar)
    {
        _content = content;
        _vScrollBar = vScrollBar;
        _hScrollBar = hScrollBar;

        _content.VerticalScrollPositionChanged += OnContentVerticalScroll;
        _content.HorizontalScrollPositionChanged += OnContentHorizontalScroll;
        _vScrollBar.ScrollPositionChanged += _content.SetVerticalNormalizedScrollPosition;
        _hScrollBar.ScrollPositionChanged += _content.SetHorizontalNormalizedScrollPosition;
    }

    public void Dispose()
    {
        _content.VerticalScrollPositionChanged -= OnContentVerticalScroll;
        _content.HorizontalScrollPositionChanged -= OnContentHorizontalScroll;
        _vScrollBar.ScrollPositionChanged -= _content.SetVerticalNormalizedScrollPosition;
        _hScrollBar.ScrollPositionChanged -= _content.SetHorizontalNormalizedScrollPosition;
    }

    private void OnContentVerticalScroll(float normalized)
        => ScrollBarSync.ApplyVertical(_vScrollBar, _content.VerticalScale, normalized);

    private void OnContentHorizontalScroll(float normalized)
        => ScrollBarSync.ApplyHorizontal(_hScrollBar, _content.HorizontalScale, normalized);
}