namespace ENV.Engine;

public interface IScene
{
    IContext Context { get; }

    void Update();
}