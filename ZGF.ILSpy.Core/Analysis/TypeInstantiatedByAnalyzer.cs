// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Shows the members whose code instantiates a type.</summary>
internal sealed class TypeInstantiatedByAnalyzer : IAnalyzer
{
    public bool Show(ISymbol? symbol) => symbol is ITypeDefinition type && !type.IsAbstract && !type.IsStatic;

    public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
    {
        var type = (ITypeDefinition)analyzedSymbol;
        return context.GetScopeOf(type).MembersWhoseCode(
            method => method.ParentModule is MetadataModule module
                && context.GetMethodBody(method).AnyToken((opCode, token) => Instantiates(module, opCode, token, type)),
            context.CancellationToken);
    }

    private static bool Instantiates(MetadataModule module, ILOpCode opCode, EntityHandle token, ITypeDefinition type)
    {
        if (opCode is not (ILOpCode.Newobj or ILOpCode.Initobj) && !(opCode == ILOpCode.Call && type.Kind == TypeKind.Struct))
            return false;
        var found = token.Kind switch
        {
            HandleKind.MethodDefinition or HandleKind.MemberReference =>
                module.ResolveMethod(token, default) is { IsConstructor: true } constructor ? constructor.DeclaringTypeDefinition : null,
            HandleKind.TypeDefinition or HandleKind.TypeReference => module.ResolveType(token, default)?.GetDefinition(),
            _ => null,
        };
        return found?.ParentModule != null && found.IsSame(type);
    }
}
