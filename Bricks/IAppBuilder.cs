namespace Bricks;

public interface IAppBuilder
{
    IAppBuilder WithWindowName(string brickz);
    IAppBuilder WithCanvasSize(int width, int height);
    IApp Build();
}