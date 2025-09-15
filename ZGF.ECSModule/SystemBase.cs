namespace ZGF.ECSModule;

public abstract class SystemBase : ISystem
{
    protected bool IsUpdating { get; private set; }
    
    public void PreUpdate()
    {
        IsUpdating = true;
        OnPreUpdate();
    }

    public void Update()
    {
        OnUpdate();
    }

    public void PostUpdate()
    {
        OnPostUpdate();
        IsUpdating = false;
    }
    
    protected virtual void OnPreUpdate() { }

    protected virtual void OnUpdate() { }
    
    protected virtual void OnPostUpdate() { }
}