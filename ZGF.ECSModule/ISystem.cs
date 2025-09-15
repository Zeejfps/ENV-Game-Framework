namespace ZGF.ECSModule;

public interface ISystem
{
    void PreUpdate();
    void FixedUpdate();
    void Update();
    void PostUpdate();
}