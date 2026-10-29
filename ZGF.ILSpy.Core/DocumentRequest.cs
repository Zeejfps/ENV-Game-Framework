using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>What <see cref="NodeDecompiler"/> writes a document of, titled <see cref="Title"/>; a
/// failure to write it is reported as one of <see cref="FullName"/>.</summary>
internal abstract record DocumentRequest
{
    private DocumentRequest(string title, string fullName)
    {
        Title = title;
        FullName = fullName;
    }

    public string Title { get; }

    public string FullName { get; }

    /// <summary>A module's summary and attributes, not its types.</summary>
    public sealed record ModulePreview(LoadedModule Module, string Title, string FullName) : DocumentRequest(Title, FullName);

    public sealed record Type(DefinedType Definition, string Title, string FullName) : DocumentRequest(Title, FullName);

    public sealed record Member(DefinedMember Definition, string Title, string FullName) : DocumentRequest(Title, FullName);

    /// <summary>What a node without code of its own lists, read from <paramref name="Module"/>, or
    /// from none for a package's.</summary>
    public sealed record Summary(SummaryInfo Info, LoadedModule? Module, string Title, string FullName) : DocumentRequest(Title, FullName);

    /// <summary>A resource as its text, or a hex dump of its bytes, which <paramref name="Read"/> reads.</summary>
    public sealed record Resource(LoadedModule? Module, string Description, Func<byte[]> Read, string Title, string FullName)
        : DocumentRequest(Title, FullName);

    /// <summary>WPF's compiled XAML, as the XAML it was compiled from.</summary>
    public sealed record Baml(LoadedModule Module, byte[] Data, string Title, string FullName) : DocumentRequest(Title, FullName);

    /// <summary>Why a listed file could not be read.</summary>
    public sealed record Failure(FailedAssembly Assembly, string Title, string FullName) : DocumentRequest(Title, FullName);
}

/// <summary>What the summary of a node without code of its own lists.</summary>
internal abstract record SummaryInfo
{
    private SummaryInfo()
    {
    }

    /// <summary>A comment a line; an empty line is left blank. Read as they are written.</summary>
    public sealed record Comments(IEnumerable<string> Lines) : SummaryInfo;

    public sealed record Namespace(string Name, IReadOnlyList<NamespaceEntry> Entries) : SummaryInfo;

    public sealed record ForwardedType(string FullName, CodeReference? Target, string? Destination) : SummaryInfo;

    /// <summary>A references folder: its assemblies, then its modules.</summary>
    public sealed record References(IReadOnlyList<SummaryLink> Assemblies, IReadOnlyList<string> Modules) : SummaryInfo;

    public sealed record ModuleReference(string Name, bool ContainsMetadata, IReadOnlyList<SummaryLink> Imports) : SummaryInfo;

    public sealed record ImportedMethod(SummaryLink Method, string EntryPoint, string ModuleName) : SummaryInfo;

    /// <summary>A reference of <paramref name="Module"/>, with where it resolves from.</summary>
    public sealed record AssemblyReference(LoadedModule Module, ICSharpCode.Decompiler.Metadata.AssemblyReference Reference) : SummaryInfo;

    /// <summary>What an assembly uses from a reference, each type with what is used of it under it.</summary>
    public sealed record Used(IReadOnlyList<UsedEntry> Entries) : SummaryInfo;

    /// <summary>A line per type, going to it, as base and derived types are listed.</summary>
    public sealed record Types(IReadOnlyList<SummaryLink> Entries) : SummaryInfo;

    public sealed record Error(Exception Exception) : SummaryInfo;
}

/// <summary>A line naming <see cref="Text"/>, going to <see cref="Target"/> when there is one.</summary>
internal readonly record struct SummaryLink(string Text, CodeReference? Target);

/// <summary>A type forwarded to <see cref="Destination"/>, labelled <see cref="Label"/>.</summary>
internal readonly record struct ForwardedLine(string Label, CodeReference? Target, string? Destination);

internal abstract record NamespaceEntry
{
    private NamespaceEntry()
    {
    }

    public sealed record Declared(ITypeDefinition Type, string Label, CodeReference? Reference) : NamespaceEntry;

    public sealed record Forwarded(ForwardedLine Line) : NamespaceEntry;
}

internal abstract record UsedEntry
{
    private UsedEntry()
    {
    }

    public sealed record Type(string FullName, CodeReference? Target, IReadOnlyList<UsedEntry> Used) : UsedEntry;

    public sealed record Member(string Label, SpanKind Kind, CodeReference? Target) : UsedEntry;

    public sealed record Forwarded(ForwardedLine Line) : UsedEntry;
}

/// <summary>What saving a node's code writes: a whole module, every type of a namespace, or the
/// document the node shows.</summary>
internal abstract record CodeExport
{
    private CodeExport()
    {
    }

    public sealed record WholeModule(LoadedModule Module) : CodeExport;

    public sealed record Namespace(LoadedModule Module, string Name, IReadOnlyList<ITypeDefinition> Types) : CodeExport;

    public sealed record Document(DocumentRequest Request) : CodeExport;
}
