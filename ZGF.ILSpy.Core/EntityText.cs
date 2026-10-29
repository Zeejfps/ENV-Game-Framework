using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.Output;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

internal static class EntityText
{
    private const ConversionFlags MemberFlags =
        ConversionFlags.ShowParameterList | ConversionFlags.ShowReturnType | ConversionFlags.PlaceReturnTypeAfterParameterList |
        ConversionFlags.ShowTypeParameterList | ConversionFlags.ShowParameterModifiers;

    private const ConversionFlags FullNameFlags =
        ConversionFlags.ShowDeclaringType | ConversionFlags.UseFullyQualifiedEntityNames | ConversionFlags.ShowTypeParameterList |
        ConversionFlags.ShowParameterList | ConversionFlags.ShowParameterModifiers | ConversionFlags.UseFullyQualifiedTypeNames;

    private const ConversionFlags DeclarationFlags =
        ConversionFlags.ShowAccessibility | ConversionFlags.ShowDefinitionKeyword | ConversionFlags.ShowModifiers |
        ConversionFlags.ShowTypeParameterList | ConversionFlags.SupportRecordClasses | ConversionFlags.SupportRecordStructs;

    public static string Label(IEntity entity) => entity is ITypeDefinition type ? TypeLabel(type) : Convert(entity, MemberFlags);

    public static NodeKind KindOf(IEntity entity) =>
        entity switch
        {
            ITypeDefinition { Kind: TypeKind.Interface } => NodeKind.Interface,
            ITypeDefinition { Kind: TypeKind.Struct } type => type.IsRecord ? NodeKind.RecordStruct : NodeKind.Struct,
            ITypeDefinition { Kind: TypeKind.Enum } => NodeKind.Enum,
            ITypeDefinition { Kind: TypeKind.Delegate } => NodeKind.Delegate,
            ITypeDefinition type => type.IsRecord ? NodeKind.Record : NodeKind.Class,
            IField { IsConst: true } => NodeKind.Constant,
            IField => NodeKind.Field,
            IProperty { IsIndexer: true } => NodeKind.Indexer,
            IProperty => NodeKind.Property,
            IEvent => NodeKind.Event,
            IMethod { IsConstructor: true } => NodeKind.Constructor,
            IMethod { IsOperator: true } => NodeKind.Operator,
            _ => NodeKind.Method,
        };

    /// <summary>What sort of type or member <paramref name="entity"/> is.</summary>
    public static SymbolShape SymbolShapeOf(IEntity entity) => entity switch
    {
        ITypeDefinition type => new SymbolShape.Type(ShapeOf(type)),
        IMember member => new SymbolShape.Member(ShapeOf(member)),
        _ => new SymbolShape.Member(MemberShape.Method),
    };

    /// <summary>What sort of type <paramref name="type"/> is.</summary>
    public static TypeShape ShapeOf(ITypeDefinition type) => type.Kind switch
    {
        TypeKind.Interface => TypeShape.Interface,
        TypeKind.Struct => type.IsRecord ? TypeShape.RecordStruct : TypeShape.Struct,
        TypeKind.Enum => TypeShape.Enum,
        TypeKind.Delegate => TypeShape.Delegate,
        _ => type.IsRecord ? TypeShape.Record : TypeShape.Class,
    };

    /// <summary>What sort of member <paramref name="member"/> is.</summary>
    public static MemberShape ShapeOf(IMember member) => member switch
    {
        IField field => field.IsConst ? MemberShape.Constant : MemberShape.Field,
        IProperty property => property.IsIndexer ? MemberShape.Indexer : MemberShape.Property,
        IEvent => MemberShape.Event,
        IMethod method => method.IsConstructor ? MemberShape.Constructor : method.IsOperator ? MemberShape.Operator : MemberShape.Method,
        _ => MemberShape.Method,
    };

    public static string FullName(IEntity entity) =>
        entity is ITypeDefinition type
            ? type.DeclaringTypeDefinition != null
                ? $"{FullName(type.DeclaringTypeDefinition)}.{TypeLabel(type)}"
                : type.Namespace.Length == 0 ? TypeLabel(type) : $"{type.Namespace}.{TypeLabel(type)}"
            : Convert(entity, FullNameFlags);

    private static string TypeLabel(ITypeDefinition type)
    {
        var inherited = type.DeclaringTypeDefinition?.TypeParameterCount ?? 0;
        if (type.TypeParameterCount <= inherited) return type.Name;
        return $"{type.Name}<{string.Join(", ", type.TypeParameters.Skip(inherited).Select(p => p.Name))}>";
    }

    public static string Declaration(ITypeDefinition type) => Convert(type, DeclarationFlags);

    public static string TypeName(IType type) =>
        new CSharpAmbience { ConversionFlags = ConversionFlags.UseFullyQualifiedTypeNames | ConversionFlags.ShowTypeParameterList }.ConvertType(type);

    /// <summary><paramref name="type"/> as C# writes it without namespaces, e.g. <c>List&lt;int&gt;</c>.</summary>
    public static string ShortTypeName(IType type) =>
        new CSharpAmbience { ConversionFlags = ConversionFlags.ShowTypeParameterList }.ConvertType(type);

    public static NodeAccessibility Map(Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => NodeAccessibility.Public,
            Accessibility.Protected => NodeAccessibility.Protected,
            Accessibility.ProtectedOrInternal => NodeAccessibility.ProtectedInternal,
            Accessibility.Internal => NodeAccessibility.Internal,
            Accessibility.ProtectedAndInternal => NodeAccessibility.PrivateProtected,
            Accessibility.Private => NodeAccessibility.Private,
            _ => NodeAccessibility.None,
        };

    public static bool IsPublicApi(IEntity entity) =>
        entity.EffectiveAccessibility() is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal;

    private static string Convert(IEntity entity, ConversionFlags flags) =>
        new CSharpAmbience { ConversionFlags = flags }.ConvertSymbol(entity);
}
