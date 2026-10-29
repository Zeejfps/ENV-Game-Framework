using System.Text.Json.Serialization;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Comparison;

namespace ZGF.ILSpy;

/// <summary>The documents a list had open as the settings keep them: its tabs in the order opened, the
/// panels of the active tab and the preview tab, and how its document panels were arranged, a layout of
/// those panels alone.</summary>
public sealed record DocumentsRecord(List<DocumentRecord> Tabs, string? Active, string? Preview, DockLayout? Layout);

/// <summary>An open tab as the settings keep it: the panel it was docked as, its title, and what it showed.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NodeDocumentRecord), "node")]
[JsonDerivedType(typeof(DiagramDocumentRecord), "diagram")]
[JsonDerivedType(typeof(ComparisonDocumentRecord), "comparison")]
public abstract record DocumentRecord(string Panel, string? Title);

/// <summary>A tab showing the node at <see cref="Place"/> under the root of the file at
/// <see cref="FilePath"/>, in <see cref="Language"/>, its view standing at <see cref="Position"/>.</summary>
public sealed record NodeDocumentRecord(
    string Panel,
    string? Title,
    AssemblyPath FilePath,
    List<NodePathSegment> Place,
    DecompilationLanguage Language,
    CodeViewPosition? Position) : DocumentRecord(Panel, Title)
{
    public NodePath ToNodePath() => NodePath.From(FilePath, Place);
}

/// <summary>A tab showing a diagram of <see cref="Types"/>, in the order added, the assemblies at
/// <see cref="HomeAssemblies"/> its own, and how it was viewed.</summary>
public sealed record DiagramDocumentRecord(
    string Panel,
    string? Title,
    List<TypeAnchor> Types,
    List<AssemblyPath>? HomeAssemblies,
    DiagramViewRecord? View) : DocumentRecord(Panel, Title);

/// <summary>A tab comparing the API of the assembly at <see cref="LeftPath"/> with that of the one at
/// <see cref="RightPath"/>, taking in what <see cref="Scope"/> says, and how it was viewed.</summary>
public sealed record ComparisonDocumentRecord(
    string Panel,
    string? Title,
    AssemblyPath LeftPath,
    AssemblyPath RightPath,
    ApiScope Scope,
    ComparisonViewRecord? View) : DocumentRecord(Panel, Title);

/// <summary>How a comparison was viewed, as the settings keep it: whether it showed only what differs.</summary>
public sealed record ComparisonViewRecord(bool OnlyDifferences);

/// <summary>How a diagram was viewed, as the settings keep it: the point of it at the view's top-left and
/// the zoom, the parts of its cards folded or opened against how they start, and whether members hidden
/// from other assemblies showed.</summary>
public sealed record DiagramViewRecord(float X, float Y, float Zoom, List<CardFold>? Folds, bool ShowsHiddenMembers);
