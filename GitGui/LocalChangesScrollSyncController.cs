using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal sealed class LocalChangesScrollSyncController : KeyboardMouseController
{
    private readonly ScrollPane _pane;
    private readonly VerticalScrollBarView _vScrollBar;
    private readonly HorizontalScrollBarView? _hScrollBar;

    public LocalChangesScrollSyncController(
        ScrollPane pane,
        VerticalScrollBarView vScrollBar,
        HorizontalScrollBarView? hScrollBar = null)
    {
        _pane = pane;
        _vScrollBar = vScrollBar;
        _hScrollBar = hScrollBar;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged += OnPaneVerticalScroll;
        _vScrollBar.ScrollPositionChanged += _pane.SetVerticalNormalizedScrollPosition;
        if (_hScrollBar != null)
        {
            _pane.HorizontalScrollPositionChanged += OnPaneHorizontalScroll;
            _hScrollBar.ScrollPositionChanged += _pane.SetHorizontalNormalizedScrollPosition;
        }
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged -= OnPaneVerticalScroll;
        _vScrollBar.ScrollPositionChanged -= _pane.SetVerticalNormalizedScrollPosition;
        if (_hScrollBar != null)
        {
            _pane.HorizontalScrollPositionChanged -= OnPaneHorizontalScroll;
            _hScrollBar.ScrollPositionChanged -= _pane.SetHorizontalNormalizedScrollPosition;
        }
    }

    private void OnPaneVerticalScroll(float normalized)
        => ScrollBarSync.ApplyVertical(_vScrollBar, _pane.VerticalScale, normalized);

    private void OnPaneHorizontalScroll(float normalized)
        => ScrollBarSync.ApplyHorizontal(_hScrollBar!, _pane.HorizontalScale, normalized);
}