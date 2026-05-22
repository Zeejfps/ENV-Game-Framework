using ZGF.Geometry;
using ZGF.Gui;

namespace GitGui;

public sealed class PopupTooltipService : ITooltipService
{
    private const int Gap = 8;

    private readonly IPopupWindowFactory _factory;
    private readonly IWindowCoordinates _coordinates;
    private object? _currentOwner;
    private IPopupWindow? _currentPopup;

    public PopupTooltipService(IPopupWindowFactory factory, IWindowCoordinates coordinates)
    {
        _factory = factory;
        _coordinates = coordinates;
    }

    public void Show(object owner, string text, RectF anchorRectCanvas)
    {
        Hide(owner);

        var view = new TooltipView(text);
        var anchorScreen = _coordinates.ToScreenPoints(anchorRectCanvas);

        var width = (int)MathF.Ceiling(view.MeasureWidth());
        var height = (int)MathF.Ceiling(view.MeasureHeight(width));

        var centerX = anchorScreen.X + anchorScreen.Width / 2;
        var preferred = new RectI(
            X: centerX - width / 2,
            Y: anchorScreen.Y + anchorScreen.Height + Gap,
            Width: width, Height: height);
        var flipped = new RectI(
            X: centerX - width / 2,
            Y: anchorScreen.Y - Gap - height,
            Width: width, Height: height);

        _currentOwner = owner;
        _currentPopup = _factory.Acquire(new PopupRequest
        {
            Root = view,
            PreferredScreenRect = preferred,
            FlippedScreenRect = flipped,
            MousePassThrough = true,
        });
    }

    public void Hide(object owner)
    {
        if (!ReferenceEquals(_currentOwner, owner)) return;
        if (_currentPopup != null) _factory.Release(_currentPopup);
        _currentPopup = null;
        _currentOwner = null;
    }
}
