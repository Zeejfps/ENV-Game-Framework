namespace OpenGLSandbox;

public abstract class Widget : IWidget
{
    public Rect ScreenRect { get; set; }
        
    private IWidget? m_Content;
        
    public virtual void Update(IBuildContext context)
    {
        m_Content ??= Build(context);
        if (m_Content != null && m_Content != this)
            m_Content.Update(context);
    }

    public virtual void Dispose()
    {
        DisposeContent();
    }

    protected virtual void DisposeContent()
    {
        m_Content?.Dispose();
        m_Content = null;
    }

    protected abstract IWidget Build(IBuildContext context);
}