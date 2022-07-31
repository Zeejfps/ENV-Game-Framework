using System.Diagnostics;
using GLFW;
using Monitor = GLFW.Monitor;

namespace Framework.GLFW.NET;

public class Window_GLFW : IWindow
{
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
            Glfw.SetWindowTitle(m_Handle, m_Title);
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
            Glfw.SetWindowAttribute(m_Handle, WindowAttribute.Resizable, m_IsResizable);
        }
    }
    
    public bool IsOpened { get; private set; }

    public IInput Input => m_Input;
    public IGpuFramebuffer Framebuffer => m_WindowFramebuffer;

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

    private string m_Title = "Untitled";

    private int m_PosX;
    private int m_PosY;  
    
    private int m_Width = 640;
    private int m_Height = 480;

    private bool m_IsResizable;
    private bool m_IsFullscreen;
    private bool m_IsVsyncEnabled;
    
    private Window m_Handle;

    private readonly WindowFramebuffer_GL m_WindowFramebuffer;
    private readonly Input_GLFW m_Input;

    private readonly KeyCallback m_KeyCallback;
    private readonly SizeCallback m_SizeCallback;
    private readonly MouseCallback m_ScrollCallback;
    private readonly MouseCallback m_MousePositionCallback;
    private readonly MouseButtonCallback m_MouseButtonCallback;
    private readonly PositionCallback m_PositionCallback;
    private readonly SizeCallback m_FramebufferSizeCallback;

    public Window_GLFW()
    {
        m_Input = new Input_GLFW();

        m_KeyCallback = Glfw_KeyCallback;
        m_SizeCallback = Glfw_SizeCallback;
        m_MousePositionCallback = Glfw_MousePosCallback;
        m_PositionCallback = Glfw_PositionCallback;
        m_FramebufferSizeCallback = Glfw_FramebufferSizeCallback;
        m_MouseButtonCallback = Glfw_MouseButtonCallback;
        m_ScrollCallback = Glfw_MouseScrollCallback;
        
        Glfw.WindowHint(Hint.Visible, false);
        Glfw.WindowHint(Hint.Resizable, m_IsResizable);
        
        m_Handle = Glfw.CreateWindow(Width, Height, Title, Monitor.None, Window.None);
        Glfw.MakeContextCurrent(m_Handle);
        Glfw.GetFramebufferSize(m_Handle, out var framebufferWidth, out var framebufferHeight);
        m_WindowFramebuffer = new WindowFramebuffer_GL(framebufferWidth, framebufferHeight, Glfw.GetProcAddress);

        Glfw.SetWindowSizeCallback(m_Handle, m_SizeCallback);
        Glfw.SetWindowPositionCallback(m_Handle, m_PositionCallback);
        Glfw.SetFramebufferSizeCallback(m_Handle, m_FramebufferSizeCallback);
        Glfw.SetKeyCallback(m_Handle, m_KeyCallback);
        Glfw.SetCursorPositionCallback(m_Handle, m_MousePositionCallback);
        Glfw.SetMouseButtonCallback(m_Handle, m_MouseButtonCallback);
        Glfw.SetScrollCallback(m_Handle, m_ScrollCallback);
    }

    public void Open()
    {
        Glfw.ShowWindow(m_Handle);
        IsOpened = true;
    }

    public void Close()
    {
        Debug.Assert(IsOpened);
        Debug.Assert(m_Handle != Window.None);
        Glfw.SetWindowShouldClose(m_Handle, true);
        
        m_Handle = default;
        IsOpened = false;
    }

    public void Update()
    {
        Debug.Assert(IsOpened);
        Debug.Assert(m_Handle != Window.None);

        m_Input.Update(m_Handle);
        Glfw.SwapBuffers(m_Handle);
        Glfw.PollEvents();

        if (Glfw.WindowShouldClose(m_Handle))
            IsOpened = false;
    }

    public void Resize(int width, int height)
    {
        m_Width = width;
        m_Height = height;
        UpdateWindowSize();
    }

    public void Reposition(int x, int y)
    {
        m_PosX = x;
        m_PosY = y;
        UpdateWindowPos();
    }

    private void UpdateWindowSize()
    {
        if (m_Handle != Window.None)
            Glfw.SetWindowSize(m_Handle, m_Width, m_Height);
    }

    private void UpdateWindowPos()
    {
        if (m_Handle != Window.None)
            Glfw.SetWindowPosition(m_Handle, m_PosX, m_PosY);
    }

    private void UpdateVsyncState()
    {
        if (m_Handle != Window.None)
            Glfw.SwapInterval(m_IsVsyncEnabled ? 1 : 0);
    }

    private void UpdateFullscreenState()
    {
        if (m_Handle == Window.None)
            return;

        var primaryMonitor = Glfw.PrimaryMonitor;
        var videoMode = Glfw.GetVideoMode(primaryMonitor);
        
        if (IsFullscreen)
        {
            var workArea = primaryMonitor.WorkArea;
            Glfw.SetWindowMonitor(m_Handle, primaryMonitor, workArea.X, workArea.Y, workArea.Width, workArea.Height, videoMode.RefreshRate);
        }
        else
        {
            Glfw.SetWindowMonitor(m_Handle, Monitor.None, m_PosX, m_PosY, m_Width, m_Height, videoMode.RefreshRate);
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
        m_WindowFramebuffer.Resize(width, height);
    }
    
    private void Glfw_MousePosCallback(Window window, double x, double y)
    {
        m_Input.Glfw_MousePosCallback(window, x, y);
    }

    private void Glfw_MouseButtonCallback(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
    {
        m_Input.Glfw_MouseButtonCallback(window, button, state, modifiers);
    }
    
    private void Glfw_KeyCallback(Window window, Keys key, int scancode, InputState state, ModifierKeys mods)
    {
        m_Input.Glfw_KeyCallback(window, key, scancode, state, mods);
    }
    
    private void Glfw_MouseScrollCallback(Window window, double x, double y)
    {
        m_Input.Glfw_MouseScrollCallback(window, x, y);
    }
    
    public override string ToString()
    {
        return $"x{PosX} y{PosY}, {Width}x{Height}";
    }
}