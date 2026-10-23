using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Output;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>How a run of documentation text is shown.</summary>
public enum DocStyle
{
    Plain,
    Bold,
    Italic,
    /// <summary>Code: a <c>&lt;c&gt;</c> element, a keyword, or a parameter named in the text.</summary>
    Code,
    /// <summary>A type or member the text refers to; <see cref="DocRun.Reference"/> names it when it was found.</summary>
    Reference,
}

public readonly record struct DocRun(string Text, DocStyle Style, CodeReference? Reference = null);

/// <summary>A block of documentation: a paragraph, a code block or a list.</summary>
public abstract record DocBlock;

/// <summary>A paragraph of styled runs.</summary>
public sealed record DocParagraph(IReadOnlyList<DocRun> Runs) : DocBlock;

/// <summary>A code block, its lines kept and their shared indentation removed.</summary>
public sealed record DocCode(string Text) : DocBlock;

/// <summary>What a <see cref="DocList"/> is: bulleted, numbered, or a table of terms and their
/// descriptions.</summary>
public enum DocListKind
{
    Bullet,
    Number,
    Table,
}

/// <summary>One item of a list: the term it names, when it names one, and its description.</summary>
public sealed record DocListItem(IReadOnlyList<DocRun> Term, IReadOnlyList<DocRun> Description);

/// <summary>A list. <see cref="Header"/> is a table's heading row, or a list's leading item.</summary>
public sealed record DocList(DocListKind Kind, DocListItem? Header, IReadOnlyList<DocListItem> Items) : DocBlock;

/// <summary>A part of an entity's XML documentation, in the order ILSpy shows them.</summary>
public enum DocSectionKind
{
    Summary,
    TypeParameter,
    Parameter,
    Returns,
    Value,
    Exception,
    Remarks,
    Example,
}

/// <summary>One part of an entity's documentation. <see cref="Label"/> is the parameter, type
/// parameter or exception the part is about.</summary>
public sealed record DocSection(DocSectionKind Kind, DocRun? Label, IReadOnlyList<DocBlock> Blocks);

/// <summary>A declaration as one line of code, and where in it each parameter starts, so it can be
/// shown with a parameter per line when the line is too long.</summary>
public sealed record CodeSignature(DocumentLine Line, IReadOnlyList<int> ParameterStarts);

/// <summary>Where a type or member is declared: in a type, by its keyword such as <c>class</c> or
/// <c>record struct</c> and its full name, or in a namespace.</summary>
public sealed record CodeContainer(string Keyword, string Name, bool IsNamespace);

/// <summary>What hovering a type or member in code explains: its declaration, highlighted as code
/// with short names, the type or namespace it is in, null for a type in the global namespace, and
/// its XML documentation when there is any.</summary>
public sealed record CodeTooltip(CodeSignature Signature, CodeContainer? Container, IReadOnlyList<DocSection> Sections)
{
    private const ConversionFlags SignatureFlags =
        ConversionFlags.All & ~(ConversionFlags.ShowBody | ConversionFlags.PlaceReturnTypeAfterParameterList |
                                ConversionFlags.UseFullyQualifiedEntityNames | ConversionFlags.UseFullyQualifiedTypeNames);

    internal static CodeTooltip Create(IEntity entity, LoadedAssembly assembly)
    {
        var signature = WriteSignature(entity, assembly);
        var sections = Documentation.Read(entity);
        return new CodeTooltip(signature, ContainerOf(entity), sections);
    }

    private static CodeSignature WriteSignature(IEntity entity, LoadedAssembly assembly)
    {
        var settings = Settings.Create();
        var output = new DocumentBuilder(DecompilationLanguage.CSharp, assembly);
        TokenWriter writer = new TextTokenWriter(output, settings);
        writer = TokenWriter.InsertRequiredSpaces(writer);
        writer = new HighlightingTokenWriter(writer, output);
        var parameters = new ParameterStarts(writer, output);
        var ambience = new CSharpAmbience { ConversionFlags = SignatureFlags };
        ambience.ConvertSymbol(entity, parameters, settings.CSharpFormattingOptions);
        var line = output.Build(entity.Name).Lines[0];
        return new CodeSignature(line, parameters.Starts);
    }

    private const string NamespaceKeyword = "namespace";

    private static CodeContainer? ContainerOf(IEntity entity)
    {
        var declaring = entity.DeclaringTypeDefinition;
        if (declaring != null)
            return new CodeContainer(KeywordOf(declaring), EntityText.FullName(declaring), false);
        if (entity.Namespace.Length == 0)
            return null;
        return new CodeContainer(NamespaceKeyword, entity.Namespace, true);
    }

    private static string KeywordOf(ITypeDefinition type) =>
        type.Kind switch
        {
            TypeKind.Interface => "interface",
            TypeKind.Enum => "enum",
            TypeKind.Delegate => "delegate",
            TypeKind.Struct => type.IsRecord ? "record struct" : "struct",
            _ => type.IsRecord ? "record" : "class",
        };

    /// <summary>Notes the column each parameter of the declaration starts at; parameters of types
    /// inside it, such as a delegate's, are not the declaration's own.</summary>
    private sealed class ParameterStarts(TokenWriter decorated, DocumentBuilder output) : DecoratingTokenWriter(decorated)
    {
        public List<int> Starts { get; } = new();

        public override void StartNode(AstNode node)
        {
            if (IsDeclaredParameter(node))
                Starts.Add(output.Column);
            base.StartNode(node);
        }

        private static bool IsDeclaredParameter(AstNode node) =>
            node is ParameterDeclaration && node.Parent is EntityDeclaration;
    }
}
