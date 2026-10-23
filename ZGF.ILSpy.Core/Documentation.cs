using System.Xml;
using System.Xml.Linq;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.Output;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>An entity's XML documentation read into sections of styled runs: the summary, each
/// type parameter, parameter and exception, what it returns, and its remarks and example.
/// <c>inheritdoc</c> is followed to the documentation it names or inherits.</summary>
internal static class Documentation
{

    private static readonly CSharpAmbience TypeNames = new() { ConversionFlags = ConversionFlags.ShowTypeParameterList };
    private static readonly CSharpAmbience MemberNames = new() { ConversionFlags = ConversionFlags.ShowTypeParameterList | ConversionFlags.ShowDeclaringType };

    public static IReadOnlyList<DocSection> Read(IEntity entity)
    {
        var root = Parse(Find(entity));
        if (root == null)
            return [];
        var documentation = new XmlDocumentationElement(root, entity, cref => FindCref(cref, entity));
        return ReadSections(documentation);
    }

    private static string? Find(IEntity entity)
    {
        var file = entity.ParentModule?.MetadataFile;
        if (file == null)
            return null;
        try
        {
            return XmlDocLoader.LoadDocumentation(file)?.GetDocumentation(entity);
        }
        catch (Exception e) when (e is XmlException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static XElement? Parse(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;
        try
        {
            return XElement.Parse("<doc>" + xml + "</doc>", LoadOptions.PreserveWhitespace);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static IEntity? FindCref(string cref, IEntity entity)
    {
        var module = entity.ParentModule;
        if (module == null)
            return null;
        return IdStringProvider.FindEntity(cref, new SimpleTypeResolveContext(module));
    }

    /// <summary>The sections in <see cref="DocSectionKind"/> order; text outside any section is
    /// the summary when there is none.</summary>
    private static List<DocSection> ReadSections(XmlDocumentationElement documentation)
    {
        var sections = new List<DocSection>();
        var loose = new DocText();
        foreach (var child in documentation.Children)
        {
            var section = ReadSection(child);
            if (section != null)
                sections.Add(section);
            else if (KindOf(child) == null)
                AddInline(loose, child);
        }
        if (!loose.IsEmpty && !sections.Any(IsSummary))
            sections.Add(new DocSection(DocSectionKind.Summary, null, loose.Finish()));
        return sections
            .Where(HasContent)
            .OrderBy(s => s.Kind)
            .ToList();
    }

    private static DocSectionKind? KindOf(XmlDocumentationElement element) =>
        element.Name switch
        {
            "summary" => DocSectionKind.Summary,
            "typeparam" => DocSectionKind.TypeParameter,
            "param" => DocSectionKind.Parameter,
            "returns" => DocSectionKind.Returns,
            "value" => DocSectionKind.Value,
            "exception" => DocSectionKind.Exception,
            "remarks" => DocSectionKind.Remarks,
            "example" => DocSectionKind.Example,
            _ => null,
        };

    private static DocSection? ReadSection(XmlDocumentationElement element)
    {
        var kind = KindOf(element);
        if (kind == null)
            return null;
        var text = new DocText();
        AddChildren(text, element);
        return new DocSection(kind.Value, LabelOf(element, kind.Value), text.Finish());
    }

    private static DocRun? LabelOf(XmlDocumentationElement element, DocSectionKind kind)
    {
        if (kind == DocSectionKind.Exception)
            return ExceptionLabel(element);
        var name = element.GetAttribute("name");
        if (name == null)
            return null;
        return new DocRun(name, DocStyle.Code);
    }

    /// <summary>The exception an <c>exception</c> element is about, by its short name: the text
    /// inside the element describes when it is thrown.</summary>
    private static DocRun? ExceptionLabel(XmlDocumentationElement element)
    {
        var cref = element.GetAttribute("cref");
        if (cref == null)
            return null;
        var name = ShortName(element.ReferencedEntity, cref);
        return CrefRun(element, name);
    }

    private static bool IsSummary(DocSection section) => section.Kind == DocSectionKind.Summary;

    private static bool HasContent(DocSection section) => section.Blocks.Count > 0 || section.Label != null;

    private static void AddChildren(DocText text, XmlDocumentationElement element)
    {
        foreach (var child in element.Children)
            AddInline(text, child);
    }

    private static void AddInline(DocText text, XmlDocumentationElement element)
    {
        if (element.IsTextNode)
        {
            text.Add(element.TextContent, DocStyle.Plain);
            return;
        }
        switch (element.Name)
        {
            case "para":
                text.Break();
                AddChildren(text, element);
                text.Break();
                break;
            case "br":
                text.Break();
                break;
            case "c":
                text.Add(element.TextContent, DocStyle.Code);
                break;
            case "code":
                text.AddCode(element.TextContent);
                break;
            case "b" or "strong":
                text.Add(element.TextContent, DocStyle.Bold);
                break;
            case "i" or "em":
                text.Add(element.TextContent, DocStyle.Italic);
                break;
            case "paramref" or "typeparamref":
                text.Add(AttributeText(element, "name"), DocStyle.Code);
                break;
            case "see" or "seealso":
                AddSee(text, element);
                break;
            case "list":
                AddList(text, element);
                break;
            default:
                AddChildren(text, element);
                break;
        }
    }

    /// <summary>A <c>see</c>: a keyword, a type or member by its short name, or a link's text.</summary>
    private static void AddSee(DocText text, XmlDocumentationElement element)
    {
        var langword = element.GetAttribute("langword");
        if (langword != null)
        {
            text.Add(langword, DocStyle.Code);
            return;
        }
        var cref = CrefRun(element, element.TextContent);
        if (cref != null)
        {
            text.Add(cref.Value.Text, cref.Value.Style, cref.Value.Reference);
            return;
        }
        var shown = element.TextContent;
        if (shown.Length == 0)
            shown = AttributeText(element, "href");
        text.Add(shown, DocStyle.Plain);
    }

    /// <summary>The value of <paramref name="element"/>'s attribute <paramref name="name"/>, empty without one.</summary>
    private static string AttributeText(XmlDocumentationElement element, string name)
    {
        var value = element.GetAttribute(name);
        if (value == null)
            return "";
        return value;
    }

    /// <summary>The type or member <paramref name="element"/>'s <c>cref</c> names, shown as
    /// <paramref name="shown"/> or else by its short name; null without a <c>cref</c>.</summary>
    private static DocRun? CrefRun(XmlDocumentationElement element, string shown)
    {
        var cref = element.GetAttribute("cref");
        if (cref == null)
            return null;
        var entity = element.ReferencedEntity;
        var name = shown;
        if (name.Length == 0)
            name = ShortName(entity, cref);
        var reference = entity != null ? CodeReference.To(entity) : null;
        return new DocRun(name, DocStyle.Reference, reference);
    }

    private static string ShortName(IEntity? entity, string cref)
    {
        if (entity is ITypeDefinition type)
            return TypeNames.ConvertSymbol(type);
        if (entity != null)
            return MemberNames.ConvertSymbol(entity);
        return ShortNameOf(cref);
    }

    /// <summary>An id string such as <c>M:System.String.Concat(System.String)</c> as
    /// <c>String.Concat</c>: a type by its name, a member with its type's.</summary>
    private static string ShortNameOf(string cref)
    {
        var isType = cref.StartsWith("T:", StringComparison.Ordinal);
        var name = StripPrefix(cref);
        var parameters = name.IndexOf('(');
        if (parameters >= 0)
            name = name.Substring(0, parameters);
        var parts = name.Split('.');
        var kept = isType ? 1 : 2;
        var shown = parts.Skip(Math.Max(0, parts.Length - kept));
        return StripArity(string.Join('.', shown));
    }

    private static string StripPrefix(string cref)
    {
        if (cref.Length > 2 && cref[1] == ':')
            return cref.Substring(2);
        return cref;
    }

    private static string StripArity(string name)
    {
        var tick = name.IndexOf('`');
        if (tick < 0)
            return name;
        return name.Substring(0, tick);
    }

    private static void AddList(DocText text, XmlDocumentationElement list)
    {
        DocListItem? header = null;
        var items = new List<DocListItem>();
        foreach (var child in list.Children)
        {
            if (child.Name == "listheader")
                header = ReadListItem(child);
            else if (child.Name == "item")
                items.Add(ReadListItem(child));
        }
        text.AddList(new DocList(ListKindOf(list), header, items));
    }

    private static DocListKind ListKindOf(XmlDocumentationElement list) =>
        list.GetAttribute("type") switch
        {
            "number" => DocListKind.Number,
            "table" => DocListKind.Table,
            _ => DocListKind.Bullet,
        };

    /// <summary>An item's <c>term</c> and <c>description</c>; text outside them describes it too.</summary>
    private static DocListItem ReadListItem(XmlDocumentationElement item)
    {
        var term = new DocText();
        var description = new DocText();
        foreach (var part in item.Children)
        {
            if (part.Name == "term")
                AddChildren(term, part);
            else if (part.Name == "description")
                AddChildren(description, part);
            else
                AddInline(description, part);
        }
        return new DocListItem(term.FinishInline(), description.FinishInline());
    }
}
