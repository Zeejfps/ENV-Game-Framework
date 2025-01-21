namespace Bricks.RaylibBackend;

public sealed class RaylibEngineBuilder : IEngineBuilder
{
    private string _windowName;
    private int _canvasWidth;
    private int _canvasHeight;
    
    public IEngineBuilder WithWindowName(string brickz)
    {
        _windowName = brickz;
        return this;
    }

    public IEngineBuilder WithFramebufferSize(int width, int height)
    {
        _canvasWidth = width;
        _canvasHeight = height;
        return this;
    }

    public IEngine Build()
    {
        return new RaylibEngine(_windowName, _canvasWidth, _canvasHeight);
    }
}