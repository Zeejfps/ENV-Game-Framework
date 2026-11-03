// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Shows the members of kind <typeparamref name="T"/> that implement an interface member,
/// or with <paramref name="overrides"/> those that override a class member.</summary>
internal sealed class InheritorsAnalyzer<T>(bool overrides) : IAnalyzer where T : class, IMember
{
    public AnalysisRun? For(ISymbol? symbol)
    {
        if (symbol is not T member || !Applies(member))
            return null;
        return context => Analyze(member, context);
    }

    private bool Applies(T member) => overrides
        ? member.IsOverridable && member.DeclaringType.Kind != TypeKind.Interface
        : member.DeclaringType.Kind == TypeKind.Interface;

    private IEnumerable<ISymbol> Analyze(T analyzed, AnalyzerContext context)
    {
        if (analyzed.DeclaringTypeDefinition is not { ParentModule.MetadataFile: not null } declaring)
            yield break;
        foreach (var type in context.GetScopeOf(analyzed).GetTypesInScope(context.CancellationToken))
        {
            if (!type.GetAllBaseTypeDefinitions().Any(t => t.IsSame(declaring)))
                continue;
            foreach (var member in type.Members.OfType<T>())
            {
                if ((!overrides || member.IsOverride) && InheritanceHelper.GetBaseMembers(member, !overrides).Any(m => m.IsSame(analyzed)))
                    yield return member;
            }
        }
    }
}

/// <summary>Shows the interface members a class or struct member implements.</summary>
internal sealed class MemberImplementsInterfaceAnalyzer : IAnalyzer
{
    public AnalysisRun? For(ISymbol? symbol)
    {
        if (symbol is not IMember { SymbolKind: SymbolKind.Event or SymbolKind.Indexer or SymbolKind.Method or SymbolKind.Property, IsStatic: false } member
            || member.DeclaringTypeDefinition?.Kind is not (TypeKind.Class or TypeKind.Struct))
            return null;
        return _ => Analyze(member);
    }

    private static IEnumerable<ISymbol> Analyze(IMember member) =>
        InheritanceHelper.GetBaseMembers(member, includeImplementedInterfaces: true)
            .Where(m => m.DeclaringTypeDefinition?.Kind == TypeKind.Interface);
}

/// <summary>Shows the base members a member overrides, up its override chain.</summary>
internal sealed class MemberOverridesAnalyzer : IAnalyzer
{
    public AnalysisRun? For(ISymbol? symbol)
    {
        if (symbol is not IMember { SymbolKind: SymbolKind.Event or SymbolKind.Indexer or SymbolKind.Method or SymbolKind.Property, IsOverride: true } member)
            return null;
        return _ => Analyze(member);
    }

    private static IEnumerable<ISymbol> Analyze(IMember member)
    {
        var visited = new HashSet<IMember>();
        while (member.IsOverride)
        {
            member = member.MemberDefinition;
            if (!visited.Add(member))
                break;
            var baseMember = InheritanceHelper.GetBaseMember(member);
            if (baseMember == null)
                break;
            yield return baseMember;
            member = baseMember;
        }
    }
}
