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

    public App(StartupConfig startupConfig) : base(startupConfig)
    {
        _mouseInputSystem = new MouseInputSystem();

        _framebufferWidth = startupConfig.WindowWidth / 2;
        _framebufferHeight = startupConfig.WindowHeight / 2;

        var bitmap = new Bitmap(_framebufferWidth, _framebufferHeight);
        _canvas = new Canvas(bitmap);
        glClearColor(0f, 0f, 0f, 0f);

        var fontFile = BMFontFileUtils.DeserializeFromXmlFile("Assets/Fonts/Charcoal/Charcoal.xml");
        var fontPng = Png.DecodeFromFile("Assets/Fonts/Charcoal/Charcoal.png");
        var fontBmp = new Bitmap(fontPng.Width, fontPng.Height);
        var pixels = fontBmp.Pixels;
        var pixelIndex = 0;
        for (var i = 0; i < fontPng.PixelData.Length; i+= fontPng.BytesPerPixel, pixelIndex++)
        {
            var r = fontPng.PixelData[i + 0];
            var g = fontPng.PixelData[i + 1];
            var b = fontPng.PixelData[i + 2];
            var a = fontPng.PixelData[i + 3];
            var color = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
            pixels[pixelIndex] = color;
        }

        var header = new AppBar
        {
            Constraints = new RectF
            {
                Height = 20f
            },
        };

        var center = new Center();
        _window = center.Window;

        var gui = new BorderLayout
        {
            Center = center,
            North = header,
            Constraints = new RectF(0, 0, _framebufferWidth, _framebufferHeight),
            Context = new Context
            {
                MouseInputSystem = _mouseInputSystem
            }
        };

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