namespace Bricks.RaylibBackend;

public sealed class RaylibAppBuilder : IAppBuilder
{
    private string _windowName;
    private int _canvasWidth;
    private int _canvasHeight;
    
    public IAppBuilder WithWindowName(string brickz)
    {
        _windowName = brickz;
        return this;
    }

    public IAppBuilder WithCanvasSize(int width, int height)
    {
        _canvasWidth = width;
        _canvasHeight = height;
        return this;
    }

    public IApp Build()
    {
        return new RaylibApp(_windowName, _canvasWidth, _canvasHeight);
    }
}