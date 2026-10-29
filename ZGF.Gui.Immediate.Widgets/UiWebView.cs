using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>
/// A web page that <see cref="UiWidgets.WebView"/> shows and forwards input to. The app opens it, keeps it
/// for as long as it shows it, and disposes it after; the page keeps running in frames that do not show
/// it. Its frames arrive between the UI's, each asking for a frame of the UI to show it.
/// </summary>
public sealed class WebPage : IDisposable
{
    private readonly Ui _ui;
    private readonly HashSet<KeyboardKey> _heldKeys = new();
    private int _widthPx;
    private int _heightPx;
    private float _pixelScale;
    private bool _focused;
    private bool _pointerInside;
    private PointF _pointer = new(float.NaN, float.NaN);
    private bool _rightDown;
    private bool _middleDown;
    private bool _disposed;

    private WebPage(Ui ui, IWebViewFactory factory, WebViewSetup setup)
    {
        _ui = ui;
        ImageId = "webpage:" + Guid.NewGuid().ToString("N");
        View = factory.Create(new FrameSink(this), setup);
        View.CursorChanged += cursor => Cursor = CursorOf(cursor);
    }

    /// <summary>The browser behind the page, to navigate, run script in, and exchange messages with.</summary>
    public IWebView View { get; }

    internal string ImageId { get; }

    internal bool HasFrame { get; private set; }

    /// <summary>The image holding the page's latest frame, to draw as a picture anywhere and at any size,
    /// such as while it moves out of its element; null before the first frame.</summary>
    public string? FrameImageId => HasFrame ? ImageId : null;

    internal MouseCursor Cursor { get; private set; } = MouseCursor.Default;

    public static WebPage Open(Ui ui, IWebViewFactory factory, string url, WebViewSetup? setup = null)
    {
        var page = new WebPage(ui, factory, setup ?? new WebViewSetup());
        page.View.Navigate(url);
        return page;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        View.Dispose();
        _ui.RemoveImage(ImageId);
    }

    internal void Fit(UiRect rect, float pixelScale)
    {
        var widthPx = Math.Max(1, (int)MathF.Round(rect.Width * pixelScale));
        var heightPx = Math.Max(1, (int)MathF.Round(rect.Height * pixelScale));
        if (widthPx == _widthPx && heightPx == _heightPx && pixelScale == _pixelScale)
            return;
        (_widthPx, _heightPx, _pixelScale) = (widthPx, heightPx, pixelScale);
        View.Resize(widthPx, heightPx, pixelScale);
    }

    internal void Focus(bool focused)
    {
        if (focused == _focused)
            return;
        _focused = focused;
        if (!focused)
            ReleaseHeldKeys();
        View.SetFocus(focused);
    }

    internal void Point(Ui ui, UiRect rect, Interaction interaction)
    {
        var input = ui.Input;
        var following = interaction.Hovered || interaction.Held || _rightDown || _middleDown;
        if (!following)
        {
            if (_pointerInside)
                View.SendMouseLeave();
            _pointerInside = false;
            _pointer = new PointF(float.NaN, float.NaN);
            return;
        }
        _pointerInside = true;
        ui.SetCursor(Cursor);
        var at = new PointF(input.Pointer.X - rect.X, input.Pointer.Y - rect.Y);
        var modifiers = ModifiersOf(input);
        if (at != _pointer)
        {
            _pointer = at;
            View.SendMouseMove(at.X, at.Y, modifiers);
        }
        if (interaction.Pressed)
            View.SendMouseButton(at.X, at.Y, WebMouseButton.Left, true, Math.Max(1, interaction.ClickCount), modifiers);
        if (interaction.Held && input.WasReleased(PointerButton.Left))
            View.SendMouseButton(at.X, at.Y, WebMouseButton.Left, false, 1, modifiers);
        _rightDown = Button(input, interaction.Hovered, PointerButton.Right, WebMouseButton.Right, _rightDown, at, modifiers);
        _middleDown = Button(input, interaction.Hovered, PointerButton.Middle, WebMouseButton.Middle, _middleDown, at, modifiers);
        if (interaction.Hovered && (input.ScrollX != 0f || input.ScrollY != 0f))
            View.SendMouseWheel(at.X, at.Y, input.ScrollX * WheelStep, input.ScrollY * WheelStep, modifiers);
    }

    internal void Type(UiInput input)
    {
        var modifiers = ModifiersOf(input);
        foreach (var stroke in input.Keys)
        {
            _heldKeys.Add(stroke.Key);
            var code = WebKeys.VirtualKeyOf(stroke.Key);
            View.SendKey(WebKeyEventKind.RawKeyDown, code, code, '\0', modifiers, isSystemKey: false);
        }
        foreach (var c in input.Text)
            View.SendKey(WebKeyEventKind.Char, c, c, c, WebInputModifiers.None, isSystemKey: false);
        foreach (var stroke in input.ReleasedKeys)
        {
            if (!_heldKeys.Remove(stroke.Key))
                continue;
            var code = WebKeys.VirtualKeyOf(stroke.Key);
            View.SendKey(WebKeyEventKind.KeyUp, code, code, '\0', modifiers, isSystemKey: false);
        }
        input.TakeKey(KeyboardKey.Tab);
    }

    private const float WheelStep = 40f;

    private bool Button(UiInput input, bool hovered, PointerButton button, WebMouseButton webButton, bool down, PointF at, WebInputModifiers modifiers)
    {
        if (!down && hovered && input.WasPressed(button))
        {
            View.SendMouseButton(at.X, at.Y, webButton, true, 1, modifiers);
            return true;
        }
        if (down && (input.WasReleased(button) || !input.IsDown(button)))
        {
            View.SendMouseButton(at.X, at.Y, webButton, false, 1, modifiers);
            return false;
        }
        return down;
    }

    private void ReleaseHeldKeys()
    {
        foreach (var key in _heldKeys)
        {
            var code = WebKeys.VirtualKeyOf(key);
            View.SendKey(WebKeyEventKind.KeyUp, code, code, '\0', WebInputModifiers.None, isSystemKey: false);
        }
        _heldKeys.Clear();
    }

    private void Show(bool shown)
    {
        if (!shown)
            return;
        HasFrame = true;
        _ui.RequestFrame();
    }

    private static WebInputModifiers ModifiersOf(UiInput input)
    {
        var modifiers = WebInputModifiers.None;
        var keys = input.Modifiers;
        if ((keys & KeyModifiers.Shift) != 0) modifiers |= WebInputModifiers.Shift;
        if ((keys & KeyModifiers.Control) != 0) modifiers |= WebInputModifiers.Control;
        if ((keys & KeyModifiers.Alt) != 0) modifiers |= WebInputModifiers.Alt;
        if ((keys & KeyModifiers.Super) != 0) modifiers |= WebInputModifiers.Meta;
        if (input.IsDown(PointerButton.Left)) modifiers |= WebInputModifiers.LeftMouseButton;
        if (input.IsDown(PointerButton.Middle)) modifiers |= WebInputModifiers.MiddleMouseButton;
        if (input.IsDown(PointerButton.Right)) modifiers |= WebInputModifiers.RightMouseButton;
        return modifiers;
    }

    private static MouseCursor CursorOf(WebCursor cursor) => cursor switch
    {
        WebCursor.Hand or WebCursor.Pointer => MouseCursor.Hand,
        WebCursor.Text => MouseCursor.Text,
        WebCursor.Crosshair => MouseCursor.Crosshair,
        WebCursor.ResizeHorizontal => MouseCursor.ResizeHorizontal,
        WebCursor.ResizeVertical => MouseCursor.ResizeVertical,
        _ => MouseCursor.Default,
    };

    private sealed class FrameSink(WebPage page) : IWebViewFrameSink
    {
        public bool AcceptsSharedTextures(SharedTextureKind kind) => page._ui.SupportsSharedTextures(kind);

        public void OnPixels(int widthPx, int heightPx, ReadOnlySpan<byte> rgbaTopDown) =>
            page.Show(page._ui.SetImage(page.ImageId, widthPx, heightPx, rgbaTopDown));

        public bool OnSharedTexture(in SharedTexture texture)
        {
            var shown = page._ui.SetSharedTextureImage(page.ImageId, texture);
            page.Show(shown);
            return shown;
        }
    }
}

public static partial class UiWidgets
{
    /// <summary>Shows <paramref name="page"/> in an element laid out by <paramref name="box"/>, sized to it
    /// in device pixels, drawn like any other picture so it clips, fades and transforms with what holds
    /// it. A press in it gives it focus, which takes text, so app shortcuts should check
    /// <see cref="Ui.WantsTextInput"/>; while it has focus it gets the keys and text, Tab included.</summary>
    public static void WebView(this Ui ui, string id, WebPage page, Box box = default)
    {
        var i = ui.Interact(id);
        var input = ui.Input;
        if (i.Pressed || (i.Hovered && (input.WasPressed(PointerButton.Right) || input.WasPressed(PointerButton.Middle))))
            ui.Focus(i.Id);
        var focused = ui.HasFocus(i.Id);
        page.Focus(focused);
        if (ui.TryGetRect(i.Id, out var rect))
        {
            page.Fit(rect, ui.PixelScale);
            page.Point(ui, rect, i);
        }
        if (focused)
            page.Type(input);
        using (ui.Box(i.Id, box))
            ui.Paint((page, ui.Theme.Surface), static (painter, shown) => Picture(painter, shown.page, shown.Surface));
    }

    private static void Picture(UiPainter painter, WebPage page, uint placeholder)
    {
        if (page.HasFrame)
            painter.Image(painter.Rect, page.ImageId);
        else
            painter.FillRect(painter.Rect, placeholder);
    }
}
