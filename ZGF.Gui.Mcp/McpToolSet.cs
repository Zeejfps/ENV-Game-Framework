using McpSdk.Protocol.Models;
using McpSdk.Server;

namespace ZGF.Gui.Mcp;

/// <summary>The tools one session of a <see cref="GuiMcpServer"/> serves, as its <see cref="IMcpToolSource"/>s add them.</summary>
public sealed class McpToolSet
{
    private readonly DefaultToolsController _tools;
    private readonly Dictionary<string, IToolHandler> _ownValidation = new(StringComparer.Ordinal);

    internal McpToolSet(DefaultToolsController tools)
    {
        _tools = tools;
    }

    /// <summary>Serves <paramref name="handler"/>. Its arguments are checked against its input schema before it is called,
    /// unless it came from <see cref="McpTools.DefineUnchecked{T}"/>, which checks its own.</summary>
    public void Add(IToolHandler handler)
    {
        _tools.AddTool(handler);
        if (handler is McpTools.UncheckedTool)
            _ownValidation[handler.Tool.Name] = handler;
        else
            _ownValidation.Remove(handler.Tool.Name);
    }

    internal IToolsController Controller() => new Router(_tools, _ownValidation);

    /// <summary>Lists every tool as the SDK does, and calls a tool that checks its own arguments without the SDK's
    /// schema check first, so the tool answers a bad argument in its own structured form.</summary>
    private sealed class Router(DefaultToolsController tools, Dictionary<string, IToolHandler> ownValidation) : IToolsController
    {
        public event Action? ListChanged
        {
            add => tools.ListChanged += value;
            remove => tools.ListChanged -= value;
        }

        public bool IsListChangedNotificationSupported => tools.IsListChangedNotificationSupported;

        public Task<ListToolsResult> ListTools(ListToolsRequest request, McpRequestContext context) => tools.ListTools(request, context);

        public Task<CallToolResult> CallTool(CallToolRequest request, McpRequestContext context) =>
            ownValidation.TryGetValue(request.ToolName, out var handler)
                ? handler.Call(request.ToolArguments, context)
                : tools.CallTool(request, context);
    }
}
