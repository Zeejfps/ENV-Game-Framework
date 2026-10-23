// Adapted from ILSpy's TextResourceDetector (MIT License, Copyright (c) 2026 AlphaSierraPapa for the SharpDevelop Team).

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace ZGF.ILSpy.Core.Resources;

/// <summary>The syntax a text resource is colored in.</summary>
internal enum ResourceSyntax
{
    Plain,
    Xml,
    Json,
}

/// <summary>
/// Tells text resources from binary ones as ILSpy does, picks the syntax to color them in by name
/// or by content, and writes them to a document colored in it.
/// </summary>
internal static class ResourceText
{
    /// <summary>Payloads larger than this are never shown as text.</summary>
    public const long MaxLength = 10 * 1024 * 1024;

    private static readonly Dictionary<string, ResourceSyntax> SyntaxByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".xml"] = ResourceSyntax.Xml,
        [".xsd"] = ResourceSyntax.Xml,
        [".xsl"] = ResourceSyntax.Xml,
        [".xslt"] = ResourceSyntax.Xml,
        [".xaml"] = ResourceSyntax.Xml,
        [".axaml"] = ResourceSyntax.Xml,
        [".config"] = ResourceSyntax.Xml,
        [".manifest"] = ResourceSyntax.Xml,
        [".resx"] = ResourceSyntax.Xml,
        [".nuspec"] = ResourceSyntax.Xml,
        [".targets"] = ResourceSyntax.Xml,
        [".props"] = ResourceSyntax.Xml,
        [".proj"] = ResourceSyntax.Xml,
        [".csproj"] = ResourceSyntax.Xml,
        [".vbproj"] = ResourceSyntax.Xml,
        [".svg"] = ResourceSyntax.Xml,
        [".settings"] = ResourceSyntax.Xml,
        [".wsdl"] = ResourceSyntax.Xml,
        [".xshd"] = ResourceSyntax.Xml,
        [".htm"] = ResourceSyntax.Xml,
        [".html"] = ResourceSyntax.Xml,
        [".json"] = ResourceSyntax.Json,
    };

    /// <summary>The payload as text and the syntax to color it in, or null when it is binary,
    /// empty or too large. A BOM picks UTF-8/16/32; without one, only strict UTF-8 is tried. A
    /// control character other than tab, line feed, carriage return or form feed makes it binary.</summary>
    public static (string Text, ResourceSyntax Syntax)? Detect(string name, byte[] data)
    {
        if (data.Length == 0 || data.Length > MaxLength) return null;
        string text;
        try
        {
            using var reader = new StreamReader(new MemoryStream(data), new UTF8Encoding(false, throwOnInvalidBytes: true), detectEncodingFromByteOrderMarks: true);
            text = reader.ReadToEnd();
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
        if (text.Length == 0) return null;
        foreach (var c in text)
        {
            if (c == '�' || c == '\u007F' || c < ' ' && c is not ('\t' or '\n' or '\r' or '\f'))
                return null;
        }
        return (text, SyntaxByExtension.TryGetValue(Path.GetExtension(name), out var syntax) ? syntax : Sniff(text));
    }

    /// <summary>XML by the leading angle bracket, JSON by parsing it; anything else is plain.</summary>
    private static ResourceSyntax Sniff(string text)
    {
        var trimmed = text.AsSpan().TrimStart();
        if (trimmed.IsEmpty) return ResourceSyntax.Plain;
        if (trimmed[0] == '<') return ResourceSyntax.Xml;
        if (trimmed[0] is '{' or '[')
        {
            try
            {
                using var _ = JsonDocument.Parse(text, JsonReadOptions);
                return ResourceSyntax.Json;
            }
            catch (JsonException)
            {
            }
        }
        return ResourceSyntax.Plain;
    }

    private const char NewLine = '\n';

    private static readonly JsonDocumentOptions JsonReadOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>Markup or JSON written on one line, indented to read; anything else, or what does
    /// not parse, as it is.</summary>
    public static string IndentOneLine(string text, ResourceSyntax syntax)
    {
        if (IsMultiline(text))
            return text;
        if (syntax == ResourceSyntax.Xml)
            return TryIndentXml(text) ?? text;
        if (syntax == ResourceSyntax.Json)
            return TryIndentJson(text) ?? text;
        return text;
    }

    private static bool IsMultiline(string text) => text.Trim().Contains(NewLine);

    private static string? TryIndentXml(string text)
    {
        try
        {
            return IndentXml(XDocument.Parse(text));
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>The document one node per line, its declaration kept as the first line.</summary>
    private static string IndentXml(XDocument document)
    {
        var builder = new StringBuilder();
        if (document.Declaration != null)
            builder.Append(document.Declaration).Append(NewLine);
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = Settings.Indentation,
            NewLineChars = NewLine.ToString(),
            OmitXmlDeclaration = true,
        };
        using (var writer = XmlWriter.Create(builder, settings))
            document.Save(writer);
        return builder.ToString();
    }

    private static string? TryIndentJson(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text, JsonReadOptions);
            return IndentJson(document);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The document one value per line, with its characters left unescaped.</summary>
    private static string IndentJson(JsonDocument document)
    {
        var options = new JsonWriterOptions
        {
            Indented = true,
            IndentSize = Settings.Indentation.Length,
            NewLine = NewLine.ToString(),
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, options))
            document.WriteTo(writer);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static void Write(DocumentBuilder output, string text, ResourceSyntax syntax)
    {
        switch (syntax)
        {
            case ResourceSyntax.Xml:
                WriteXml(output, text);
                break;
            case ResourceSyntax.Json:
                WriteJson(output, text);
                break;
            default:
                output.Write(text, SpanKind.Plain);
                break;
        }
    }

    /// <summary>Colors markup: comments, declarations and processing instructions, element names,
    /// attribute names and their quoted values; the rest is plain.</summary>
    private static void WriteXml(DocumentBuilder output, string text)
    {
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] != '<')
            {
                var next = text.IndexOf('<', i);
                if (next < 0) next = text.Length;
                output.Write(text[i..next], SpanKind.Plain);
                i = next;
                continue;
            }
            if (Starts(text, i, "<!--"))
            {
                i = Through(output, text, i, "-->", SpanKind.Comment);
                continue;
            }
            if (Starts(text, i, "<![CDATA["))
            {
                i = Through(output, text, i, "]]>", SpanKind.String);
                continue;
            }
            if (Starts(text, i, "<?") || Starts(text, i, "<!"))
            {
                i = Through(output, text, i, ">", SpanKind.Preprocessor);
                continue;
            }

            var start = i;
            i++;
            if (i < text.Length && text[i] == '/') i++;
            output.Write(text[start..i], SpanKind.Plain);
            start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] is not ('>' or '/')) i++;
            output.Write(text[start..i], SpanKind.Keyword);
            while (i < text.Length && text[i] != '>')
            {
                var c = text[i];
                if (c is '"' or '\'')
                {
                    var close = text.IndexOf(c, i + 1);
                    close = close < 0 ? text.Length : close + 1;
                    output.Write(text[i..close], SpanKind.String);
                    i = close;
                }
                else if (char.IsWhiteSpace(c) || c is '=' or '/')
                {
                    start = i;
                    while (i < text.Length && (char.IsWhiteSpace(text[i]) || text[i] is '=' or '/')) i++;
                    output.Write(text[start..i], SpanKind.Plain);
                }
                else
                {
                    start = i;
                    while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] is not ('=' or '>' or '/' or '"' or '\'')) i++;
                    output.Write(text[start..i], SpanKind.Field);
                }
            }
            if (i < text.Length)
            {
                output.Write(">", SpanKind.Plain);
                i++;
            }
        }

        static bool Starts(string text, int at, string token) => string.CompareOrdinal(text, at, token, 0, token.Length) == 0;

        static int Through(DocumentBuilder output, string text, int at, string end, SpanKind kind)
        {
            var close = text.IndexOf(end, at, StringComparison.Ordinal);
            close = close < 0 ? text.Length : close + end.Length;
            output.Write(text[at..close], kind);
            return close;
        }
    }

    /// <summary>Colors JSON: property names, string values, numbers and the literals true, false
    /// and null; punctuation and anything unexpected stay plain.</summary>
    private static void WriteJson(DocumentBuilder output, string text)
    {
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            var start = i;
            if (c == '"')
            {
                i++;
                while (i < text.Length && text[i] != '"')
                    i += text[i] == '\\' ? 2 : 1;
                i = Math.Min(text.Length, i + 1);
                var after = i;
                while (after < text.Length && char.IsWhiteSpace(text[after])) after++;
                output.Write(text[start..i], after < text.Length && text[after] == ':' ? SpanKind.Field : SpanKind.String);
            }
            else if (c == '-' || char.IsAsciiDigit(c))
            {
                i++;
                while (i < text.Length && (char.IsAsciiDigit(text[i]) || text[i] is '.' or 'e' or 'E' or '+' or '-')) i++;
                output.Write(text[start..i], SpanKind.Number);
            }
            else if (char.IsAsciiLetter(c))
            {
                while (i < text.Length && char.IsAsciiLetter(text[i])) i++;
                var word = text[start..i];
                output.Write(word, word is "true" or "false" or "null" ? SpanKind.Keyword : SpanKind.Plain);
            }
            else
            {
                while (i < text.Length && text[i] is not ('"' or '-') && !char.IsAsciiDigit(text[i]) && !char.IsAsciiLetter(text[i])) i++;
                output.Write(text[start..i], SpanKind.Plain);
            }
        }
    }
}
