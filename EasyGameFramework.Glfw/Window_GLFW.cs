using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;
using EasyGameFramework.Core.InputDevices;
using EasyGameFramework.OpenGL;
using GLFW;
using OpenGL;
using static GLFW.Glfw;
using CursorMode = EasyGameFramework.Api.CursorMode;
using Monitor = GLFW.Monitor;
using MouseButton = GLFW.MouseButton;

namespace EasyGameFramework.Glfw;

public sealed class Window_GLFW : IWindow, IDisposable
{
    public event Action? Paint;
    public event Action? Closed;

    private readonly IDisplayManager m_DisplayManager;
    private readonly SizeCallback m_FramebufferSizeCallback;

    private readonly KeyCallback m_KeyCallback;
    private readonly MouseButtonCallback m_MouseButtonCallback;
    private readonly MouseCallback m_MousePositionCallback;
    private readonly PositionCallback m_PositionCallback;
    private readonly MouseCallback m_ScrollCallback;
    private readonly SizeCallback m_SizeCallback;
    private readonly JoystickCallback m_JoystickCallback;
    private readonly FocusCallback m_FocusCallback;

    private readonly WindowFramebuffer_GL m_WindowFramebuffer;

    private Window m_Handle;
    private bool m_IsFullscreen;

    private bool m_IsResizable;
    private bool m_IsVsyncEnabled;

    private string m_Title = "Untitled";
    private int m_PosX;
    private int m_PosY;
    private int m_ViewportWidth = 640;
    private int m_ViewportHeight = 480;
    private int m_MouseX;
    private int m_MouseY;
    
    private ILogger Logger { get; }
    private IMouse Mouse { get; }
    private IKeyboard Keyboard { get; }

    private readonly Dictionary<IGamepad, int> m_GamepadToSlotMap = new();

    public Window_GLFW(
        ILogger logger, 
        IDisplayManager displayManager,
        IInputSystem input)
    {
        Init();
        WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        WindowHint(Hint.ContextVersionMajor, 4);
        WindowHint(Hint.ContextVersionMinor, 6);
        WindowHint(Hint.OpenglProfile, Profile.Core);
        WindowHint(Hint.Doublebuffer, true);
        WindowHint(Hint.Decorated, true);

        Logger = logger;
        Input = input;
        Mouse = input.Mouse;
        Keyboard = input.Keyboard;
        
        m_DisplayManager = displayManager;

        m_KeyCallback = Glfw_KeyCallback;
        m_SizeCallback = Glfw_SizeCallback;
        m_MousePositionCallback = Glfw_MousePosCallback;
        m_PositionCallback = Glfw_PositionCallback;
        m_FramebufferSizeCallback = Glfw_FramebufferSizeCallback;
        m_MouseButtonCallback = Glfw_MouseButtonCallback;
        m_ScrollCallback = Glfw_MouseScrollCallback;
        m_JoystickCallback = Glfw_JoystickCallback;
        m_FocusCallback = Glfw_FocusCallback;

        WindowHint(Hint.Visible, false);
        WindowHint(Hint.Resizable, m_IsResizable);

        m_Handle = CreateWindow(ScreenWidth, ScreenHeight, Title, Monitor.None, Window.None);
        MakeContextCurrent(m_Handle);
        GetFramebufferSize(m_Handle, out var framebufferWidth, out var framebufferHeight);
        Gl.Import(GetProcAddress);
        GL46.Import(GetProcAddress);
        m_WindowFramebuffer = new WindowFramebuffer_GL(framebufferWidth, framebufferHeight);

        SetWindowSizeCallback(m_Handle, m_SizeCallback);
        SetWindowPositionCallback(m_Handle, m_PositionCallback);
        SetFramebufferSizeCallback(m_Handle, m_FramebufferSizeCallback);
        SetKeyCallback(m_Handle, m_KeyCallback);
        SetCursorPositionCallback(m_Handle, m_MousePositionCallback);
        SetMouseButtonCallback(m_Handle, m_MouseButtonCallback);
        SetScrollCallback(m_Handle, m_ScrollCallback);
        SetWindowFocusCallback(m_Handle, m_FocusCallback);
        SetJoystickCallback(m_JoystickCallback);

        for (var i = 0; i < 16; i++)
        {
            var joystick = (Joystick)i;
            if (JoystickPresent(joystick) && JoystickIsGamepad(joystick))
                ConnectGamepad(joystick);
        }

        Gpu = new Gpu_GL(Framebuffer);
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
    
    public int ScreenWidth
    {
        get => m_ViewportWidth;
        set
        {
            if (m_ViewportWidth == value)
                return;

            m_ViewportWidth = value;
            UpdateScreenSize();
        }
    }

    public int ScreenHeight
    {
        get => m_ViewportHeight;
        set
        {
            if (m_ViewportHeight == value)
                return;

            m_ViewportHeight = value;
            UpdateScreenSize();
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
    public bool IsFocused { get; private set; }

    private IGpuFramebuffer Framebuffer => m_WindowFramebuffer;

    private CursorMode m_CursorMode;

    public CursorMode CursorMode
    {
        get => m_CursorMode;
        set
        {
            if (m_CursorMode == value)
                return;
            
            if (IsOpened)
                PollEvents();
            
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

    public IInputSystem Input { get; }
    public IGpu Gpu { get; }

    public void Open()
    {
        if (IsOpened)
            return;
        
        ShowWindow(m_Handle);
        UpdateVsyncState();
        IsOpened = true;
        Mouse.Moved += OnMouseMoved;

        while (IsOpened)
        {
            PollEvents();
            if (IsOpened)
            {
                Paint?.Invoke();
                SwapBuffers();
            }
        }
        
        OnClosed();
    }

    private void OnMouseMoved(in MouseMovedEvent evt)
    {
        var mouse = evt.Mouse;
        if (m_MouseX == mouse.ScreenX && m_MouseY == mouse.ScreenY)
            return;

        m_MouseX = mouse.ScreenX;
        m_MouseY = mouse.ScreenY;
        SetCursorPosition(m_Handle, mouse.ScreenX, mouse.ScreenY);
    }

    public void OpenCentered()
    {
        if (IsOpened)
            return;
        
        PosX = (int)((m_DisplayManager.PrimaryDisplay.ResolutionX - ScreenWidth) * 0.5f);
        PosY = (int)((m_DisplayManager.PrimaryDisplay.ResolutionY - ScreenHeight) * 0.5f);
        Open();
    }

    public void Close()
    {
        Debug.Assert(IsOpened);
        Debug.Assert(m_Handle != Window.None);
        SetWindowShouldClose(m_Handle, true);
    }

    private void OnClosed()
    {
        m_Handle = default;
        Mouse.Moved -= OnMouseMoved;
        IsOpened = false;
        Closed?.Invoke();
    }

    public void SetScreenSize(int width, int height)
    {
        if (IsFullscreen)
            return;
        
        m_ViewportWidth = width;
        m_ViewportHeight = height;
        UpdateScreenSize();
    }

    public void SetTopLeftPosition(int x, int y)
    {
        m_PosX = x;
        m_PosY = y;
        UpdateWindowPos();
    }
    
    private void PollEvents()
    {
        Debug.Assert(IsOpened);
        Debug.Assert(m_Handle != Window.None);
        
        GLFW.Glfw.PollEvents();

        if (WindowShouldClose(m_Handle))
        {
            IsOpened = false;
            HideWindow(m_Handle);
            return;
        }
        
        GetCursorPosition(m_Handle, out var x, out var y);
        m_MouseX = (int)x;
        m_MouseY = (int)y;
        var mouse = Mouse;
        mouse.MoveTo(m_MouseX, m_MouseY);

        foreach (var (gamepad, slot) in m_GamepadToSlotMap)
        {
            GetGamepadState(slot, out var state);
            
            //gamepad.LeftTrigger.Value = state.GetAxis(GamePadAxis.LeftTrigger);
            //gamepad.RightRigger.Value = state.GetAxis(GamePadAxis.RightTrigger);
            
            gamepad.SetAxisValue(GamepadAxis.LeftStickX, state.GetAxis(GamePadAxis.LeftX));
            gamepad.SetAxisValue(GamepadAxis.LeftStickY, state.GetAxis(GamePadAxis.LeftY));
            
            UpdateGamepadButtonState(gamepad, state, GamePadButton.A);
            UpdateGamepadButtonState(gamepad, state, GamePadButton.Y);
            UpdateGamepadButtonState(gamepad, state, GamePadButton.B);
            UpdateGamepadButtonState(gamepad, state, GamePadButton.X);
            
            UpdateGamepadButtonState(gamepad, state, GamePadButton.DpadUp);
            UpdateGamepadButtonState(gamepad, state, GamePadButton.DpadRight);
            UpdateGamepadButtonState(gamepad, state, GamePadButton.DpadDown);
            UpdateGamepadButtonState(gamepad, state, GamePadButton.DpadLeft);
            
            UpdateGamepadButtonState(gamepad, state, GamePadButton.LeftBumper);
            UpdateGamepadButtonState(gamepad, state, GamePadButton.RightBumper);
            
            UpdateGamepadButtonState(gamepad, state, GamePadButton.Back);
            UpdateGamepadButtonState(gamepad, state, GamePadButton.Start);
        }
    }

    private void UpdateGamepadButtonState(IGamepad gamepad, GamePadState gamepadState, GamePadButton gamepadButton)
    {
        var button = gamepadButton.ToGamepadButton();
        var isPressed = gamepad.IsButtonPressed(button);
        var buttonState = gamepadState.GetButtonState(gamepadButton);
        
        if (isPressed && buttonState == InputState.Release)
            gamepad.ReleaseButton(button);
        else if (!isPressed && buttonState == InputState.Press)
            gamepad.PressButton(button);
    } 

    private void SwapBuffers()
    {
        GLFW.Glfw.SwapBuffers(m_Handle);
    }

    public Vector2 ScreenToViewportPoint(Vector2 screenPoint, IViewport viewport)
    {
        var screenWidth = m_ViewportWidth;
        var screenHeight = m_ViewportHeight;

        var screenViewportLeft = viewport.Left * screenWidth;
        var screenViewportRight = viewport.Right * screenWidth;
        var screenViewportTop = viewport.Top * screenHeight;
        var screenViewportBottom = viewport.Bottom * screenHeight;
        var screenViewportWidth = screenViewportRight - screenViewportLeft;
        var screenViewportHeight = screenViewportTop - screenViewportBottom;

        var aspectRatio = viewport.AspectRatio;
        var test = screenViewportWidth / screenViewportHeight;
        
        if (test < aspectRatio)
        {
            var h = screenViewportWidth * aspectRatio;
            var verticalDelta = (screenViewportHeight - h) / 2f;
            screenViewportTop -= verticalDelta;
            screenViewportBottom += verticalDelta;
        }
        else if (test > aspectRatio)
        {
            var w = screenViewportHeight / aspectRatio;
            var horizontalDelta = (screenViewportWidth - w) / 2f;
            screenViewportRight -= horizontalDelta;
            screenViewportLeft += horizontalDelta;
        }
        
        var viewportX = (screenPoint.X - screenViewportLeft) / (screenViewportRight - screenViewportLeft);
        var viewportY = 1f - (screenPoint.Y - screenViewportBottom) / (screenViewportTop - screenViewportBottom);
        
        return new Vector2(viewportX, viewportY);
    }

    private void UpdateScreenSize()
    {
        if (IsFullscreen)
            return;

        if (m_Handle != Window.None)
            SetWindowSize(m_Handle, m_ViewportWidth, m_ViewportHeight);
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
            SetWindowMonitor(m_Handle, Monitor.None, m_PosX, m_PosY, m_ViewportWidth, m_ViewportHeight, videoMode.RefreshRate);
        }
    }

    private void Glfw_SizeCallback(Window _, int width, int height)
    {
        if (IsFullscreen)
            return;

        m_ViewportWidth = width;
        m_ViewportHeight = height;
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
        var mouse = Mouse;
        mouse.MoveTo((int)x, (int)y);
    }

    private void Glfw_MouseButtonCallback(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
    {
        var mouse = Mouse;
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
        var keyboard = Keyboard;
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
                keyboard.PressKey(key);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void Glfw_MouseScrollCallback(Window window, double x, double y)
    {
        var mouse = Mouse;
        mouse.Scroll((float)x, (float)y);
    }

    private void Glfw_JoystickCallback(Joystick joystick, ConnectionStatus status)
    {
        var slot = (int)joystick;
        var gamepadManager = Input.GamepadManager;
        
        if (status != ConnectionStatus.Connected)
        {
            if (gamepadManager.TryGetGamepadInSlot(slot, out var connectedGamepad))
            {
                m_GamepadToSlotMap.Remove(connectedGamepad!);
                gamepadManager.DisconnectGamepad(slot);
            }
            return;
        }
        
        if (!JoystickIsGamepad(joystick))
            return;
       
        ConnectGamepad(joystick);
    }
    
    private void Glfw_FocusCallback(Window window, bool focusing)
    {
        IsFocused = focusing;
    }

    private void ConnectGamepad(Joystick joystick)
    {
        var slot = (int)joystick;
        var joystickName = GetJoystickName(joystick);
        var guid = GetJoystickGuid(joystick);
        var gamepad = new Gamepad(guid, joystickName);
        m_GamepadToSlotMap[gamepad] = slot;
        Input.GamepadManager.ConnectGamepad(slot, gamepad);
    }
    
    public override string ToString()
    {
        return $"x{PosX} y{PosY}, {ScreenWidth}x{ScreenHeight}";
    }

    private Api.InputDevices.MouseButton MapToMouseButton(MouseButton mouseButton)
    {
        switch (mouseButton)
        {
            case MouseButton.Left:
                return Api.InputDevices.MouseButton.Left;
            case MouseButton.Right:
                return Api.InputDevices.MouseButton.Right;
            case MouseButton.Middle:
                return Api.InputDevices.MouseButton.Middle;
            case MouseButton.Button4:
            case MouseButton.Button5:
            case MouseButton.Button6:
            case MouseButton.Button7:
            case MouseButton.Button8:
                return new Api.InputDevices.MouseButton((int)mouseButton);
            default:
                throw new ArgumentOutOfRangeException(nameof(mouseButton), mouseButton, null);
        }
    }

    private void ReleaseUnmanagedResources()
    {
        Terminate();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Window_GLFW()
    {
        ReleaseUnmanagedResources();
    }
}