// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Shows the members whose code uses a method, each compiler-generated method standing for
/// the method it came from. A virtual call to the member a non-virtual method overrides counts.</summary>
internal sealed class MethodUsedByAnalyzer : IAnalyzer
{
    public AnalysisRun? For(ISymbol? symbol)
    {
        if (symbol is not IMethod method || (!method.IsVirtual && method.ParentModule == null))
            return null;
        return context => Analyze(method, context);
    }

    internal IEnumerable<ISymbol> Analyze(IMethod analyzed, AnalyzerContext context)
    {
        if (analyzed.ParentModule?.MetadataFile == null || analyzed.DeclaringTypeDefinition?.MetadataToken.IsNil != false)
            yield break;
        var baseMethod = analyzed.IsVirtual ? null : (IMethod?)InheritanceHelper.GetBaseMember(analyzed);

        var parent = ParentOf(analyzed);
        if (parent.MetadataToken != analyzed.MetadataToken)
            yield return parent;

        var users = context.GetScopeOf(analyzed).MembersWhoseCode(
            method => method.ParentModule is MetadataModule module
                && context.GetMethodBody(method).AnyToken((opCode, token) => References(module, opCode, token)),
            context.CancellationToken);
        foreach (var user in users)
            yield return user is IMethod method ? ParentOf(method) : user;

        IMethod ParentOf(IMethod method) =>
            ((MetadataModule)method.ParentModule!).GetDefinition(
                context.MappingOf(method.DeclaringTypeDefinition!).GetParentMethod((MethodDefinitionHandle)method.MetadataToken));

        bool References(MetadataModule module, ILOpCode opCode, EntityHandle token)
        {
            if (opCode is not (ILOpCode.Call or ILOpCode.Callvirt or ILOpCode.Ldtoken or ILOpCode.Ldftn or ILOpCode.Ldvirtftn or ILOpCode.Newobj)
                && !(analyzed.IsVirtual && opCode == ILOpCode.Jmp))
                return false;
            if (!AnalyzerHelpers.IsPossibleReferenceTo(token, module.MetadataFile, analyzed)
                && (baseMethod == null || !AnalyzerHelpers.IsPossibleReferenceTo(token, module.MetadataFile, baseMethod)))
                return false;
            var member = (module.ResolveEntity(token) as IMember)?.MemberDefinition;
            return member != null && (opCode == ILOpCode.Callvirt && baseMethod != null && member.IsSame(baseMethod) || member.IsSame(analyzed));
        }
    }
}
