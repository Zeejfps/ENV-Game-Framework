using System.Numerics;
using EasyGameFramework.Api;
using GLFW;
using SoftwareRendererModule;
using ZGF.Core;
using ZGF.Geometry;
using static GL46;

namespace ZGF.Gui.Tests;

public sealed class App : OpenGlApp
{
    private readonly BitmapCanvas _canvas;

    private readonly Clock _clock;
    private Window _window;
    
    private readonly KeyCallback _keyCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    
    public App(StartupConfig startupConfig) : base(startupConfig)
    {
        _clock = new Clock();
        
        var framebufferWidth = startupConfig.WindowWidth / 2;
        var framebufferHeight = startupConfig.WindowHeight / 2;
        var bitmap = new Bitmap(framebufferWidth, framebufferHeight);
        _canvas = new BitmapCanvas(bitmap);
        glClearColor(0f, 0f, 0f, 0f);
        
        var header = new Header
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
            Constraints = new RectF(0, 0, framebufferWidth, framebufferHeight)
        };
        gui.ApplyStyleSheet(new StyleSheet());

        Gui = gui;

        _keyCallback = HandleKeyEvent;
        _mouseButtonCallback = HandleMouseButtonEvent;
        Glfw.SetKeyCallback(WindowHandle, _keyCallback);
        Glfw.SetMouseButtonCallback(WindowHandle, _mouseButtonCallback);
    }

    private bool _isDragging;
    private double _x;
    private double _y;
    
    private void HandleMouseButtonEvent(GLFW.Window window, MouseButton button, InputState state, ModifierKeys modifiers)
    {
        if (button != MouseButton.Left)
            return;

        if (state == InputState.Press)
        {
            _isDragging = true;
            Glfw.GetCursorPosition(WindowHandle, out var x, out var y);
            _x = x;
            _y = y;
        }
        else if (state == InputState.Release)
        {
            _isDragging = false;
        }
    }

    private void HandleKeyEvent(GLFW.Window window, Keys key, int scanCode, InputState state, ModifierKeys mods)
    {
        if (state != InputState.Press)
            return;
        
        if (key != Keys.Space)
            return;
        
        _isMoving = !_isMoving;
    }

    private bool _isMoving;
    private Component Gui { get; }

    protected override void OnUpdate()
    {
        if (_isDragging)
        {
            Glfw.GetCursorPosition(WindowHandle, out var x, out var y);
            var dx = x - _x;
            var dy = y - _y;
            _x = x;
            _y = y;
            _window.Move((int)dx*0.5f, (int)-dy * 0.5f);
        }
        
        glClear(GL_COLOR_BUFFER_BIT);
        _canvas.BeginFrame();
        Gui.LayoutSelf();
        Gui.DrawSelf(_canvas);
        _canvas.EndFrame();
        EventSystem.Instance.Update();
    }

    protected override void DisposeManagedResources()
    {
    }

    protected override void DisposeUnmanagedResources()
    {
    }
}