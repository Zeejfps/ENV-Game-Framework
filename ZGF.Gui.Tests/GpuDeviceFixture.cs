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
