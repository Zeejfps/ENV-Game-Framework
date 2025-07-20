namespace ZGF.Gui.Tests;

public sealed class SpecialMenuItemController : IMenuItemController
{
    public SpecialMenuItemController(IMenuItem menuItem)
    {
        menuItem.Text = "Special";
        menuItem.IsDisabled = true;
    }

    public void Dispose()
    {

    }

    public void OnMouseEnter()
    {

    }

    public void OnMouseExit()
    {
    }
}