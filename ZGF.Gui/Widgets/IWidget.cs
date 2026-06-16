namespace ZGF.Gui.Widgets;

/// <summary>
/// A window-agnostic, immutable description of UI. <see cref="BuildView"/> turns it into a
/// retained <see cref="View"/> wired against one window's <see cref="Context"/>. Components are
/// shareable and rebuildable; built Views belong to the window whose context built them.
/// </summary>
public interface IWidget
{
    View BuildView(Context ctx);
}