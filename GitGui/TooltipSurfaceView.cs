using ZGF.Geometry;
using ZGF.Gui;

namespace GitGui;

public sealed class TooltipSurfaceView : MultiChildView, ITooltipService
{
    private object? _currentOwner;
    private TooltipView? _currentView;

    public TooltipSurfaceView()
    {
        ZIndex = 2000;
    }

    public void Show(object owner, string text, RectF anchorRect)
    {
        if (_currentView != null)
        {
            Children.Remove(_currentView);
            _currentView = null;
        }

        _currentOwner = owner;
        _currentView = new TooltipView(text)
        {
            AnchorRect = anchorRect,
        };
        Children.Add(_currentView);
    }

    public void Hide(object owner)
    {
        if (!ReferenceEquals(_currentOwner, owner)) return;
        if (_currentView != null)
        {
            Children.Remove(_currentView);
            _currentView = null;
        }
        _currentOwner = null;
    }
}
