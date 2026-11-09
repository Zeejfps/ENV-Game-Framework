using System.Runtime.InteropServices;
using ZGF.Gui.Testing.Gpu;

namespace ZGF.Gui.Immediate.Tests;

/// <summary>A test that needs a <see cref="GpuDevice"/>, skipped where the platform has no backend for one.</summary>
public sealed class GpuFactAttribute : FactAttribute
{
    public GpuFactAttribute()
    {
        if (GpuDevice.PlatformBackend == GpuBackend.None)
            Skip = $"GpuDevice has no backend for {RuntimeInformation.OSDescription}.";
    }
}

/// <summary>The one device the <see cref="GpuCollection"/> tests share, made on first use.</summary>
public sealed class GpuDeviceFixture : IDisposable
{
    private readonly Lazy<GpuDevice> _device = new(GpuDevice.Create);

    public GpuDevice Device => _device.Value;

    public void Dispose()
    {
        if (_device.IsValueCreated)
            _device.Value.Dispose();
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GpuCollection : ICollectionFixture<GpuDeviceFixture>
{
    public const string Name = "Gpu";
}
