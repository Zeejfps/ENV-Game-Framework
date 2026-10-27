// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Text.Json.Serialization;

namespace ZGF.ILSpy.Core.Diagrams;

/// <summary>The types of an assembly as the HTML diagrammer reads them: those it lets you pick, by
/// namespace and then by id, and the display names of the types they refer to but that it leaves out,
/// by id.</summary>
internal sealed class DiagramModel
{
    public required Dictionary<string, string> OutsideReferences { get; init; }
    public required Dictionary<string, Dictionary<string, DiagramType>> TypesByNamespace { get; init; }
}

/// <summary>One type of a <see cref="DiagramModel"/>: its own members as a mermaid class definition
/// (https://mermaid.js.org/syntax/classDiagram.html), what it derives from and implements, the
/// members it inherits by the id of the type declaring them, and its XML documentation, the type's
/// own under an empty key. Each dictionary of types is keyed by type id.</summary>
internal sealed class DiagramType
{
    /// <summary>The label of the type, when it differs from its id.</summary>
    public string? Name { get; init; }

    public required string Body { get; init; }

    /// <summary>The type it derives from with the label of a closed generic one; null when that is object.</summary>
    public Dictionary<string, string?>? BaseType { get; init; }

    public Dictionary<string, string?[]>? Interfaces { get; init; }
    public Dictionary<string, InheritedMembers>? Inherited { get; init; }
    public Dictionary<string, string>? XmlDocs { get; init; }

    /// <summary>Properties holding one of the diagram's types, the type's id by property label.</summary>
    public Dictionary<string, string>? HasOne { get; init; }

    /// <summary>Properties holding many of the diagram's types, the type's id by property label.</summary>
    public Dictionary<string, string>? HasMany { get; init; }
}

/// <summary>The members a type inherits from one of its base types: its plain ones as mermaid
/// members and its relations, as <see cref="DiagramType"/> has them.</summary>
internal sealed class InheritedMembers
{
    public string? FlatMembers { get; init; }
    public Dictionary<string, string>? HasOne { get; init; }
    public Dictionary<string, string>? HasMany { get; init; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DiagramModel))]
internal sealed partial class DiagramJson : JsonSerializerContext;
