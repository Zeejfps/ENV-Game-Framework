using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
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

/// <summary>A paragraph of documentation. A code block keeps its line breaks.</summary>
public sealed record DocParagraph(IReadOnlyList<DocRun> Runs, bool IsCode = false);

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
public sealed record DocSection(DocSectionKind Kind, DocRun? Label, IReadOnlyList<DocParagraph> Paragraphs);

/// <summary>What hovering a type or member in code explains: its declaration, highlighted as code,
/// and its XML documentation when there is any.</summary>
public sealed record CodeTooltip(IReadOnlyList<DocumentLine> Signature, IReadOnlyList<DocSection> Sections)
{
    private const ConversionFlags SignatureFlags =
        ConversionFlags.All & ~(ConversionFlags.ShowBody | ConversionFlags.PlaceReturnTypeAfterParameterList);

    internal static CodeTooltip Create(IEntity entity, LoadedAssembly assembly)
    {
        var signature = WriteSignature(entity, assembly);
        var sections = Documentation.Read(entity);
        return new CodeTooltip(signature, sections);
    }

    private static IReadOnlyList<DocumentLine> WriteSignature(IEntity entity, LoadedAssembly assembly)
    {
        var settings = Settings.Create();
        var output = new DocumentBuilder(DecompilationLanguage.CSharp, assembly);
        TokenWriter writer = new TextTokenWriter(output, settings);
        writer = TokenWriter.InsertRequiredSpaces(writer);
        writer = new HighlightingTokenWriter(writer, output);
        var ambience = new CSharpAmbience { ConversionFlags = SignatureFlags };
        ambience.ConvertSymbol(entity, writer, settings.CSharpFormattingOptions);
        return output.Build(entity.Name).Lines;
    }
}
