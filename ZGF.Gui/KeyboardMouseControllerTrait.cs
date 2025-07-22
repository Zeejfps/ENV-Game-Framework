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
    
    public static void RegisterController(this IKeyboardMouseController controller)
    {
        var context = controller.Component.Context;
        context?.InputSystem.AddInteractable(controller);
    }
    
    public static void UnregisterController(this IKeyboardMouseController controller)
    {
        var context = controller.Component.Context;
        context?.InputSystem.RemoveInteractable(controller);
    }
}