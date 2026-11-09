using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZGF.Gui.Testing.Gpu.Apple;
using ZGF.Gui.Testing.Gpu.Linux;
using ZGF.Gui.Testing.Gpu.Windows;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>How <see cref="GpuDevice"/> makes a device on the current platform.</summary>
public enum GpuBackend
{
    None,
    Wgl,
    Egl,
    Metal,
}

/// <summary>A GPU with no window: one dedicated render thread and the GL context or Metal device on it. All
/// GPU work runs on that thread through <see cref="Run"/>. Only one OpenGL device may be live in a process at
/// a time.</summary>
public sealed class GpuDevice : IDisposable
{
    private const int Live = 0;
    private const int ShuttingDown = 1;

    private readonly RenderThread _thread;
    private readonly Native _native;
    private readonly List<IDisposable> _owned = [];
    private readonly TaskCompletionSource _shutdown = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _state = Live;

    private GpuDevice(RenderThread thread, Native native, GpuBackend backend, string description)
    {
        _thread = thread;
        _native = native;
        Backend = backend;
        Description = description;
    }

    /// <summary>The backend <see cref="Create"/> uses on this platform; <see cref="GpuBackend.None"/> where there is none yet.</summary>
    public static GpuBackend PlatformBackend =>
        OperatingSystem.IsWindows() ? GpuBackend.Wgl
        : OperatingSystem.IsLinux() ? GpuBackend.Egl
        : OperatingSystem.IsMacOS() ? GpuBackend.Metal
        : GpuBackend.None;

    public GpuBackend Backend { get; }

    /// <summary>The API version and renderer the device reports.</summary>
    public string Description { get; }

    /// <summary>Whether the caller is on this device's render thread.</summary>
    public bool IsRenderThread => _thread.IsCurrent;

    /// <exception cref="InvalidOperationException">Another OpenGL device is live in this process.</exception>
    /// <exception cref="NotSupportedException">The GPU cannot run the shipping renderer: no GL 4.1 core context
    /// with every required entry point, a missing EGL extension, or a Metal GPU that is not Apple's. The
    /// message names what is missing.</exception>
    /// <exception cref="PlatformNotSupportedException">This platform has no backend yet.</exception>
    public static GpuDevice Create()
    {
        switch (PlatformBackend)
        {
            case GpuBackend.Wgl when OperatingSystem.IsWindows():
                return CreateOpenGl(GpuBackend.Wgl, CreateWglContext);
            case GpuBackend.Egl when OperatingSystem.IsLinux():
                return CreateOpenGl(GpuBackend.Egl, CreateEglContext);
            case GpuBackend.Metal when OperatingSystem.IsMacOS():
                return CreateMetal();
            case GpuBackend.None:
                throw new PlatformNotSupportedException($"GpuDevice has no backend for {RuntimeInformation.OSDescription}.");
            default:
                throw new InvalidOperationException($"Unhandled GPU backend {PlatformBackend}.");
        }
    }

    private static GpuDevice CreateOpenGl(GpuBackend backend, Func<RenderThread, IGlContext> createContext)
    {
        var entryPoints = GlEntryPointOwner.Claim("an OpenGL GpuDevice");
        RenderThread? thread = null;
        IGlContext? context = null;
        try
        {
            thread = new RenderThread("ZGF GpuDevice");
            var (created, description) = thread.Run(() =>
            {
                context = createContext(thread);
                GL46.Import(entryPoints, context.GetProcAddress);
                return (context, GlRequirements.Verify());
            });
            return new GpuDevice(thread, new Native.OpenGl(created, entryPoints), backend, description);
        }
        catch (Exception creation)
        {
            Exception? teardown;
            try
            {
                teardown = ShutDownAfterFailedCreation(thread, context);
            }
            finally
            {
                entryPoints.Dispose();
            }
            if (teardown != null)
                throw new AggregateException("Creating the GpuDevice failed, and so did releasing what it had made.", creation, teardown);
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    private static IGlContext CreateWglContext(RenderThread thread)
    {
        var context = WglContext.Create(GlRequirements.Major, GlRequirements.Minor);
        thread.OnIdle(context.PumpMessages);
        return context;
    }

    [SupportedOSPlatform("linux")]
    private static IGlContext CreateEglContext(RenderThread thread) =>
        EglContext.Create(GlRequirements.Major, GlRequirements.Minor);

    [SupportedOSPlatform("macos")]
    private static GpuDevice CreateMetal()
    {
        RenderThread? thread = null;
        MetalContext? context = null;
        try
        {
            thread = new RenderThread("ZGF GpuDevice", MetalContext.InAutoreleasePool);
            var created = thread.Run(() => context = MetalContext.Create());
            return new GpuDevice(thread, new Native.Metal(created), GpuBackend.Metal, created.Description);
        }
        catch (Exception creation)
        {
            var teardown = ShutDownAfterFailedCreation(thread, context);
            if (teardown != null)
                throw new AggregateException("Creating the GpuDevice failed, and so did releasing what it had made.", creation, teardown);
            throw;
        }
    }

    /// <summary>Ends <paramref name="thread"/> after disposing <paramref name="context"/> on it, and returns what
    /// that threw so the creation failure is not replaced by it.</summary>
    private static Exception? ShutDownAfterFailedCreation(RenderThread? thread, IDisposable? context)
    {
        try
        {
            thread?.Shutdown(() => context?.Dispose());
            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary>A canvas session <paramref name="width"/> by <paramref name="height"/> points at
    /// <paramref name="dpiScale"/>, drawn by this device's backend.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size is invalid or exceeds what the device can render to.</exception>
    public ICanvasSession CreateSession(int width, int height, float dpiScale = 1f) => _native switch
    {
        Native.OpenGl => GlCanvasSession.Create(this, width, height, dpiScale),
        Native.Metal when OperatingSystem.IsMacOS() => MetalCanvasSession.Create(this, width, height, dpiScale),
        _ => throw new InvalidOperationException($"Unhandled GPU backend {Backend}."),
    };

    /// <exception cref="InvalidOperationException">This is not an OpenGL device.</exception>
    internal void RequireOpenGl()
    {
        if (_native is not Native.OpenGl)
            throw new InvalidOperationException($"This is a {Backend} device, not an OpenGL one.");
    }

    /// <exception cref="InvalidOperationException">This is not a Metal device.</exception>
    internal MetalContext Metal => _native is Native.Metal metal
        ? metal.Device
        : throw new InvalidOperationException($"This is a {Backend} device, not a Metal one.");

    /// <summary>Runs <paramref name="work"/> on the render thread, waiting for it; rethrows what it throws.</summary>
    /// <exception cref="ObjectDisposedException">The device is shutting down or disposed.</exception>
    public void Run(Action work) => _thread.Run(work);

    /// <inheritdoc cref="Run(Action)"/>
    public T Run<T>(Func<T> work) => _thread.Run(work);

    /// <summary>Runs <paramref name="work"/> on the render thread, or returns false once the device no longer
    /// accepts work.</summary>
    internal bool TryRun(Action work) => _thread.TryRun(work);

    /// <summary>Has <paramref name="resource"/> disposed on the render thread when the device shuts down, if it
    /// has not been by then. Render thread only.</summary>
    internal void Own(IDisposable resource)
    {
        ThrowIfNotRenderThread();
        _owned.Add(resource);
    }

    /// <summary>Render thread only.</summary>
    internal void Disown(IDisposable resource)
    {
        ThrowIfNotRenderThread();
        _owned.Remove(resource);
    }

    private void ThrowIfNotRenderThread()
    {
        if (!_thread.IsCurrent)
            throw new InvalidOperationException("Only the render thread may do this.");
    }

    /// <summary>Stops accepting work, finishes what was accepted, disposes the sessions and targets still open
    /// and then the context, all on the render thread. Every caller returns once that is done, and rethrows
    /// what it threw.</summary>
    /// <exception cref="InvalidOperationException">Called from the render thread.</exception>
    public void Dispose()
    {
        if (_thread.IsCurrent)
            throw new InvalidOperationException("A GpuDevice cannot be disposed from its own render thread.");
        if (Interlocked.CompareExchange(ref _state, ShuttingDown, Live) == Live)
            ShutDown();
        _shutdown.Task.GetAwaiter().GetResult();
    }

    private void ShutDown()
    {
        ThreadInterruptedException? interrupted = null;
        try
        {
            _thread.Shutdown(ReleaseEverything);
            ReleaseGuard();
            _shutdown.SetResult();
        }
        catch (ThreadInterruptedException e)
        {
            // The thread has finished; only this caller was interrupted, so the others see a clean shutdown.
            ReleaseGuard();
            _shutdown.SetResult();
            interrupted = e;
        }
        catch (Exception e)
        {
            ReleaseGuard();
            _shutdown.SetException(e);
        }
        if (interrupted != null)
            ExceptionDispatchInfo.Throw(interrupted);
    }

    private void ReleaseGuard()
    {
        if (_native is Native.OpenGl gl)
            gl.EntryPoints.Dispose();
    }

    private void ReleaseEverything()
    {
        var failures = new List<Exception>();
        while (_owned.Count > 0)
        {
            var last = _owned[^1];
            _owned.RemoveAt(_owned.Count - 1);
            try
            {
                last.Dispose();
            }
            catch (Exception e)
            {
                // Collected so the context is still destroyed; rethrown together below.
                failures.Add(e);
            }
        }
        try
        {
            _native.Context.Dispose();
        }
        catch (Exception e)
        {
            failures.Add(e);
        }
        if (failures.Count > 0)
            throw new AggregateException("Releasing the GpuDevice's open resources or its context failed.", failures);
    }

    private abstract record Native(IDisposable Context)
    {
        public sealed record OpenGl(IGlContext Gl, GlEntryPointOwner EntryPoints) : Native(Gl);

        public sealed record Metal(MetalContext Device) : Native(Device);
    }
}
