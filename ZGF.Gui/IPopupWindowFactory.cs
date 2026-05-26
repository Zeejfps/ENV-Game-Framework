using ZGF.Core;
using ZGF.Geometry;
using ZGF.Observable;

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

    /// <summary>
    /// Optional reactive style sheet for the popup's root. When non-null, the popup factory
    /// applies the current value on acquire and re-applies on every change for as long as the
    /// popup is alive. Subscription is disposed on release. Live theme swaps reach open popups
    /// through this channel.
    /// </summary>
    public IReadable<StyleSheet>? Sheet { get; init; }
}

public interface IPopupWindow
{
    IWindow Window { get; }
    event Action<PointI> OutsideClick;
    void SetRoot(View root);
}
