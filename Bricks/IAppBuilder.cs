namespace Bricks;

public interface IAppBuilder
{
    void WithWindowName(string brickz);
    void WithCanvasSize(int width, int height);
    IApp Build();
}