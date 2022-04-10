namespace Framework;

public interface IScene
{
    IContext Context { get; }

    void Update();
}