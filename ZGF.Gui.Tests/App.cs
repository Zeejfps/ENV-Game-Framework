using GLFW;
using ZGF.Core;
using ZGF.Geometry;
using ZGF.Gui.Layouts;
using static GL46;

namespace ZGF.Gui.Tests;

public sealed class App : OpenGlApp
{
    private readonly Canvas _canvas;
    private readonly Component _gui;

    private readonly Window _window;
    private readonly MouseInputSystem _mouseInputSystem;
    private readonly KeyCallback _keyCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly SizeCallback _windowSizeCallback;
    private readonly BitmapFont _bitmapFont;
    private readonly ContextMenuManager _contextMenuManager;

    public App(StartupConfig startupConfig) : base(startupConfig)
    {
        _mouseInputSystem = new MouseInputSystem();

        var imageManager = new ImageManager();
        imageManager.LoadImage("Assets/Icons/arrow_right.png");

        _bitmapFont = BitmapFont.LoadFromFile("Assets/Fonts/Charcoal/Charcoal_p12.xml");
        var textMeasurer = new TextMeasurer(_bitmapFont);

        _canvas = new Canvas(
            startupConfig.WindowWidth,
            startupConfig.WindowHeight,
            _bitmapFont,
            textMeasurer, imageManager
        );
        glClearColor(0f, 0f, 0f, 0f);

        var header = new AppBar
        {
            //PreferredHeight = 20f
        };

        var center = new Center();
        _window = center.Window;

        var contextMenuPane = new Component();
        var contents = new BorderLayout
        {
            North = header,
            Center = center,
        };

        _contextMenuManager = new ContextMenuManager(contextMenuPane);
        var context = new Context
        {
            MouseInputSystem = _mouseInputSystem,
            TextMeasurer = textMeasurer,
            ImageManager = imageManager,
            Canvas = _canvas
        };
        context.AddService(_contextMenuManager);

        var gui = new Component
        {
            PreferredWidth = _canvas.Width,
            PreferredHeight = _canvas.Height,
            Context = context
        };
        
        gui.Add(contents);
        gui.Add(contextMenuPane);
        
        var ss = new StyleSheet();
        ss.AddStyleForClass("inset_panel", new Style
        {
            BackgroundColor = 0x000000,
            Padding = PaddingStyle.All(1),
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Left = 0x9C9C9C,
                Top = 0x9C9C9C,
                Right = 0xFFFFFF,
                Bottom = 0xFFFFFF
            },
        });
        ss.AddStyleForClass("raised_panel", new Style
        {
            BorderColor = new BorderColorStyle
            {
                Top = 0xFFFFFF,
                Left = 0xFFFFFF,
                Right = 0x9C9C9C,
                Bottom = 0x9C9C9C
            },
        });
        ss.AddStyleForClass("window_button", new Style
        {
            PreferredWidth = 10f,
            BackgroundColor = 0x000000,
        });

        ss.AddStyleForClass("disabled", new Style
        {
            TextColor = 0xFF00FF
        });
        
        gui.ApplyStyleSheet(ss);

        _gui = gui;

        _keyCallback = HandleKeyEvent;
        _mouseButtonCallback = HandleMouseButtonEvent;
        _windowSizeCallback = HandleWindowSizeChanged;
        Glfw.SetKeyCallback(WindowHandle, _keyCallback);
        Glfw.SetMouseButtonCallback(WindowHandle, _mouseButtonCallback);
        Glfw.SetWindowSizeCallback(WindowHandle, _windowSizeCallback);
    }

    private void HandleWindowSizeChanged(GLFW.Window window, int width, int height)
    {
        //glViewport(0, 0, width, height);
        _gui.PreferredWidth = width;
        _gui.PreferredHeight = height;
        _canvas.Resize(width, height);
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
        };

        var guiPoint = WindowToGuiCoords(windowX, windowY);
        _mouseInputSystem.HandleMouseButtonEvent(new MouseButtonEvent
        {
            Position = guiPoint,
            Button = b,
            State = s,
        });
    }

    private void HandleKeyEvent(GLFW.Window window, Keys key, int scanCode, GLFW.InputState state, ModifierKeys mods)
    {
        if (state != GLFW.InputState.Press)
            return;
        
        if (key != Keys.Space)
            return;
        
        _window.Move(10, 10);
    }
    
    protected override void OnUpdate()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        _canvas.BeginFrame();
        _gui.LayoutSelf();
        _gui.DrawSelf();
        _canvas.EndFrame();
        
        Glfw.GetCursorPosition(WindowHandle, out var mouseX, out var mouseY);
        var guiPoint = WindowToGuiCoords(mouseX, mouseY);
        _mouseInputSystem.UpdateMousePosition(guiPoint);
        _contextMenuManager.Update();
    }

    private PointF WindowToGuiCoords(double windowX, double windowY)
    {
        Glfw.GetWindowSize(WindowHandle, out var width, out var height);
        var scaleX = _canvas.Width / (float)width;
        var scaleY = _canvas.Height / (float)height;
        var screenX = windowX * scaleX;
        var screenY = (height - windowY) * scaleY;
        return new PointF((float)screenX, (float)screenY);
    }

    protected override void DisposeManagedResources()
    {
    }

    protected override void DisposeUnmanagedResources()
    {
    }
}