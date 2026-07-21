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
/// <c>OnScheduleMessagePumpWork</c> callback: it arms a timer that posts
/// <see cref="CefRuntime.DoMessageLoopWork"/> onto the UI thread via the dispatcher, so CEF's browser
/// callbacks (including <c>OnPaint</c>) all run on ZGF's main thread and the loop stays idle-friendly.</para>
/// </summary>
internal sealed class CefRuntimeHost : IDisposable
{
    private readonly IUiDispatcher _dispatcher;
    private readonly List<CefWebView> _pending = new();
    private readonly Lock _gate = new();
    private Timer? _pumpTimer;
    private bool _initialized;
    private bool _disposed;

    public CefRuntimeHost(IUiDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public bool IsInitialized => _initialized;

    /// <summary>Initialises CEF on the UI thread. Called once by the hosted service after the app is
    /// built.</summary>
    public void Initialize()
    {
        if (_initialized || _disposed)
            return;

        CefRuntime.Load(CefPaths.LibCefDir);

        var settings = new CefSettings
        {
            WindowlessRenderingEnabled = true,
            // External pump: we call DoMessageLoopWork ourselves from the UI thread, so OnPaint & co.
            // fire there too — sidestepping GL-context threading pain.
            ExternalMessagePump = true,
            MultiThreadedMessageLoop = false,
            NoSandbox = true,
            BrowserSubprocessPath = CefPaths.HelperExe,
            ResourcesDirPath = CefPaths.ResourcesDir,
            LocalesDirPath = CefPaths.LocalesDir,
            // Both must be set: CachePath (a subdir of RootCachePath) is what relocates the GPU/Dawn
            // shader caches out of the working directory into the app-data profile.
            RootCachePath = CefPaths.CacheRoot,
            CachePath = CefPaths.ProfileCachePath,
            LogSeverity = CefLog.Enabled ? CefLogSeverity.Verbose : CefLogSeverity.Warning,
            LogFile = Environment.GetEnvironmentVariable("ZGF_CEF_LOGFILE") ?? string.Empty,
        };

        Directory.CreateDirectory(CefPaths.ProfileCachePath);

        CefLog.Write($"initializing: libcef={CefPaths.LibCefDir}, helper={CefPaths.HelperExe}, resources={CefPaths.ResourcesDir}, locales={CefPaths.LocalesDir}");
        var mainArgs = new CefMainArgs(GetProcessArgs());
        CefRuntime.Initialize(mainArgs, settings, new ZgfBrowserCefApp(this), IntPtr.Zero);
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

        // Drive CEF's browser-UI-thread work by pumping DoMessageLoopWork on the UI thread at a steady
        // cadence (plan piece 2: "call DoMessageLoopWork() once per tick"). A periodic pump keeps the
        // browser making progress even when OnScheduleMessagePumpWork goes quiet; idling it when no
        // browser needs work is a later optimization.
        _pumpTimer = new Timer(_ => _dispatcher.Post(Pump), null, 0, PumpIntervalMs);
        Pump();
    }

    private const int PumpIntervalMs = 15;

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
        // The periodic pump already covers work within one interval; only nudge an extra immediate pump
        // for "do it now" requests so sub-interval work isn't delayed. Never reconfigure the periodic
        // timer here — that would turn it into a one-shot and stall the loop.
        if (delayMs <= 0)
            _dispatcher.Post(Pump);
    }

    private void Pump()
    {
        if (_initialized && !_disposed)
        {
            if (_pumpCount++ < 8) CefLog.Write($"pump #{_pumpCount}");
            CefRuntime.DoMessageLoopWork();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _pumpTimer?.Dispose();
        _pumpTimer = null;
        if (_initialized)
        {
            CefRuntime.Shutdown();
            _initialized = false;
        }
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

    public ZgfBrowserCefApp(CefRuntimeHost host) => _handler = new BrowserProcessHandler(host);

    protected override CefBrowserProcessHandler GetBrowserProcessHandler() => _handler;

    private sealed class BrowserProcessHandler(CefRuntimeHost host) : CefBrowserProcessHandler
    {
        protected override void OnScheduleMessagePumpWork(long delayMs) =>
            host.ScheduleMessagePump(delayMs);
    }
}
