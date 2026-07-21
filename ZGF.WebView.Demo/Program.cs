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

// Parse: optional "--screenshot <path>" flag, then the first positional arg is the mode/URL.
string? screenshotPath = null;
var positional = new List<string>();
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--screenshot") { screenshotPath = i + 1 < args.Length ? args[++i] : null; continue; }
    if (args[i].StartsWith("--")) continue;
    positional.Add(args[i]);
}
var mode = positional.Count > 0 ? positional[0] : "browser";

var builder = GuiApp.CreateBuilder(new StartupConfig
    {
        WindowTitle = "ZGF WebView Demo",
        WindowWidth = 1280,
        WindowHeight = 820,
    })
    .UseCefWebView();

// Root widget by mode: the rich mini-browser by default; the bare WebView for the two automated checks.
IWidget root = mode switch
{
    "test" => new WebView
    {
        Id = "web",
        Html = "<html><body style='margin:0;background:#ffcc00;font-family:sans-serif'>" +
               "<h1 style='color:#202080'>ZGF WebView works!</h1>" +
               "<p>Rendered off-screen by CEF into a ZGF texture.</p></body></html>",
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
    _ => new BrowserScreen { InitialUrl = mode == "browser" ? null : mode },
};

using var app = builder.Build(root);

if (screenshotPath != null)
{
    var thread = new Thread(() =>
    {
        Thread.Sleep(4500); // CEF subprocess start + page load + first paints.

        if (mode == "drive")
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
