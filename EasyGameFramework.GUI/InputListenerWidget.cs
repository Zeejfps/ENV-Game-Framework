using EasyGameFramework.Api.InputDevices;

namespace OpenGLSandbox;

public sealed class InputListenerWidget : Widget
{
    public Action? OnPointerEnter { get; set; }
    public Action? OnPointerExit { get; set; }
    public Action? OnPointerPressed { get; set; }
    public Action? OnPointerReleased { get; set; }
    public Action<KeyboardKey>? OnKeyPressed { get; set; }
    public Action? OnFocusGained { get; set; }
    public Action? OnFocusLost { get; set; }

    public IWidget? Child { get; init; }

    private bool m_IsHovered;
    public bool IsPointerHovering
    {
        get => m_IsHovered;
        set
        {
            if (m_IsHovered == value)
                return;
            m_IsHovered = value;
            if (m_IsHovered)
                OnPointerEnter?.Invoke();
            else
                OnPointerExit?.Invoke();
        }
    }

    private bool m_IsPressed;
    public bool IsPressed
    {
        get => m_IsPressed;
        set
        {
            if (m_IsPressed == value)
                return;
            m_IsPressed = value;
            if(m_IsPressed)
                OnPointerPressed?.Invoke();
            else
                OnPointerReleased?.Invoke();
        }
    }

    private bool m_IsFocused;

    public bool IsFocused
    {
        get => m_IsFocused;
        set
        {
            if (m_IsFocused == value)
                return;
            m_IsFocused = value;
            if (m_IsFocused)
                OnFocusGained?.Invoke();
            else
                OnFocusLost?.Invoke();
        }
    }
    
    public IInputListenerController Controller { get; }

    private FocusTree? m_Tree;

    public InputListenerWidget(IInputListenerController controller)
    {
        Controller = controller;
        m_IsFocused = controller.IsFocused;
        m_IsHovered = controller.IsHovered;
        m_IsPressed = controller.IsPressed;
    }
    
    protected override IWidget Build(IBuildContext context)
    {
        m_Tree = context.FocusTree;
        m_Tree.Add(this);
        Controller.Add(this);
        
        if (Child != null)
            Child.ScreenRect = ScreenRect;
        
        return Child;
    }

    public override void Dispose()
    {
        if (m_Tree != null)
        {
            m_Tree.Remove(this);
        }
        
        Controller.Remove(this);
        base.Dispose();
    }
}

public interface IInputListenerController
{
    bool IsHovered { get; set; }
    bool IsPressed { get; set; }
    bool IsFocused { get; set; }
    void Add(InputListenerWidget widget);
    void Remove(InputListenerWidget widget);
    void PressKey(KeyboardKey evtKey);
}

public sealed class InputListenerController : IInputListenerController
{
    private bool m_IsHovered;
    public bool IsHovered
    {
        get => m_IsHovered;
        set
        {
            if (m_IsHovered == value)
                return;
            m_IsHovered = value;
            foreach (var widget in m_Widgets)
                widget.IsPointerHovering = m_IsHovered;
        }
    }

    private bool m_IsPressed;
    public bool IsPressed
    {
        get => m_IsPressed;
        set
        {
            if (m_IsPressed == value)
                return;
            m_IsPressed = value;
            foreach (var widget in m_Widgets)
                widget.IsPressed = m_IsPressed;
        }
    }

    private bool m_IsFocused;

    public bool IsFocused
    {
        get => m_IsFocused;
        set
        {
            if (m_IsFocused == value)
                return;

            m_IsFocused = value;
            foreach (var widget in m_Widgets)
                widget.IsFocused = m_IsFocused;
        }
    }

    private readonly HashSet<InputListenerWidget> m_Widgets = new();

    public void Add(InputListenerWidget widget)
    {
        m_Widgets.Add(widget);
    }

    public void Remove(InputListenerWidget widget)
    {
        m_Widgets.Remove(widget);
    }

    public void PressKey(KeyboardKey key)
    {
        foreach (var widget in m_Widgets)
        {
            widget.OnKeyPressed?.Invoke(key);
        }
    }
}