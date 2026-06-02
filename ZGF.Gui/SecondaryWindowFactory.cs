using ZGF.Core;
using ZGF.Fonts;
using static GL46;
using static ZGF.Core.MacOs.Objc;

namespace ZGF.Gui;

/// <summary>
/// Creates decorated, resizable secondary windows (see <see cref="ISecondaryWindowFactory"/>).
/// Mirrors the canvas/input/context/render wiring of <see cref="PopupWindowFactory"/>, but the
/// windows are persistent (not pooled), have no capture/outside-click behavior, and handle
/// their own resize and native-close lifecycle.
/// </summary>
public sealed class SecondaryWindowFactory : ISecondaryWindowFactory
{
    private readonly IApp _app;
    private readonly FreeTypeFontBackend _fonts;
    private readonly FontHandle _defaultFont;
    private readonly GlSharedResources? _glShared;
    private readonly MetalSharedResources? _metalShared;
    private readonly Context _mainContext;
    private readonly RenderedCanvasBase? _mainCanvasForFontRegistry;

    private readonly List<SecondaryWindowImpl> _active = new();

    public SecondaryWindowFactory(
        IApp app,
        FreeTypeFontBackend fonts,
        FontHandle defaultFont,
        GlSharedResources? glShared,
        MetalSharedResources? metalShared,
        Context mainContext,
        RenderedCanvasBase? mainCanvasForFontRegistry = null)
    {
        _app = app;
        _fonts = fonts;
        _defaultFont = defaultFont;
        _glShared = glShared;
        _metalShared = metalShared;
        _mainContext = mainContext;
        _mainCanvasForFontRegistry = mainCanvasForFontRegistry;
    }

    public ISecondaryWindow Open(in SecondaryWindowRequest request)
    {
        var window = _app.CreateWindow(new WindowOptions
        {
            WidthPoints = request.Width,
            HeightPoints = request.Height,
            Title = request.Title,
        });

        RenderedCanvasBase canvas;
        if (_glShared != null)
        {
            window.MakeContextCurrent();
            canvas = new OpenGlRenderedCanvas(request.Width, request.Height, _fonts, _defaultFont, _glShared, window.DpiScale);
            if (_mainCanvasForFontRegistry != null)
                canvas.CopyFontsFrom(_mainCanvasForFontRegistry);
        }
        else if (_metalShared != null)
        {
            canvas = new MetalRenderedCanvas(request.Width, request.Height, _fonts, _defaultFont, _metalShared, window.DpiScale);
            if (_mainCanvasForFontRegistry != null)
                canvas.CopyFontsFrom(_mainCanvasForFontRegistry);
        }
        else
        {
            throw new InvalidOperationException("Either glShared or metalShared must be non-null.");
        }

        var input = new GlfwInputSystem(window.WindowHandle, canvas);

        var context = new Context(_mainContext);
        context.Canvas = canvas;
        context.AddService(input.InputSystem);
        context.AddService<IWindowCoordinates>(new WindowCoordinates(window.WindowHandle, canvas));

        var impl = new SecondaryWindowImpl(window, canvas, input, context, _glShared, _metalShared);
        impl.SetRoot(request.Root);

        // Paint once before showing so the first frame isn't a flash of an empty window.
        window.MakeContextCurrent();
        window.RenderNow();
        window.Show();

        _active.Add(impl);
        return impl;
    }

    /// <summary>
    /// Ticks each window's input and disposes any that requested close. Called once per app
    /// tick (deferring disposal out of the GLFW close callback that set the flag).
    /// </summary>
    public void Update()
    {
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var w = _active[i];
            if (w.CloseRequested)
            {
                _active.RemoveAt(i);
                w.Dispose();
                // w.Dispose() left no GL context current (it destroyed its own window after
                // deleting its objects under its own context). Restore the main context so the
                // run loop's next GL calls — and any GL work between now and the next per-window
                // MakeContextCurrent — target a valid context.
                _app.MakeMainContextCurrent();
            }
            else
            {
                w.UpdateInput();
            }
        }
    }

    public void Dispose()
    {
        foreach (var w in _active) w.Dispose();
        _active.Clear();
    }
}

internal sealed class SecondaryWindowImpl : ISecondaryWindow, IDisposable
{
    private readonly IWindow _window;
    private readonly RenderedCanvasBase _canvas;
    private readonly GlfwInputSystem _input;
    private readonly Context _context;
    private readonly GlSharedResources? _glShared;
    private readonly MetalSharedResources? _metalShared;
    private View? _root;
    private bool _disposed;

    public IWindow Window => _window;
    public bool CloseRequested { get; private set; }
    public event Action? Closed;

    public SecondaryWindowImpl(
        IWindow window,
        RenderedCanvasBase canvas,
        GlfwInputSystem input,
        Context context,
        GlSharedResources? glShared,
        MetalSharedResources? metalShared)
    {
        _window = window;
        _canvas = canvas;
        _input = input;
        _context = context;
        _glShared = glShared;
        _metalShared = metalShared;

        _input.OnAnyInput = () => _window.RequestRedraw();
        _window.OnResize += HandleResize;
        _window.OnFramebufferResize += HandleFramebufferResize;
        // The native close button asks to close — defer the actual teardown to the next
        // factory Update() so we don't destroy the window from inside its GLFW callback.
        _window.OnClose += () => CloseRequested = true;

        WireRenderFrame();
    }

    public void SetRoot(View? root)
    {
        if (_root != null) _root.Context = null;
        _root = root;
        if (root != null)
        {
            root.Width = _window.Width;
            root.Height = _window.Height;
            root.Context = _context;
        }
    }

    public void UpdateInput() => _input.Update();

    public void Close() => CloseRequested = true;

    private void HandleResize(int width, int height)
    {
        _canvas.Resize(width, height);
        if (_root != null)
        {
            _root.Width = width;
            _root.Height = height;
        }
        // Repaint synchronously so a live drag-resize doesn't show stretched/stale content.
        _window.MakeContextCurrent();
        _window.RenderNow();
    }

    private void HandleFramebufferResize(int width, int height)
    {
        // Keep the atlas/viewport DPI in sync when the window moves between monitors of
        // different scale. The canvas recomputes its glViewport from Width*DpiScale each frame.
        _canvas.UpdateDpiScale(_window.DpiScale);
    }

    private void WireRenderFrame()
    {
        if (_window is OpenGlWindow ogw && _glShared != null)
        {
            ogw.RenderFrame = () =>
            {
                glClearColor(0f, 0f, 0f, 1f);
                glClear(GL_COLOR_BUFFER_BIT);
                _canvas.BeginFrame();
                if (_root != null)
                {
                    _root.LayoutSelf();
                    _root.DrawSelf();
                }
                _canvas.EndFrame();
            };
        }
        else if (_window is MetalWindow mw && _metalShared != null)
        {
            var nextDrawableSel = Sel("nextDrawable");
            var commandBufferSel = Sel("commandBuffer");
            var renderCommandEncoderSel = Sel("renderCommandEncoderWithDescriptor:");
            var endEncodingSel = Sel("endEncoding");
            var presentDrawableSel = Sel("presentDrawable:");
            var commitSel = Sel("commit");
            var textureSel = Sel("texture");
            mw.RenderFrame = () =>
            {
                var queue = _metalShared.CommandQueue;
                var drawable = msg_IntPtr(mw.Layer, nextDrawableSel);
                if (drawable == IntPtr.Zero) return;
                var commandBuffer = msg_IntPtr(queue, commandBufferSel);
                var descClass = Class("MTLRenderPassDescriptor");
                var desc = msg_IntPtr(descClass, Sel("renderPassDescriptor"));
                Retain(desc);
                var attachments = msg_IntPtr(desc, Sel("colorAttachments"));
                var color0 = msg_IntPtr_NUInt_NUInt(attachments, Sel("objectAtIndexedSubscript:"), 0, 0);
                msg_Void_IntPtr(color0, Sel("setTexture:"), msg_IntPtr(drawable, textureSel));
                msg_Void_UInt(color0, Sel("setLoadAction:"), 2);
                msg_Void_UInt(color0, Sel("setStoreAction:"), 1);

                var encoder = msg_IntPtr(commandBuffer, renderCommandEncoderSel, desc);
                _canvas.BeginFrame();
                if (_root != null)
                {
                    _root.LayoutSelf();
                    _root.DrawSelf();
                }
                ((MetalRenderedCanvas)_canvas).EndFrame(encoder, commandBuffer);
                msg_Void(encoder, endEncodingSel);
                msg_Void_IntPtr(commandBuffer, presentDrawableSel, drawable);
                msg_Void(commandBuffer, commitSel);
                Release(desc);
            };
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _window.OnResize -= HandleResize;
        _window.OnFramebufferResize -= HandleFramebufferResize;
        SetRoot(null);
        // VAOs are per-context (not shared across the GL share group). Make THIS window's
        // context current before deleting the canvas's objects, otherwise glDeleteVertexArrays
        // runs against whatever context is current (often the main window) and destroys that
        // context's same-named VAOs — corrupting the main window's rendering.
        _window.MakeContextCurrent();
        if (_canvas is IDisposable d) d.Dispose();
        _window.Dispose();
        Closed?.Invoke();
    }
}
