using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>A top-level type an assembly forwards to another: its exported type row, its namespace,
/// its name as the tree shows it (<c>Dictionary&lt;,&gt;</c>), its name without type parameters and how
/// many it has, which it sorts by, and the assembly it is forwarded to.</summary>
internal sealed record ForwardedType(ExportedTypeHandle Handle, string Namespace, string Label, string SortName, int Arity, string Destination);

/// <summary>Reading the types an assembly forwards from its ExportedType table.</summary>
internal static class ForwardedTypes
{
    /// <summary>The top-level types <paramref name="metadata"/> forwards, by namespace. Nested
    /// forwarded types are left out: they come with the type that holds them.</summary>
    public static Dictionary<string, List<ForwardedType>> ListByNamespace(MetadataReader metadata)
    {
        var byNamespace = new Dictionary<string, List<ForwardedType>>(StringComparer.Ordinal);
        foreach (var handle in metadata.ExportedTypes)
        {
            var exported = metadata.GetExportedType(handle);
            if (!IsTopLevelForwarder(exported))
                continue;
            var type = Read(metadata, handle, exported);
            if (!byNamespace.TryGetValue(type.Namespace, out var types))
                byNamespace.Add(type.Namespace, types = new List<ForwardedType>());
            types.Add(type);
        }
        return byNamespace;
    }

    private static bool IsTopLevelForwarder(ExportedType exported)
    {
        if (!exported.IsForwarder)
            return false;
        return exported.Implementation.Kind == HandleKind.AssemblyReference;
    }

    private static ForwardedType Read(MetadataReader metadata, ExportedTypeHandle handle, ExportedType exported)
    {
        var name = NameOf(metadata, handle);
        var label = LabelOf(name.Name, name.TypeParameterCount);
        var destination = metadata.GetAssemblyReference((AssemblyReferenceHandle)exported.Implementation);
        return new ForwardedType(handle, name.Namespace, label, name.Name, name.TypeParameterCount, metadata.GetString(destination.Name));
    }

    /// <summary>The name with a slot for each type parameter, as <c>typeof</c> writes an open generic
    /// type: <c>List&lt;&gt;</c>, <c>Dictionary&lt;,&gt;</c>.</summary>
    private static string LabelOf(string name, int arity)
    {
        if (arity == 0)
            return name;
        var commas = new string(',', arity - 1);
        return $"{name}<{commas}>";
    }

    /// <summary>The full name of the type <paramref name="handle"/> forwards, to find it by.</summary>
    public static TopLevelTypeName NameOf(MetadataReader metadata, ExportedTypeHandle handle)
    {
        var exported = metadata.GetExportedType(handle);
        var ns = metadata.GetString(exported.Namespace);
        var name = ReflectionHelper.SplitTypeParameterCountFromReflectionName(metadata.GetString(exported.Name), out var arity);
        return new TopLevelTypeName(ns, name, arity);
    }
}
