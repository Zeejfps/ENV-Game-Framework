using System.Diagnostics;
using System.Numerics;
using GLFW;
using OpenGL.NET;
using ZGF.Core;
using ZGF.Fonts;
using ZGF.Gui.Layouts;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Tests;

public sealed class App : IDisposable
{
    private readonly OpenGlApp _windowApp;
    private readonly OpenGlWindow _mainWindow;
    private readonly OpenGlRenderedCanvas _canvas;
    private readonly GlSharedResources _shared;
    private readonly MultiChildView _gui;

    private readonly GlfwInputSystem _inputSystem;
    private readonly FreeTypeFontBackend _fontBackend;
    private readonly FontHandle _defaultFont;
    private readonly ContextMenuManager _contextMenuManager;
    private readonly PopupWindowFactory _popupFactory;
    private readonly GlImageManager _imageManager;

    private GlFrameBufferHandle _frameBufferHandle;
    private ImageView _modelView;
    private int _modelMatrixUniformLocation;
    private int _viewProjectionMatrixUniformLocation;

    public App(StartupConfig startupConfig)
    {
        _windowApp = new OpenGlApp(startupConfig);
        _mainWindow = (OpenGlWindow)_windowApp.MainWindow;
        _imageManager = new GlImageManager();
        _imageManager.LoadImageFromFile("Assets/Icons/arrow_right.png");
        _imageManager.LoadImageFromFile("Assets/Icons/arrow_up.png");
        _imageManager.LoadImageFromFile("Assets/Icons/arrow_down.png");
        _frameBufferHandle = _imageManager.CreateFrameBuffer(640, 480);

        _fontBackend = new FreeTypeFontBackend();
        _defaultFont = _fontBackend.LoadFontFromFile("Assets/Fonts/Inter/Inter-Regular.ttf", 16);

        _shared = new GlSharedResources(_fontBackend, _imageManager);
        _canvas = new OpenGlRenderedCanvas(
            startupConfig.WindowWidth, startupConfig.WindowHeight,
            _fontBackend, _defaultFont, _shared);

        _inputSystem = new GlfwInputSystem(_mainWindow.WindowHandle, _canvas);

        _mesh = Mesh.LoadFromFile("Assets/Models/Suzan_tri.obj");
        _shaderProgram = new ShaderProgramCompiler()
            .WithVertexShader("Assets/Shaders/color_vert.glsl")
            .WithFragmentShader("Assets/Shaders/color_frag.glsl")
            .Compile();

        glUseProgram(_shaderProgram.Id);
        _modelMatrixUniformLocation = glGetUniformLocation(_shaderProgram.Id, "model_matrix");
        _viewProjectionMatrixUniformLocation = glGetUniformLocation(_shaderProgram.Id, "view_projection_matrix");
        AssertNoGlError();

        var context = new Context { Canvas = _canvas };
        context.AddService(_inputSystem.InputSystem);
#if OSX
        context.AddService<IClipboard>(new OsxClipboard());
#elif WIN
        context.AddService<IClipboard>(new Win32Clipboard());
#else
        context.AddService<IClipboard>(new AppClipboard());
#endif

        var coordinates = new WindowCoordinates(_mainWindow.WindowHandle, _canvas);
        var noopDecorator = new SampleNoopDecorator();
        _popupFactory = new PopupWindowFactory(
            _windowApp, _fontBackend, _defaultFont, _shared, metalShared: null,
            noopDecorator, context);
        _contextMenuManager = new ContextMenuManager(_popupFactory, coordinates, _inputSystem.InputSystem);
        context.AddService(_contextMenuManager);
        context.AddService<IWindowCoordinates>(coordinates);
        context.AddService<IPopupWindowFactory>(_popupFactory);

        glClearColor(0f, 0f, 0f, 0f);

        var appBar = new AppBar(this);
        var center = new Center();

        _modelView = center.ModelView;
        _modelView.ImageId = _frameBufferHandle.ImageId;

        var contents = new BorderLayoutView
        {
            North = appBar,
            Center = center,
        };

        _gui = new MultiChildView
        {
            Width = _canvas.Width,
            Height = _canvas.Height,
            Context = context,
            Children = { contents }
        };

        var ss = new StyleSheet();
        ss.AddStyleForClass("inset_panel", new Style
        {
            BackgroundColor = 0xFF000000,
            Padding = PaddingStyle.All(1),
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Left = 0xFF9C9C9C, Top = 0xFF9C9C9C,
                Right = 0xFFFFFFFF, Bottom = 0xFFFFFFFF
            },
        });
        ss.AddStyleForClass("raised_panel", new Style
        {
            BorderColor = new BorderColorStyle
            {
                Top = 0xFFFFFFFF, Left = 0xFFFFFFFF,
                Right = 0xFF9C9C9C, Bottom = 0xFF9C9C9C
            },
        });
        ss.AddStyleForClass("window_button", new Style
        {
            PreferredWidth = 10f,
            BackgroundColor = 0xFF000000,
        });
        ss.AddStyleForClass("disabled", new Style { TextColor = 0xFF959595 });
        _gui.ApplyStyleSheet(ss);

        _windowApp.OnTick += HandleTick;
        _mainWindow.OnResize += HandleResize;
        _mainWindow.OnFramebufferResize += HandleFramebufferResize;
        _mainWindow.RenderFrame = Render;

        var fov = 45f * (MathF.PI / 180f);
        var aspectRatio = 640f / 480f;
        var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspectRatio, 0.001f, 1000f);
        _viewProjectionMatrix = projectionMatrix;

        _modelMatrix = Matrix4x4.Identity;

        glEnable(GL_DEPTH_TEST);
        _stopwatch.Start();
    }

    public void Run() => _windowApp.Run();

    public void Dispose()
    {
        _windowApp.OnTick -= HandleTick;
        _mainWindow.OnResize -= HandleResize;
        _mainWindow.OnFramebufferResize -= HandleFramebufferResize;
        _popupFactory.Dispose();
        _shared.Dispose();
        _windowApp.Dispose();
    }

    private void HandleResize(int width, int height)
    {
        _gui.Width = width;
        _gui.Height = height;
        _canvas.Resize(width, height);
        _mainWindow.RenderNow();
    }

    private void HandleFramebufferResize(int width, int height)
    {
        glViewport(0, 0, width, height);
    }

    private float rr = 0f;
    private Vector3 _cameraPos = new Vector3();
    private Stopwatch _stopwatch = new();
    private int _frames = 0;

    private void HandleTick()
    {
        rr += 0.005f;
        var t = Matrix4x4.CreateTranslation(0f, 0f, -20);
        var r = Matrix4x4.CreateRotationY(rr);
        var s = Matrix4x4.CreateScale(5f, 5f, 5f);
        _modelMatrix = s * r * t;

        _inputSystem.Update();
        _contextMenuManager.Update();

        ++_frames;
        if (_stopwatch.ElapsedMilliseconds >= 1000)
        {
            _frames = 0;
            _stopwatch.Restart();
        }
    }

    private void Render()
    {
        RenderMesh();

        Glfw.GetFramebufferSize(_mainWindow.GlfwWindow, out var width, out var height);
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
        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, _frameBufferHandle.FrameBufferId);
        glViewport(0, 0, _frameBufferHandle.Width, _frameBufferHandle.Height);
        glEnable(GL_DEPTH_TEST);
        glDisable(GL_BLEND);
        glClearColor(0, 0, 0, 0);
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

    public void Exit()
    {
        Glfw.SetWindowShouldClose(_mainWindow.GlfwWindow, true);
    }

    private sealed class SampleNoopDecorator : IPopupNativeDecorator
    {
        public void DecoratePopup(IntPtr handle, bool mousePassThrough) { }
        public void BeginCapture(IntPtr handle, Action<ZGF.Geometry.PointI> cb) { }
        public void EndCapture(IntPtr handle) { }
        public void TransferCapture(IntPtr from, IntPtr to, Action<ZGF.Geometry.PointI> cb) { }
    }
}
