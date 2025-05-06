using GLFW;
using WindowHandle = GLFW.Window;

namespace NodeGraphApp;

public sealed class GlfwKeyboardController
{
    private readonly Keyboard _keyboard;
    private readonly KeyCallback _keyCallback;
    
    public GlfwKeyboardController(WindowHandle window, Keyboard keyboard)
    {
        _keyboard = keyboard;
        _keyCallback = (_, key, _, state, _) =>
        {
            if (state == InputState.Press)
            {
                keyboard.PressKey(key);
            }
            else if (state == InputState.Release)
            {
                keyboard.ReleaseKey(key);
            }
        };
        Glfw.SetKeyCallback(window, _keyCallback);
    }

    public void Update()
    {
        _keyboard.Update();
    }
}