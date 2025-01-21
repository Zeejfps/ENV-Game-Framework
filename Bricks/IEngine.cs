namespace Bricks;

public interface IEngine : IDisposable
{
    IKeyboard Keyboard { get; }
    void Run(IGame game);
    void Render(World world);
}