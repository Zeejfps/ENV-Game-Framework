namespace Bricks;

public interface IApp : IDisposable
{
    bool IsCloseRequested { get; }
    IKeyboard Keyboard { get; }
    void Update();
    void Render(World world);
}