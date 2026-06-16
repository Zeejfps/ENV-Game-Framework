using System.Numerics;
using OpenGL.NET;
using ZGF.Desktop;
using ZGF.Gui.Bindings;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Backends.OpenGl;
using ZGF.Gui.Desktop.Components.Calendar;
using ZGF.Gui.Views;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Sandbox;

/// <summary>
/// The "GUI inside a game engine" configuration: a GuiApp forced onto the OpenGL backend,
/// with engine resources created in the startup hook and a scene rendered into a frame
/// buffer every frame via the render hook — composited into the GUI as an ImageView.
/// </summary>
public sealed class App : IDisposable
{
    private readonly GuiApp _gui;

    private GlImageManager _imageManager = null!;
    private GlFrameBufferHandle _frameBufferHandle;
    private Mesh _mesh = null!;
    private ShaderProgramInfo _shaderProgram;
    private int _modelMatrixUniformLocation;
    private int _viewProjectionMatrixUniformLocation;
    private Matrix4x4 _modelMatrix = Matrix4x4.Identity;
    private Matrix4x4 _viewProjectionMatrix;
    private float _spin;

    public App(StartupConfig startupConfig)
    {
        var builder = GuiApp.CreateBuilder(startupConfig);

        builder.Services.AddService(this);
        builder.Services.AddService(new CalendarViewModel());

        _gui = builder
            .UseRenderBackend(GuiRenderBackendKind.OpenGl)
            .UseStartup(SetupEngineResources)
            .UseRenderHook(RenderScene)
            .UseContent(BuildGui)
            .Build();

        builder.Services.Require<IFrameTicker>().Add(dt =>
        {
            _spin += 0.3f * dt;
            var t = Matrix4x4.CreateTranslation(0f, 0f, -20);
            var r = Matrix4x4.CreateRotationY(_spin);
            var s = Matrix4x4.CreateScale(5f, 5f, 5f);
            _modelMatrix = s * r * t;
            // The model matrix isn't a view, so no SetDirty schedules the next frame —
            // request it directly to keep the animation self-sustaining.
            _gui.RequestRedraw();
        });
    }

    private void SetupEngineResources(Context context)
    {
        _imageManager = context.Require<GlImageManager>();
        _imageManager.LoadImageFromFile("Assets/Icons/arrow_right.png");
        _imageManager.LoadImageFromFile("Assets/Icons/arrow_up.png");
        _imageManager.LoadImageFromFile("Assets/Icons/arrow_down.png");
        _frameBufferHandle = _imageManager.CreateFrameBuffer(640, 480);

        _mesh = Mesh.LoadFromFile("Assets/Models/Suzan_tri.obj");
        _shaderProgram = new ShaderProgramCompiler()
            .WithVertexShader("Assets/Shaders/color_vert.glsl")
            .WithFragmentShader("Assets/Shaders/color_frag.glsl")
            .Compile();

        glUseProgram(_shaderProgram.Id);
        _modelMatrixUniformLocation = glGetUniformLocation(_shaderProgram.Id, "model_matrix");
        _viewProjectionMatrixUniformLocation = glGetUniformLocation(_shaderProgram.Id, "view_projection_matrix");
        AssertNoGlError();

        var fov = 45f * (MathF.PI / 180f);
        var aspectRatio = 640f / 480f;
        _viewProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspectRatio, 0.001f, 1000f);
    }

    private View BuildGui(Context context)
    {
        var canvas = context.Canvas;
        var calendarVm = context.Require<CalendarViewModel>();

        var appBar = new AppBar().BuildView(context);
        var center = new MainPanel { ModelImageId = _frameBufferHandle.ImageId }.BuildView(context);
        var calendar = new Calendar().BuildView(context);

        var selectedLabel = new TextView(canvas)
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

        return new BorderLayoutView
        {
            North = appBar,
            West = calendarPanel,
            Center = center,
        };
    }

    private unsafe void RenderScene()
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

        glDrawElements(GL_TRIANGLES, _mesh.TriangleCount * 3, GL_UNSIGNED_INT, (void*)0);
        AssertNoGlError();

        // Hand the default framebuffer back to the GUI pass with engine state undone.
        glBindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);
        glDisable(GL_DEPTH_TEST);
    }

    public void Run() => _gui.Run();

    public void Exit() => _gui.Quit();

    public void Dispose() => _gui.Dispose();
}
