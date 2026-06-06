using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.ContextMenu;

public sealed class ContextMenuKbmController : KeyboardMouseController
{
    private readonly IOpenedContextMenu _contextMenu;

    public ContextMenuKbmController(IOpenedContextMenu contextMenu)
    {
        _contextMenu = contextMenu;
    }
    
    
    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        _contextMenu.CancelCloseRequest();
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _contextMenu.CloseRequest();
    }
}