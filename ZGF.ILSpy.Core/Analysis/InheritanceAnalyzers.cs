// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Shows the members of kind <typeparamref name="T"/> that implement an interface member,
/// or with <paramref name="overrides"/> those that override a class member.</summary>
internal sealed class InheritorsAnalyzer<T>(bool overrides) : IAnalyzer where T : class, IMember
{
    public bool Show(ISymbol? symbol) =>
        symbol is T member && (overrides
            ? member.IsOverridable && member.DeclaringType.Kind != TypeKind.Interface
            : member.DeclaringType.Kind == TypeKind.Interface);

    public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
    {
        var analyzed = (T)analyzedSymbol;
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
    public bool Show(ISymbol? symbol) =>
        symbol is IMember { SymbolKind: SymbolKind.Event or SymbolKind.Indexer or SymbolKind.Method or SymbolKind.Property, IsStatic: false } member
        && member.DeclaringTypeDefinition?.Kind is TypeKind.Class or TypeKind.Struct;

    public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context) =>
        InheritanceHelper.GetBaseMembers((IMember)analyzedSymbol, includeImplementedInterfaces: true)
            .Where(m => m.DeclaringTypeDefinition?.Kind == TypeKind.Interface);
}

/// <summary>Shows the base members a member overrides, up its override chain.</summary>
internal sealed class MemberOverridesAnalyzer : IAnalyzer
{
    public bool Show(ISymbol? symbol) =>
        symbol is IMember { SymbolKind: SymbolKind.Event or SymbolKind.Indexer or SymbolKind.Method or SymbolKind.Property, IsOverride: true };

    public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
    {
        var member = (IMember)analyzedSymbol;
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
