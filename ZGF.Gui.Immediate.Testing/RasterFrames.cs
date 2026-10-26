using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>Draws frames of a <see cref="Ui"/> onto a <see cref="RasterCanvas"/> with real fonts —
/// Inter, as a window draws with, and those the app registers — for a PNG of what the window shows.
/// It takes no input, and popups draw over the window.</summary>
public sealed class RasterFrames<TState> where TState : class, IUiState
{
    private const int DefaultPixels = 16;
    private const uint ClearColor = 0xFF000000u;

    private readonly FreeTypeFontBackend _fonts = new();
    private readonly RasterCanvas _canvas;
    private readonly FontRegistry _registry;
    private readonly Ui<TState> _ui;
    private readonly Action<Ui<TState>> _body;
    private readonly int _pixelSize;
    private readonly float _width;
    private readonly float _height;
    private double _time;

    /// <param name="body">What each frame records, as an app's update.</param>
    public RasterFrames(TState state, Action<Ui<TState>> body, int width, int height, float dpiScale = 1f)
    {
        _body = body;
        _width = width;
        _height = height;
        _pixelSize = Math.Max(1, (int)MathF.Round(DefaultPixels * dpiScale));
        var regular = LoadFont(LoadGuiFont("Inter-Regular.ttf"), 0);
        _canvas = new RasterCanvas(width, height, _fonts, regular, dpiScale, ClearColor);
        _canvas.RegisterBoldFont(null, LoadFont(LoadGuiFont("Inter-SemiBold.ttf"), 0));
        _registry = new FontRegistry(_fonts, _canvas, LoadFont);
        _ui = new Ui<TState>(state, _canvas, new TestClipboard());
    }

    /// <summary>The icon the window shows, from the next frame; null while it has none.</summary>
    public WindowIcon? WindowIcon { get; set; }

    /// <summary>The frames of <paramref name="app"/>, each what its <c>OnUpdate</c> records.</summary>
    public static RasterFrames<TState> Running(ImmediateGuiApp<TState> app, int width, int height, float dpiScale = 1f) =>
        new(app.UiState, app.Update, width, height, dpiScale);

    private static byte[] LoadGuiFont(string fileName) => EmbeddedAssets.LoadBytes(typeof(RenderedCanvasBase).Assembly, fileName);

    private FontHandle LoadFont(byte[] data, int faceIndex) => _fonts.LoadFontFromMemory(data, _pixelSize, faceIndex);

    /// <summary>Draws a frame <paramref name="seconds"/> after the last, then registers the fonts it asked for.</summary>
    public void Frame(double seconds = 1.0 / 60.0)
    {
        _time += seconds;
        var input = new UiInput();
        input.Seal();
        _canvas.BeginFrame();
        _ui.WindowIcon = WindowIcon;
        _ui.BeginFrame(input, (float)seconds, _time, _width, _height);
        _body(_ui);
        _ui.EndFrame();
        _canvas.EndFrame();
        TakeFontChanges();
    }

    private void TakeFontChanges()
    {
        if (!_ui.HasFontChanges)
            return;
        foreach (var change in _ui.TakeFontChanges())
            TakeFontChange(change);
    }

    private void TakeFontChange(FontChange change)
    {
        if (change.Face == null)
        {
            _registry.Unregister(change.Family);
            return;
        }
        _registry.Register(change.Face);
    }

    public void SavePng(string path) => _canvas.SavePng(path);

    /// <summary>The last frame drawn as straight-alpha RGBA, rows top-down, as <see cref="SavePng"/> writes it.</summary>
    public byte[] ToRgbaTopDown() => _canvas.ToRgbaTopDown();
}
