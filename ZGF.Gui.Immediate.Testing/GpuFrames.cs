using System.Globalization;
using System.Text;
using ZGF.AppUtils;
using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Gui.Hosting;
using ZGF.Gui.Testing;
using ZGF.Gui.Testing.Gpu;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>The two files a <c>Look</c> wrote, as absolute paths.</summary>
public sealed record LookFiles(string Text, string Png)
{
    public override string ToString() => $"{Text}\n{Png}";
}

/// <summary>Makes <see cref="GpuFrames{TState}"/>.</summary>
public static class GpuFrames
{
    /// <summary>The seconds <see cref="Running{TState}(GpuDevice, ImmediateGuiApp{TState}, int, int, float, BackgroundWork?, Func{IReadOnlyList{SystemFontFile}}?, TimeSpan?, Action{string}?)"/>
    /// waits for the app to be ready by default.</summary>
    public static readonly TimeSpan DefaultReadyTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Frames of <paramref name="app"/>'s <c>OnUpdate</c> drawn by the shipping canvas on
    /// <paramref name="device"/>, a window <paramref name="width"/> by <paramref name="height"/> points at
    /// <paramref name="dpiScale"/>, returned once ready, as <see cref="GpuFrames{TState}.WaitUntilReady"/> says, without
    /// advancing time: <paramref name="background"/>'s work and the fallback fonts <paramref name="fallbackFonts"/> finds
    /// (the system's by default) loaded, posted work run, and a frame drawn after. Animations still running are left
    /// to <see cref="GpuFrames{TState}.Settle"/>; <see cref="GpuFrames{TState}.Animating"/> and every <c>Look</c> say so.</summary>
    /// <param name="output">Where <c>Look</c> reports the files it writes, besides the console, such as an xUnit
    /// test's output.</param>
    /// <exception cref="TimeoutException">Not ready within <paramref name="readyTimeout"/>.</exception>
    public static GpuFrames<TState> Running<TState>(GpuDevice device, ImmediateGuiApp<TState> app, int width, int height,
        float dpiScale = 1f, BackgroundWork? background = null, Func<IReadOnlyList<SystemFontFile>>? fallbackFonts = null,
        TimeSpan? readyTimeout = null, Action<string>? output = null)
        where TState : class, IUiState =>
        Running(device, app.UiState, app.Update, width, height, dpiScale, background, fallbackFonts, readyTimeout, output);

    /// <summary>As <see cref="Running{TState}(GpuDevice, ImmediateGuiApp{TState}, int, int, float, BackgroundWork?, Func{IReadOnlyList{SystemFontFile}}?, TimeSpan?, Action{string}?)"/>,
    /// each frame recording <paramref name="body"/> on a <see cref="Ui{T}"/> carrying <paramref name="state"/>.</summary>
    public static GpuFrames<TState> Running<TState>(GpuDevice device, TState state, Action<Ui<TState>> body, int width, int height,
        float dpiScale = 1f, BackgroundWork? background = null, Func<IReadOnlyList<SystemFontFile>>? fallbackFonts = null,
        TimeSpan? readyTimeout = null, Action<string>? output = null)
        where TState : class, IUiState
    {
        var frames = new GpuFrames<TState>(device, state, body, width, height, dpiScale, background ?? new BackgroundWork(), output);
        try
        {
            frames.LoadFallbackFonts(fallbackFonts ?? (() => SystemFallbackFonts.Find()));
            frames.WaitUntilReady(readyTimeout ?? DefaultReadyTimeout);
            return frames;
        }
        catch
        {
            frames.Dispose();
            throw;
        }
    }
}

/// <summary>Drives an immediate-mode UI as <see cref="UiFrames{TState}"/> does, with the same input, stepping,
/// readiness and selector actions, but draws every frame with the shipping GPU canvas into offscreen targets on a
/// <see cref="GpuDevice"/>: the main window's, and one for each popup and <see cref="Ui.Window"/>, placed on a fake
/// monitor the size of the main window. Every call runs on the device's render thread, as do the frames' posted work
/// and <c>await</c> continuations. Coordinates are the main window's UI points, origin top-left.</summary>
public sealed class GpuFrames<TState> : IDisposable where TState : class, IUiState
{
    private const int DefaultPixels = 16;

    private readonly GpuDevice _device;
    private readonly ICanvasSession _session;
    private readonly CapturingCanvas _main;
    private readonly GpuPopupHost _popups;
    private readonly FontRegistry _registry;
    private readonly FrameDriver<TState> _driver;
    private readonly BackgroundWork _background;
    private readonly Action<string>? _output;
    private bool _disposed;

    internal GpuFrames(GpuDevice device, TState state, Action<Ui<TState>> body, int width, int height, float dpiScale,
        BackgroundWork background, Action<string>? output)
    {
        _device = device;
        _background = background;
        _output = output;
        _session = device.CreateSession(width, height, dpiScale);
        try
        {
            (_main, _popups, _registry, _driver) = device.Run(() =>
            {
                _session.Canvas.RegisterBoldFont(null, LoadFont(EmbeddedAssets.LoadBytes(typeof(RenderedCanvasBase).Assembly, "Inter-SemiBold.ttf"), 0));
                var main = new CapturingCanvas(_session.Canvas);
                var popups = new GpuPopupHost(_session) { Area = new UiRect(0f, 0f, width, height) };
                var registry = new FontRegistry(_session.Fonts, _session.Canvas, LoadFont);
                var driver = new FrameDriver<TState>(state, main, popups, new GpuTarget(this), width, height, body, background);
                return (main, popups, registry, driver);
            });
        }
        catch
        {
            _session.Dispose();
            throw;
        }
        ScratchDirectory = ScratchFolder.Find();
    }

    public GpuDevice Device => _device;

    /// <summary>What every frame's <see cref="Ui{T}"/> carries; change it between frames.</summary>
    public TState State => _driver.State;

    /// <summary>The main window's size in points.</summary>
    public int Width => _session.Size.Width;

    public int Height => _session.Size.Height;

    /// <summary>Device pixels per UI point of the main window and every popup.</summary>
    public float DpiScale => _session.Size.DpiScale;

    /// <summary>How many frames have been drawn.</summary>
    public long FrameCount { get; private set; }

    /// <summary>Where <see cref="Look(string)"/> writes: <c>scratch/</c> at the repository root by default.</summary>
    public string ScratchDirectory { get; set; }

    /// <summary>The seconds each <see cref="Frame"/> advances the clock by.</summary>
    public float FrameTime
    {
        get => _driver.FrameTime;
        set => _driver.FrameTime = value;
    }

    /// <summary>The fake monitor popups are kept on and windows centered in, in UI points; the main window's
    /// rect by default.</summary>
    public UiRect MonitorArea
    {
        get => _popups.Area;
        set => _popups.Area = value;
    }

    public TestClipboard Clipboard => _driver.Clipboard;
    public TestFilePicker FilePicker => _driver.FilePicker;

    /// <summary>What <see cref="Ui.ListSystemFonts"/> lists, answered once the frame that asked is drawn.</summary>
    public TestSystemFonts SystemFonts => _driver.SystemFonts;

    /// <summary>The folders the app asked to show in the OS file manager, in order.</summary>
    public List<string> OpenedFolders => _driver.OpenedFolders;

    /// <summary>The files the app asked to show selected in the OS file manager, in order.</summary>
    public List<string> RevealedFiles => _driver.RevealedFiles;

    /// <summary>The files the app asked to open in the OS's app for them, in order.</summary>
    public List<string> OpenedFiles => _driver.OpenedFiles;

    /// <summary>The web pages the app asked to open in the browser, in order.</summary>
    public List<string> OpenedUrls => _driver.OpenedUrls;

    public double RealTime => _driver.RealTime;

    /// <summary>The cursor the last frame asked for.</summary>
    public MouseCursor Cursor => _driver.Cursor;

    /// <summary>Whether a widget that takes text held focus at the end of the last frame.</summary>
    public bool WantsTextInput => _driver.WantsTextInput;

    /// <summary>Whether a frame asked the app to close with <see cref="Ui.Quit"/>.</summary>
    public bool QuitRequested => _driver.QuitRequested;

    /// <summary>Runs <paramref name="count"/> frames, each <see cref="FrameTime"/> long.</summary>
    public GpuFrames<TState> Frame(int count = 1) => Do(() => _driver.Frame(count));

    public GpuFrames<TState> Wait(double seconds) => Do(() => _driver.Wait(seconds));

    /// <summary>The host locks the pointer to the window, or frees it, as it does for <see cref="Ui.LockPointer"/>.</summary>
    public GpuFrames<TState> LockPointer(bool locked) => Do(() => _driver.LockPointer(locked));

    public GpuFrames<TState> MovePointer(float x, float y) => Do(() => _driver.MovePointer(x, y));

    public GpuFrames<TState> MovePointer(PointF at) => MovePointer(at.X, at.Y);

    public GpuFrames<TState> Press(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(() => _driver.Press(button, modifiers));

    public GpuFrames<TState> Release(PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(() => _driver.Release(button, modifiers));

    public GpuFrames<TState> Wheel(double y, double x = 0) => Do(() => _driver.Wheel(y, x));

    /// <summary>A scroll on a trackpad: the fingers' <paramref name="gesture"/> phase, or the system's coast after
    /// them when <paramref name="momentum"/> is not <see cref="ScrollPhase.None"/>, as macOS reports them.</summary>
    public GpuFrames<TState> Swipe(double x, double y, ScrollPhase gesture, ScrollPhase momentum = ScrollPhase.None) =>
        Do(() => _driver.Swipe(x, y, gesture, momentum));

    /// <summary>Files dragged from the OS file manager are dropped where the pointer is.</summary>
    public GpuFrames<TState> DropFiles(params string[] paths) => Do(() => _driver.DropFiles(paths));

    public GpuFrames<TState> DismissPopups() => Do(_driver.DismissPopups);

    /// <summary>Another app takes the focus, as when the user switches to it.</summary>
    public GpuFrames<TState> Background() => Do(() => _driver.SetForeground(false));

    /// <summary>The app takes the focus back.</summary>
    public GpuFrames<TState> Foreground() => Do(() => _driver.SetForeground(true));

    /// <summary>The main window is minimized to the dock or taskbar, or restored from it.</summary>
    public GpuFrames<TState> Minimized(bool minimized) => Do(() => _driver.Minimized(minimized));

    /// <summary>The app lost focus: held buttons come up without the user releasing them.</summary>
    public GpuFrames<TState> LoseFocus() => Do(_driver.LoseFocus);

    public GpuFrames<TState> Key(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => Do(() => _driver.Key(key, modifiers));

    public GpuFrames<TState> ReleaseKey(KeyboardKey key, KeyModifiers modifiers = KeyModifiers.None) => Do(() => _driver.ReleaseKey(key, modifiers));

    /// <summary>Presses <paramref name="shortcut"/>'s chords in turn, with the modifiers they take on the platform
    /// the tests run on.</summary>
    public GpuFrames<TState> Key(Shortcut shortcut) => Do(() => _driver.Key(shortcut));

    public GpuFrames<TState> Type(string text) => Do(() => _driver.Type(text));

    /// <summary>The IME replaces its composition with <paramref name="text"/>, caret at <paramref name="caret"/>
    /// (the end by default); an empty text ends it.</summary>
    public GpuFrames<TState> Compose(string text, int? caret = null, IReadOnlyList<PreeditBlock>? blocks = null, int focusedBlock = -1) =>
        Do(() => _driver.Compose(text, caret, blocks, focusedBlock));

    /// <summary>Moves to (x, y), lets a frame see the pointer there, then presses and releases
    /// <paramref name="button"/> over two more frames.</summary>
    public GpuFrames<TState> Click(float x, float y, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(() => _driver.Click(x, y, _driver.BoundBody, button, modifiers));

    /// <summary>As <see cref="Click"/>, then presses and releases <paramref name="button"/> again over two more frames.</summary>
    public GpuFrames<TState> DoubleClick(float x, float y, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(() => _driver.DoubleClick(x, y, button, modifiers));

    /// <summary>Clicks the middle of the element <see cref="Ui.GetId(string)"/> names in the root scope.</summary>
    public GpuFrames<TState> ClickOn(string id, PointerButton button = PointerButton.Left) => Do(() =>
    {
        var body = _driver.BoundBody;
        _driver.Click(_driver.RectOf(body, ui => ui.GetId(id)), body, button, KeyModifiers.None);
    });

    /// <summary>Moves to <paramref name="from"/> and presses there, then moves through <paramref name="path"/>, a
    /// frame after each step; release to drop.</summary>
    public GpuFrames<TState> Drag(PointF from, params PointF[] path) => Drag(KeyModifiers.None, from, path);

    /// <summary>As <see cref="Drag(PointF, PointF[])"/>, pressing with <paramref name="modifiers"/> held.</summary>
    public GpuFrames<TState> Drag(KeyModifiers modifiers, PointF from, params PointF[] path) => Do(() => _driver.Drag(modifiers, from, path));

    /// <summary>The last frame as a test sees it. The first call runs a frame without advancing time to build it;
    /// every frame after builds one too.</summary>
    public SemanticSnapshot Snapshot() => _device.Run(_driver.Snapshot);

    /// <summary>The one node <paramref name="selector"/> names in the last frame; throws a
    /// <see cref="UiTargetException"/> when there is none or more than one, with a <c>Look</c> of that moment.</summary>
    public SemanticNode Find(Selector selector) => Guarded(() => _device.Run(() => _driver.Find(selector)));

    /// <summary>Whether a press in the last frame would reach the node <paramref name="selector"/> names, through
    /// the same topmost-hit and modal rules input follows.</summary>
    public TargetResult Target(Selector selector) => _device.Run(() => _driver.Target(selector));

    /// <summary>Moves the pointer onto the node <paramref name="selector"/> names, at a point a press would reach
    /// it, and runs a frame; throws a <see cref="UiTargetException"/> with a <c>Look</c> of that moment, moving
    /// nothing, when no such point exists, or when after that frame a press there would no longer reach it.</summary>
    public GpuFrames<TState> MoveTo(Selector selector) => Do(() => _driver.MoveTo(selector));

    /// <summary>Clicks the node <paramref name="selector"/> names, as <see cref="MoveTo"/> then a press and a
    /// release over two more frames; throws as <see cref="MoveTo"/> does, clicking nothing.</summary>
    public GpuFrames<TState> ClickOn(Selector selector, PointerButton button = PointerButton.Left, KeyModifiers modifiers = KeyModifiers.None) =>
        Do(() => _driver.ClickOn(selector, button, modifiers));

    /// <summary>As <see cref="WaitUntil"/>, until exactly one node matches <paramref name="selector"/>, and returns it;
    /// the <see cref="TimeoutException"/> says what was found instead and holds a <c>Look</c> of that moment.</summary>
    public SemanticNode WaitFor(Selector selector, TimeSpan timeout) => Guarded(() => _driver.WaitFor(selector, timeout));

    /// <summary>The seconds apart <see cref="Settle"/> runs frames.</summary>
    public const float SettleFrameTime = FrameDriver<TState>.SettleFrameTime;

    /// <summary>Runs frames <see cref="SettleFrameTime"/> apart until one ends with nothing but
    /// ambient updates pending, or <paramref name="maxSeconds"/> of virtual time have passed.</summary>
    public SettleResult Settle(double maxSeconds = 5) => _device.Run(() => _driver.Settle(maxSeconds, SettleFrameTime));

    /// <summary>Runs a frame, then more without advancing time for as long as work is posted or one asks for the next
    /// with <see cref="Ui.RequestUpdate"/>, until neither is pending and
    /// <paramref name="predicate"/>, run on the render thread, holds. Throws a
    /// <see cref="TimeoutException"/>, with a <c>Look</c> of that moment, once <paramref name="timeout"/> of real
    /// time has passed.</summary>
    public GpuFrames<TState> WaitUntil(Func<bool> predicate, TimeSpan timeout) => Waiting(() => _driver.WaitUntil(predicate, timeout));

    /// <summary>As <see cref="WaitUntil"/>, until the background work these frames were given, fallback fonts
    /// included, has finished and a frame has run after it. Frames asked for with <see cref="Ui.RequestAnimationFrame"/> are
    /// not waited for; <see cref="Animating"/> reports them and <see cref="Settle"/> runs them out. Waits on the calling thread, running each frame on
    /// the render thread, so tracked work may use the device meanwhile.</summary>
    public GpuFrames<TState> WaitUntilReady(TimeSpan timeout) => Waiting(() => _driver.WaitUntilReady(timeout));

    /// <summary>What still animates after the last frame, which readiness does not wait for and <see cref="Settle"/> runs out.</summary>
    public FramePending Animating => _driver.Animating;

    /// <summary>Sizes the main window, and the fake monitor with it, from the next frame.</summary>
    public GpuFrames<TState> Resize(int width, int height) => Do(() =>
    {
        _session.Resize(width, height);
        _driver.Width = width;
        _driver.Height = height;
        _popups.Area = new UiRect(0f, 0f, width, height);
    });

    /// <summary>Every text the last frame drew, on every surface, where it drew it.</summary>
    public IReadOnlyList<DrawnText> DrawnText() => _device.Run(() => _main.Texts.Concat(_popups.Shown.SelectMany(s => s.Canvas.Texts)).ToList());

    /// <summary>Runs on the render thread whenever a popup's or window's surface is disposed after it closed.</summary>
    internal Action? SurfaceReleased
    {
        set => _device.Run(() => _popups.SurfaceReleased = value);
    }

    /// <summary>The last frame as the screen shows it: the main window with every window and popup drawn over
    /// it where it is, at <see cref="DpiScale"/>. RGBA, rows top-down, premultiplied alpha.</summary>
    public RgbaImage Read() => _device.Run(Composite);

    /// <summary>What <paramref name="surface"/>, one of the last snapshot's, drew in the last frame, alone, at its own scale.</summary>
    /// <exception cref="ArgumentException"><paramref name="surface"/> is not one of the last snapshot's.</exception>
    public RgbaImage Read(SemanticSurface surface) => _device.Run(() => SurfaceOf(surface).Read());

    /// <summary>Writes the last frame's snapshot, and the text drawn that no node of it shows, to
    /// <c>scratch/&lt;name&gt;.txt</c>, and <see cref="Read()"/> to <c>scratch/&lt;name&gt;.png</c>, and reports
    /// both paths. Builds the snapshot with a frame that does not advance time if the last frame built none.</summary>
    public LookFiles Look(string name) => _device.Run(() =>
    {
        var snapshot = _driver.Snapshot();
        return Write(name, Describe(snapshot), Composite());
    });

    /// <summary>As <see cref="Look(string)"/>, of the node <paramref name="selector"/> names: its subtree, and the
    /// pixels its own surface drew around it in the last frame, <paramref name="margin"/> points beyond its bounds and
    /// cut to the surface, at that surface's scale, each pixel <paramref name="magnify"/> times as wide and high.</summary>
    public LookFiles Look(string name, Selector selector, float margin = 8f, int magnify = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(magnify);
        var node = Find(selector);
        return _device.Run(() =>
        {
            var crop = Crop(SurfaceOf(node.Surface).Read(), node.Surface, node.Bounds, margin);
            var text = new StringBuilder();
            text.Append("# ").Append(selector).Append(" in frame ").Append(FrameCount).Append(", ").Append(margin.ToString(CultureInfo.InvariantCulture))
                .Append("pt around it, ").Append(magnify).Append("x\n");
            AppendSubtree(text, node, 0);
            return Write(name, text.ToString(), magnify == 1 ? crop : crop.Magnify(magnify));
        });
    }

    /// <summary>As <see cref="Look(string)"/>, of <paramref name="surface"/> alone, one of the last snapshot's.</summary>
    public LookFiles Look(string name, SemanticSurface surface) => _device.Run(() =>
    {
        var snapshot = _driver.Snapshot();
        return Write(name, Describe(snapshot), SurfaceOf(surface).Read());
    });

    /// <summary>Draws the app again at <paramref name="dpiScale"/>, without advancing time, and writes it as
    /// <see cref="Look(string)"/> does to <c>&lt;name&gt;@&lt;scale&gt;x-frame&lt;n&gt;</c>, then draws it at its own scale again.
    /// The rerender is not the frame the app showed: glyph baking, snapping and stroke widths follow the scale.</summary>
    public LookFiles LookAtScale(string name, float dpiScale) => _device.Run(() =>
    {
        var own = DpiScale;
        Rescale(dpiScale);
        try
        {
            _driver.Ui.BuildsSemantics = true;
            _driver.Run(0f, _driver.BoundBody);
            var snapshot = _driver.Snapshot();
            var scale = dpiScale.ToString("0.##", CultureInfo.InvariantCulture);
            return Write($"{name}@{scale}x-frame{FrameCount}", Describe(snapshot), Composite());
        }
        finally
        {
            Rescale(own);
            _driver.Run(0f, _driver.BoundBody);
        }
    });

    private void Rescale(float dpiScale)
    {
        _session.Resize(Width, Height, dpiScale);
        _driver.Ui.InvalidateMeasurements();
    }

    private GpuFrames<TState> Do(Action action) => Waiting(() => _device.Run(action));

    private GpuFrames<TState> Waiting(Action action)
    {
        Guarded(() =>
        {
            action();
            return true;
        });
        return this;
    }

    /// <summary>Runs <paramref name="action"/> on the calling thread; a target that cannot be reached, or a wait that
    /// times out, writes a <c>Look</c> of that moment and names its files in the exception.</summary>
    private T Guarded<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (UiTargetException failed)
        {
            throw failed.WithNote(LookAtFailure());
        }
        catch (TimeoutException timedOut)
        {
            throw new TimeoutException(LookAtFailure() + "\n" + timedOut.Message, timedOut);
        }
    }

    private string LookAtFailure() => _device.Run(() =>
    {
        var snapshot = _driver.Snapshot();
        var files = Write($"failure-frame{FrameCount}", Describe(snapshot), Composite());
        return $"Look at the moment it failed:\n{files}";
    });

    /// <summary>The target <paramref name="surface"/>, one of the last snapshot's, was drawn into.</summary>
    private ICanvasSurface SurfaceOf(SemanticSurface surface)
    {
        var snapshot = _driver.Ui.Snapshot ?? throw new ArgumentException("No frame has built a snapshot yet.", nameof(surface));
        var canvas = snapshot.CanvasOf(surface);
        if (canvas == _main)
            return _session;
        var shown = _popups.ShownOn(canvas);
        if (shown == null)
            throw new ArgumentException($"{surface} was not shown in the last frame.", nameof(surface));
        return shown.Surface;
    }

    private RgbaImage Composite()
    {
        var image = _session.Read();
        var scale = DpiScale;
        foreach (var shown in _popups.Shown)
            image = image.Overlaid(shown.Surface.Read(), (int)MathF.Round(shown.Rect.X * scale), (int)MathF.Round(shown.Rect.Y * scale));
        return image;
    }

    /// <summary>The pixels of <paramref name="image"/>, <paramref name="surface"/>'s, around <paramref name="bounds"/>,
    /// which are in main-window UI points.</summary>
    private static RgbaImage Crop(RgbaImage image, SemanticSurface surface, UiRect bounds, float margin)
    {
        var scale = surface.DpiScale;
        var x = bounds.X - surface.Rect.X;
        var y = bounds.Y - surface.Rect.Y;
        var left = Math.Clamp((int)MathF.Floor((x - margin) * scale), 0, image.Width - 1);
        var top = Math.Clamp((int)MathF.Floor((y - margin) * scale), 0, image.Height - 1);
        var right = Math.Clamp((int)MathF.Ceiling((x + bounds.Width + margin) * scale), left + 1, image.Width);
        var bottom = Math.Clamp((int)MathF.Ceiling((y + bounds.Height + margin) * scale), top + 1, image.Height);
        return image.Crop(left, top, right - left, bottom - top);
    }

    private string Describe(SemanticSnapshot snapshot)
    {
        var text = new StringBuilder();
        var scale = DpiScale.ToString("0.##", CultureInfo.InvariantCulture);
        text.Append("# png: the main window with every window and popup over it, ").Append(_session.Size.PixelWidth).Append('x')
            .Append(_session.Size.PixelHeight).Append(" px at @").Append(scale).Append("x: pixel = UI point x ").Append(scale).Append('\n');
        var animating = Animation(_driver.Animating);
        if (animating != null)
            text.Append("# still animating: ").Append(animating).Append("; Settle() runs it out\n");
        text.Append(snapshot.ToText());
        var unshown = Unannotated(snapshot);
        if (unshown.Count == 0)
            return text.ToString();
        text.Append("# drawn text no node shows:\n");
        foreach (var drawn in unshown)
        {
            text.Append("text \"").Append(drawn.Text.Replace("\"", "\\\"")).Append("\" ").Append(Rect(drawn.Bounds));
            if (drawn.Surface != SurfaceKind.Main)
                text.Append(" on ").Append(drawn.Surface.ToString().ToLowerInvariant());
            text.Append('\n');
        }
        return text.ToString();
    }

    private static string? Animation(FramePending animating)
    {
        var reasons = new List<string>();
        if ((animating & FramePending.Animation) != 0)
            reasons.Add("an animation asks for the next frame");
        if ((animating & FramePending.ScheduledUpdate) != 0)
            reasons.Add("an update is scheduled");
        return reasons.Count == 0 ? null : string.Join(", ", reasons);
    }

    /// <summary>The text the last frame drew that no snapshot node on the same surface, overlapping it, holds in its
    /// label or value, as custom painting draws.</summary>
    private static List<DrawnText> Unannotated(SemanticSnapshot snapshot)
    {
        var unannotated = new List<DrawnText>();
        foreach (var surface in snapshot.Surfaces)
        {
            if (snapshot.CanvasOf(surface) is not CapturingCanvas canvas) continue;
            var nodes = snapshot.Nodes.Where(n => n.Surface == surface).ToList();
            foreach (var drawn in canvas.Texts)
            {
                if (string.IsNullOrWhiteSpace(drawn.Text)) continue;
                if (!nodes.Any(n => Overlap(n.Bounds, drawn.Bounds) && Holds(n, drawn.Text)))
                    unannotated.Add(drawn);
            }
        }
        return unannotated;
    }

    private static bool Holds(SemanticNode node, string text) =>
        node.Label?.Contains(text, StringComparison.Ordinal) == true || node.Value?.Contains(text, StringComparison.Ordinal) == true;

    private static bool Overlap(UiRect a, UiRect b) => a.X < b.Right && b.X < a.Right && a.Y < b.Bottom && b.Y < a.Bottom;

    private static void AppendSubtree(StringBuilder text, SemanticNode node, int depth)
    {
        text.Append(' ', depth * 2).Append(node).Append('\n');
        foreach (var child in node.Children)
            AppendSubtree(text, child, depth + 1);
    }

    private static string Rect(UiRect rect) => FormattableString.Invariant($"[{MathF.Round(rect.X)},{MathF.Round(rect.Y)} {MathF.Round(rect.Width)}x{MathF.Round(rect.Height)}]");

    private LookFiles Write(string name, string text, RgbaImage image)
    {
        Directory.CreateDirectory(ScratchDirectory);
        var files = new LookFiles(Path.GetFullPath(Path.Combine(ScratchDirectory, name + ".txt")), Path.GetFullPath(Path.Combine(ScratchDirectory, name + ".png")));
        File.WriteAllText(files.Text, text);
        image.SavePng(files.Png);
        var report = $"Look \"{name}\":\n  {files.Text}\n  {files.Png}";
        Console.WriteLine(report);
        _output?.Invoke(report);
        return files;
    }

    internal void LoadFallbackFonts(Func<IReadOnlyList<SystemFontFile>> find)
    {
        var ui = _driver.Ui;
        var work = Task.Run(() =>
        {
            foreach (var font in find())
            {
                var data = File.ReadAllBytes(font.Path);
                ui.Post(() => RegisterFallbackFont(data, font.FaceIndex));
            }
        });
        _background.Track("fallback fonts", work);
    }

    private void RegisterFallbackFont(byte[] data, int faceIndex)
    {
        if (_disposed) return;
        _session.Fonts.RegisterFallbackFont(LoadFont(data, faceIndex));
        _driver.Ui.InvalidateMeasurements();
    }

    private FontHandle LoadFont(byte[] data, int faceIndex) =>
        _session.Fonts.LoadFontFromMemory(data, Math.Max(1, (int)MathF.Round(DefaultPixels * DpiScale)), faceIndex);

    /// <summary>Releases the main canvas, every popup's and their targets on the render thread.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.Dispose();
    }

    /// <summary>Draws each frame on the main session and the popup host's surfaces, and registers the fonts it asks for.</summary>
    private sealed class GpuTarget(GpuFrames<TState> frames) : IFrameTarget
    {
        public void BeginFrame()
        {
            frames._registry.ReleaseUnused();
            frames._session.Fonts.AdvanceFrame();
            frames._session.BeginFrame();
            frames._main.Reset(SurfaceKind.Main, new UiRect(0f, 0f, frames.Width, frames.Height));
            frames._popups.BeginFrame();
        }

        public T Run<T>(Func<T> work) => frames._device.Run(work);

        public void EndFrame(uint background)
        {
            frames._popups.EndFrame();
            frames._session.EndFrame(background);
            frames.FrameCount++;
        }

        public bool ApplyFontChange(FontChange change)
        {
            if (change.Face == null)
                frames._registry.Unregister(change.Family);
            else
                frames._registry.Register(change.Face);
            frames._popups.FollowFonts();
            return true;
        }
    }
}
