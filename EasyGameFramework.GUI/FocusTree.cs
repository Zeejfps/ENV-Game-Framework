using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.GUI;

public class FocusTree
{
    private IInputListenerController? m_HoveredWidget;
    private IInputListenerController? m_FocusedWidget;
    private IInputSystem m_InputSystem;
    private readonly LinkedList<InputListenerWidget> m_InputListenerWidgets = new();
    private IMouse m_Mouse;
    private IWindow m_Window;
    
    public FocusTree(IInputSystem inputSystem, IWindow window)
    {
        m_InputSystem = inputSystem;
        var mouse = inputSystem.Mouse;
        mouse.Moved += Mouse_OnMoved;
        mouse.ButtonStateChanged += Mouse_OnButtonStateChanged;

        var keyboard = inputSystem.Keyboard;
        keyboard.KeyPressed += Keyboard_OnKeyPressed;
        
        m_Mouse = mouse;
        m_Window = window;
    }

    private void Keyboard_OnKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        m_FocusedWidget?.PressKey(evt.Key);
    }

    private void Mouse_OnButtonStateChanged(in MouseButtonStateChangedEvent evt)
    {
        if (m_FocusedWidget == null)
            return;
        
        m_FocusedWidget.IsPressed = evt.Mouse.IsButtonPressed(MouseButton.Left);
    }

    private void Mouse_OnMoved(in MouseMovedEvent evt)
    {
        UpdatePointerPosition(evt.Mouse.ScreenX, evt.Mouse.ScreenY);
    }

    public void UpdatePointerPosition(int x, int y)
    {
        IInputListenerController? hoveredWidget = null;
        var screenHeight = m_Window.ScreenHeight;
        foreach (var widget in m_InputListenerWidgets.Reverse())
        {
            var isHovered = widget.ScreenRect.Contains(m_Mouse.ScreenX, screenHeight - m_Mouse.ScreenY);
            if (isHovered)
            {
                hoveredWidget = widget.Controller;
                break;
            }
        }

        var prevHoveredWidget = m_HoveredWidget;
        m_HoveredWidget = hoveredWidget;

        if (prevHoveredWidget == m_FocusedWidget)
        {
            return;
        }
        
        if (prevHoveredWidget != null)
        {
            prevHoveredWidget.IsHovered = false;
            prevHoveredWidget.IsPressed = false;
        }

        if (m_HoveredWidget != null)
        {
            m_HoveredWidget.IsHovered = true;
        }
    }
    
    public void Add(InputListenerWidget listener)
    {
        m_InputListenerWidgets.AddLast(listener);
        if (listener.IsFocused)
            m_FocusedWidget = listener.Controller;
    }
    
    public void Remove(InputListenerWidget listener)
    {
        m_InputListenerWidgets.Remove(listener);
    }
}