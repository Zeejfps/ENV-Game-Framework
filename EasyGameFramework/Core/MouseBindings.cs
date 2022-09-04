using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class MouseBindings : IMouseBindings
{
    private readonly Dictionary<MouseButton, string> m_ButtonToActionMap = new();

    public void BindButtonToAction(MouseButton button, string action)
    {
        m_ButtonToActionMap[button] = action;
    }

    public void UnbindButton(MouseButton button)
    {
        m_ButtonToActionMap.Remove(button);
    }

    public bool TryGetAction(MouseButton button, out string? action)
    {
        return m_ButtonToActionMap.TryGetValue(button, out action);
    }
}