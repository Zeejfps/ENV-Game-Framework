using System.Buffers;
using System.Text;
using System.Text.Json;
using McpSdk.Adapter.System.Text.Json;
using McpSdk.Protocol;
using McpSdk.Protocol.Models;
using McpSdk.Server;

namespace ZGF.Gui.Desktop;

/// <summary>
/// Building blocks for an <see cref="IMcpToolSource"/>: a tool whose body reads
/// <see cref="ToolArguments"/> and whose exceptions become tool errors, and a tool that declares an
/// output schema and answers with structured content plus a text rendering.
/// </summary>
public static class McpTools
{
    private static readonly SystemJson Json = new();

    public static IToolHandler Define(string name, string description, ObjectSchema input, bool readOnly,
        Func<ToolArguments, CallToolResult> call) =>
        new DelegateTool(NewTool(name, description, input, readOnly), call);

    public static IToolHandler Define<T>(string name, string description, ObjectSchema input, bool readOnly,
        ToolOutput<T> output, Func<ToolArguments, ToolReply<T>> call)
    {
        var tool = NewTool(name, description, input, readOnly);
        tool.OutputSchema = output.Schema;
        return new DelegateTool(tool, args => Structured(output, call(args)));
    }

    public static CallToolResult Text(string text) => CallToolResult.Ok(new TextContent(text));

    public static CallToolResult Image(byte[] png) => CallToolResult.Ok([new ImageContent("image/png", png)]);

    private static Tool NewTool(string name, string description, ObjectSchema input, bool readOnly)
    {
        var tool = new Tool(name, description, input);
        if (readOnly) tool.Annotations = new ToolAnnotations { ReadOnlyHint = true };
        return tool;
    }

    private static CallToolResult Structured<T>(ToolOutput<T> output, ToolReply<T> reply)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            output.WriteMembers(writer, reply.Value);
            writer.WriteEndObject();
        }
        var serialized = Encoding.UTF8.GetString(buffer.WrittenSpan);
        return new CallToolResult(
            [new TextContent(reply.Text), new TextContent(serialized)],
            null, Json.Parse(serialized), null);
    }

    private sealed class DelegateTool(Tool tool, Func<ToolArguments, CallToolResult> call) : IToolHandler
    {
        public Tool Tool { get; } = tool;

        // Every failure inside a tool, an argument the parse rejected or a driver that could not find
        // its target, is reported to the client as a tool error rather than a protocol error.
        public Task<CallToolResult> Call(IJsonObject arguments, McpRequestContext context)
        {
            try { return Task.FromResult(call(ToolArguments.Parse(arguments))); }
            catch (Exception ex) { return Task.FromResult(CallToolResult.Error(new TextContent(ex.Message))); }
        }
    }
}

/// <summary>A tool's declared <c>outputSchema</c> paired with the writer that emits a value of
/// <typeparamref name="T"/> as the members of the matching <c>structuredContent</c> object. The SDK
/// validates structured content at System.Text.Json's default depth of 64 when it parses and again
/// when it writes the response, so the shape written here must stay shallower than that.</summary>
public sealed class ToolOutput<T>(ObjectSchema schema, Action<Utf8JsonWriter, T> writeMembers)
{
    public ObjectSchema Schema { get; } = schema;
    internal void WriteMembers(Utf8JsonWriter writer, T value) => writeMembers(writer, value);
}

/// <summary>A structured tool's answer: the value sent as <c>structuredContent</c>, and the text
/// sent as the first <c>content</c> block, ahead of a second holding the structured value serialized,
/// for clients that read only <c>content</c>.</summary>
public readonly record struct ToolReply<T>(T Value, string Text);

/// <summary>
/// A tool call's arguments. Names match case-insensitively, and each failure below is a tool error
/// naming the argument: two arguments whose names differ only in case, a <c>null</c> or wrongly
/// typed value (a string where a number is expected, say), and a number outside the finite
/// <see cref="float"/> range. An absent argument reads as null or the fallback.
/// </summary>
public sealed class ToolArguments
{
    private readonly Dictionary<string, IJsonProperty> _values;

    private ToolArguments(Dictionary<string, IJsonProperty> values) => _values = values;

    internal static ToolArguments Parse(IJsonObject? arguments)
    {
        var values = new Dictionary<string, IJsonProperty>(StringComparer.OrdinalIgnoreCase);
        if (arguments != null)
            foreach (var (name, value) in arguments)
                if (!values.TryAdd(name, value))
                    throw new InvalidOperationException($"'{name}' is given more than once.");
        return new ToolArguments(values);
    }

    public string? Str(string name)
    {
        if (!_values.TryGetValue(name, out var value)) return null;
        if (!value.IsString) throw new InvalidOperationException($"'{name}' must be a string.");
        return value.AsString();
    }

    public string RequiredStr(string name) =>
        Str(name) ?? throw new InvalidOperationException($"'{name}' is required.");

    public float? Num(string name)
    {
        if (!_values.TryGetValue(name, out var value)) return null;
        var number = Read(name, "a number", value, v => v.AsFloat());
        if (!float.IsFinite(number)) throw new InvalidOperationException($"'{name}' must be a finite number.");
        return number;
    }

    public bool Bool(string name, bool fallback)
    {
        if (!_values.TryGetValue(name, out var value)) return fallback;
        return Read(name, "a boolean", value, v => v.AsBool());
    }

    // IJsonProperty reports only IsString and IsArray; its typed accessors throw
    // InvalidOperationException on any other kind, so that is the type test for numbers and booleans.
    private static T Read<T>(string name, string expected, IJsonProperty value, Func<IJsonProperty, T> read)
    {
        try { return read(value); }
        catch (InvalidOperationException) { throw new InvalidOperationException($"'{name}' must be {expected}."); }
    }
}
