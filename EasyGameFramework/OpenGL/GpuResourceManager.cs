namespace EasyGameFramework.OpenGL;

public abstract class GpuResourceManager<THandle, TResource>
{
    private readonly Dictionary<THandle, TResource> m_HandleToResourceMap = new();
    private readonly Dictionary<string, THandle> m_LoadedHandles = new();
    protected TResource? BoundResource { get; private set; }

    public void Bind(THandle? handle)
    {
        if (handle == null)
        {
            BoundResource = default;
            OnUnbound();
            return;
        }

        BoundResource = m_HandleToResourceMap[handle];
        OnBound(BoundResource);
    }

    public void Unbind()
    {
        Bind(default);
    }

    public void Add(THandle handle, TResource resource)
    {
        m_HandleToResourceMap[handle] = resource;
    }

    public THandle Load(string assetPath)
    {
        if (m_LoadedHandles.TryGetValue(assetPath, out var handle))
        {
            Bind(handle);
            return handle;
        }

        var resource = LoadAndBindResource(assetPath);
        handle = CreateHandle(resource);
        m_LoadedHandles[assetPath] = handle;
        BoundResource = resource;
        Add(handle, resource);
        return handle;
    }

    protected abstract void OnBound(TResource resource);
    protected abstract void OnUnbound();
    protected abstract TResource LoadAndBindResource(string assetPath);
    protected abstract THandle CreateHandle(TResource resource);
}