using System.Runtime.InteropServices;

namespace ZGF.Gui.Immediate.Scratch;

/// <summary>A test that needs a <see cref="GpuDevice"/>, skipped where the platform has no backend for one.</summary>
public sealed class GpuFactAttribute : FactAttribute
{
    public GpuFactAttribute()
    {
        if (GpuDevice.PlatformBackend == GpuBackend.None)
            Skip = $"GpuDevice has no backend for {RuntimeInformation.OSDescription}.";
    }
}

/// <summary>The one device every <see cref="GpuCollection"/> test shares, made on first use.</summary>
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
