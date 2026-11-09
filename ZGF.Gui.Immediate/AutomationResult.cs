namespace ZGF.Gui.Immediate;

/// <summary>Which commands a server takes: <see cref="Automation"/>, every one, from an app in automation mode, or
/// <see cref="Inspection"/>, only <see cref="AutomationCommand.Capabilities"/>, <see cref="AutomationCommand.Snapshot"/>
/// and <see cref="AutomationCommand.Capture"/>, from an app running as usual, which no command may take over.</summary>
public enum AutomationCapabilitySet
{
    Inspection,
    Automation,
}

/// <summary>A node of a snapshot, as a command found it.</summary>
public sealed record NodeSummary(
    string? Path,
    SemanticRole Role,
    string? Label,
    string? Value,
    SemanticStates States,
    int Surface,
    UiRect Bounds,
    Visibility Visibility,
    string Line)
{
    public static NodeSummary Of(SemanticNode node) => new(
        node.Path?.ToString(), node.Role, node.Label, node.Value, node.States, node.Surface.Index, node.Bounds, node.Visibility, node.ToString());

    public override string ToString() => Line;
}

/// <summary>What a command answered with.</summary>
public abstract record AutomationPayload
{
    private AutomationPayload()
    {
    }

    public sealed record None : AutomationPayload;

    public sealed record Capabilities(int Version, AutomationCapabilitySet Set, IReadOnlyList<string> Commands) : AutomationPayload;

    /// <summary>The snapshot, as <see cref="SemanticSnapshot.ToText"/> writes it.</summary>
    public sealed record Snapshot(string Text) : AutomationPayload;

    public sealed record Node(NodeSummary Found) : AutomationPayload;

    /// <summary>A PNG <see cref="Width"/> by <see cref="Height"/> pixels, and <see cref="Text"/> saying what it shows:
    /// the snapshot, or the subtree of the node it crops to.</summary>
    public sealed record Image(byte[] Png, int Width, int Height, string Text) : AutomationPayload;

    public sealed record Settled(SettleResult Result) : AutomationPayload;

    public sealed record Clipboard(string? Text, string? Html) : AutomationPayload;

    public sealed record MenuItems(IReadOnlyList<SystemMenuItem> Items) : AutomationPayload;

    public sealed record FilePickers(IReadOnlyList<FilePickerRequest> Requests) : AutomationPayload;

    public sealed record Effects(IReadOnlyList<AutomationEffect> Recorded) : AutomationPayload;
}

/// <summary>
/// How a command went: <see cref="Ok"/>, or why not. A target that could not be pressed and a wait that timed out
/// carry the snapshot of that moment, as an in-process harness's exceptions do.
/// </summary>
public abstract record AutomationResult
{
    private AutomationResult()
    {
    }

    /// <summary>Done, with the number of the last frame drawn.</summary>
    public sealed record Ok(long Frame, AutomationPayload Payload) : AutomationResult;

    /// <summary>No node matched the selector.</summary>
    public sealed record NotFound(string Message, string Snapshot) : AutomationResult;

    /// <summary>More than one node matched the selector.</summary>
    public sealed record Ambiguous(string Message, string Snapshot) : AutomationResult;

    /// <summary>Something else takes the pointer everywhere over the node; nothing was pressed.</summary>
    public sealed record Occluded(string Message, string Snapshot) : AutomationResult;

    /// <summary>A modal keeps the pointer from the node; nothing was pressed.</summary>
    public sealed record Blocked(string Message, string Snapshot) : AutomationResult;

    /// <summary>None of the node is shown; nothing was pressed.</summary>
    public sealed record Clipped(string Message, string Snapshot) : AutomationResult;

    /// <summary>No press reaches the node wherever it is; nothing was pressed.</summary>
    public sealed record Unreachable(string Message, string Snapshot) : AutomationResult;

    /// <summary>A wait ran out, or the app's UI thread did not take the command in time; <see cref="Snapshot"/> is
    /// null when none could be taken.</summary>
    public sealed record Timeout(string Message, string? Snapshot) : AutomationResult;

    /// <summary>The server does not take this command, as an inspection-only one takes no input.</summary>
    public sealed record Unsupported(string Message) : AutomationResult;

    /// <summary>The command could not run as given: an argument out of range, a surface not shown, an app that has closed.</summary>
    public sealed record Rejected(string Message) : AutomationResult;

    /// <summary>The snapshot a failure was found in; null for <see cref="Ok"/>, <see cref="Unsupported"/> and
    /// <see cref="Rejected"/>.</summary>
    public string? FailureSnapshot => this switch
    {
        NotFound r => r.Snapshot,
        Ambiguous r => r.Snapshot,
        Occluded r => r.Snapshot,
        Blocked r => r.Snapshot,
        Clipped r => r.Snapshot,
        Unreachable r => r.Snapshot,
        Timeout r => r.Snapshot,
        Ok or Unsupported or Rejected => null,
        _ => throw new InvalidOperationException($"Unknown result {GetType().Name}."),
    };

    /// <summary>What went wrong; null for <see cref="Ok"/>.</summary>
    public string? FailureMessage => this switch
    {
        Ok => null,
        NotFound r => r.Message,
        Ambiguous r => r.Message,
        Occluded r => r.Message,
        Blocked r => r.Message,
        Clipped r => r.Message,
        Unreachable r => r.Message,
        Timeout r => r.Message,
        Unsupported r => r.Message,
        Rejected r => r.Message,
        _ => throw new InvalidOperationException($"Unknown result {GetType().Name}."),
    };
}
