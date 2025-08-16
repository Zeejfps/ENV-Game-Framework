namespace ZGF.Gui.Tests;

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

    public override View View => _contextMenu.View;
}