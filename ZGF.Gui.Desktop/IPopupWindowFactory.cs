using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui.Desktop;

public interface IPopupWindowFactory : IDisposable
{
    IPopupWindow Acquire(in PopupRequest request);
    void Release(IPopupWindow popup);
}

public readonly struct PopupRequest
{
    /// <summary>
    /// Builds the popup's root view against the popup window's own per-window
    /// <see cref="Context"/> (canvas, input system, coordinates). Views are pinned to the
    /// window they are built for, so popup content is always built fresh, here.
    /// </summary>
    public required Func<Context, View> BuildRoot { get; init; }

    /// <summary>
    /// Computes the preferred (and optional flipped fallback) screen rect from the built
    /// root's measured size. The factory measures after building, then places.
    /// </summary>
    public required Func<int, int, (RectI Preferred, RectI? Flipped)> Place { get; init; }

    public required bool MousePassThrough { get; init; }
}

public interface IPopupWindow
{
    IWindow Window { get; }

    /// <summary>The popup window's build context — resolve per-window services
    /// (input system, coordinates) from here.</summary>
    Context Context { get; }

    /// <summary>The root view built by <see cref="PopupRequest.BuildRoot"/>.</summary>
    View? Root { get; }

    event Action<PointI> OutsideClick;
}
