using GLFW;
using ZGF.Core;
using ZGF.Geometry;
using ZGF.Gui.Layouts;
using ZGF.KeyboardModule.GlfwAdapter;
using static GL46;

namespace ZGF.Gui.Tests;

public sealed class App : OpenGlApp
{
    private readonly Canvas _canvas;
    private readonly Component _gui;

    private readonly Window _window;
    private readonly InputSystem _inputSystem;
    private readonly KeyCallback _keyCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly SizeCallback _windowSizeCallback;
    private readonly MouseCallback _scrollCallback;
    private readonly BitmapFont _bitmapFont;
    private readonly ContextMenuManager _contextMenuManager;

    public App(StartupConfig startupConfig) : base(startupConfig)
    {
        _inputSystem = new InputSystem();

        var imageManager = new ImageManager();
        imageManager.LoadImage("Assets/Icons/arrow_right.png");

        _bitmapFont = BitmapFont.LoadFromFile("Assets/Fonts/Charcoal/Charcoal_p20.xml");
        var textMeasurer = new TextMeasurer(_bitmapFont);

        _canvas = new Canvas(
            startupConfig.WindowWidth,
            startupConfig.WindowHeight,
            _bitmapFont,
            textMeasurer, imageManager
        );
        
        var contextMenuPane = new Component();
        _contextMenuManager = new ContextMenuManager(contextMenuPane);
        
        var context = new Context
        {
            InputSystem = _inputSystem,
            TextMeasurer = textMeasurer,
            ImageManager = imageManager,
            Canvas = _canvas
        };

        context.AddService(_contextMenuManager);
#if OSX
        context.AddService<IClipboard>(new OsxClipboard());
#elif WIN
        context.AddService<IClipboard>(new Win32Clipboard());
#else
        context.AddService<IClipboard>(new AppClipboard());
#endif

        glClearColor(0f, 0f, 0f, 0f);
        
        var appBar = new AppBar(this, _contextMenuManager);
        var center = new Center();
        _window = center.Window;

        var contents = new BorderLayout
        {
            North = appBar,
            Center = center,
        };

        var gui = new Component
        {
            PreferredWidth = _canvas.Width,
            PreferredHeight = _canvas.Height,
            Context = context,
            Children =
            {
                contents,
                contextMenuPane,
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
            TextColor = 0x959595
        });
        
        gui.ApplyStyleSheet(ss);

        _gui = gui;

        _keyCallback = HandleKeyEvent;
        _mouseButtonCallback = HandleMouseButtonEvent;
        _windowSizeCallback = HandleWindowSizeChanged;
        _scrollCallback = HandleScrollEvent;
        Glfw.SetKeyCallback(WindowHandle, _keyCallback);
        Glfw.SetMouseButtonCallback(WindowHandle, _mouseButtonCallback);
        Glfw.SetWindowSizeCallback(WindowHandle, _windowSizeCallback);
        Glfw.SetScrollCallback(WindowHandle, _scrollCallback);

        //PrintTree(gui);
    }

    private void HandleScrollEvent(GLFW.Window window, double x, double y)
    {
        _inputSystem.HandleMouseScrollEvent(new MouseWheelScrolledEvent
        {
            Mouse = _inputSystem,
            DeltaX = x,
            DeltaY = y
        });
    }

    private void PrintTree(Component component, int depth = 0)
    {
        var indent = new string(' ', depth * 4);
        Console.WriteLine($"{indent}{component}");
        foreach (var child in component.Children)
        {
            PrintTree(child, depth + 1);
        }
    }

    private void HandleWindowSizeChanged(GLFW.Window window, int width, int height)
    {
        glViewport(0, 0, width, height);
        _gui.PreferredWidth = width;
        _gui.PreferredHeight = height;
        _canvas.Resize(width, height);
        Render();
        Glfw.SwapBuffers(window);
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
        _inputSystem.HandleMouseButtonEvent(new MouseButtonEvent
        {
            Mouse = _inputSystem,
            Button = b,
            State = s,
        });
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

        _inputSystem.HandleKeyboardKeyEvent(new KeyboardKeyEvent
        {
            Key = key.Adapt(),
            State = s,
            Modifiers = (InputModifiers)mods
        });
    }
    
    protected override void OnUpdate()
    {
        Render();
        Glfw.GetCursorPosition(WindowHandle, out var mouseX, out var mouseY);
        var guiPoint = WindowToGuiCoords(mouseX, mouseY);
        _inputSystem.UpdateMousePosition(guiPoint);
        _inputSystem.Update();
        _contextMenuManager.Update();
    }

    private void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        _canvas.BeginFrame();
        _gui.LayoutSelf();
        _gui.DrawSelf();
        _canvas.EndFrame();
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

    public void Exit()
    {
        Glfw.SetWindowShouldClose(WindowHandle, true);
    }
}