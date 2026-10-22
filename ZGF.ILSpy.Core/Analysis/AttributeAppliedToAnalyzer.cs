// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Analysis;

/// <summary>Shows the symbols an attribute is applied to.</summary>
internal sealed class AttributeAppliedToAnalyzer : IAnalyzer
{
    public bool Show(ISymbol? symbol) =>
        symbol is ITypeDefinition type && type.GetNonInterfaceBaseTypes().Any(t => t.IsKnownType(KnownTypeCode.Attribute));

    public IEnumerable<ISymbol> Analyze(ISymbol analyzedSymbol, AnalyzerContext context)
    {
        if (analyzedSymbol is not ITypeDefinition attributeType)
            return [];
        var scope = context.GetScopeOf(attributeType);
        var known = attributeType.IsBuiltinAttribute();
        var found = scope.GetModulesInScope(context.CancellationToken).SelectMany(module =>
        {
            var ts = scope.ConstructTypeSystem(module);
            context.CancellationToken.ThrowIfCancellationRequested();
            return known.IsCustomAttribute() ? CustomAppliedTo(attributeType, ts) : BuiltinAppliedTo(known, ts);
        });
        return known.IsCustomAttribute() ? found.Distinct() : found;
    }

    private static IEnumerable<ISymbol> CustomAppliedTo(ITypeDefinition attributeType, DecompilerTypeSystem ts)
    {
        var metadata = ts.MainModule.MetadataFile.Metadata;
        var decoder = new FindTypeDecoder(ts.MainModule, attributeType);
        return AnalyzerHelpers.AttributeOwners(ts, a => AnalyzerHelpers.IsCustomAttributeOfType(a.Constructor, metadata, decoder));
    }

    private static IEnumerable<ISymbol> BuiltinAppliedTo(KnownAttribute attribute, DecompilerTypeSystem ts)
    {
        var types = ts.MainModule.TypeDefinitions;
        var methods = types.SelectMany(static t => t.Members.OfType<IMethod>());
        return attribute switch
        {
            KnownAttribute.Serializable or KnownAttribute.ComImport or KnownAttribute.StructLayout => types.Where(t => t.HasAttribute(attribute)),
            KnownAttribute.DllImport or KnownAttribute.PreserveSig or KnownAttribute.MethodImpl =>
                methods.Where(m => m.HasAttribute(attribute)).Select(static m => m.AccessorOwner ?? m),
            KnownAttribute.FieldOffset or KnownAttribute.NonSerialized => Fields(),
            KnownAttribute.MarshalAs => Fields().Concat(Parameters()).Concat(Parameters()),
            KnownAttribute.Optional or KnownAttribute.In or KnownAttribute.Out => Parameters(),
            KnownAttribute.IndexerName => types.SelectMany(static t => t.Properties).Where(p => p.HasAttribute(attribute)),
            _ => [],
        };

        IEnumerable<ISymbol> Fields() => types.SelectMany(static t => t.Fields).Where(f => f.HasAttribute(attribute));

        IEnumerable<ISymbol> Parameters() =>
            methods.Where(m => m.Parameters.Any(p => p.HasAttribute(attribute))).Select(static m => m.AccessorOwner ?? m);
    }
}
