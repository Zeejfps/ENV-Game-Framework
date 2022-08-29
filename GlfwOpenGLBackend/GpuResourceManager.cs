namespace GlfwOpenGLBackend;

public abstract class GpuResourceManager<THandle, TResource>
{
    protected TResource? BoundResource { get; private set; }

    private readonly Dictionary<THandle, TResource> m_HandleToResourceMap = new();

    public void Use(THandle? handle)
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
    
    public void Add(THandle handle, TResource resource)
    {
        m_HandleToResourceMap[handle] = resource;
    }

    protected abstract void OnBound(TResource resource);

    protected abstract void OnUnbound();
}