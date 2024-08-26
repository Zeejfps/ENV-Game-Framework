using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace OpenGLSandbox;

public class FocusTree
{
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
        IInputListenerController? focusedWidget = null;
        var screenHeight = m_Window.ScreenHeight;
        foreach (var widget in m_InputListenerWidgets.Reverse())
        {
            var isHovered = widget.ScreenRect.Contains(m_Mouse.ScreenX, screenHeight - m_Mouse.ScreenY);
            if (isHovered)
            {
                focusedWidget = widget.Controller;
                break;
            }
        }

        var prevFocusedWidget = m_FocusedWidget;
        m_FocusedWidget = focusedWidget;

        if (prevFocusedWidget == m_FocusedWidget)
        {
            return;
        }
        
        if (prevFocusedWidget != null)
        {
            prevFocusedWidget.IsFocused = false;
            prevFocusedWidget.IsHovered = false;
            prevFocusedWidget.IsPressed = false;
        }

        if (m_FocusedWidget != null)
        {
            m_FocusedWidget.IsFocused = true;
            m_FocusedWidget.IsHovered = true;
        }
    }
    
    public void Add(InputListenerWidget listener)
    {
        m_InputListenerWidgets.AddLast(listener);
    }
    
    public void Remove(InputListenerWidget listener)
    {
        m_InputListenerWidgets.Remove(listener);
    }
}