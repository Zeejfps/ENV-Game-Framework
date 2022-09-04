using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class KeyboardBindings : IKeyboardBindings
{
    private readonly Dictionary<KeyboardKey, string> m_KeyToActionMap = new();

    public bool TryGetAction(KeyboardKey key, out string? action)
    {
        return m_KeyToActionMap.TryGetValue(key, out action);
    }

    public void BindKeyToAction(KeyboardKey key, string actionName)
    {
        m_KeyToActionMap[key] = actionName;
    }

    public void UnbindKey(KeyboardKey key)
    {
        m_KeyToActionMap.Remove(key);
    }
}