namespace EasyGameFramework.API;

public interface IScene
{
    IApplication Context { get; }

    void Update();
}