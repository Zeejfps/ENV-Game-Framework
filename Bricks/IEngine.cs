namespace Bricks;

public interface IEngine : IDisposable
{
    IKeyboard Keyboard { get; }
}