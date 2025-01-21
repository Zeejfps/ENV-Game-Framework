namespace Bricks;

public interface IEngineBuilder
{
    IEngineBuilder WithWindowName(string brickz);
    IEngineBuilder WithFramebufferSize(int width, int height);
    IEngine Build();
}