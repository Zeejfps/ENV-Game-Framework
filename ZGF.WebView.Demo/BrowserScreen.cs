using ZGF.Gui;
using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Components.WebView;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;
using ZGF.KeyboardModule;
using ZGF.Observable;
// This file's namespace (ZGF.WebView.Demo) makes the bare name `WebView` resolve to the ZGF.WebView
// namespace, so alias the widget type explicitly.
using WebViewWidget = ZGF.Gui.Desktop.Components.WebView.WebView;

namespace ZGF.WebView.Demo;

/// <summary>
/// A mini-browser built from native ZGF widgets wrapped around a live CEF web view — the whole point of
/// the texture approach. The native toolbar (back/forward/reload/address bar), the native side panel
/// (bookmarks, JS↔C# bridge, live message log, page title) and a semi-transparent native overlay badge
/// all composite with the off-screen browser pixels through the ordinary canvas.
/// </summary>
public sealed record BrowserScreen : Widget
{
    private const uint AppBg = 0xFF0F172A;      // slate-900
    private const uint ChromeBg = 0xFF1E293B;   // slate-800
    private const uint PanelBg = 0xFF111827;    // gray-900
    private const uint FieldBg = 0xFF0B1220;
    private const uint BtnBg = 0xFF334155;
    private const uint BtnHover = 0xFF475569;
    private const uint Accent = 0xFF3B82F6;
    private const uint TextLight = 0xFFE2E8F0;
    private const uint TextMuted = 0xFF94A3B8;

    public string? InitialUrl { get; init; }

    private readonly WebViewController _web = new();
    private readonly State<string> _address = new("about:welcome");
    private readonly State<string> _title = new("(loading)");
    private readonly State<IReadOnlyList<string>> _log = new([]);

    private const string WelcomeHtml =
        "<!doctype html><html><head><meta charset='utf-8'><title>ZGF Browser — Welcome</title><style>" +
        "*{box-sizing:border-box}html,body{height:100%}" +
        "body{margin:0;font-family:-apple-system,Segoe UI,Roboto,sans-serif;color:#fff;" +
        "background:linear-gradient(135deg,#0ea5e9,#6366f1 55%,#a855f7);" +
        "display:flex;align-items:center;justify-content:center}" +
        ".card{background:rgba(255,255,255,.12);border:1px solid rgba(255,255,255,.25);" +
        "border-radius:22px;padding:44px 52px;max-width:660px;box-shadow:0 24px 70px rgba(0,0,0,.4)}" +
        "h1{margin:0 0 10px;font-size:44px;letter-spacing:-1px}" +
        "p{opacity:.92;line-height:1.55;font-size:19px}" +
        "button{font-size:18px;padding:13px 24px;border:0;border-radius:12px;background:#fff;" +
        "color:#4f46e5;font-weight:600;cursor:pointer;margin-top:6px}" +
        "#from{margin-top:22px;font-size:17px;min-height:24px;font-weight:600;opacity:.95}" +
        "</style></head><body><div class='card'>" +
        "<h1>ZGF &times; Chromium</h1>" +
        "<p>A live web page rendered off-screen into a texture and composited by the ZGF canvas — " +
        "framed by native ZGF widgets. Try the buttons in the native panel on the left.</p>" +
        "<button onclick=\"window.zgf&&window.zgf.postMessage('Hello from the web page!')\">" +
        "Send message to native &rarr;</button>" +
        "<div id='from'></div></div>" +
        "<script>window.zgf.onmessage=function(m){" +
        "document.getElementById('from').textContent='\\u25C0 from native: '+m;};" +
        "window.addEventListener('load',function(){window.zgf&&window.zgf.postMessage('page loaded');});" +
        "</script></body></html>";

    public BrowserScreen()
    {
        _web.TitleChanged += t => _title.Value = string.IsNullOrWhiteSpace(t) ? "(untitled)" : t;
        // Reflect real navigations in the address bar, but don't splatter a huge base64 data: URL there
        // (the welcome page is a data URL) — leave whatever friendly text is showing.
        _web.UrlChanged += u => { if (!string.IsNullOrEmpty(u) && !u.StartsWith("data:")) _address.Value = u; };
        _web.MessageReceived += m => Append($"page: {m}");
    }

    private void Append(string line)
    {
        var next = new List<string>(_log.Value) { line };
        if (next.Count > 14) next.RemoveRange(0, next.Count - 14);
        _log.Value = next;
    }

    private void Go() => _web.Navigate(Normalize(_address.Value));

    private static string Normalize(string s)
    {
        s = s.Trim();
        if (s.Length == 0 || s.StartsWith("about:")) return "about:blank";
        if (s.Contains("://") || s.StartsWith("data:")) return s;
        return "https://" + s;
    }

    protected override IWidget Build(Context ctx)
    {
        var input = ctx.Get<InputSystem>();
        if (!string.IsNullOrEmpty(InitialUrl))
            _address.Value = InitialUrl;

        return new Box
        {
            Background = AppBg,
            Children =
            [
                new BorderLayout
                {
                    North = Toolbar(input),
                    West = SidePanel(),
                    Center = Content(),
                },
            ],
        };
    }

    private IWidget Toolbar(InputSystem? input)
    {
        // Single-line TextInput bubbles Enter, so an enclosing KbmInput.OnKey turns it into "navigate".
        IWidget addressBar = new Box
        {
            Background = FieldBg,
            BorderRadius = BorderRadiusStyle.All(8),
            Children =
            [
                new Padding
                {
                    Amount = new PaddingStyle { Left = 12, Right = 12, Top = 8, Bottom = 8 },
                    Children = [new TextInput { Value = _address, Background = FieldBg, Color = TextLight, FontSize = 15 }],
                },
            ],
        };
        if (input != null)
            addressBar = new KbmInput
            {
                Child = addressBar,
                OnKey = (ref KeyboardKeyEvent e) =>
                {
                    if (e.State == InputState.Pressed && e.Key == KeyboardKey.Enter) Go();
                },
            };

        return new Box
        {
            Height = 52,
            Background = ChromeBg,
            Children =
            [
                new Padding
                {
                    Amount = PaddingStyle.All(8),
                    Children =
                    [
                        new Row
                        {
                            Gap = 6,
                            CrossAxis = CrossAxisAlignment.Center,
                            Children =
                            [
                                NavButton("Back", () => _web.GoBack()),
                                NavButton("Fwd", () => _web.GoForward()),
                                NavButton("Reload", () => _web.Reload()),
                                new Grow { Child = addressBar },
                                new Button { Label = "Go", OnClick = Go, Background = Accent, HoverBackground = 0xFF2563EB },
                            ],
                        },
                    ],
                },
            ],
        };
    }

    private Button NavButton(string label, Action onClick) => new()
    {
        Label = label,
        OnClick = onClick,
        Background = BtnBg,
        HoverBackground = BtnHover,
        Padding = new PaddingStyle { Left = 12, Right = 12, Top = 7, Bottom = 7 },
    };

    private IWidget SidePanel() => new Box
    {
        Width = 288,
        Background = PanelBg,
        Children =
        [
            new Padding
            {
                Amount = PaddingStyle.All(16),
                Children =
                [
                    new Column
                    {
                        Gap = 10,
                        Children =
                        [
                            new Text { Value = "ZGF Browser", Color = TextLight, FontSize = 20, Weight = FontWeight.Bold },
                            new Text { Value = "native widgets + a live web view", Color = TextMuted, FontSize = 13 },

                            SectionLabel("PAGE"),
                            new Text { Value = _title, Color = Accent, FontSize = 14, Weight = FontWeight.Bold, Wrap = TextWrap.Wrap },

                            SectionLabel("BOOKMARKS"),
                            NavButton("Welcome page", () => { _address.Value = "about:welcome"; _web.LoadHtml(WelcomeHtml); }),
                            NavButton("example.com", () => { _address.Value = "https://example.com"; _web.Navigate("https://example.com"); }),

                            SectionLabel("JS ↔ C# BRIDGE"),
                            NavButton("Post message to page", () => _web.PostMessage("\"Ping from the native GUI!\"")),
                            NavButton("Run JS in page", () => _web.ExecuteScript(
                                "var h=document.querySelector('h1'); if(h) h.textContent='JS ran from native!';")),

                            SectionLabel("MESSAGES FROM PAGE"),
                            new Grow
                            {
                                Child = new ScrollArea
                                {
                                    FillParent = true,
                                    Gap = 4,
                                    Children =
                                    [
                                        new Column<string>
                                        {
                                            Gap = 4,
                                            Items = _log,
                                            Template = m => new Text { Value = m, Color = TextMuted, FontSize = 12, Wrap = TextWrap.Wrap },
                                        },
                                    ],
                                },
                            },
                        ],
                    },
                ],
            },
        ],
    };

    private static IWidget SectionLabel(string text) => new Padding
    {
        Amount = new PaddingStyle { Top = 8 },
        Children = [new Text { Value = text, Color = 0xFF64748B, FontSize = 11, Weight = FontWeight.Bold }],
    };

    private IWidget Content() => new Stack
    {
        Children =
        [
            // The live browser fills the content region — either a passed URL or the welcome page.
            string.IsNullOrEmpty(InitialUrl)
                ? new WebViewWidget { Id = "web", Controller = _web, Html = WelcomeHtml }
                : new WebViewWidget { Id = "web", Controller = _web, Url = InitialUrl },

            // A semi-transparent native badge drawn ON TOP of the live web pixels — proof the web view
            // composites like any other canvas layer (z-order + alpha), which a native overlay can't do.
            new Padding
            {
                Amount = PaddingStyle.All(14),
                Children =
                [
                    new Row
                    {
                        MainAxis = MainAxisAlignment.End,
                        CrossAxis = CrossAxisAlignment.Start,
                        Children =
                        [
                            new Box
                            {
                                Opacity = 0.9f,
                                Background = 0xCC0EA5E9,
                                BorderRadius = BorderRadiusStyle.All(10),
                                Children =
                                [
                                    new Padding
                                    {
                                        Amount = new PaddingStyle { Left = 12, Right = 12, Top = 8, Bottom = 8 },
                                        Children = [new Text { Value = "native overlay, composited over live web", Color = 0xFFFFFFFF, FontSize = 12, Weight = FontWeight.Bold }],
                                    },
                                ],
                            },
                        ],
                    },
                ],
            },
        ],
    };
}
