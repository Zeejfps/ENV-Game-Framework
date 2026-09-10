using System.Buffers;
using System.Text;
using System.Text.Json;
using ZGF.Geometry;

namespace ZGF.Gui.Desktop.Inspection;

/// <summary>One window in a <see cref="MultiWindowSnapshot"/>: its role
/// (main / context-menu / tooltip / secondary), screen rect, OS focus, and the laid-out tree.</summary>
/// <param name="Scale">Device pixels per logical point. The bounds are screen coordinates and the
/// tree's positions are logical points, which are the same number only at 1 — so a reader comparing
/// either against a screenshot needs this.</param>
public sealed record WindowSnapshot(string Role, RectI ScreenBounds, float Scale, bool Focused, UiSnapshot Content);

/// <summary>The whole app rendered as text — every live window stacked, each under a header line.
/// This is what lets an inspector see a context menu (a separate OS window) alongside the main
/// window. Reuses <see cref="UiSnapshot.ToText"/> / <see cref="UiSnapshot.ToJson"/> per window.</summary>
public sealed record MultiWindowSnapshot(IReadOnlyList<WindowSnapshot> Windows)
{
    public string Render(bool asJson) => asJson ? ToJson() : ToText();

    /// <summary>Each window as <c>=== window: ROLE [x,y wxh] focused ===</c> followed by its tree.</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Windows.Count; i++)
        {
            if (i > 0) sb.Append('\n');
            var w = Windows[i];
            var b = w.ScreenBounds;
            sb.Append("=== window: ").Append(w.Role)
              .Append(" [").Append(b.X).Append(',').Append(b.Y)
              .Append(' ').Append(b.Width).Append('x').Append(b.Height).Append(']');
            if (MathF.Abs(w.Scale - 1f) > 0.001f) sb.Append(" scale=").Append(w.Scale);
            if (w.Focused) sb.Append(" focused");
            sb.Append(" ===\n");
            sb.Append(w.Content.ToText());
        }
        return sb.ToString();
    }

    public override string ToString() => ToText();

    /// <summary>An array of <c>{ role, bounds, focused, tree }</c>; <c>tree</c> is the per-window
    /// <see cref="UiSnapshot.ToJson"/> embedded raw, so the node shape stays identical.</summary>
    public string ToJson()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartArray();
            foreach (var w in Windows)
            {
                writer.WriteStartObject();
                writer.WriteString("role", w.Role);
                writer.WriteStartObject("bounds");
                writer.WriteNumber("x", w.ScreenBounds.X);
                writer.WriteNumber("y", w.ScreenBounds.Y);
                writer.WriteNumber("w", w.ScreenBounds.Width);
                writer.WriteNumber("h", w.ScreenBounds.Height);
                writer.WriteEndObject();
                writer.WriteNumber("scale", w.Scale);
                writer.WriteBoolean("focused", w.Focused);
                writer.WritePropertyName("tree");
                writer.WriteRawValue(w.Content.ToJson(), skipInputValidation: true);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
