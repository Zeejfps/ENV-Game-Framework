namespace EasyGameFramework.Api;

public interface IScene
{
    IContext Context { get; }

    void Update(float dt);
}