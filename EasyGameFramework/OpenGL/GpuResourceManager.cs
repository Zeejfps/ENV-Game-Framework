namespace EasyGameFramework.OpenGL;

internal abstract class GpuResourceManager<THandle, TResource>
{
    private readonly Dictionary<THandle, TResource> m_HandleToResourceMap = new();
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
        var resource = LoadAndBindResource(assetPath);
        var handle = CreateHandle(resource);
        BoundResource = resource;
        Add(handle, resource);
        return handle;
    }

    protected abstract void OnBound(TResource resource);
    protected abstract void OnUnbound();
    protected abstract TResource LoadAndBindResource(string assetPath);
    protected abstract THandle CreateHandle(TResource resource);
}