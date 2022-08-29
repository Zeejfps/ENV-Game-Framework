namespace EasyGameFramework.API;

public interface IScene
{
    IApplication App { get; }

    void Update();
}