using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

internal static class Definitions
{
    /// <summary>The definition a type or member handle of <paramref name="module"/> stands for —
    /// itself, or what a reference, specification or forwarded type resolves to — if it has a
    /// metadata file.</summary>
    public static IEntity? Resolve(MetadataModule module, EntityHandle handle)
    {
        IEntity? definition = handle.Kind == HandleKind.ExportedType
            ? ResolveForwarded(module, (ExportedTypeHandle)handle)
            : module.ResolveEntity(handle) switch
            {
                IType type => type.GetDefinition(),
                IMember member => member.MemberDefinition,
                _ => null,
            };
        return definition is { ParentModule.MetadataFile: not null, MetadataToken.IsNil: false } ? definition : null;
    }

    /// <summary>The type an exported type of <paramref name="module"/> names, found by its name among
    /// the assemblies the module's type system loaded, however many forwards away it is defined.</summary>
    private static ITypeDefinition? ResolveForwarded(MetadataModule module, ExportedTypeHandle handle)
    {
        var name = ForwardedTypes.NameOf(module.MetadataFile.Metadata, handle);
        var type = module.Compilation.FindType(name);
        return type.GetDefinition();
    }
}
