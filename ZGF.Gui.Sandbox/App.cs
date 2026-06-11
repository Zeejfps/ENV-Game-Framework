using System.Diagnostics;
using System.Numerics;
using GLFW;
using OpenGL.NET;
using ZGF.Desktop;
using ZGF.Desktop.Backends.OpenGl;
using ZGF.Fonts;
using ZGF.Gui.Bindings;
using ZGF.Gui.Components;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Backends.OpenGl;
using ZGF.Gui.Desktop.Components.Calendar;
using ZGF.Gui.Desktop.Components.ContextMenu;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Platforms.Osx;
using ZGF.Gui.Desktop.Platforms.Windows;
using ZGF.Gui.Views;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Sandbox;

public sealed class App : IDisposable
{
    private readonly OpenGlApp _windowApp;
    private readonly OpenGlWindow _mainWindow;
    private readonly OpenGlRenderedCanvas _canvas;
    private readonly GlSharedResources _shared;
    private readonly MultiChildView _gui;

    private readonly DesktopInputSystem _inputSystem;
    private readonly FreeTypeFontBackend _fontBackend;
    private readonly FontHandle _defaultFont;
    private readonly ContextMenuManager _contextMenuManager;
    private readonly PopupWindowFactory _popupFactory;
    private readonly GlImageManager _imageManager;

    private GlFrameBufferHandle _frameBufferHandle;
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

        var dpiScale = _mainWindow.DpiScale;
        _fontBackend = new FreeTypeFontBackend();
        _defaultFont = _fontBackend.LoadFontFromFile("Assets/Fonts/Inter/Inter-Regular.ttf", (int)MathF.Round(16 * dpiScale));

        _shared = new GlSharedResources(_fontBackend, _imageManager);
        _canvas = new OpenGlRenderedCanvas(
            startupConfig.WindowWidth, startupConfig.WindowHeight,
            _fontBackend, _defaultFont, _shared, dpiScale);

        var pointerArbiter = new PointerOwnershipArbiter();
        _inputSystem = new DesktopInputSystem(_mainWindow, _canvas, pointerArbiter);
        pointerArbiter.Register(_inputSystem, isModal: false);

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

        var coordinates = new WindowCoordinates(_mainWindow, _canvas);
        var noopDecorator = new SampleNoopDecorator();
        _popupFactory = new PopupWindowFactory(
            _windowApp, _fontBackend, _defaultFont,
            new GlRenderBackend(_shared, _fontBackend, _defaultFont),
            noopDecorator, context, pointerArbiter);
        _contextMenuManager = new ContextMenuManager(_popupFactory, coordinates, _inputSystem);
        context.AddService<IContextMenuHost>(_contextMenuManager);
        context.AddService<IWindowCoordinates>(coordinates);
        context.AddService<IPopupWindowFactory>(_popupFactory);

        glClearColor(0f, 0f, 0f, 0f);

        context.AddService(this);
        var appBar = new AppBar().BuildView(context);
        var center = new MainPanel { ModelImageId = _frameBufferHandle.ImageId }.BuildView(context);

        var calendarVm = new CalendarViewModel();
        context.AddService(calendarVm);
        var calendar = new Calendar().BuildView(context);
        var selectedLabel = new TextView(_canvas)
        {
            FontSize = 14,
            TextColor = 0xFFE0E0E0,
            HorizontalTextAlignment = TextAlignment.Center,
        };
        selectedLabel.BindText(() =>
            calendarVm.SelectedDate.Value is { } picked ? picked.ToString("yyyy-MM-dd") : "No date selected");

        var calendarPanel = new RectView
        {
            BackgroundColor = 0xFF101010,
            Padding = PaddingStyle.All(12),
            Children =
            {
                new ColumnView
                {
                    Gap = 10,
                    Children = { calendar, selectedLabel },
                }
            }
        };

        var contents = new BorderLayoutView
        {
            North = appBar,
            West = calendarPanel,
            Center = center,
        };

        _gui = new MultiChildView
        {
            Width = _canvas.Width,
            Height = _canvas.Height,
            Children = { contents }
        };
        _gui.Mount();
        _gui.OnRedrawNeeded = _mainWindow.RequestRedraw;
        _inputSystem.OnAnyInput = () => _mainWindow.RequestRedraw();

        _frameTicker = new FrameTicker(onActivated: _mainWindow.RequestRedraw);
        context.AddService<IFrameTicker>(_frameTicker);
        _lastAnimationTimestamp = Stopwatch.GetTimestamp();
        _frameTicker.Add(dt =>
        {
            rr += 0.3f * dt;
            var t = Matrix4x4.CreateTranslation(0f, 0f, -20);
            var r = Matrix4x4.CreateRotationY(rr);
            var sc = Matrix4x4.CreateScale(5f, 5f, 5f);
            _modelMatrix = sc * r * t;
            // The model matrix isn't a view, so no SetDirty schedules the next frame —
            // request it directly to keep the animation self-sustaining.
            _mainWindow.RequestRedraw();
        });

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
    private readonly FrameTicker _frameTicker;
    private long _lastAnimationTimestamp;

    private void HandleTick()
    {
        var now = Stopwatch.GetTimestamp();
        var dt = (float)((now - _lastAnimationTimestamp) / (double)Stopwatch.Frequency);
        _lastAnimationTimestamp = now;
        // An idle wait or a stall isn't animation time — cap the step so the first frame
        // after a gap doesn't lurch.
        const float maxStep = 0.1f;
        _frameTicker.Tick(dt > maxStep ? maxStep : dt);

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
        _gui.DrawSelf(_canvas);
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
