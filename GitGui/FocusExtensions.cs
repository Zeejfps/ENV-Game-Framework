using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal static class FocusExtensions
{
    public static void RequestFocus(this Context? context, IKeyboardMouseController controller)
        => context?.Get<InputSystem>()?.RequestFocus(controller);

    public static void Blur(this Context? context, IKeyboardMouseController controller)
        => context?.Get<InputSystem>()?.Blur(controller);

    public static void StealFocus(this Context? context, IKeyboardMouseController controller)
        => context?.Get<InputSystem>()?.StealFocus(controller);
}
