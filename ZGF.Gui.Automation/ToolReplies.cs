using ZGF.Gui.Immediate;

namespace ZGF.Gui.Automation;

/// <summary>Reads a gui_* tool's reply into the result it carries, at the client's boundary.</summary>
internal static class ToolReplies
{
    /// <exception cref="McpToolFailedException">The tool failed inside the server, with no structured answer.</exception>
    /// <exception cref="McpProtocolException">The reply is no result this version writes, or its isError disagrees with
    /// its status.</exception>
    public static AutomationResult Read(string tool, ToolCallReply reply)
    {
        if (reply.Structured == null)
        {
            if (reply.IsError)
                throw new McpToolFailedException(tool, reply.Text);
            throw new McpProtocolException($"{tool} answered without structured content: {reply.Text}");
        }
        AutomationResult result;
        try
        {
            result = AutomationWire.ReadResult(reply.Structured.Value, reply.Png);
        }
        catch (FormatException e)
        {
            throw new McpProtocolException($"{tool} answered with something other than a result: {e.Message}", e);
        }
        if (reply.IsError != result is not AutomationResult.Ok)
            throw new McpProtocolException($"{tool} answered {AutomationWire.StatusName(result)} with isError {reply.IsError}.");
        return result;
    }
}
