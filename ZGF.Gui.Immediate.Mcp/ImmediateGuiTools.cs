using McpSdk.Protocol.Models;
using McpSdk.Server;
using ZGF.Gui.Mcp;

namespace ZGF.Gui.Immediate.Mcp;

/// <summary>
/// The <c>gui_*</c> tools over an immediate-mode app, one per <see cref="AutomationWire.All"/> command, each
/// answering with an <see cref="AutomationResult"/> as structured content under one output schema. What they may do is
/// the <paramref name="runner"/>'s: every command on an app in automation mode, or snapshot and screenshot alone on an
/// app running as usual, where every other tool answers <c>unsupported</c>. Each call blocks a thread-pool thread until
/// the app's UI thread has run it.
/// </summary>
public sealed class ImmediateGuiTools(AutomationRunner runner) : IMcpToolSource
{
    private static readonly ToolOutput<AutomationResult> Output = new(
        new ObjectSchema()
            .Add("status", new StringSchema
            {
                Description = "ok, or why not: not_found, ambiguous, occluded, blocked, clipped, unreachable, timeout, unsupported, rejected.",
                Options = [.. AutomationWire.Statuses],
            })
            .AddOption("frame", new NumberSchema { IsInteger = true, Description = "With ok: the number of the last frame drawn." })
            .AddOption("payload", new ObjectSchema().Add("kind", new StringSchema { Description = "With ok: what the command answered with; its other members depend on it." }))
            .AddOption("message", new StringSchema { Description = "Without ok: what went wrong." })
            .AddOption("snapshot", new StringSchema { Description = "With a failed lookup or a timeout: the snapshot of that moment." }),
        AutomationWire.WriteMembers);

    /// <summary>What a client is told on initialize: the protocol version and what this server may do.</summary>
    public string Instructions => runner.Capabilities == AutomationCapabilitySet.Automation
        ? $"ZGF immediate-mode automation, protocol {AutomationWire.ProtocolVersion}, capabilities: automation. The app runs hidden, on a virtual clock, with fake OS services: nothing moves until a tool runs frames. Start with gui_snapshot; target nodes by path, role and label, or text."
        : $"ZGF immediate-mode inspection, protocol {AutomationWire.ProtocolVersion}, capabilities: inspection. The app is running as usual for a person: only gui_capabilities, gui_snapshot and gui_screenshot work, and every tool that would drive it answers unsupported.";

    public void Register(McpSession session, McpToolSet tools)
    {
        foreach (var verb in AutomationWire.All)
        {
            var name = verb.Name;
            var description = runner.Capabilities == AutomationCapabilitySet.Automation || verb.Inspection
                ? verb.Description
                : verb.Description + " Unsupported here: this server only inspects.";
            tools.Add(McpTools.DefineUnchecked(name, description, InputSchema(verb), verb.ReadOnly, Output, (arguments, cancel) => Call(name, arguments, cancel)));
        }
    }

    private ToolReply<AutomationResult> Call(string name, string arguments, CancellationToken cancel)
    {
        var result = AutomationWire.Parse(name, arguments) switch
        {
            WireCommand.Parsed parsed => runner.Run(parsed.Command, cancel),
            WireCommand.Rejected rejected => new AutomationResult.Rejected(rejected.Reason),
            _ => throw new InvalidOperationException("Unknown parse outcome."),
        };
        return new ToolReply<AutomationResult>(result, AutomationWire.Text(result))
        {
            Png = AutomationWire.Png(result),
            IsError = result is not AutomationResult.Ok,
        };
    }

    private static ObjectSchema InputSchema(WireVerb verb)
    {
        var schema = new ObjectSchema();
        foreach (var arg in verb.Args)
        {
            JsonSchema type = arg.Kind switch
            {
                WireArgKind.String => new StringSchema { Description = arg.Description, Options = arg.Options?.ToArray() },
                WireArgKind.Number => new NumberSchema { Description = arg.Description },
                WireArgKind.Integer => new NumberSchema { Description = arg.Description, IsInteger = true },
                WireArgKind.Boolean => new BooleanSchema { Description = arg.Description },
                WireArgKind.StringList => new ArraySchema("string") { Description = arg.Description },
                _ => throw new ArgumentOutOfRangeException(nameof(verb), arg.Kind, null),
            };
            if (arg.Required)
                schema.Add(arg.Name, type);
            else
                schema.AddOption(arg.Name, type);
        }
        return schema;
    }
}
