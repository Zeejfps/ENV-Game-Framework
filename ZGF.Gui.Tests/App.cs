using EasyGameFramework.Api;
using GLFW;
using PngSharp.Api;
using SoftwareRendererModule;
using ZGF.BMFontModule;
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

    private readonly int _framebufferWidth;
    private readonly int _framebufferHeight;

    private readonly MouseInputSystem _mouseInputSystem;
    private readonly KeyCallback _keyCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private Bitmap _colorBuffer;
    private readonly BitmapFont _bitmapFont;

    public App(StartupConfig startupConfig) : base(startupConfig)
    {
        _mouseInputSystem = new MouseInputSystem();

        _framebufferWidth = startupConfig.WindowWidth / 2;
        _framebufferHeight = startupConfig.WindowHeight / 2;

        _colorBuffer = new Bitmap(_framebufferWidth, _framebufferHeight);
        _bitmapFont = BitmapFont.LoadFromFile("Assets/Fonts/Charcoal/Charcoal_p12.xml");

        _canvas = new Canvas(_colorBuffer, _bitmapFont);
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
            PreferredWidth = _framebufferWidth,
            PreferredHeight = _framebufferHeight,
            North = header,
            Center = center,
        };

        var context = new Context
        {
            MouseInputSystem = _mouseInputSystem,
            TextMeasurer = new TextMeasurer(_bitmapFont),
        };
        context.AddService(new ContextMenuManager(contextMenuPane));

        var gui = new Component
        {
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
        gui.ApplyStyleSheet(ss);

        _gui = gui;

        _keyCallback = HandleKeyEvent;
        _mouseButtonCallback = HandleMouseButtonEvent;
        Glfw.SetKeyCallback(WindowHandle, _keyCallback);
        Glfw.SetMouseButtonCallback(WindowHandle, _mouseButtonCallback);
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

        Console.WriteLine($"Mouse button: {s}");
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
        _gui.DrawSelf(_canvas);
        _canvas.EndFrame();
        
        Glfw.GetCursorPosition(WindowHandle, out var mouseX, out var mouseY);
        var guiPoint = WindowToGuiCoords(mouseX, mouseY);
        _mouseInputSystem.UpdateMousePosition(guiPoint);
    }

    private PointF WindowToGuiCoords(double windowX, double windowY)
    {
        Glfw.GetWindowSize(WindowHandle, out var width, out var height);
        var scaleX = _framebufferWidth / (float)width;
        var scaleY = _framebufferHeight / (float)height;
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