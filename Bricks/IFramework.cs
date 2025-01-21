namespace Bricks;

public interface IFramework : IDisposable
{
    IKeyboard Keyboard { get; }
}