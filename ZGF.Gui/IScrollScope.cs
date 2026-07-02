using ZGF.Geometry;

namespace ZGF.Gui;

/// <summary>
/// The ambient scrolling viewport a subtree lives in. A scroll container registers itself as
/// this service on its content's build context; interactive content asks it to bring a rect
/// (in absolute canvas coordinates) into view — a text editor keeping its caret visible, a
/// focus ring revealing the control it just focused. Implementations no-op when the rect
/// already fits, so callers can request a reveal after every interaction.
/// </summary>
public interface IScrollScope
{
    void EnsureVisible(RectF rect);
}
