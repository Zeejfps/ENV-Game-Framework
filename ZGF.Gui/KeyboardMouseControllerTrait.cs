namespace ZGF.Gui;

public static class KeyboardMouseControllerTrait
{
    public static void RequestFocus(this IKeyboardMouseController controller)
    {
        controller.View.Context?.InputSystem.RequestFocus(controller);
    }
    
    public static void Blur(this IKeyboardMouseController controller)
    {
        controller.View.Context?.InputSystem.Blur(controller);
    }
    
    public static void RegisterController(this IKeyboardMouseController controller, Context context)
    {
        context.InputSystem.AddInteractable(controller);
    }
    
    public static void UnregisterController(this IKeyboardMouseController controller, Context context)
    {
        context.InputSystem.RemoveInteractable(controller);
    }
}