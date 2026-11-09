using System.Diagnostics;
using System.Runtime.InteropServices;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Tests;

/// <summary>A test that needs a <see cref="GpuDevice"/>, skipped where the platform has no backend for one.</summary>
public sealed class GpuFactAttribute : FactAttribute
{
    public GpuFactAttribute()
    {
        if (GpuDevice.PlatformBackend == GpuBackend.None)
            Skip = $"GpuDevice has no backend for {RuntimeInformation.OSDescription}.";
    }
}

/// <summary>A theory that needs a <see cref="GpuDevice"/>, skipped where the platform has no backend for one.</summary>
public sealed class GpuTheoryAttribute : TheoryAttribute
{
    public GpuTheoryAttribute()
    {
        if (GpuDevice.PlatformBackend == GpuBackend.None)
            Skip = $"GpuDevice has no backend for {RuntimeInformation.OSDescription}.";
    }
}

/// <summary>A test of what only an OpenGL <see cref="GpuDevice"/> does, skipped where the platform's backend is not OpenGL.</summary>
public sealed class OpenGlFactAttribute : FactAttribute
{
    public OpenGlFactAttribute()
    {
        if (GpuDevice.PlatformBackend is not (GpuBackend.Wgl or GpuBackend.Egl))
            Skip = $"GpuDevice has no OpenGL backend for {RuntimeInformation.OSDescription}.";
    }
}

/// <summary>Asks the platform's GL binding, not GL itself, whether this thread has a current context.</summary>
public static partial class CurrentGlContext
{
    public static bool Exists() => GpuDevice.PlatformBackend switch
    {
        GpuBackend.Wgl => WglGetCurrentContext() != 0,
        GpuBackend.Egl => EglGetCurrentContext() != 0,
        _ => throw new PlatformNotSupportedException($"No OpenGL GpuDevice backend on {RuntimeInformation.OSDescription}."),
    };

    [LibraryImport("opengl32.dll", EntryPoint = "wglGetCurrentContext")]
    private static partial nint WglGetCurrentContext();

    [LibraryImport("libEGL.so.1", EntryPoint = "eglGetCurrentContext")]
    private static partial nint EglGetCurrentContext();
}

public abstract record GpuAvailability
{
    public sealed record Available(GpuDevice Device, TimeSpan CreationTime) : GpuAvailability;
    public sealed record Unavailable(string Reason) : GpuAvailability;
}

/// <summary>The one OpenGL device the <see cref="GpuCollection"/> tests share, or why there is none.</summary>
public sealed class GpuDeviceFixture : IDisposable
{
    public GpuDeviceFixture()
    {
        Availability = Probe();
    }

    public GpuAvailability Availability { get; }

    public GpuDevice Device => Availability switch
    {
        GpuAvailability.Available available => available.Device,
        GpuAvailability.Unavailable unavailable => throw new InvalidOperationException($"No GPU device: {unavailable.Reason}"),
        _ => throw new InvalidOperationException($"Unhandled {Availability}."),
    };

    public TimeSpan CreationTime => Availability is GpuAvailability.Available available ? available.CreationTime : TimeSpan.Zero;

    private static GpuAvailability Probe()
    {
        if (GpuDevice.PlatformBackend == GpuBackend.None)
            return new GpuAvailability.Unavailable($"GpuDevice has no backend for {RuntimeInformation.OSDescription}.");
        var clock = Stopwatch.StartNew();
        try
        {
            var device = GpuDevice.Create();
            return new GpuAvailability.Available(device, clock.Elapsed);
        }
        catch (NotSupportedException e)
        {
            return new GpuAvailability.Unavailable(e.Message);
        }
    }

    public void Dispose()
    {
        if (Availability is GpuAvailability.Available available)
            available.Device.Dispose();
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GpuCollection : ICollectionFixture<GpuDeviceFixture>
{
    public const string Name = "Gpu";
}

/// <summary>Tests that create devices themselves, so must not overlap <see cref="GpuCollection"/>'s.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GpuLifecycleCollection
{
    public const string Name = "Gpu lifecycle";
}
