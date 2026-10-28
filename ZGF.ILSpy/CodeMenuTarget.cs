using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>What a code view's context menu was opened on: the reference under the pointer, whether
/// that is where it is defined, and the line; one value, so that the view forgets it whole.</summary>
internal sealed record CodeMenuTarget(CodeReference? Reference, bool OnDefinition, int Line)
{
    public static readonly CodeMenuTarget None = new(null, false, CodeViewState.NoLine);

    /// <summary>The reference to go to from here; null on its definition.</summary>
    public CodeReference? Navigable => Reference is { IsNavigable: true } && !OnDefinition ? Reference : null;

    /// <summary>The entity to analyze, find in the API reference or show in a diagram.</summary>
    public CodeReference? Entity => Reference is { IsNavigable: true, Kind: not CodeReferenceKind.Assembly } ? Reference : null;

    /// <summary>The reference to show among the metadata tables.</summary>
    public CodeReference? Metadata => Reference is { IsNavigable: true } ? Reference : null;
}
