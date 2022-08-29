namespace EasyGameFramework.API;

public interface IScene
{
    IContext Context { get; }

    void Update();
}