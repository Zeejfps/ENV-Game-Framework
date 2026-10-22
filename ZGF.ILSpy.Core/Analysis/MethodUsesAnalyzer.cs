// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Reflection.Metadata;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Shows the members and types a method's code, compiler-generated parts included, uses.</summary>
internal sealed class MethodUsesAnalyzer : IAnalyzer
{
    public bool Show(ISymbol? symbol) => symbol is IMethod method && method.HasBody;

    public IEnumerable<ISymbol> Analyze(ISymbol symbol, AnalyzerContext context)
    {
        if (symbol is not IMethod { ParentModule.MetadataFile: { } file } method)
            throw new InvalidOperationException("Should never happen.");
        var module = context.AssemblyList.TypeSystemOf(file).MainModule;
        return context.MappingOf(method).GetMethodParts((MethodDefinitionHandle)method.MetadataToken)
            .SelectMany(part => ScanMethod(module, part)).Distinct();
    }

    private static IEnumerable<IEntity> ScanMethod(MetadataModule module, MethodDefinitionHandle handle)
    {
        if (module.MetadataFile.ReadBody(handle) is not { } body)
            yield break;
        var types = new TypeDefinitionCollector();
        foreach (var (opCode, token) in body.Tokens())
        {
            if (opCode.GetOperandType() == OperandType.Type)
                continue;
            switch (Resolve(module, token))
            {
                case IType type:
                    type.AcceptVisitor(types);
                    break;
                case IEntity entity:
                    yield return entity;
                    break;
            }
        }
        foreach (var type in types.UsedTypes)
            yield return type;
    }

    private static object? Resolve(MetadataModule module, EntityHandle token)
    {
        try
        {
            return token.Kind switch
            {
                HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification => module.ResolveType(token, default),
                HandleKind.MethodDefinition or HandleKind.MethodSpecification or HandleKind.MemberReference or HandleKind.FieldDefinition =>
                    module.ResolveEntity(token, default),
                _ => null,
            };
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    private sealed class TypeDefinitionCollector : TypeVisitor
    {
        public readonly List<ITypeDefinition> UsedTypes = new();

        public override IType VisitTypeDefinition(ITypeDefinition type)
        {
            UsedTypes.Add(type);
            return base.VisitTypeDefinition(type);
        }
    }
}
