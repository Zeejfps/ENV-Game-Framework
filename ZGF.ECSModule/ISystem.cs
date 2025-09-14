namespace ZGF.ECSModule;

public interface ISystem
{
    void PreUpdate();
    void Update();
    void PostUpdate();
}