namespace Bricks.RaylibBackend;

public sealed class RaylibAppBuilder : IAppBuilder
{
    private string _windowName;
    private int _canvasWidth;
    private int _canvasHeight;
    
    public void WithWindowName(string brickz)
    {
        _windowName = brickz;
    }

    public void WithCanvasSize(int width, int height)
    {
        _canvasWidth = width;
        _canvasHeight = height;
    }

    public IApp Build()
    {
        return new RaylibApp(_windowName, _canvasWidth, _canvasHeight);
    }
}