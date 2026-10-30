using System.Diagnostics;
using ZGF.Gui;
using ZGF.Gui.Desktop;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.WebView.Demo;

/// <summary>A bare web view for the benchmark that hands its <see cref="IWebView"/> out, so the bench can
/// click inside the page without going through the OS.</summary>
internal sealed record BenchWebView(string Url, Action<IWebView> OnCreated) : Widget
{
    protected override View CreateView(Context ctx)
    {
        var view = new WebViewView(ctx.Canvas, ctx.Get<IWebViewFactory>());
        if (view.WebView == null)
            throw new InvalidOperationException("no web view engine registered");
        view.WebView.Navigate(Url);
        OnCreated(view.WebView);
        return view;
    }
}

internal sealed class Bench(DemoOptions options)
{
    private IWebView? _webView;

    public FrameIntervals Frames { get; } = new();

    public void Attach(IWebView webView) => _webView = webView;

    public void Run(GuiApp app)
    {
        var self = Process.GetCurrentProcess();
        var started = self.StartTime;
        Console.WriteLine($"[bench] {(options.Software ? "software" : "accelerated")} path, {options.Url}");
        Thread.Sleep(TimeSpan.FromSeconds(options.WarmupSeconds));

        if (options.Clicks.Count > 0)
        {
            var dispatcher = app.Context.Require<IUiDispatcher>();
            foreach (var (x, y) in options.Clicks)
            {
                dispatcher.Post(() => Click(x, y));
                Thread.Sleep(1000);
            }
            Thread.Sleep(TimeSpan.FromSeconds(options.SettleSeconds));
        }

        var selfBefore = CpuTime(self);
        var helpersBefore = HelperCpu(started);
        var wallStart = Stopwatch.GetTimestamp();
        Frames.Start();
        Thread.Sleep(TimeSpan.FromSeconds(options.Seconds));
        var intervals = Frames.Stop();
        var wall = Stopwatch.GetElapsedTime(wallStart).TotalSeconds;
        var selfCpu = (CpuTime(self) - selfBefore).TotalSeconds;
        var helpersAfter = HelperCpu(started);
        var helpersCpu = helpersAfter.Sum(p => helpersBefore.TryGetValue(p.Key, out var before) ? (p.Value - before).TotalSeconds : p.Value.TotalSeconds);

        Array.Sort(intervals);
        var cores = Environment.ProcessorCount;
        Console.WriteLine($"[bench] {wall:F1}s measured, {intervals.Length} frames presented = {intervals.Length / wall:F1} fps");
        if (intervals.Length > 0)
        {
            Console.WriteLine($"[bench] frame interval ms: avg {intervals.Average():F2} p50 {Percentile(intervals, 0.5):F2} " +
                              $"p95 {Percentile(intervals, 0.95):F2} p99 {Percentile(intervals, 0.99):F2} max {intervals[^1]:F2}");
        }
        Console.WriteLine($"[bench] CPU: ZGF process {selfCpu / wall * 100:F1}% of one core ({selfCpu / wall / cores * 100:F2}% of {cores}); " +
                          $"CEF helpers {helpersCpu / wall * 100:F1}% of one core ({helpersAfter.Count} processes)");

        if (options.ScreenshotPath != null)
        {
            var done = new ManualResetEventSlim();
            app.CaptureScreenshot(options.ScreenshotPath, done.Set);
            done.Wait(TimeSpan.FromSeconds(5));
            Console.WriteLine($"[screenshot] wrote {options.ScreenshotPath}");
        }
        app.Quit();
    }

    private void Click(float x, float y)
    {
        if (_webView == null)
            return;
        _webView.SetFocus(true);
        _webView.SendMouseMove(x, y, WebInputModifiers.None);
        _webView.SendMouseButton(x, y, WebMouseButton.Left, pressed: true, clickCount: 1, WebInputModifiers.LeftMouseButton);
        _webView.SendMouseButton(x, y, WebMouseButton.Left, pressed: false, clickCount: 1, WebInputModifiers.None);
        Console.WriteLine($"[bench] clicked {x},{y}");
    }

    private static TimeSpan CpuTime(Process process)
    {
        process.Refresh();
        return process.TotalProcessorTime;
    }

    private static Dictionary<int, TimeSpan> HelperCpu(DateTime notBefore)
    {
        var result = new Dictionary<int, TimeSpan>();
        foreach (var helper in Process.GetProcessesByName("zgf-cef-helper"))
        {
            try
            {
                if (helper.StartTime >= notBefore)
                    result[helper.Id] = helper.TotalProcessorTime;
            }
            catch (InvalidOperationException)
            {
                // Exited between listing and reading.
            }
        }
        return result;
    }

    private static double Percentile(double[] sorted, double p) =>
        sorted[Math.Clamp((int)Math.Ceiling(p * sorted.Length) - 1, 0, sorted.Length - 1)];
}

internal sealed class FrameIntervals
{
    private readonly Lock _gate = new();
    private readonly List<double> _intervalsMs = new();
    private long _last;
    private bool _recording;

    public void Record()
    {
        var now = Stopwatch.GetTimestamp();
        lock (_gate)
        {
            if (_recording && _last != 0)
                _intervalsMs.Add(Stopwatch.GetElapsedTime(_last, now).TotalMilliseconds);
            _last = now;
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            _intervalsMs.Clear();
            _last = 0;
            _recording = true;
        }
    }

    public double[] Stop()
    {
        lock (_gate)
        {
            _recording = false;
            return _intervalsMs.ToArray();
        }
    }
}
