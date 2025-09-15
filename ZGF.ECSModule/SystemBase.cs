namespace ZGF.ECSModule;

public abstract class SystemBase : ISystem
{
    public void PreUpdate()
    {
        OnPostUpdate();
    }

    public void Update()
    {
        OnUpdate();
    }

    public void PostUpdate()
    {
        OnPostUpdate();
    }
    
    protected virtual void OnPreUpdate() { }

    protected virtual void OnUpdate() { }
    
    protected virtual void OnPostUpdate() { }
}