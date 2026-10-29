using System.Globalization;
using System.Text;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.Output;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Comparison;

/// <summary>How an entity is declared, as C# writes it, with what of it callers depend on: its
/// accessibility, modifiers and types, a type's base types, a constant's value, a property's accessors,
/// and whether it is obsolete or flags. Two entities matched by <see cref="ApiKeys"/> changed when
/// their declarations differ. Parameter names are left out, so renaming one changes nothing.</summary>
internal static class ApiDeclarations
{
    private const ConversionFlags TypeFlags =
        ConversionFlags.ShowAccessibility | ConversionFlags.ShowModifiers | ConversionFlags.ShowDefinitionKeyword |
        ConversionFlags.ShowTypeParameterList | ConversionFlags.ShowTypeParameterVarianceModifier | ConversionFlags.ShowReturnType |
        ConversionFlags.ShowParameterList | ConversionFlags.ShowParameterModifiers | ConversionFlags.UseFullyQualifiedTypeNames |
        ConversionFlags.SupportRecordClasses | ConversionFlags.SupportRecordStructs;

    private const ConversionFlags MemberFlags =
        ConversionFlags.ShowAccessibility | ConversionFlags.ShowModifiers | ConversionFlags.ShowDefinitionKeyword |
        ConversionFlags.ShowReturnType | ConversionFlags.ShowParameterList | ConversionFlags.ShowParameterModifiers |
        ConversionFlags.ShowParameterDefaultValues | ConversionFlags.ShowTypeParameterList |
        ConversionFlags.ShowTypeParameterVarianceModifier | ConversionFlags.UseFullyQualifiedTypeNames;

    private const string AttributeSuffix = "Attribute";
    private const string GetKeyword = "get";
    private const string SetKeyword = "set";
    private const string InitKeyword = "init";
    private const string NullText = "null";
    private const string TrueText = "true";
    private const string FalseText = "false";

    /// <summary>The attributes that change how callers use what they are applied to.</summary>
    private static readonly HashSet<string> NotableAttributes = new(StringComparer.Ordinal)
    {
        "System.ObsoleteAttribute",
        "System.FlagsAttribute",
    };

    /// <summary>How <paramref name="entity"/> is declared, after its notable attributes.</summary>
    public static string Of(IEntity entity)
    {
        var attributes = WriteNotableAttributes(entity);
        var declaration = Declare(entity);
        return attributes + declaration;
    }

    private static string WriteNotableAttributes(IEntity entity)
    {
        var text = new StringBuilder();
        foreach (var attribute in entity.GetAttributes())
        {
            if (!NotableAttributes.Contains(attribute.AttributeType.FullName))
                continue;
            text.Append(WriteAttribute(attribute));
            text.Append(' ');
        }
        return text.ToString();
    }

    private static string WriteAttribute(IAttribute attribute)
    {
        var name = attribute.AttributeType.Name;
        var shortName = name.Substring(0, name.Length - AttributeSuffix.Length);
        if (attribute.FixedArguments.Length == 0)
            return $"[{shortName}]";
        var arguments = attribute.FixedArguments.Select(static a => WriteValue(a.Value));
        return $"[{shortName}({string.Join(", ", arguments)})]";
    }

    private static string Declare(IEntity entity) =>
        entity switch
        {
            ITypeDefinition type => DeclareType(type),
            IField field => DeclareField(field),
            IProperty property => DeclareProperty(property),
            _ => Convert(entity, MemberFlags),
        };

    private static string DeclareType(ITypeDefinition type)
    {
        var declaration = Convert(type, TypeFlags);
        var bases = ListBases(type);
        if (bases.Count == 0)
            return declaration;
        return declaration + " : " + string.Join(", ", bases);
    }

    /// <summary>The types <paramref name="type"/> derives from as C# declares them: the base class, then
    /// the interfaces sorted by name; for an enum, its underlying type unless it is <c>int</c>.</summary>
    private static List<string> ListBases(ITypeDefinition type)
    {
        if (type.Kind == TypeKind.Enum)
            return ListUnderlyingType(type);
        var classes = type.DirectBaseTypes
            .Where(IsDeclaredBaseClass)
            .Select(EntityText.TypeName);
        var interfaces = type.DirectBaseTypes
            .Where(static t => t.Kind == TypeKind.Interface)
            .Select(EntityText.TypeName)
            .Order(StringComparer.Ordinal);
        return classes
            .Concat(interfaces)
            .ToList();
    }

    private static List<string> ListUnderlyingType(ITypeDefinition type)
    {
        var underlying = type.EnumUnderlyingType;
        if (underlying.IsKnownType(KnownTypeCode.Int32))
            return [];
        return [EntityText.TypeName(underlying)];
    }

    /// <summary>Whether <paramref name="type"/> is a base class C# writes out: none of the implicit ones
    /// classes, structs, enums and delegates derive from.</summary>
    private static bool IsDeclaredBaseClass(IType type)
    {
        if (type.Kind == TypeKind.Interface)
            return false;
        if (type.IsKnownType(KnownTypeCode.Object))
            return false;
        if (type.IsKnownType(KnownTypeCode.ValueType))
            return false;
        if (type.IsKnownType(KnownTypeCode.Enum))
            return false;
        return !type.IsKnownType(KnownTypeCode.MulticastDelegate);
    }

    private static string DeclareField(IField field)
    {
        var declaration = Convert(field, MemberFlags);
        if (!field.IsConst)
            return declaration;
        var value = field.GetConstantValue();
        return declaration + " = " + WriteValue(value);
    }

    /// <summary>The property, then its accessors, each with its accessibility when it differs from the property's.</summary>
    private static string DeclareProperty(IProperty property)
    {
        var text = new StringBuilder(Convert(property, MemberFlags));
        text.Append(" { ");
        if (property.Getter != null)
            AppendAccessor(text, property, property.Getter, GetKeyword);
        if (property.Setter != null)
            AppendAccessor(text, property, property.Setter, SetterKeyword(property.Setter));
        text.Append('}');
        return text.ToString();
    }

    private static string SetterKeyword(IMethod setter)
    {
        if (setter.IsInitOnly)
            return InitKeyword;
        return SetKeyword;
    }

    private static void AppendAccessor(StringBuilder text, IProperty property, IMethod accessor, string keyword)
    {
        if (accessor.Accessibility != property.Accessibility)
        {
            text.Append(WriteAccessibility(accessor.Accessibility));
            text.Append(' ');
        }
        text.Append(keyword);
        text.Append("; ");
    }

    private static string WriteAccessibility(Accessibility accessibility) =>
        accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "private",
        };

    private static string WriteValue(object? value) =>
        value switch
        {
            null => NullText,
            string text => $"\"{text}\"",
            bool flag => flag ? TrueText : FalseText,
            IType type => $"typeof({EntityText.TypeName(type)})",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? NullText,
        };

    private static string Convert(IEntity entity, ConversionFlags flags)
    {
        var ambience = new CSharpAmbience { ConversionFlags = flags };
        return ambience.ConvertSymbol(entity);
    }
}
