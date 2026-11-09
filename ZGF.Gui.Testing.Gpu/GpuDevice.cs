using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZGF.Gui.Testing.Gpu.Windows;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>How <see cref="GpuDevice"/> makes a device on the current platform.</summary>
public enum GpuBackend
{
    None,
    Wgl,
}

/// <summary>A GPU with no window: one dedicated render thread and the GL context on it. All GPU work runs on
/// that thread through <see cref="Run"/>. Only one OpenGL device may be live in a process at a time.</summary>
public sealed class GpuDevice : IDisposable
{
    private const int Live = 0;
    private const int ShuttingDown = 1;

    private readonly RenderThread _thread;
    private readonly IDisposable _context;
    private readonly OpenGlDeviceGuard.Claim _claim;
    private readonly List<IDisposable> _owned = [];
    private readonly TaskCompletionSource _shutdown = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _state = Live;

    private GpuDevice(RenderThread thread, IDisposable context, OpenGlDeviceGuard.Claim claim, string description)
    {
        _thread = thread;
        _context = context;
        _claim = claim;
        Description = description;
    }

    /// <summary>The backend <see cref="Create"/> uses on this platform; <see cref="GpuBackend.None"/> where there is none yet.</summary>
    public static GpuBackend PlatformBackend => OperatingSystem.IsWindows() ? GpuBackend.Wgl : GpuBackend.None;

    /// <summary>The API version and renderer the context reports.</summary>
    public string Description { get; }

    /// <summary>Whether the caller is on this device's render thread.</summary>
    public bool IsRenderThread => _thread.IsCurrent;

    /// <exception cref="InvalidOperationException">Another OpenGL device is live in this process.</exception>
    /// <exception cref="NotSupportedException">The driver cannot give a GL 4.1 core context with every
    /// required entry point; the message names what is missing.</exception>
    /// <exception cref="PlatformNotSupportedException">This platform has no backend yet.</exception>
    public static GpuDevice Create()
    {
        switch (PlatformBackend)
        {
            case GpuBackend.Wgl when OperatingSystem.IsWindows():
                return CreateWgl();
            case GpuBackend.None:
                throw new PlatformNotSupportedException($"GpuDevice has no backend for {RuntimeInformation.OSDescription}.");
            default:
                throw new InvalidOperationException($"Unhandled GPU backend {PlatformBackend}.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static GpuDevice CreateWgl()
    {
        var claim = OpenGlDeviceGuard.Acquire();
        RenderThread? thread = null;
        WglContext? context = null;
        try
        {
            thread = new RenderThread("ZGF GpuDevice");
            var (wgl, description) = thread.Run(() =>
            {
                context = WglContext.Create(GlRequirements.Major, GlRequirements.Minor);
                GL46.Import(context.GetProcAddress);
                return (context, GlRequirements.Verify());
            });
            thread.OnIdle(wgl.PumpMessages);
            return new GpuDevice(thread, wgl, claim, description);
        }
        catch
        {
            try
            {
                thread?.Shutdown(() => context?.Dispose());
            }
            finally
            {
                OpenGlDeviceGuard.Release(claim);
            }
            throw;
        }
    }

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
            OpenGlDeviceGuard.Release(_claim);
            _shutdown.SetResult();
        }
        catch (ThreadInterruptedException e)
        {
            // The thread has finished; only this caller was interrupted, so the others see a clean shutdown.
            OpenGlDeviceGuard.Release(_claim);
            _shutdown.SetResult();
            interrupted = e;
        }
        catch (Exception e)
        {
            OpenGlDeviceGuard.Release(_claim);
            _shutdown.SetException(e);
        }
        if (interrupted != null)
            ExceptionDispatchInfo.Throw(interrupted);
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
        _context.Dispose();
        if (failures.Count > 0)
            throw new AggregateException("Disposing resources still open on the GpuDevice failed.", failures);
    }
}

internal static class OpenGlDeviceGuard
{
    internal sealed class Claim;

    // Process-wide by necessity: GL46's entry points are static fields, so one GL device may be live at a time.
    private static Claim? s_owner;

    public static Claim Acquire()
    {
        var claim = new Claim();
        if (Interlocked.CompareExchange(ref s_owner, claim, null) != null)
            throw new InvalidOperationException(
                "An OpenGL GpuDevice is already live in this process; GL entry points are process-wide, so dispose it before creating another.");
        return claim;
    }

    /// <summary>Releases <paramref name="claim"/> if it still holds the guard; a stale claim changes nothing.</summary>
    public static void Release(Claim claim) => Interlocked.CompareExchange(ref s_owner, null, claim);
}
