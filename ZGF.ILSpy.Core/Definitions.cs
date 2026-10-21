using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

internal static class Definitions
{
    /// <summary>The definition a type or member handle of <paramref name="module"/> stands for —
    /// itself, or what a reference or specification resolves to — if it has a metadata file.</summary>
    public static IEntity? Resolve(MetadataModule module, EntityHandle handle)
    {
        IEntity? definition = module.ResolveEntity(handle) switch
        {
            IType type => type.GetDefinition(),
            IMember member => member.MemberDefinition,
            _ => null,
        };
        return definition is { ParentModule.MetadataFile: not null, MetadataToken.IsNil: false } ? definition : null;
    }
}
