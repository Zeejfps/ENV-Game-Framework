namespace ZGF.Gui;

public sealed class EventSystem
{
    public static EventSystem Instance { get; } = new();

    public void AddMouseListener(Component component, IMouseListener mouseListener)
    {
        
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