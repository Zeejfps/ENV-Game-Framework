namespace Framework;

public class PbrTestScene : IScene
{
    public IContext Context { get; }

    private PbrRenderer m_PbrRenderer;
    private PbrTestObject m_PbrTestObject;
    
    public PbrTestScene(IContext context)
    {
        Context = context;
        m_PbrRenderer = new PbrRenderer();
        m_PbrTestObject = new PbrTestObject(m_PbrRenderer);
    }

    public void Load()
    {
        m_PbrRenderer.Load(this);
    }

    public void Update()
    {
        m_PbrRenderer.Update(this);
    }
}

public class PbrTestObject
{
    private PbrRenderer m_PbrRenderer;
    
    public PbrTestObject(PbrRenderer pbrRenderer)
    {
        m_PbrRenderer = pbrRenderer;
        m_PbrRenderer.Add(new PbrRenderable());
    }
}