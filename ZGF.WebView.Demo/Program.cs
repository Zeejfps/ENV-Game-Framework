using System.Diagnostics;
using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Components.WebView;
using ZGF.Gui.Widgets;
using ZGF.WebView.Cef;
using ZGF.WebView.Demo;

// Embeds a native web view. The ONLY CEF-referencing symbol touched is UseCefWebView(); the WebView
// widget and the surrounding browser chrome are engine-agnostic ZGF UI.
//
//   dotnet run --project ZGF.WebView.Demo                         # mini-browser (native chrome + web view)
//   dotnet run --project ZGF.WebView.Demo -- https://example.com  # open a URL in the mini-browser
//   dotnet run --project ZGF.WebView.Demo -- test  --screenshot out.png   # bare-pipeline render check
//   dotnet run --project ZGF.WebView.Demo -- drive --screenshot out.png   # input + JS-bridge check
//   dotnet run --project ZGF.WebView.Demo -- bench <url> [--seconds 20] [--warmup 10] [--click x,y]... [--settle 2] [--software] [--screenshot out.png]
//
// --software forces CEF's CPU OnPaint path; --stats prints CEF paints per second; --click sends a click
// at web-view logical point x,y to the page itself (not the OS) after the warmup, one per second, then
// waits --settle seconds before measuring.

var options = DemoOptions.Parse(args);

var builder = GuiApp.CreateBuilder(new StartupConfig
    {
        WindowTitle = "ZGF WebView Demo",
        WindowWidth = 1280,
        WindowHeight = 820,
        StartUnfocused = options.Mode == "bench",
    })
    .UseCefWebView(new CefWebViewOptions
    {
        PaintPath = options.Software ? CefPaintPath.Software : CefPaintPath.PreferAccelerated,
        ReportPaintStats = options.Stats || options.Mode == "bench",
    });

var bench = new Bench(options);
if (options.Mode == "bench")
    builder.UseRenderHook(bench.Frames.Record);

// Root widget by mode: the rich mini-browser by default; the bare WebView for the automated checks.
IWidget root = options.Mode switch
{
    "test" => new WebView
    {
        Id = "web",
        Html = "<html><body style='margin:0;background:#ffcc00;font-family:sans-serif'>" +
               "<h1 style='color:#202080'>ZGF WebView works!</h1>" +
               "<p>Rendered off-screen by CEF into a ZGF texture.</p>" +
               "<div style='position:fixed;left:0;bottom:0;width:200px;height:60px;background:#ff0000'></div>" +
               "<div style='position:fixed;right:0;bottom:0;width:200px;height:60px;background:#0000ff'></div>" +
               "</body></html>",
    },
    "drive" => new WebView
    {
        Id = "web",
        Html = "<html><body style='margin:0'><input id='i' autofocus " +
               "style='position:fixed;inset:0;width:100%;height:100%;font-size:48px;box-sizing:border-box;background:#e8f0ff' " +
               "oninput=\"window.zgf&&window.zgf.postMessage('val:'+this.value)\">" +
               "<script>window.addEventListener('load',function(){window.zgf&&window.zgf.postMessage('loaded');});</script>" +
               "</body></html>",
        OnMessage = msg => Console.WriteLine($"[page->C#] {msg}"),
    },
    "bench" => new BenchWebView(options.Url!, bench.Attach),
    _ => new BrowserScreen { InitialUrl = options.Mode == "browser" ? null : options.Mode },
};

using var app = builder.Build(root);

if (options.Mode == "bench")
{
    var thread = new Thread(() => bench.Run(app)) { IsBackground = true };
    thread.Start();
}
else if (options.ScreenshotPath != null)
{
    var screenshotPath = options.ScreenshotPath;
    var thread = new Thread(() =>
    {
        Thread.Sleep(4500); // CEF subprocess start + page load + first paints.

        if (options.Mode == "drive")
        {
            var driver = app.CreateDriver();
            driver.WaitFor("web");
            driver.Click("web");
            Thread.Sleep(400);
            driver.Type("ZGF");
            Thread.Sleep(1200);
        }

        app.CaptureScreenshot(screenshotPath, () => Console.WriteLine($"[screenshot] wrote {screenshotPath}"));
        Thread.Sleep(1500);
        app.Quit();
    })
    {
        IsBackground = true,
    };
    thread.Start();
}

app.Run();

internal sealed record DemoOptions(
    string Mode, string? Url, string? ScreenshotPath, bool Software, bool Stats, int Seconds, int WarmupSeconds, int SettleSeconds,
    IReadOnlyList<(float X, float Y)> Clicks)
{
    public static DemoOptions Parse(string[] args)
    {
        string? screenshot = null;
        var software = false;
        var stats = false;
        var seconds = 20;
        var warmup = 10;
        var settle = 2;
        var clicks = new List<(float X, float Y)>();
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--screenshot": screenshot = i + 1 < args.Length ? args[++i] : null; break;
                case "--software": software = true; break;
                case "--stats": stats = true; break;
                case "--seconds": seconds = int.Parse(args[++i]); break;
                case "--warmup": warmup = int.Parse(args[++i]); break;
                case "--settle": settle = int.Parse(args[++i]); break;
                case "--click": clicks.Add(ParsePoint(args[++i])); break;
                default:
                    if (!args[i].StartsWith("--"))
                        positional.Add(args[i]);
                    break;
            }
        }
        var mode = positional.Count > 0 ? positional[0] : "browser";
        if (mode == "bench" && positional.Count < 2)
            throw new ArgumentException("bench needs a URL: bench <url>");
        var url = mode == "bench" ? positional[1] : null;
        return new DemoOptions(mode, url, screenshot, software, stats, seconds, warmup, settle, clicks);
    }

    private static (float X, float Y) ParsePoint(string text)
    {
        var parts = text.Split(',');
        if (parts.Length != 2)
            throw new ArgumentException($"--click takes x,y, got '{text}'");
        return (float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
    }
}
