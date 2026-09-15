using McpSdk.Shared;

namespace ZGF.Gui.Desktop;

/// <summary>McpSdk's logger seam routed to the console for errors and warnings only, so a session
/// that fails to set up — a tool source throwing in <c>Register</c>, a schema the adapter rejects —
/// is printed rather than answered with a bare 500.</summary>
internal sealed class McpConsoleLogger : ILoggerFactory, ILogger
{
    public ILogger Create<T>() => this;
    public ILogger Create(Type type) => this;

    public void LogDebug(string message) { }
    public void LogInfo(string message) { }
    public void LogWarning(string message) => Console.WriteLine($"[GuiMcpServer] {message}");
    public void LogError(string message) => Console.WriteLine($"[GuiMcpServer] {message}");
    public void LogError(Exception exception) => Console.WriteLine($"[GuiMcpServer] {exception}");
}
