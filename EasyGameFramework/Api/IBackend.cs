namespace EasyGameFramework.Api;

public interface IBackend
{
    IDisplayManager DisplayManager { get; }
    
    IWindowFactory WindowFactory { get; }
}

public interface IWindowFactory
{
    IWindow Create();
}