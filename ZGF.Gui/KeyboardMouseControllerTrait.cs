namespace ZGF.Gui;

public static class KeyboardMouseControllerTrait
{
    public static void RequestFocus(this IKeyboardMouseController controller)
    {
        controller.Component.Context?.InputSystem.RequestFocus(controller);
    }
    
    public static void Blur(this IKeyboardMouseController controller)
    {
        controller.Component.Context?.InputSystem.Blur(controller);
    }
}