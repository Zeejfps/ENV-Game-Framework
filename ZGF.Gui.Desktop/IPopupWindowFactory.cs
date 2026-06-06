using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui;

public interface IPopupWindowFactory : IDisposable
{
    IPopupWindow Acquire(in PopupRequest request);
    void Release(IPopupWindow popup);
}

public readonly struct PopupRequest
{
    public required View Root { get; init; }
    public required RectI PreferredScreenRect { get; init; }
    public required RectI? FlippedScreenRect { get; init; }
    public required bool MousePassThrough { get; init; }
}

public interface IPopupWindow
{
    IWindow Window { get; }
    event Action<PointI> OutsideClick;
    void SetRoot(View root);
}
