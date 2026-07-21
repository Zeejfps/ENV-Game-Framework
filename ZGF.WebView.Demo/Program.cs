using ZGF.Desktop;
using ZGF.Gui.Desktop;
using ZGF.Gui.Desktop.Components.WebView;
using ZGF.WebView.Cef;

// Minimal app that embeds a native web view. The ONLY CEF-referencing symbol touched is
// UseCefWebView(); the WebView widget itself is engine-agnostic core UI.
//
//   dotnet run --project ZGF.WebView.Demo -- https://example.com
//   dotnet run --project ZGF.WebView.Demo -- test  --screenshot out.png   (offline render check)
//   dotnet run --project ZGF.WebView.Demo -- drive --screenshot out.png   (input + JS bridge check)

var mode = args.Length > 0 ? args[0] : "https://example.com";
var screenshotIndex = Array.IndexOf(args, "--screenshot");
var screenshotPath = screenshotIndex >= 0 && screenshotIndex + 1 < args.Length ? args[screenshotIndex + 1] : null;

const string renderHtml =
    "<html><body style='margin:0;background:#ffcc00;font-family:sans-serif'>" +
    "<h1 style='color:#202080'>ZGF WebView works!</h1>" +
    "<p>Rendered off-screen by CEF into a ZGF texture.</p>" +
    "<button onclick=\"window.zgf&&window.zgf.postMessage('clicked')\">post to C#</button>" +
    "</body></html>";

// A full-window text field that echoes each edit back to C# via the JS bridge — so a driver click +
// type exercises focus, keyboard forwarding and the page→C# channel at once.
const string driveHtml =
    "<html><body style='margin:0'>" +
    "<input id='i' autofocus " +
    "style='position:fixed;inset:0;width:100%;height:100%;font-size:48px;box-sizing:border-box;background:#e8f0ff' " +
    "oninput=\"window.zgf&&window.zgf.postMessage('val:'+this.value)\">" +
    "<script>window.addEventListener('load',function(){window.zgf&&window.zgf.postMessage('loaded');});</script>" +
    "</body></html>";

string? url = null;
string? html = null;
switch (mode)
{
    case "test": html = renderHtml; break;
    case "drive": html = driveHtml; break;
    default: url = mode; break;
}

var builder = GuiApp.CreateBuilder(new StartupConfig
    {
        WindowTitle = "ZGF WebView Demo",
        WindowWidth = 1024,
        WindowHeight = 768,
    })
    .UseCefWebView();

using var app = builder.Build(new WebView
{
    Id = "web",
    Url = url,
    Html = html,
    OnMessage = msg => Console.WriteLine($"[page->C#] {msg}"),
});

if (screenshotPath != null)
{
    var thread = new Thread(() =>
    {
        Thread.Sleep(4000); // CEF subprocess start + page load + first paints.

        if (mode == "drive")
        {
            var driver = app.CreateDriver();
            driver.WaitFor("web");
            driver.Click("web");        // routes a click into the page and grabs keyboard focus
            Thread.Sleep(400);
            driver.Type("ZGF");         // forwarded as Char events; the field echoes each edit to C#
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
