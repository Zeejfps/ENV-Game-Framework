namespace Bricks;

public interface IApp : IDisposable
{
    bool IsCloseRequested { get; }
    IInput Input { get; }
    void Update();
    void Render(World world);
}