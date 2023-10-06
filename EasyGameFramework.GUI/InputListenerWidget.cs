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
    private bool IsPointerHovering
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

    private int m_ScreenHeight;
    private IMouse? m_Mouse;

    protected override IWidget Build(IBuildContext context)
    {
        //Console.WriteLine("Build:InputHandlerWidget");
            
        var window = context.Get<IWindow>();
        m_ScreenHeight = window.ScreenHeight;
            
        var inputSystem = context.Get<IInputSystem>();
        m_Mouse = inputSystem.Mouse;
        m_Mouse.Moved += Mouse_OnMoved;
        m_Mouse.ButtonStateChanged += Mouse_OnButtonStateChanged;
            
        Child.ScreenRect = ScreenRect;

        m_IsHovered = ScreenRect.Contains(m_Mouse.ScreenX, m_ScreenHeight - m_Mouse.ScreenY);
        m_IsPressed = m_IsHovered && m_Mouse.IsButtonPressed(MouseButton.Left);
            
        return Child;
    }

    public override void Dispose()
    {
        if (m_Mouse != null)
        {
            m_Mouse.Moved -= Mouse_OnMoved;
            m_Mouse.ButtonStateChanged -= Mouse_OnButtonStateChanged;
        }
        base.Dispose();
    }

    private void Mouse_OnMoved(in MouseMovedEvent evt)
    {
        var mouse = evt.Mouse;
        IsPointerHovering = ScreenRect.Contains(mouse.ScreenX, m_ScreenHeight - mouse.ScreenY);
        if (IsPressed && !IsPointerHovering) IsPressed = false;
    }

    private void Mouse_OnButtonStateChanged(in MouseButtonStateChangedEvent evt)
    {
        var mouse = evt.Mouse;
        IsPressed = m_IsHovered && mouse.IsButtonPressed(MouseButton.Left);
    }
}