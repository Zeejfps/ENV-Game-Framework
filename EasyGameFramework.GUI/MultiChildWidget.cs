using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public sealed class MultiChildWidget : IWidget
{
    private readonly IEnumerable<IWidget> m_Children;

    public Rect ScreenRect { get; set; }

    public MultiChildWidget(IEnumerable<IWidget> children)
    {
        m_Children = children;
    }

    public void Update(IBuildContext context)
    {
        foreach (var child in m_Children)
            child.Update(context);
    }

    public void Build(IBuildContext context)
    {
        foreach (var child in m_Children)
            child.Build(context);
    }

    public void Layout(IBuildContext context)
    {
        foreach (var child in m_Children)
            child.Layout(context);
    }

    public Rect Measure(IBuildContext context)
    {
        return ScreenRect;
    }

    public void Dispose()
    {
        foreach (var child in m_Children)
            child.Dispose();
    }
}