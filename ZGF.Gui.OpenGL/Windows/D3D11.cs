using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ZGF.Gui.OpenGL.Windows;

/// <summary>The slice of DXGI and D3D11 the shared-texture bridge calls, through raw vtable slots so no
/// built-in COM interop is involved.</summary>
[SupportedOSPlatform("windows")]
internal static unsafe partial class D3D11
{
    public const uint BindShaderResource = 0x8;
    public const uint BindRenderTarget = 0x20;
    private const uint CreateDeviceBgraSupport = 0x20;
    private const uint SdkVersion = 7;
    private const int DriverTypeUnknown = 0;
    private const int DriverTypeHardware = 1;

    private static readonly Guid IidDxgiFactory1 = new("770aae78-f26f-4dba-a829-253c83d1b387");
    private static readonly Guid IidDevice1 = new("a04bfb29-08ef-43d6-a49c-a9bdbdcbe686");
    private static readonly Guid IidTexture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    [StructLayout(LayoutKind.Sequential)]
    public struct Texture2DDesc
    {
        public uint Width;
        public uint Height;
        public uint MipLevels;
        public uint ArraySize;
        public uint Format;
        public uint SampleCount;
        public uint SampleQuality;
        public uint Usage;
        public uint BindFlags;
        public uint CpuAccessFlags;
        public uint MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AdapterDesc1
    {
        public fixed char Description[128];
        public uint VendorId;
        public uint DeviceId;
        public uint SubSysId;
        public uint Revision;
        public nuint DedicatedVideoMemory;
        public nuint DedicatedSystemMemory;
        public nuint SharedSystemMemory;
        public ulong AdapterLuid;
        public uint Flags;
    }

    [LibraryImport("d3d11.dll")]
    private static partial int D3D11CreateDevice(
        void* adapter, int driverType, nint software, uint flags, int* featureLevels, uint featureLevelCount,
        uint sdkVersion, void** device, int* featureLevel, void** immediateContext);

    [LibraryImport("dxgi.dll")]
    private static partial int CreateDXGIFactory1(Guid* riid, void** factory);

    /// <summary>Creates a device on the adapter whose LUID is <paramref name="adapterLuid"/>, or on the
    /// default adapter when it is null. Returns the device's <c>ID3D11Device1</c>.</summary>
    public static int CreateDevice(ulong? adapterLuid, out void* device1, out void* immediateContext)
    {
        device1 = null;
        immediateContext = null;
        void* adapter = null;
        if (adapterLuid != null)
            adapter = FindAdapter(adapterLuid.Value);

        void* device;
        void* context;
        var hr = D3D11CreateDevice(adapter, adapter == null ? DriverTypeHardware : DriverTypeUnknown, 0,
            CreateDeviceBgraSupport, null, 0, SdkVersion, &device, null, &context);
        if (adapter != null)
            Release(adapter);
        if (hr < 0)
            return hr;

        void* queried;
        var iid = IidDevice1;
        hr = QueryInterface(device, &iid, &queried);
        Release(device);
        if (hr < 0)
        {
            Release(context);
            return hr;
        }
        device1 = queried;
        immediateContext = context;
        return 0;
    }

    private static void* FindAdapter(ulong luid)
    {
        void* factory;
        var iid = IidDxgiFactory1;
        if (CreateDXGIFactory1(&iid, &factory) < 0)
            return null;
        try
        {
            for (uint i = 0; ; i++)
            {
                void* adapter;
                if (EnumAdapters1(factory, i, &adapter) < 0)
                    return null;
                AdapterDesc1 desc;
                if (GetDesc1(adapter, &desc) >= 0 && desc.AdapterLuid == luid)
                    return adapter;
                Release(adapter);
            }
        }
        finally
        {
            Release(factory);
        }
    }

    public static int OpenSharedTexture(void* device1, nint ntHandle, out void* texture)
    {
        void* opened;
        var iid = IidTexture2D;
        var hr = ((delegate* unmanaged<void*, nint, Guid*, void**, int>)Slot(device1, 48))(device1, ntHandle, &iid, &opened);
        texture = hr < 0 ? null : opened;
        return hr;
    }

    public static int CreateTexture2D(void* device, in Texture2DDesc desc, out void* texture)
    {
        void* created;
        int hr;
        fixed (Texture2DDesc* d = &desc)
            hr = ((delegate* unmanaged<void*, Texture2DDesc*, void*, void**, int>)Slot(device, 5))(device, d, null, &created);
        texture = hr < 0 ? null : created;
        return hr;
    }

    public static Texture2DDesc GetDesc(void* texture2D)
    {
        Texture2DDesc desc;
        ((delegate* unmanaged<void*, Texture2DDesc*, void>)Slot(texture2D, 10))(texture2D, &desc);
        return desc;
    }

    public static void CopyResource(void* context, void* destination, void* source) =>
        ((delegate* unmanaged<void*, void*, void*, void>)Slot(context, 47))(context, destination, source);

    public static void Flush(void* context) =>
        ((delegate* unmanaged<void*, void>)Slot(context, 111))(context);

    public static uint Release(void* unknown) =>
        ((delegate* unmanaged<void*, uint>)Slot(unknown, 2))(unknown);

    private static int QueryInterface(void* unknown, Guid* iid, void** result) =>
        ((delegate* unmanaged<void*, Guid*, void**, int>)Slot(unknown, 0))(unknown, iid, result);

    private static int EnumAdapters1(void* factory1, uint index, void** adapter) =>
        ((delegate* unmanaged<void*, uint, void**, int>)Slot(factory1, 12))(factory1, index, adapter);

    private static int GetDesc1(void* adapter1, AdapterDesc1* desc) =>
        ((delegate* unmanaged<void*, AdapterDesc1*, int>)Slot(adapter1, 10))(adapter1, desc);

    private static void* Slot(void* comObject, int index) => (*(void***)comObject)[index];
}
