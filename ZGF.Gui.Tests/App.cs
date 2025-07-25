using GLFW;
using ZGF.Core;
using ZGF.Geometry;
using ZGF.Gui.Layouts;
using ZGF.KeyboardModule.GlfwAdapter;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Tests;

public sealed class App : OpenGlApp
{
    private readonly Canvas _canvas;
    private readonly View _gui;

    private readonly InputSystem _inputSystem;
    private readonly KeyCallback _keyCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly SizeCallback _windowSizeCallback;
    private readonly SizeCallback _framebufferSizeCallback;
    private readonly MouseCallback _scrollCallback;
    private readonly BitmapFont _bitmapFont;
    private readonly ContextMenuManager _contextMenuManager;

    private ModelView _modelView;

    public App(StartupConfig startupConfig) : base(startupConfig)
    {
        _inputSystem = new InputSystem();

        var imageManager = new ImageManager();
        imageManager.LoadImageFromFile("Assets/Icons/arrow_right.png");
        imageManager.LoadImageFromFile("Assets/Icons/arrow_up.png");
        imageManager.LoadImageFromFile("Assets/Icons/arrow_down.png");

        _bitmapFont = BitmapFont.LoadFromFile("Assets/Fonts/Charcoal/Charcoal_p20.xml");
        var textMeasurer = new TextMeasurer(_bitmapFont);

        _canvas = new Canvas(
            startupConfig.WindowWidth,
            startupConfig.WindowHeight,
            _bitmapFont,
            textMeasurer, imageManager
        );
        
        var contextMenuPane = new View();
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

        _modelView = center.ModelView;

        var contents = new BorderLayoutView
        {
            North = appBar,
            Center = center,
        };

        var gui = new View
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
        _framebufferSizeCallback = HandleFramebufferSizeChanged;
        _scrollCallback = HandleScrollEvent;
        Glfw.SetKeyCallback(WindowHandle, _keyCallback);
        Glfw.SetMouseButtonCallback(WindowHandle, _mouseButtonCallback);
        Glfw.SetWindowSizeCallback(WindowHandle, _windowSizeCallback);
        Glfw.SetFramebufferSizeCallback(WindowHandle, _framebufferSizeCallback);
        Glfw.SetScrollCallback(WindowHandle, _scrollCallback);

        //PrintTree(gui);
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

    private void PrintTree(View view, int depth = 0)
    {
        var indent = new string(' ', depth * 4);
        Console.WriteLine($"{indent}{view}");
        foreach (var child in view.Children)
        {
            PrintTree(child, depth + 1);
        }
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
        glViewport(0, 0, width, height);
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
        // TODO: Render stuff into the window
        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, _modelView.FrameBufferId);
        glClearColor(0, 0, 1, 1);
        glClear(GL_COLOR_BUFFER_BIT);

        // TODO: Main UI rendering
        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);
        glClearColor(0, 0, 0, 0);
        glClear(GL_COLOR_BUFFER_BIT);

        _canvas.BeginFrame();
        _gui.LayoutSelf();
        _gui.DrawSelf();
        _canvas.EndFrame();
    }

    private void BlitFrameBufferTest()
    {
        glBindFramebuffer(GL_READ_FRAMEBUFFER, _modelView.FrameBufferId);
        AssertNoGlError();

        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);
        AssertNoGlError();

        var position = _modelView.Position;
        glBlitFramebuffer(
            0, 0, 640, 480,
            (int)position.Left*2, (int)position.Bottom*2, (int)position.Right*2, (int)position.Top*2,
            GL_COLOR_BUFFER_BIT,
            GL_LINEAR
        );
        AssertNoGlError();

        glBindFramebuffer(GL_READ_FRAMEBUFFER, 0);
        AssertNoGlError();
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