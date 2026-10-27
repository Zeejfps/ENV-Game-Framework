using ICSharpCode.Decompiler.TypeSystem;
using ZGF.ILSpy.Core.Analysis;

namespace ZGF.ILSpy.Core;

/// <summary>ILSpy's search for the types deriving from a type: those in its scope, among the assemblies
/// a list holds now, that name it among their direct base types by full name and arity. Each call
/// searches again, so assemblies opened since are searched too.</summary>
internal static class DerivedTypes
{
    public static List<ITypeDefinition> Find(AssemblyList list, ITypeDefinition type, CancellationToken cancellationToken)
    {
        var context = new AnalyzerContext { AssemblyList = list, CancellationToken = cancellationToken };
        var found = new List<ITypeDefinition>();
        foreach (var candidate in context.GetScopeOf(type).GetTypesInScope(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (NamesAsBase(candidate, type))
                found.Add(candidate);
        }
        return found;
    }

    /// <summary>The types of <paramref name="type"/>'s own module that derive from it directly.</summary>
    public static List<ITypeDefinition> FindInModule(ITypeDefinition type)
    {
        return type.ParentModule!.TypeDefinitions
            .Where(c => NamesAsBase(c, type))
            .ToList();
    }

    private static bool NamesAsBase(ITypeDefinition candidate, ITypeDefinition type)
    {
        foreach (var baseType in candidate.DirectBaseTypes)
        {
            if (IsNamedAs(baseType, type))
                return true;
        }
        return false;
    }

    private static bool IsNamedAs(IType baseType, ITypeDefinition type)
    {
        if (baseType.FullName != type.FullName)
            return false;
        return baseType.TypeParameterCount == type.TypeParameterCount;
    }
}
