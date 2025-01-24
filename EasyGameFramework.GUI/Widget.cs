using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public abstract class Widget : IWidget
{
    public Rect ScreenRect { get; set; }
        
    private IWidget? m_Content;
        
    public virtual void Update(IBuildContext context)
    {
        if (m_Content == null)
        {
            Build(context);
            Layout(context);
        }
        if (m_Content != null && m_Content != this)
        {
            m_Content.Update(context);
        }
    }

    public virtual void Layout(IBuildContext context)
    {
        if (m_Content != null && m_Content != this)
        {
            m_Content.ScreenRect = ScreenRect;
            m_Content.Layout(context);
        }
    }

    public virtual void Dispose()
    {
        DisposeContent();
    }

    protected virtual void DisposeContent()
    {
        if (m_Content != null && m_Content != this)
        {
            m_Content.Dispose();
        }
        m_Content = null;
    }

    public void Build(IBuildContext context)
    {
        if (m_Content != null)
            return;
        
        m_Content = BuildContent(context);
        if (m_Content != null && m_Content != this)
        {
            m_Content.Build(context);
        }
    }

    protected abstract IWidget BuildContent(IBuildContext context);

    public virtual Rect Measure(IBuildContext context)
    {
        if (m_Content != null && m_Content != this)
            return m_Content.Measure(context);
        return ScreenRect;
    }
}