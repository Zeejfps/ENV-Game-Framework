using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ZGF.ILSpy;

/// <summary>The text of a new icon theme file that extends another and lists every icon key, each
/// commented out as drawn now. An icon left commented follows the parent; uncommenting a line changes
/// that one icon.</summary>
public static class IconThemeDraft
{
    private const string SchemaLine = "\"$schema\": \"./icon-theme.schema.json\",";
    private const string Indent = "  ";
    private const string KeyIndent = "    ";
    private const string CodepointFormat = "X4";

    /// <summary>An icon theme named <paramref name="name"/> extending <paramref name="parent"/>, its icons
    /// listed as <paramref name="shown"/> writes them.</summary>
    public static string Compose(string name, ThemeId<IconThemeFile> parent, IconTheme shown)
    {
        var text = new StringBuilder();
        text.Append("{\n");
        text.Append(Indent).Append(SchemaLine).Append('\n');
        text.Append(Indent).Append("\"name\": ").Append(Quote(name)).Append(",\n");
        text.Append(Indent).Append("\"extends\": ").Append(Quote(parent.Text)).Append(",\n");
        text.Append(Indent).Append("\"icons\": {\n");
        foreach (var key in AppIcons.Keys)
            AppendCommentedIcon(text, key.Key, shown.Values[key.Key]);
        text.Append(Indent).Append("}\n");
        text.Append("}\n");
        return text.ToString();
    }

    private static void AppendCommentedIcon(StringBuilder text, string key, IconValue value)
    {
        text.Append(KeyIndent).Append("// ").Append(Quote(key)).Append(": ");
        AppendIcon(text, value);
        text.Append(",\n");
    }

    private static void AppendIcon(StringBuilder text, IconValue value)
    {
        if (value.SvgPath != null)
        {
            text.Append("{ \"svg\": ").Append(Quote(value.SvgPath)).Append(" }");
            return;
        }
        var codepoint = value.Codepoint.ToString(CodepointFormat, CultureInfo.InvariantCulture);
        text.Append("{ \"font\": ").Append(Quote(value.Font!)).Append(", \"glyph\": ").Append(Quote(codepoint)).Append(" }");
    }

    private static string Quote(string value)
    {
        var encoded = JsonEncodedText.Encode(value, JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
        return "\"" + encoded + "\"";
    }
}
