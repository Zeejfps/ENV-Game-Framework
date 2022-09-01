using System.Diagnostics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.OpenGL;
using GLFW;
using OpenGL;
using static GLFW.Glfw;
using CursorMode = EasyGameFramework.Api.CursorMode;
using Monitor = GLFW.Monitor;

namespace EasyGameFramework.Glfw;

public class Window_GLFW : IWindow
{
    private readonly IDisplays m_Displays;
    private readonly SizeCallback m_FramebufferSizeCallback;
    private readonly IInput m_Input;

    private readonly KeyCallback m_KeyCallback;
    private readonly MouseButtonCallback m_MouseButtonCallback;
    private readonly MouseCallback m_MousePositionCallback;
    private readonly PositionCallback m_PositionCallback;
    private readonly MouseCallback m_ScrollCallback;
    private readonly SizeCallback m_SizeCallback;

    private readonly WindowFramebuffer_GL m_WindowFramebuffer;

    private Window m_Handle;
    private int m_Height = 480;
    private bool m_IsFullscreen;

    private bool m_IsResizable;
    private bool m_IsVsyncEnabled;

    private int m_PosX;
    private int m_PosY;

    private string m_Title = "Untitled";

    private int m_Width = 640;

    public Window_GLFW(IDisplays displays, IInput input)
    {
        Init();
        WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        WindowHint(Hint.ContextVersionMajor, 3);
        WindowHint(Hint.ContextVersionMinor, 3);
        WindowHint(Hint.OpenglProfile, Profile.Core);
        WindowHint(Hint.Doublebuffer, true);
        WindowHint(Hint.Decorated, true);

        m_Displays = displays;
        m_Input = input;

        m_KeyCallback = Glfw_KeyCallback;
        m_SizeCallback = Glfw_SizeCallback;
        m_MousePositionCallback = Glfw_MousePosCallback;
        m_PositionCallback = Glfw_PositionCallback;
        m_FramebufferSizeCallback = Glfw_FramebufferSizeCallback;
        m_MouseButtonCallback = Glfw_MouseButtonCallback;
        m_ScrollCallback = Glfw_MouseScrollCallback;

        WindowHint(Hint.Visible, false);
        WindowHint(Hint.Resizable, m_IsResizable);

        m_Handle = CreateWindow(Width, Height, Title, Monitor.None, Window.None);
        MakeContextCurrent(m_Handle);
        GetFramebufferSize(m_Handle, out var framebufferWidth, out var framebufferHeight);
        Gl.Import(GetProcAddress);
        m_WindowFramebuffer = new WindowFramebuffer_GL(framebufferWidth, framebufferHeight);

        SetWindowSizeCallback(m_Handle, m_SizeCallback);
        SetWindowPositionCallback(m_Handle, m_PositionCallback);
        SetFramebufferSizeCallback(m_Handle, m_FramebufferSizeCallback);
        SetKeyCallback(m_Handle, m_KeyCallback);
        SetCursorPositionCallback(m_Handle, m_MousePositionCallback);
        SetMouseButtonCallback(m_Handle, m_MouseButtonCallback);
        SetScrollCallback(m_Handle, m_ScrollCallback);
    }

    public int PosX
    {
        get => m_PosX;
        set
        {
            if (m_PosX == value)
                return;

            m_PosX = value;
            UpdateWindowPos();
        }
    }

    public int PosY
    {
        get => m_PosY;
        set
        {
            if (m_PosY == value)
                return;

            m_PosY = value;
            UpdateWindowPos();
        }
    }

    public int Width
    {
        get => m_Width;
        set
        {
            if (m_Width == value)
                return;

            m_Width = value;
            UpdateWindowSize();
        }
    }

    public int Height
    {
        get => m_Height;
        set
        {
            if (m_Height == value)
                return;

            m_Height = value;
            UpdateWindowSize();
        }
    }

    public string Title
    {
        get => m_Title;
        set
        {
            if (m_Title == value)
                return;
            m_Title = value;
            SetWindowTitle(m_Handle, m_Title);
        }
    }

    public bool IsResizable
    {
        get => m_IsResizable;
        set
        {
            if (m_IsResizable == value)
                return;
            m_IsResizable = value;
            SetWindowAttribute(m_Handle, WindowAttribute.Resizable, m_IsResizable);
        }
    }

    public bool IsOpened { get; private set; }

    public IGpuFramebuffer Framebuffer => m_WindowFramebuffer;

    private CursorMode m_CursorMode;

    public CursorMode CursorMode
    {
        get => m_CursorMode;
        set
        {
            m_CursorMode = value;
            switch (m_CursorMode)
            {
                case CursorMode.Visible:
                    SetInputMode(m_Handle, InputMode.Cursor, CURSOR_NORMAL);
                    break;
                case CursorMode.Hidden:
                    SetInputMode(m_Handle, InputMode.Cursor, CURSOR_HIDDEN);
                    break;
                case CursorMode.HiddenAndLocked:
                    SetInputMode(m_Handle, InputMode.Cursor, CURSOR_DISABLED);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public bool IsFullscreen
    {
        get => m_IsFullscreen;
        set
        {
            if (m_IsFullscreen == value)
                return;
            m_IsFullscreen = value;
            UpdateFullscreenState();
        }
    }

    public bool IsVsyncEnabled
    {
        get => m_IsVsyncEnabled;
        set
        {
            if (m_IsVsyncEnabled == value)
                return;
            m_IsVsyncEnabled = value;
            UpdateVsyncState();
        }
    }

    public void Show()
    {
        ShowWindow(m_Handle);
        IsOpened = true;
    }

    public void ShowCentered()
    {
        PosX = (int)((m_Displays.PrimaryDisplay.ResolutionX - Width) * 0.5f);
        PosY = (int)((m_Displays.PrimaryDisplay.ResolutionY - Height) * 0.5f);
        Show();
    }

    public void Hide()
    {
        Debug.Assert(IsOpened);
        Debug.Assert(m_Handle != Window.None);
        SetWindowShouldClose(m_Handle, true);

        m_Handle = default;
        IsOpened = false;
    }

    public void Update()
    {
        Debug.Assert(IsOpened);
        Debug.Assert(m_Handle != Window.None);
        
        PollEvents();

        if (WindowShouldClose(m_Handle))
        {
            IsOpened = false;
            return;
        }
        
        GetCursorPosition(m_Handle, out var x, out var y);
        m_Input.Mouse.ScreenX = (int)x;
        m_Input.Mouse.ScreenY = (int)y;
        
        SwapBuffers(m_Handle);
    }

    public void SetSize(int width, int height)
    {
        m_Width = width;
        m_Height = height;
        UpdateWindowSize();
    }

    public void SetPosition(int x, int y)
    {
        m_PosX = x;
        m_PosY = y;
        UpdateWindowPos();
    }

    private void UpdateWindowSize()
    {
        if (m_Handle != Window.None)
            SetWindowSize(m_Handle, m_Width, m_Height);
    }

    private void UpdateWindowPos()
    {
        if (m_Handle != Window.None)
            SetWindowPosition(m_Handle, m_PosX, m_PosY);
    }

    private void UpdateVsyncState()
    {
        if (m_Handle != Window.None)
            SwapInterval(m_IsVsyncEnabled ? 1 : 0);
    }

    private void UpdateFullscreenState()
    {
        if (m_Handle == Window.None)
            return;

        var primaryMonitor = PrimaryMonitor;
        var videoMode = GetVideoMode(primaryMonitor);

        if (IsFullscreen)
        {
            var workArea = primaryMonitor.WorkArea;
            SetWindowMonitor(m_Handle, primaryMonitor, workArea.X, workArea.Y, workArea.Width, workArea.Height,
                videoMode.RefreshRate);
        }
        else
        {
            SetWindowMonitor(m_Handle, Monitor.None, m_PosX, m_PosY, m_Width, m_Height, videoMode.RefreshRate);
        }
    }

    private void Glfw_SizeCallback(Window _, int width, int height)
    {
        if (IsFullscreen)
            return;

        m_Width = width;
        m_Height = height;
    }

    private void Glfw_PositionCallback(Window _, int x, int y)
    {
        if (IsFullscreen)
            return;

        m_PosX = x;
        m_PosY = y;
    }

    private void Glfw_FramebufferSizeCallback(Window window, int width, int height)
    {
        m_WindowFramebuffer.SetSize(width, height);
    }

    private void Glfw_MousePosCallback(Window window, double x, double y)
    {
        var mouse = m_Input.Mouse;
        mouse.ScreenX = (int)x;
        mouse.ScreenY = (int)y;
    }

    private void Glfw_MouseButtonCallback(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
    {
        var mouse = m_Input.Mouse;
        var mouseButton = MapToMouseButton(button);
        switch (state)
        {
            case InputState.Release:
                mouse.ReleaseButton(mouseButton);
                break;
            case InputState.Press:
                mouse.PressButton(mouseButton);
                break;
            case InputState.Repeat:
                mouse.PressButton(mouseButton);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void Glfw_KeyCallback(Window window, Keys glfwKey, int scancode, InputState state, ModifierKeys mods)
    {
        var keyboard = m_Input.Keyboard;
        var key = glfwKey.ToKeyboardKey();
        switch (state)
        {
            case InputState.Release:
                keyboard.ReleaseKey(key);
                break;
            case InputState.Press:
                keyboard.PressKey(key);
                break;
            case InputState.Repeat:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void Glfw_MouseScrollCallback(Window window, double x, double y)
    {
        var mouse = m_Input.Mouse;
        mouse.ScrollDeltaX = (float)x;
        mouse.ScrollDeltaX = (float)y;
    }

    public override string ToString()
    {
        return $"x{PosX} y{PosY}, {Width}x{Height}";
    }

    private EasyGameFramework.Api.InputDevices.MouseButton MapToMouseButton(MouseButton mouseButton)
    {
        switch (mouseButton)
        {
            case MouseButton.Left:
                return EasyGameFramework.Api.InputDevices.MouseButton.Left;
            case MouseButton.Right:
                return EasyGameFramework.Api.InputDevices.MouseButton.Right;
            case MouseButton.Middle:
                return EasyGameFramework.Api.InputDevices.MouseButton.Middle;
            case MouseButton.Button4:
            case MouseButton.Button5:
            case MouseButton.Button6:
            case MouseButton.Button7:
            case MouseButton.Button8:
                return new EasyGameFramework.Api.InputDevices.MouseButton((int)mouseButton);
            default:
                throw new ArgumentOutOfRangeException(nameof(mouseButton), mouseButton, null);
        }
    }
}