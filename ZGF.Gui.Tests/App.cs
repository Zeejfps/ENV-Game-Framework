using System.Numerics;
using GLFW;
using OpenGL.NET;
using ZGF.Core;
using ZGF.Geometry;
using ZGF.Gui.Layouts;
using ZGF.KeyboardModule.GlfwAdapter;
using ZGF.WavefrontObjModule;
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
    private readonly ImageManager _imageManager;

    private FrameBufferHandle _frameBufferHandle;
    private ImageView _modelView;
    private int _modelMatrixUniformLocation;
    private int _viewProjectionMatrixUniformLocation;

    public App(StartupConfig startupConfig) : base(startupConfig)
    {
        _inputSystem = new InputSystem();

        _imageManager = new ImageManager();
        _imageManager.LoadImageFromFile("Assets/Icons/arrow_right.png");
        _imageManager.LoadImageFromFile("Assets/Icons/arrow_up.png");
        _imageManager.LoadImageFromFile("Assets/Icons/arrow_down.png");
        _frameBufferHandle = _imageManager.CreateFrameBufferImage(640, 480);

        _bitmapFont = BitmapFont.LoadFromFile("Assets/Fonts/Charcoal/Charcoal_p20.xml");
        var textMeasurer = new TextMeasurer(_bitmapFont);

        _canvas = new Canvas(
            startupConfig.WindowWidth,
            startupConfig.WindowHeight,
            _bitmapFont,
            textMeasurer, _imageManager
        );
        
        _mesh = Mesh.LoadFromFile("Assets/Models/Suzan_tri.obj");
        _shaderProgram = new ShaderProgramCompiler()
            .WithVertexShader("Assets/Shaders/color_vert.glsl")
            .WithFragmentShader("Assets/Shaders/color_frag.glsl")
            .Compile();

        glUseProgram(_shaderProgram.Id);
        _modelMatrixUniformLocation = glGetUniformLocation(_shaderProgram.Id, "model_matrix");
        _viewProjectionMatrixUniformLocation = glGetUniformLocation(_shaderProgram.Id, "view_projection_matrix");
        AssertNoGlError();
        Console.WriteLine($"view_projection_matrix locaiton: {_viewProjectionMatrixUniformLocation}");

        var contextMenuPane = new View();
        _contextMenuManager = new ContextMenuManager(contextMenuPane);
        
        var context = new Context
        {
            InputSystem = _inputSystem,
            TextMeasurer = textMeasurer,
            ImageManager = _imageManager,
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
        _modelView.ImageId = _frameBufferHandle.ImageId;

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

        var fov = 45f * (MathF.PI / 180f);
        var aspectRatio = 640f / 480f;
        var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspectRatio, 0.001f, 1000f);
        _viewProjectionMatrix = projectionMatrix;

        _modelMatrix = Matrix4x4.Identity;

        glEnable(GL_DEPTH_TEST);

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

    private float rr = 0f;
    private Vector3 _cameraPos = new Vector3();

    protected override void OnUpdate()
    {
        rr += 0.01f;
        var t = Matrix4x4.CreateTranslation(0f, 0f, -20);
        var r = Matrix4x4.CreateRotationY(rr);
        var s = Matrix4x4.CreateScale(5f, 5f, 5f);
        _modelMatrix = s * r * t;

        _imageManager.RenderFrameBuffersToBitmaps();
        Render();
        Glfw.GetCursorPosition(WindowHandle, out var mouseX, out var mouseY);
        var guiPoint = WindowToGuiCoords(mouseX, mouseY);
        _inputSystem.UpdateMousePosition(guiPoint);
        _inputSystem.Update();
        _contextMenuManager.Update();
    }

    private void Render()
    {
        RenderMesh();
        
        // TODO: Main UI rendering
        Glfw.GetFramebufferSize(WindowHandle, out var width, out var height);
        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);
        glViewport(0, 0, width, height);
        glClearColor(0, 0, 0, 0);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        _canvas.BeginFrame();
        _gui.LayoutSelf();
        _gui.DrawSelf();
        _canvas.EndFrame();
    }

    private Mesh _mesh;
    private ShaderProgramInfo _shaderProgram;
    private Matrix4x4 _modelMatrix;
    private Matrix4x4 _viewProjectionMatrix;

    private unsafe void RenderMesh()
    {
        glDisable(GL_CULL_FACE);
        // TODO: Render stuff into the window
        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, _frameBufferHandle.FrameBufferId);
        glViewport(0, 0, _frameBufferHandle.Bitmap.Width, _frameBufferHandle.Bitmap.Height);
        glClearColor(0, 0, 1, 1);
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

        glUseProgram(_shaderProgram.Id);

        fixed (float* ptr = &_modelMatrix.M11)
            glUniformMatrix4fv(_modelMatrixUniformLocation, 1, false, ptr);

        fixed (float* ptr = &_viewProjectionMatrix.M11)
            glUniformMatrix4fv(_viewProjectionMatrixUniformLocation, 1, false, ptr);

        glBindVertexArray(_mesh.VaoId);
        AssertNoGlError();

        glDrawElements(GL_TRIANGLES, _mesh.TriangleCount*3, GL_UNSIGNED_INT, (void*)0);
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