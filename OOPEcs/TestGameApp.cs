using EasyGameFramework.Api;
using OpenGLSandbox;
using Tetris;

namespace OOPEcs;

public sealed class TestGameApp : WindowedApp
{
    public TestGameApp(IWindow window, ILogger logger) : base(window)
    {
        RegisterSingleton<ILogger>(logger);
        RegisterSingleton<IWindow>(window);
        RegisterSingletonEntity<ITextRenderer, MyTextRenderer>();
        RegisterSingletonEntity<IClock, GameClock>();
        RegisterTransientEntity<MainWorld>();
    }
}