// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Finds the visible members whose signature exposes a type.</summary>
internal sealed class TypeExposedByAnalyzer : IAnalyzer
{
    public bool Show(ISymbol? symbol) => symbol is ITypeDefinition;

    public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
    {
        var analyzed = (ITypeDefinition)analyzedSymbol;
        var visitor = new TypeDefinitionUsedVisitor(analyzed);
        foreach (var type in context.GetScopeOf(analyzed).GetTypesInScope(context.CancellationToken))
        {
            if (analyzed.Kind == TypeKind.Enum && type.IsSame(analyzed) || Members.IsHidden(type))
                continue;
            foreach (var member in type.Fields.Concat<IMember>(type.Properties).Concat(type.Events).Concat(type.Methods))
            {
                if (visitor.IsExposedBy(member))
                    yield return member;
            }
        }
    }

    private sealed class TypeDefinitionUsedVisitor(ITypeDefinition definition) : TypeVisitor
    {
        private bool _found;

        public bool IsExposedBy(IMember member)
        {
            if (member.Accessibility == Accessibility.Private && !member.IsExplicitInterfaceImplementation)
                return false;
            _found = false;
            member.ReturnType.AcceptVisitor(this);
            if (member is IParameterizedMember parameterized)
            {
                foreach (var parameter in parameterized.Parameters)
                    parameter.Type.AcceptVisitor(this);
            }
            return _found;
        }

        public override IType VisitTypeDefinition(ITypeDefinition type)
        {
            _found |= type.IsSame(definition);
            return base.VisitTypeDefinition(type);
        }

        public override IType VisitParameterizedType(ParameterizedType type) => type.GenericType.AcceptVisitor(this);
    }
}
