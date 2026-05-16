using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

public sealed class RepoBarContextMenuKbmController : KeyboardMouseController
{
    private readonly ContextMenu _menu;
    private readonly IOpenedContextMenu _opened;

    public RepoBarContextMenuKbmController(ContextMenu menu, IOpenedContextMenu opened)
    {
        _menu = menu;
        _opened = opened;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e) => _opened.CancelCloseRequest();

    public override void OnMouseExit(ref MouseExitEvent e) => _opened.CloseRequest();

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State != InputState.Pressed) return;
        if (_menu.Position.ContainsPoint(e.Mouse.Point)) return;
        _opened.CloseRequest();
    }
}
