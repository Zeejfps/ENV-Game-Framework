namespace ZGF.Gui;

public sealed class EventSystem
{
    public static EventSystem Instance { get; } = new();
    
    private Dictionary<Component, IMouseListener> _mouseListeners = new();

    public void AddMouseListener(Component component, IMouseListener mouseListener)
    {
        _mouseListeners.Add(component, mouseListener);
    }

    public void Update()
    {
        
    }
}

public interface IMouseListener
{
    void OnMouseEnter();
    void OnMouseExit();
}