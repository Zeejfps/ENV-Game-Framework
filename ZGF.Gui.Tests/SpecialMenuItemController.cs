namespace ZGF.Gui.Tests;

public sealed class SpecialMenuItemController : IKeyboardMouseController
{
    private readonly MenuItem _menuItem;

    public SpecialMenuItemController(MenuItem menuItem)
    {
        _menuItem = menuItem;
        menuItem.Text = "Special";
        menuItem.IsDisabled = true;
    }
    
    public Component Component => _menuItem;
    
    public void OnEnabled(Context context)
    {
    }

    public void OnDisabled(Context context)
    {
    }
}