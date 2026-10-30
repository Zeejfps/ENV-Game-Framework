using System.Globalization;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.WebView.Cef;

// dotnet run --project ZGF.WebView.Immediate.Demo -- [url] [--overlay] [--stats] [--seconds N] [--screenshot out.png] [--shot-at N]
var options = DemoOptions.Parse(args);
new BrowserApp(options).Run(new StartupConfig
{
    WindowWidth = 1280,
    WindowHeight = 860,
    WindowTitle = "ZGF Web View (immediate mode)",
    StartUnfocused = options.Seconds != null,
});

internal sealed record DemoOptions(string Url, bool Overlay, bool Stats, double? Seconds, string? ScreenshotPath, double ShotAt)
{
    private const string DefaultUrl = "https://ad48n9lfslt0tl9zjffw.h5-apps.getreel.com/1.0.0/index.html";

    public static DemoOptions Parse(string[] args)
    {
        var url = DefaultUrl;
        var overlay = false;
        var stats = false;
        double? seconds = null;
        string? screenshot = null;
        var shotAt = 10.0;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--overlay": overlay = true; break;
                case "--stats": stats = true; break;
                case "--seconds": seconds = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--screenshot": screenshot = args[++i]; break;
                case "--shot-at": shotAt = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                default: url = args[i]; break;
            }
        }
        return new DemoOptions(url, overlay, stats, seconds, screenshot, shotAt);
    }
}

internal sealed class BrowserApp(DemoOptions options) : ImmediateGuiApp<DefaultUiState>(new DefaultUiState())
{
    private ImmediateGuiAppHost<DefaultUiState>? _host;
    private CefWebViewHost? _web;
    private WebPage? _page;
    private string _title = "";
    private bool _overlay = options.Overlay;
    private bool _shotTaken;
    private int _frames;
    private double _countedFrom;

    protected override void OnStartup(ImmediateGuiAppHost<DefaultUiState> host)
    {
        _host = host;
        _web = CefWebViewHost.Start(host.Dispatcher, new CefWebViewOptions { ReportPaintStats = options.Stats });
    }

    protected override void OnUpdate(Ui<DefaultUiState> ui)
    {
        var page = _page ??= OpenPage(ui);
        var theme = ui.Theme;
        ui.WindowBackground = theme.Background;
        using var screen = ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f });
        Toolbar(ui, page);
        using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }))
        {
            ui.WebView("page", page, new Box { Width = Sizing.Grow(), Height = Sizing.Grow() });
            if (_overlay)
                Overlay(ui);
        }
        CountFrames(ui);
        Script(ui);
    }

    protected override void OnShutdown(ImmediateGuiAppHost<DefaultUiState> host)
    {
        _page?.Dispose();
        _web?.Dispose();
    }

    private WebPage OpenPage(Ui ui)
    {
        var page = WebPage.Open(ui, _web!, options.Url);
        page.View.TitleChanged += title => _title = title;
        return page;
    }

    private void Toolbar(Ui ui, WebPage page)
    {
        var theme = ui.Theme;
        using var bar = ui.Row(new Box
        {
            Width = Sizing.Grow(),
            Padding = new Spacing(8f, 6f),
            Gap = 6f,
            AlignY = Align.Center,
            Background = theme.Surface,
        });
        if (ui.Button("Back", id: "back"))
            page.View.GoBack();
        if (ui.Button("Forward", id: "forward"))
            page.View.GoForward();
        if (ui.Button("Reload", id: "reload"))
            page.View.Reload();
        ui.Label(ui.Ellipsize(_title, 600f));
        ui.Spacer();
        if (ui.Button(_overlay ? "Hide overlay" : "Show overlay", id: "overlay"))
            _overlay = !_overlay;
    }

    private static void Overlay(Ui ui)
    {
        var theme = ui.Theme;
        var lift = 0.5f + 0.5f * MathF.Sin((float)ui.Time * 2f);
        ui.RequestUpdate();
        using var toast = ui.Float(1f, 0f, -336f, 16f, new Box
        {
            Width = Sizing.Fixed(320f),
            Padding = 12f,
            Gap = 4f,
            Radius = 10f,
            Background = 0xE0181820u,
            BorderColor = theme.Accent,
            BorderWidth = 2f,
            Transform = new UiTransform(offsetY: -6f * lift, scale: 1f + 0.03f * lift),
        });
        ui.Label("Drawn by ZGF over the page", bold: true);
        ui.Label("Clipped, faded and transformed with it", theme.TextMuted);
    }

    private void CountFrames(Ui ui)
    {
        if (!options.Stats)
            return;
        _frames++;
        var elapsed = ui.RealTime - _countedFrom;
        if (elapsed < 1.0)
            return;
        Console.Error.WriteLine($"[demo] ui frames {_frames / elapsed:F1}/s");
        _frames = 0;
        _countedFrom = ui.RealTime;
    }

    private void Script(Ui ui)
    {
        if (options.ScreenshotPath != null && !_shotTaken)
        {
            if (ui.RealTime >= options.ShotAt)
            {
                _shotTaken = true;
                var path = options.ScreenshotPath;
                _host!.RequestScreenshot(path, () => Console.WriteLine($"[screenshot] wrote {path}"));
            }
            else
            {
                ui.RequestUpdateIn(options.ShotAt - ui.RealTime);
            }
        }
        if (options.Seconds == null)
            return;
        var seconds = options.Seconds.Value;
        if (ui.RealTime >= seconds)
            ui.Quit();
        else
            ui.RequestUpdateIn(seconds - ui.RealTime);
    }
}
