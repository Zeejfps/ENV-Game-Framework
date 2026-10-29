namespace ZGF.Gui;

/// <summary>
/// A single off-screen-rendered web view. Lives in core <c>ZGF.Gui</c> next to
/// <see cref="IClipboard"/> so the <see cref="Views.WebViewView"/> widget can be written and resolved
/// without referencing any web engine; the concrete implementation (CEF) is an opt-in package that an
/// app pulls in with <c>UseCefWebView()</c>. All members are used on the UI thread; the implementation
/// marshals any engine callbacks onto it.
/// <para>The engine renders off-screen and pushes each frame to the <see cref="IWebViewFrameSink"/> it was
/// created with, which hands it to the canvas as an image, so a web view composes with the rest of the
/// canvas exactly like an <see cref="Views.ImageView"/>.</para>
/// </summary>
public interface IWebView : IDisposable
{
    // --- Navigation / content -------------------------------------------------
    void Navigate(string url);
    void LoadHtml(string html, string baseUrl = "about:blank");
    void GoBack();
    void GoForward();
    void Reload();

    // --- Scripting / JS<->C# bridge ------------------------------------------
    /// <summary>Runs <paramref name="js"/> in the page's top frame (fire-and-forget).</summary>
    void ExecuteScript(string js);

    /// <summary>C# → page: delivers <paramref name="json"/> to a <c>window.zgf.onmessage</c> handler
    /// the page installs.</summary>
    void PostMessage(string json);

    /// <summary>Page → C#: raised (on the UI thread) when the page calls
    /// <c>window.zgf.postMessage(string)</c>.</summary>
    event Action<string>? MessageReceived;

    // --- Page state -----------------------------------------------------------
    event Action<string>? TitleChanged;
    event Action<string>? UrlChanged;

    /// <summary>Raised (on the UI thread) when the page requests a different mouse cursor.</summary>
    event Action<WebCursor>? CursorChanged;

    // --- Off-screen surface (UI thread only) ----------------------------------
    /// <summary>Sizes the off-screen browser to <paramref name="widthPx"/> × <paramref name="heightPx"/>
    /// device pixels rendered at <paramref name="dpiScale"/>. No-op if unchanged.</summary>
    void Resize(int widthPx, int heightPx, float dpiScale);

    // --- Input forwarding (widget-local logical points) -----------------------
    void SendMouseMove(float x, float y, WebInputModifiers modifiers);
    void SendMouseLeave();
    void SendMouseButton(float x, float y, WebMouseButton button, bool pressed, int clickCount, WebInputModifiers modifiers);
    void SendMouseWheel(float x, float y, float deltaX, float deltaY, WebInputModifiers modifiers);
    void SendKey(WebKeyEventKind kind, int windowsKeyCode, int nativeKeyCode, char character, WebInputModifiers modifiers, bool isSystemKey);
    void SetFocus(bool focused);
}

/// <summary>Creates <see cref="IWebView"/> instances. Registered on the <see cref="Context"/> by the
/// engine package (see <c>UseCefWebView()</c>); a <see cref="Views.WebViewView"/> resolves it and
/// tolerates its absence by drawing a placeholder — the same interface-in-core, resolved-from-Context
/// pattern text-input widgets use for <see cref="IClipboard"/>.</summary>
public interface IWebViewFactory
{
    /// <summary>Creates a web view that delivers its frames to <paramref name="frames"/>.</summary>
    IWebView Create(IWebViewFrameSink frames);
}

/// <summary>Receives a web view's painted frames on the UI thread. Each frame is valid only for the
/// call it arrives in.</summary>
public interface IWebViewFrameSink
{
    /// <summary>Asked before the engine starts painting: whether frames may arrive as
    /// <see cref="OnSharedTexture"/> of <paramref name="kind"/> instead of pixels.</summary>
    bool AcceptsSharedTextures(SharedTextureKind kind);

    /// <summary>A frame as straight-alpha RGBA8 rows, top row first.</summary>
    void OnPixels(int widthPx, int heightPx, ReadOnlySpan<byte> rgbaTopDown);

    /// <summary>A frame as a GPU texture. Returns false when it could not be imported.</summary>
    bool OnSharedTexture(in SharedTexture texture);
}

/// <summary>Modifier / mouse-button state carried by a forwarded web-view input event. An
/// engine-neutral subset the implementation maps onto its own flags.</summary>
[Flags]
public enum WebInputModifiers
{
    None = 0,
    Shift = 1 << 0,
    Control = 1 << 1,
    Alt = 1 << 2,
    Meta = 1 << 3,
    LeftMouseButton = 1 << 4,
    MiddleMouseButton = 1 << 5,
    RightMouseButton = 1 << 6,
}

public enum WebMouseButton
{
    Left,
    Middle,
    Right,
}

/// <summary>Kind of a forwarded key event, mirroring CEF's key-event types.</summary>
public enum WebKeyEventKind
{
    RawKeyDown,
    KeyDown,
    KeyUp,
    Char,
}

/// <summary>Engine-neutral subset of the cursor a page can request; the widget maps it onto the
/// host's own cursor type.</summary>
public enum WebCursor
{
    Default,
    Pointer,
    Hand,
    Text,
    Crosshair,
    ResizeHorizontal,
    ResizeVertical,
    Wait,
    Progress,
    NotAllowed,
    None,
}
