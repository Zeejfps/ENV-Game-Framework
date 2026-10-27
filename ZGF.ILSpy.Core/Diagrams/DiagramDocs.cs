// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Text.RegularExpressions;
using System.Xml;
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Diagrams;

/// <summary>The XML documentation of an assembly's types and members as the diagrammer shows it:
/// plain text, with the tags around summaries, paragraphs and references taken out.</summary>
internal sealed partial class DiagramDocs
{
    private readonly IDocumentationProvider? _provider;

    private DiagramDocs(IDocumentationProvider? provider)
    {
        _provider = provider;
    }

    /// <summary>The documentation <paramref name="file"/> has, wherever it is found; none when it
    /// has none or it cannot be read.</summary>
    public static DiagramDocs Of(MetadataFile file)
    {
        try
        {
            return new DiagramDocs(XmlDocLoader.LoadDocumentation(file));
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            return new DiagramDocs(null);
        }
    }

    /// <summary>The documentation of <paramref name="type"/>, under an empty key, and of each of
    /// <paramref name="members"/>, under its name; null when none has any.</summary>
    public Dictionary<string, string>? Collect(ITypeDefinition type, IEnumerable<IMember> members)
    {
        if (_provider == null)
            return null;
        var docs = new Dictionary<string, string>();
        Add(docs, string.Empty, type);
        foreach (var member in members)
            Add(docs, member.Name, member);
        if (docs.Count == 0)
            return null;
        return docs;
    }

    private void Add(Dictionary<string, string> docs, string key, IEntity entity)
    {
        var text = TextOf(entity);
        if (string.IsNullOrEmpty(text))
            return;
        docs[key] = text;
    }

    private string? TextOf(IEntity entity)
    {
        var xml = ReadXml(entity);
        if (xml == null)
            return null;
        var text = xml
            .Replace("<summary>", null)
            .Replace("</summary>", null)
            .Replace("<para>", "\n")
            .Replace("</para>", "\n")
            .Trim()
            .Replace('<', '[')
            .Replace('>', ']');
        var plain = Noise().Replace(text, string.Empty);
        return HorizontalSpace().Replace(plain, " ");
    }

    private string? ReadXml(IEntity entity)
    {
        try
        {
            return _provider!.GetDocumentation(entity);
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // Spaces at either end of a line, and what references leave behind once their brackets are
    // squared: the start of a see cref (with its symbol kind) or paramref up to the name, and the
    // quote and slash after it.
    [GeneratedRegex("""^[ \t]+|[ \t]+$|(see\s.ref="(.:)?)|(paramref\sname=")|("\s/)""", RegexOptions.Multiline)]
    private static partial Regex Noise();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex HorizontalSpace();
}
