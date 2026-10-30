using ZGF.Gui.Immediate.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Tests;

public sealed class WebViewTests
{
    private readonly FakeWebViews _web = new();
    private readonly UiFrames _frames;
    private WebPage? _page;
    private bool _showPage = true;

    public WebViewTests() => _frames = new UiFrames(new DefaultUiState(), width: 400f, height: 300f, body: Body);

    private void Body(Ui ui)
    {
        using var screen = ui.Screen();
        _page ??= WebPage.Open(ui, _web, "https://example.com/");
        if (_showPage)
            ui.WebView("page", _page, new Box { Width = Sizing.Fixed(200f), Height = Sizing.Fixed(100f) });
        ui.Button("Elsewhere", id: "elsewhere");
    }

    private FakeWebView View => _web.View!;

    private UiRect Page => _frames.RectOf("page");

    private void ClickPage(float x, float y) => _frames.Click(Page.X + x, Page.Y + y);

    [Fact]
    public void Opening_a_page_navigates_its_view()
    {
        _frames.Frame();

        Assert.Equal("navigate https://example.com/", View.Events[0]);
    }

    [Fact]
    public void The_view_is_sized_to_the_element_in_device_pixels()
    {
        _frames.Canvas.DpiScale = 2f;
        _frames.Frame(2);

        Assert.Contains("resize 400x200@2", View.Events);
    }

    [Fact]
    public void A_placeholder_shows_until_the_first_frame_then_the_frame()
    {
        _frames.Frame(2);
        Assert.Empty(_frames.Canvas.Images);

        _web.Sink!.OnPixels(2, 2, new byte[16]);
        _frames.Frame();

        var image = Assert.Single(_frames.Canvas.Images);
        Assert.Equal(Page.Width, image.Inputs.Position.Width);
    }

    [Fact]
    public void The_latest_frame_can_be_drawn_as_a_picture_once_there_is_one()
    {
        _frames.Frame();
        Assert.Null(_page!.FrameImageId);

        _web.Sink!.OnPixels(2, 2, new byte[16]);

        Assert.NotNull(_page.FrameImageId);
        Assert.Contains(_page.FrameImageId, _frames.Canvas.ImageIds);
    }

    [Fact]
    public void A_click_reaches_the_page_where_it_landed_and_focuses_it()
    {
        _frames.Frame();
        ClickPage(10f, 20f);

        Assert.Equal(["move 10,20", "focus True", "down Left 1", "up Left"], View.Events.Where(IsPointerOrFocus).ToArray());
    }

    [Fact]
    public void Keys_and_text_reach_a_focused_page_down_then_text_then_up()
    {
        _frames.Frame();
        ClickPage(10f, 10f);
        View.Events.Clear();

        _frames.Key(KeyboardKey.A).Type("a").ReleaseKey(KeyboardKey.A).Frame();

        Assert.Equal(["key RawKeyDown 65", "key Char 97", "key KeyUp 65"], View.Events.Where(IsKey).ToArray());
    }

    [Fact]
    public void Keys_carry_the_scan_code_a_page_reads_their_physical_key_from()
    {
        _frames.Frame();
        ClickPage(10f, 10f);
        View.NativeKeyCodes.Clear();

        _frames.Key(KeyboardKey.W).Key(KeyboardKey.UpArrow).Key(KeyboardKey.Space).Frame();

        Assert.Equal([0x11, 0xE048, 0x39], View.NativeKeyCodes);
    }

    [Fact]
    public void An_unfocused_page_gets_no_keys()
    {
        _frames.Frame();

        _frames.Key(KeyboardKey.A).Type("a").ReleaseKey(KeyboardKey.A).Frame();

        Assert.DoesNotContain(View.Events, IsKey);
    }

    [Fact]
    public void Tab_stays_with_the_page()
    {
        _frames.Frame();
        ClickPage(10f, 10f);

        _frames.Key(KeyboardKey.Tab).Frame().ReleaseKey(KeyboardKey.Tab).Frame(2);

        Assert.Contains("key RawKeyDown 9", View.Events);
        Assert.Contains("key KeyUp 9", View.Events);
        Assert.DoesNotContain("focus False", View.Events);
    }

    [Fact]
    public void Focus_moving_away_releases_the_keys_still_held()
    {
        _frames.Frame();
        ClickPage(10f, 10f);
        _frames.Key(KeyboardKey.W).Frame();
        View.Events.Clear();

        _frames.ClickOn("elsewhere");

        Assert.Equal(["key KeyUp 87", "focus False"], View.Events.Where(e => IsKey(e) || e.StartsWith("focus")).ToArray());
    }

    [Fact]
    public void The_pointer_leaving_the_page_is_told_once()
    {
        _frames.Frame();
        _frames.MovePointer(Page).Frame(2);
        _frames.MovePointer(-50f, -50f).Frame(3);

        Assert.Single(View.Events, e => e == "leave");
    }

    [Fact]
    public void The_wheel_over_the_page_scrolls_it()
    {
        _frames.Frame();
        _frames.MovePointer(Page).Frame(2);

        _frames.Wheel(-1).Frame();

        Assert.Contains("wheel 0,-40", View.Events);
    }

    [Fact]
    public void The_page_keeps_running_while_not_shown_and_closes_when_disposed()
    {
        _frames.Frame();
        _web.Sink!.OnPixels(2, 2, new byte[16]);
        _showPage = false;
        _frames.Frame(3);
        Assert.False(View.Disposed);

        _page!.Dispose();

        Assert.True(View.Disposed);
        Assert.Empty(_frames.Canvas.ImageIds);
    }

    private static bool IsKey(string e) => e.StartsWith("key ");

    private static bool IsPointerOrFocus(string e) => e.StartsWith("move") || e.StartsWith("down") || e.StartsWith("up") || e.StartsWith("focus");

    private sealed class FakeWebViews : IWebViewFactory
    {
        public FakeWebView? View { get; private set; }
        public IWebViewFrameSink? Sink { get; private set; }

        public IWebView Create(IWebViewFrameSink frames, WebViewSetup setup)
        {
            Sink = frames;
            View = new FakeWebView();
            return View;
        }
    }

    private sealed class FakeWebView : IWebView
    {
        public List<string> Events { get; } = new();
        public bool Disposed { get; private set; }

        public event Action<string>? MessageReceived { add { } remove { } }
        public event Action<string>? TitleChanged { add { } remove { } }
        public event Action<string>? UrlChanged { add { } remove { } }
        public event Action<WebCursor>? CursorChanged { add { } remove { } }

        public void Navigate(string url) => Events.Add($"navigate {url}");
        public void LoadHtml(string html, string baseUrl = "about:blank") => Events.Add("html");
        public void GoBack() { }
        public void GoForward() { }
        public void Reload() { }
        public void ExecuteScript(string js) { }
        public void PostMessage(string json) { }
        public void Resize(int widthPx, int heightPx, float dpiScale) => Events.Add($"resize {widthPx}x{heightPx}@{dpiScale}");
        public void SendMouseMove(float x, float y, WebInputModifiers modifiers) => Events.Add($"move {x},{y}");
        public void SendMouseLeave() => Events.Add("leave");
        public void SendMouseButton(float x, float y, WebMouseButton button, bool pressed, int clickCount, WebInputModifiers modifiers) =>
            Events.Add(pressed ? $"down {button} {clickCount}" : $"up {button}");
        public void SendMouseWheel(float x, float y, float deltaX, float deltaY, WebInputModifiers modifiers) => Events.Add($"wheel {deltaX},{deltaY}");
        public List<int> NativeKeyCodes { get; } = new();

        public void SendKey(WebKeyEventKind kind, int windowsKeyCode, int nativeKeyCode, char character, WebInputModifiers modifiers, bool isSystemKey)
        {
            Events.Add($"key {kind} {windowsKeyCode}");
            NativeKeyCodes.Add(nativeKeyCode);
        }
        public void SetFocus(bool focused) => Events.Add($"focus {focused}");
        public void Dispose() => Disposed = true;
    }
}
