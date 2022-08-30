namespace EasyGameFramework.API;

public interface IScene
{
    IContext App { get; }

    void Update();
}