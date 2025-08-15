using GLFW;
using ZGF.Core;
using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule.GlfwAdapter;
using InputState = ZGF.Gui.InputState;
using MouseButton = ZGF.Gui.MouseButton;

namespace LLMit;

public sealed class GuiApp : OpenGlApp
{
    private readonly InputSystem _inputSystem;
    private readonly ImageManager _imageManager;
    private readonly SoftwareRenderedCanvas _canvas;
    private readonly View _gui;
    
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly KeyCallback _keyCallback;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly MouseButtonCallback _mouseButtonCallback;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly SizeCallback _windowSizeCallback;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly SizeCallback _framebufferSizeCallback;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly MouseCallback _scrollCallback;

    public GuiApp(StartupConfig startupConfig, View content) : base(startupConfig)
    {
        _inputSystem = new InputSystem();
        _imageManager = new ImageManager();
        var contextMenuPane = new View();
        var contextMenuManager = new ContextMenuManager(contextMenuPane);
        var bitmapFont = BitmapFont.LoadFromFile("Assets/Fonts/Charcoal/Charcoal_p20.xml");
        _canvas = new SoftwareRenderedCanvas(
            startupConfig.WindowWidth,
            startupConfig.WindowHeight,
            bitmapFont,
            _imageManager
        );
        
        var context = new Context
        {
            InputSystem = _inputSystem,
            Canvas = _canvas
        };

        context.AddService(contextMenuManager);
#if OSX
        context.AddService<IClipboard>(new OsxClipboard());
#elif WIN
        context.AddService<IClipboard>(new Win32Clipboard());
#else
        context.AddService<IClipboard>(new AppClipboard());
#endif
        
        _gui = new View
        {
            PreferredWidth = _canvas.Width,
            PreferredHeight = _canvas.Height,
            Context = context,
            Children =
            {
                content,
                contextMenuPane,
            }
        };
        
        _keyCallback = HandleKeyEvent;
        _mouseButtonCallback = HandleMouseButtonEvent;
        _windowSizeCallback = HandleWindowSizeChanged;
        _framebufferSizeCallback = HandleFramebufferSizeChanged;
        _scrollCallback = HandleScrollEvent;
        Glfw.SetKeyCallback(WindowHandle, _keyCallback);
        Glfw.SetMouseButtonCallback(WindowHandle, _mouseButtonCallback);
        Glfw.SetWindowSizeCallback(WindowHandle, _windowSizeCallback);
        Glfw.SetFramebufferSizeCallback(WindowHandle, _framebufferSizeCallback);
        Glfw.SetScrollCallback(WindowHandle, _scrollCallback);

    }

    protected override void OnUpdate()
    {
        
    }

    protected override void DisposeManagedResources()
    {
    }

    protected override void DisposeUnmanagedResources()
    {
    }
    
    private void HandleScrollEvent(GLFW.Window window, double x, double y)
    {
        var e = new MouseWheelScrolledEvent
        {
            Mouse = _inputSystem,
            DeltaX = (float)x,
            DeltaY = (float)y,
            Phase = EventPhase.Capturing
        };
        _inputSystem.SendMouseScrollEvent(ref e);
    }

    private void HandleWindowSizeChanged(GLFW.Window window, int width, int height)
    {
        _gui.PreferredWidth = width;
        _gui.PreferredHeight = height;
        _canvas.Resize(width, height);
        Render();
        Glfw.SwapBuffers(window);
    }

    private void HandleFramebufferSizeChanged(GLFW.Window window, int width, int height)
    {
        GL46.glViewport(0, 0, width, height);
    }

    private void HandleMouseButtonEvent(GLFW.Window window, GLFW.MouseButton button, GLFW.InputState state, ModifierKeys modifiers)
    {
        Glfw.GetCursorPosition(WindowHandle, out var windowX, out var windowY);
        var b = button switch
        {
            GLFW.MouseButton.Left => MouseButton.Left,
            GLFW.MouseButton.Right => MouseButton.Right,
            GLFW.MouseButton.Middle => MouseButton.Middle,
            _ => new MouseButton((int)button),
        };
        var s = state switch
        {
            GLFW.InputState.Press => InputState.Pressed,
            GLFW.InputState.Release => InputState.Released,
            GLFW.InputState.Repeat => InputState.Pressed,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        //var guiPoint = WindowToGuiCoords(windowX, windowY);
        var e = new MouseButtonEvent
        {
            Mouse = _inputSystem,
            Button = b,
            State = s,
            Phase = EventPhase.Capturing,
        };
        _inputSystem.SendMouseButtonEvent(ref e);
    }

    private void HandleKeyEvent(GLFW.Window window, Keys key, int scanCode, GLFW.InputState state, ModifierKeys mods)
    {
        var s = state switch
        {
            GLFW.InputState.Press => InputState.Pressed,
            GLFW.InputState.Release => InputState.Released,
            GLFW.InputState.Repeat => InputState.Pressed,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };

        var e = new KeyboardKeyEvent
        {
            Key = key.Adapt(),
            State = s,
            Modifiers = (InputModifiers)mods,
            Phase = EventPhase.Capturing
        };
        _inputSystem.SendKeyboardKeyEvent(ref e);
    }

    private void Render()
    {
        _canvas.BeginFrame();
        _gui.LayoutSelf();
        _gui.DrawSelf();
        _canvas.EndFrame();
    }
}