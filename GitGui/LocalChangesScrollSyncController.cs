using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal sealed class LocalChangesScrollSyncController : KeyboardMouseController
{
    private readonly ScrollPane _pane;
    private readonly VerticalScrollBarView _vScrollBar;

    public LocalChangesScrollSyncController(ScrollPane pane, VerticalScrollBarView vScrollBar)
    {
        _pane = pane;
        _vScrollBar = vScrollBar;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged += OnPaneVerticalScroll;
        _vScrollBar.ScrollPositionChanged += _pane.SetVerticalNormalizedScrollPosition;
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged -= OnPaneVerticalScroll;
        _vScrollBar.ScrollPositionChanged -= _pane.SetVerticalNormalizedScrollPosition;
    }

    private void OnPaneVerticalScroll(float normalized)
        => ScrollBarSync.ApplyVertical(_vScrollBar, _pane.VerticalScale, normalized);
}