namespace ZGF.Gui;

/// <summary>
/// A service the host starts once the application is fully wired. Register it with
/// <see cref="Context.AddHostedService{T}()"/>; the host resolves it and calls <see cref="Start"/>
/// after Build, so its constructor can inject any framework service (dispatcher, clipboard, ...) that
/// only exists once the window is built. Mirrors ASP.NET Core's <c>IHostedService</c>, minus the async
/// lifecycle this single-threaded UI has no use for.
/// </summary>
public interface IHostedService
{
    void Start();
}
