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
            m_Content = Build(context);
            if (m_Content != null && m_Content != this)
                m_Content.DoLayout(context);
        }
        
        if (m_Content != null && m_Content != this)
        {
            m_Content.Update(context);
        }
    }

    public virtual void DoLayout(IBuildContext context)
    {
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

    protected abstract IWidget Build(IBuildContext context);

    public virtual Rect Measure(IBuildContext context)
    {
        if (m_Content != null && m_Content != this)
            return m_Content.Measure(context);
        return ScreenRect;
    }
}