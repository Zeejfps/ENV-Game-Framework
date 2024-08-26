using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace OpenGLSandbox;

public sealed class InputListenerWidget : Widget
{
    public Action? OnPointerEnter { get; set; }
    public Action? OnPointerExit { get; set; }
    public Action? OnPointerPressed { get; set; }
    public Action? OnPointerReleased { get; set; }
        
    public IWidget Child { get; init; }

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

    public IInputListenerController Controller { get; }

    private int m_ScreenHeight;
    private IMouse? m_Mouse;
    private FocusTree? m_Tree;

    public InputListenerWidget(IInputListenerController controller)
    {
        Controller = controller;
        m_IsHovered = controller.IsHovered;
        m_IsPressed = controller.IsPressed;
    }
    
    protected override IWidget Build(IBuildContext context)
    {
        m_Tree = context.Get<FocusTree>();
        m_Tree.Add(this);
        //Console.WriteLine("Build:InputHandlerWidget");
            
        var window = context.Get<IWindow>();
        m_ScreenHeight = window.ScreenHeight;
            
        // var inputSystem = context.Get<IInputSystem>();
        // m_Mouse = inputSystem.Mouse;
        // m_Mouse.Moved += Mouse_OnMoved;
        // m_Mouse.ButtonStateChanged += Mouse_OnButtonStateChanged;
            
        Child.ScreenRect = ScreenRect;

        // m_IsHovered = ScreenRect.Contains(m_Mouse.ScreenX, m_ScreenHeight - m_Mouse.ScreenY);
        // m_IsPressed = m_IsHovered && m_Mouse.IsButtonPressed(MouseButton.Left);

        Controller.Add(this);
        return Child;
    }

    public override void Dispose()
    {
        if (m_Tree != null)
        {
            m_Tree.Remove(this);
        }
        
        if (m_Mouse != null)
        {
            m_Mouse.Moved -= Mouse_OnMoved;
            m_Mouse.ButtonStateChanged -= Mouse_OnButtonStateChanged;
        }
        
        Controller.Remove(this);
        base.Dispose();
    }

    private void Mouse_OnMoved(in MouseMovedEvent evt)
    {
        var mouse = evt.Mouse;
        
    }

    private void Mouse_OnButtonStateChanged(in MouseButtonStateChangedEvent evt)
    {
        var mouse = evt.Mouse;
        IsPressed = m_IsHovered && mouse.IsButtonPressed(MouseButton.Left);
    }
}

public interface IInputListenerController
{
    bool IsHovered { get; set; }
    bool IsPressed { get; set; }
    void Add(InputListenerWidget widget);
    void Remove(InputListenerWidget widget);
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

    private readonly HashSet<InputListenerWidget> m_Widgets = new();

    public void Add(InputListenerWidget widget)
    {
        m_Widgets.Add(widget);
    }

    public void Remove(InputListenerWidget widget)
    {
        m_Widgets.Remove(widget);
    }
}