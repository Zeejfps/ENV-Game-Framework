using System.Diagnostics;
using System.Runtime.InteropServices;
using Xilium.CefGlue;
using ZGF.Observable;

namespace ZGF.WebView.Cef;

/// <summary>
/// Owns the single process-wide CEF runtime: initialisation, the external message-loop pump, and
/// shutdown. There is one browser per <see cref="CefWebView"/> but exactly one runtime.
/// <para>Bootstrap uses the <b>separate subprocess</b> model — <see cref="CefSettings.BrowserSubprocessPath"/>
/// points at <c>zgf-cef-helper</c>, so there is no <c>ExecuteProcess</c> gate in the app's <c>Main</c>
/// and the whole lifecycle fits in a hosted service. The pump is driven by CEF's
/// <c>OnScheduleMessagePumpWork</c> callback, as cefclient's external pump is: work wanted now posts one
/// <see cref="CefRuntime.DoMessageLoopWork"/> onto the UI thread, however often it is asked for before it runs,
/// and work wanted later arms a timer that posts it then. After each pump a fallback is armed, a thirtieth of a
/// second away while a browser is open and a quarter of a second while none is, in case CEF asks for nothing.
/// So CEF's browser callbacks (including <c>OnPaint</c>) all run on ZGF's main thread, and each pump, which
/// holds that thread for some milliseconds, runs only as often as there is work for it.</para>
/// </summary>
internal sealed class CefRuntimeHost : IDisposable
{
    private readonly IUiDispatcher _dispatcher;
    private readonly List<CefWebView> _pending = new();
    private readonly Lock _gate = new();
    private Timer? _pumpTimer;
    private bool _initialized;
    private bool _disposed;
    private int _openBrowsers;

    private readonly CefWebViewOptions _options;

    /// <summary>When, in <see cref="Stopwatch"/> ticks, the pump timer is due; <see cref="long.MaxValue"/> while
    /// it is not armed. Guarded by <see cref="_gate"/>, as CEF schedules pumps from its own threads.</summary>
    private long _timerDueAt = long.MaxValue;

    /// <summary>1 while a pump is posted and has not run, so asking again does not post another.</summary>
    private int _pumpPosted;

    /// <summary>Whether a pump is running, which CEF asks for more work from.</summary>
    private bool _pumping;

    /// <summary>How soon a pump CEF asks for at once, from inside a pump, runs: not at once, as the UI thread
    /// runs what is posted to it until none is left, and a pump posting the next would keep it from drawing.</summary>
    private const int ReentrantPumpMs = 1;

    public CefRuntimeHost(IUiDispatcher dispatcher, CefWebViewOptions options)
    {
        _dispatcher = dispatcher;
        _options = options;
    }

    public bool IsInitialized => _initialized;

    /// <summary>Initialises CEF on the UI thread. Called once by the hosted service after the app is
    /// built.</summary>
    public void Initialize()
    {
        if (_initialized || _disposed)
            return;

        CefRuntime.Load(CefPaths.LibCefDir);

        // Capture Chromium's internal log to a discoverable file (the deepest source of render/GPU crash
        // detail). Honour ZGF_CEF_LOGFILE if set, else default under the profile dir; verbose only when
        // debugging, otherwise Info so a renderer's own fatal line is still recorded.
        var logFile = Environment.GetEnvironmentVariable("ZGF_CEF_LOGFILE");
        if (string.IsNullOrEmpty(logFile))
            logFile = Path.Combine(_options.ProfileFolder, "cef-debug.log");
        // CachePath, under RootCachePath, is what keeps the GPU process's shader caches out of the
        // working directory it was launched from.
        var profile = Path.Combine(_options.ProfileFolder, "Default");

        var settings = new CefSettings
        {
            WindowlessRenderingEnabled = true,
            // External pump: we call DoMessageLoopWork ourselves from the UI thread, so OnPaint & co.
            // fire there too — sidestepping GL-context threading pain.
            ExternalMessagePump = true,
            MultiThreadedMessageLoop = false,
            NoSandbox = true,
            BrowserSubprocessPath = CefPaths.HelperExe,
            // Both must be set: CachePath (a subdir of RootCachePath) is what relocates the GPU/Dawn
            // shader caches out of the working directory into the app-data profile.
            RootCachePath = _options.ProfileFolder,
            CachePath = profile,
            LogSeverity = CefLog.Enabled ? CefLogSeverity.Verbose : CefLogSeverity.Info,
            LogFile = logFile,
        };

        // macOS reads resources and locales from the framework bundle and ignores the two paths. The
        // helper bundle stands in as the main bundle, so the browser and its children agree on the
        // bundle ID their Mach port rendezvous is named after.
        if (OperatingSystem.IsMacOS())
        {
            settings.FrameworkDirPath = CefPaths.FrameworkDir;
            settings.MainBundlePath = CefPaths.HelperBundle;
            MacApplication.AdoptCefAppProtocol();
        }
        else
        {
            settings.ResourcesDirPath = CefPaths.ResourcesDir;
            settings.LocalesDirPath = CefPaths.LocalesDir;
        }

        Directory.CreateDirectory(profile);

        // Always tell the user where the deep log is — it's what to attach when a page crashes the renderer.
        Console.Error.WriteLine($"[zgf-cef] Chromium log: {logFile}");
        CefLog.Write($"initializing: libcef={CefPaths.LibCefDir}, helper={CefPaths.HelperExe}, resources={CefPaths.ResourcesDir}, locales={CefPaths.LocalesDir}");
        var mainArgs = new CefMainArgs(GetProcessArgs());
        CefRuntime.Initialize(mainArgs, settings, new ZgfBrowserCefApp(this, _options.AutoplayWithoutGesture), IntPtr.Zero);
        _initialized = true;
        CefLog.Write($"initialized (chrome {CefRuntime.ChromeVersion}); creating {_pending.Count} pending browser(s)");

        CefWebView[] pending;
        lock (_gate)
        {
            pending = _pending.ToArray();
            _pending.Clear();
        }
        foreach (var wv in pending)
            wv.CreateBrowser();

        _pumpTimer = new Timer(_ => TimerFired(), null, Timeout.Infinite, Timeout.Infinite);
        Pump();
    }

    /// <summary>The longest CEF waits for a pump while a browser is open and it asks for none, as in cefclient.</summary>
    private const int FallbackPumpMs = 1000 / 30;

    /// <summary>The longest it waits while no browser is open.</summary>
    private const int IdleFallbackPumpMs = 250;
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Runs <paramref name="action"/> at a later turn of the UI thread.</summary>
    internal void Post(Action action) => _dispatcher.Post(action);

    internal void BrowserOpened() => _openBrowsers++;

    internal void BrowserClosed() => _openBrowsers--;

    /// <summary>Registers a web view for browser creation. Before CEF is initialised the creation is
    /// deferred until <see cref="Initialize"/>; afterwards it is posted so the owning widget can finish
    /// configuring (initial URL) in the same tick before the browser is created.</summary>
    public void RegisterWebView(CefWebView webView)
    {
        if (_disposed)
            return;
        if (_initialized)
            _dispatcher.Post(webView.CreateBrowser);
        else
            lock (_gate) _pending.Add(webView);
    }

    // Called by the browser-process handler (possibly off the UI thread) when CEF wants the next pump.
    private int _scheduleCount;
    private int _pumpCount;

    public void ScheduleMessagePump(long delayMs)
    {
        if (_disposed)
            return;
        if (_scheduleCount++ < 8) CefLog.Write($"schedule pump #{_scheduleCount} in {delayMs}ms");
        if (delayMs <= 0 && !_pumping)
            PostPump();
        else
            ArmTimer(Math.Clamp(delayMs, ReentrantPumpMs, FallbackPumpMs));
    }

    /// <summary>Posts a pump to the UI thread, unless one is posted already and has not run.</summary>
    private void PostPump()
    {
        if (Interlocked.Exchange(ref _pumpPosted, 1) == 0)
            _dispatcher.Post(Pump);
    }

    /// <summary>Arms the timer to post a pump in <paramref name="delayMs"/>, unless it is due sooner already.</summary>
    private void ArmTimer(long delayMs)
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            var due = Stopwatch.GetTimestamp() + delayMs * Stopwatch.Frequency / 1000;
            if (due >= _timerDueAt)
                return;
            _timerDueAt = due;
            _pumpTimer?.Change(delayMs, Timeout.Infinite);
        }
    }

    private void TimerFired()
    {
        lock (_gate)
            _timerDueAt = long.MaxValue;
        PostPump();
    }

    private void Pump()
    {
        Interlocked.Exchange(ref _pumpPosted, 0);
        if (!_initialized || _disposed)
            return;
        // A pump now does the work a timer was armed for, as cefclient's DoWork does; CEF arms it again,
        // inside the pump, for work it wants later.
        lock (_gate)
        {
            _timerDueAt = long.MaxValue;
            _pumpTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
        if (_pumpCount++ < 8) CefLog.Write($"pump #{_pumpCount}");
        _pumping = true;
        try
        {
            CefRuntime.DoMessageLoopWork();
        }
        finally
        {
            _pumping = false;
        }
        ArmTimer(_openBrowsers > 0 ? FallbackPumpMs : IdleFallbackPumpMs);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        lock (_gate)
        {
            _disposed = true;
            _pumpTimer?.Dispose();
            _pumpTimer = null;
        }
        if (_initialized)
        {
            PumpUntilBrowsersClose();
            CefRuntime.Shutdown();
            _initialized = false;
        }
    }

    /// <summary>CEF must not shut down with a browser open, and a disposed view's browser closes over
    /// later pumps, which no longer come once the app has stopped drawing.</summary>
    private void PumpUntilBrowsersClose()
    {
        var deadline = DateTime.UtcNow + CloseTimeout;
        while (_openBrowsers > 0 && DateTime.UtcNow < deadline)
        {
            CefRuntime.DoMessageLoopWork();
            Thread.Sleep(5);
        }
        if (_openBrowsers > 0)
            CefLog.Error($"shutting down with {_openBrowsers} browser(s) still open");
    }

    // CEF expects the process's own command-line args (minus the executable path on Windows).
    private static string[] GetProcessArgs()
    {
        var argv = Environment.GetCommandLineArgs();
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && argv.Length > 0
            ? argv[1..]
            : argv;
    }
}

/// <summary>The browser-process <see cref="CefApp"/>: its only job is to route CEF's external
/// message-pump schedule requests back to <see cref="CefRuntimeHost"/>.</summary>
internal sealed class ZgfBrowserCefApp : CefApp
{
    private readonly BrowserProcessHandler _handler;
    private readonly bool _autoplayWithoutGesture;

    public ZgfBrowserCefApp(CefRuntimeHost host, bool autoplayWithoutGesture)
    {
        _handler = new BrowserProcessHandler(host);
        _autoplayWithoutGesture = autoplayWithoutGesture;
    }

    protected override CefBrowserProcessHandler GetBrowserProcessHandler() => _handler;

    protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
    {
        if (_autoplayWithoutGesture)
            commandLine.AppendSwitch("autoplay-policy", "no-user-gesture-required");

        // Diagnostics (no rendering change): with ZGF_CEF_DEBUG set, make every Chromium process —
        // including the render subprocess — log verbosely to stderr, so a renderer's own fatal line
        // (a CHECK/NOTREACHED, a GPU error) is visible when a page crashes it.
        if (CefLog.Enabled)
        {
            commandLine.AppendSwitch("enable-logging", "stderr");
            commandLine.AppendSwitch("v", "1");
        }

        // Escape hatch / diagnostic — OFF by default: set ZGF_CEF_DISABLE_GPU=1 to force software
        // rendering. If a plain page crashes the render process on one machine but not another, the GPU
        // process (driver-specific) is the usual culprit; flipping this and seeing the crash stop
        // confirms it. WebGL still works via SwiftShader. This is a lever, not the default behaviour.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ZGF_CEF_DISABLE_GPU")))
        {
            commandLine.AppendSwitch("disable-gpu");
            commandLine.AppendSwitch("disable-gpu-compositing");
            CefLog.Error($"ZGF_CEF_DISABLE_GPU set: forcing software rendering ({processType} process)");
        }
    }

    private sealed class BrowserProcessHandler(CefRuntimeHost host) : CefBrowserProcessHandler
    {
        protected override void OnScheduleMessagePumpWork(long delayMs) =>
            host.ScheduleMessagePump(delayMs);
    }
}
