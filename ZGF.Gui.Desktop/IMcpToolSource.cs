using McpSdk.Server;

namespace ZGF.Gui.Desktop;

/// <summary>
/// Contributes tools to a <see cref="GuiMcpServer"/>. Asked once per client session, with that
/// session's controller, so each tool can be handed the <see cref="McpSession"/> it serves. Both
/// <see cref="Register"/> and a tool's <c>Call</c> run on thread-pool threads — never the UI thread —
/// and concurrently across sessions and calls; a <c>Call</c> may be genuinely asynchronous and should
/// honour <c>McpRequestContext.CancellationToken</c>, which fires when the client cancels that request.
/// </summary>
public interface IMcpToolSource
{
    void Register(McpSession session, DefaultToolsController tools);
}
