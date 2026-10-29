using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>The text of a new theme file that extends another and lists every color a theme can set,
/// each commented out at the color shown now. A color left commented follows the parent, so derived
/// colors keep following the base roles; uncommenting a line changes that one color.</summary>
public static class ThemeDraft
{
    private const string SchemaLine = "\"$schema\": \"./theme.schema.json\",";
    private const string Indent = "  ";
    private const string KeyIndent = "    ";

    /// <summary>A theme named <paramref name="name"/> extending <paramref name="parent"/>, its colors
    /// listed at those of <paramref name="shown"/>.</summary>
    public static string Compose(string name, ThemeId<ThemeFile> parent, AppTheme shown)
    {
        var text = new StringBuilder();
        text.Append("{\n");
        text.Append(Indent).Append(SchemaLine).Append('\n');
        text.Append(Indent).Append("\"name\": ").Append(Quote(name)).Append(",\n");
        text.Append(Indent).Append("\"extends\": ").Append(Quote(parent.Text)).Append(",\n");
        AppendSection(text, "ui", UiTheme.ColorKeys, shown.Ui.ToColors(), isLast: false);
        AppendSection(text, "code", CodeViewPalette.ColorKeys, shown.Code.ToColors(), isLast: false);
        AppendSection(text, "icons", NodeIconPalette.ColorKeys, shown.Icons.ToColors(), isLast: true);
        text.Append("}\n");
        return text.ToString();
    }

    private static void AppendSection(StringBuilder text, string section, IReadOnlyList<ThemeColorKey> keys, Dictionary<string, uint> colors, bool isLast)
    {
        text.Append(Indent).Append(Quote(section)).Append(": {\n");
        foreach (var key in keys)
            AppendCommentedColor(text, key.Key, colors[key.Key]);
        text.Append(Indent).Append('}');
        if (!isLast)
            text.Append(',');
        text.Append('\n');
    }

    private static void AppendCommentedColor(StringBuilder text, string key, uint color)
    {
        text.Append(KeyIndent).Append("// ").Append(Quote(key)).Append(": ").Append(Quote(UiColor.ToHex(color))).Append(",\n");
    }

    private static string Quote(string value)
    {
        var encoded = JsonEncodedText.Encode(value, JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
        return "\"" + encoded + "\"";
    }
}
