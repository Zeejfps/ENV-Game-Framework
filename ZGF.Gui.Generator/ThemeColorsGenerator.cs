using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ZGF.Gui.Generator;

/// <summary>
/// Gives each partial type marked <c>[ThemeColors]</c> the table of colors a theme file can set: a
/// static <c>ColorKeys</c> list, one key per <c>uint</c> property and one per <c>UiShadow</c>
/// property's color, and a static <c>FromColors</c> that builds the type from colors by key. Keys are
/// the property names in camelCase, so the type stays the source of truth and every new color is
/// themeable. A property whose getter has no body is required; one that derives its color is not.
/// </summary>
[Generator]
public sealed class ThemeColorsGenerator : IIncrementalGenerator
{
    private const string AttributeName = "ZGF.Gui.Immediate.ThemeColorsAttribute";
    private const string ShadowTypeName = "ZGF.Gui.Immediate.UiShadow";
    private const string ShadowKeySuffix = "Color";

    private static readonly DiagnosticDescriptor NestedType = new(
        id: "THEME001",
        title: "Themeable type is nested",
        messageFormat: "'{0}' is nested in another type; only a top-level type can be marked [ThemeColors]",
        category: "Theming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ShadowOnNonRecord = new(
        id: "THEME002",
        title: "Shadow colors need a record",
        messageFormat: "'{0}' has shadow properties, whose colors are set with 'with', so it must be a record",
        category: "Theming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private sealed record ColorProperty(string Name, string Key, bool IsRequired, bool IsShadow);

    /// <summary>Why a marked type gets no table, and where, held as values rather than a
    /// <see cref="Location"/> so the model compares by value.</summary>
    private sealed record Problem(DiagnosticDescriptor Descriptor, string FilePath, TextSpan Span, LinePositionSpan LineSpan)
    {
        public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
    }

    /// <summary>What the generator needs of a marked type. It compares by value, so an edit that leaves
    /// the type's colors as they were reuses the generated table.</summary>
    private sealed record ThemeableType(string? Namespace, string Name, string Keyword, EquatableArray<ColorProperty> Colors, Problem? Problem);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            static (node, _) => node is TypeDeclarationSyntax,
            static (ctx, _) => Describe(ctx));
        context.RegisterSourceOutput(types, static (spc, type) => Generate(spc, type));
    }

    private static ThemeableType Describe(GeneratorAttributeSyntaxContext ctx)
    {
        var declaration = (TypeDeclarationSyntax)ctx.TargetNode;
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var colors = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(IsSettable)
            .Select(DescribeColor)
            .Where(static c => c != null)
            .Select(static c => c!)
            .ToImmutableArray();
        var ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
        var problem = FindProblem(declaration, symbol, colors);
        return new ThemeableType(ns, symbol.Name, KeywordOf(declaration), new EquatableArray<ColorProperty>(colors), problem);
    }

    private static Problem? FindProblem(TypeDeclarationSyntax declaration, INamedTypeSymbol symbol, ImmutableArray<ColorProperty> colors)
    {
        if (symbol.ContainingType != null)
            return ProblemAt(NestedType, declaration);
        var hasShadows = colors.Any(static c => c.IsShadow);
        if (hasShadows && declaration is not RecordDeclarationSyntax)
            return ProblemAt(ShadowOnNonRecord, declaration);
        return null;
    }

    private static Problem ProblemAt(DiagnosticDescriptor descriptor, TypeDeclarationSyntax declaration)
    {
        var location = declaration.Identifier.GetLocation();
        var lines = location.GetLineSpan();
        return new Problem(descriptor, lines.Path, location.SourceSpan, lines.Span);
    }

    private static bool IsSettable(IPropertySymbol property)
    {
        if (property.IsStatic || property.IsIndexer)
            return false;
        if (property.DeclaredAccessibility != Accessibility.Public)
            return false;
        return property.SetMethod != null;
    }

    private static ColorProperty? DescribeColor(IPropertySymbol property)
    {
        if (property.Type.SpecialType == SpecialType.System_UInt32)
            return new ColorProperty(property.Name, CamelCase(property.Name), IsRequired(property), IsShadow: false);
        if (property.Type.ToDisplayString() == ShadowTypeName)
            return new ColorProperty(property.Name, CamelCase(property.Name) + ShadowKeySuffix, IsRequired: false, IsShadow: true);
        return null;
    }

    /// <summary>True when the property keeps the color it is given: an auto-property, whose getter
    /// has no body, as opposed to one that derives a color left unset.</summary>
    private static bool IsRequired(IPropertySymbol property)
    {
        if (property.IsRequired)
            return true;
        foreach (var reference in property.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is not PropertyDeclarationSyntax syntax)
                continue;
            if (syntax.ExpressionBody != null)
                return false;
            var getter = syntax.AccessorList?.Accessors.FirstOrDefault(static a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
            if (getter == null)
                return false;
            return getter.Body == null && getter.ExpressionBody == null;
        }
        return false;
    }

    private static string KeywordOf(TypeDeclarationSyntax declaration)
    {
        if (declaration is RecordDeclarationSyntax record && !record.ClassOrStructKeyword.IsKind(SyntaxKind.None))
            return "record " + record.ClassOrStructKeyword.Text;
        return declaration.Keyword.Text;
    }

    /// <summary>A property name as System.Text.Json's camel case writes it: <c>ILOpcode</c> becomes
    /// <c>ilOpcode</c>, <c>TextMuted</c> becomes <c>textMuted</c>.</summary>
    private static string CamelCase(string name)
    {
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var nextIsLower = i + 1 < chars.Length && char.IsLower(chars[i + 1]);
            if (i > 0 && nextIsLower)
                break;
            if (!char.IsUpper(chars[i]))
                break;
            chars[i] = char.ToLowerInvariant(chars[i]);
        }
        return new string(chars);
    }

    private static void Generate(SourceProductionContext spc, ThemeableType type)
    {
        if (type.Problem != null)
        {
            spc.ReportDiagnostic(Diagnostic.Create(type.Problem.Descriptor, type.Problem.ToLocation(), type.Name));
            return;
        }
        spc.AddSource($"{type.Name}.ThemeColors.g.cs", SourceText.From(Emit(type), Encoding.UTF8));
    }

    private static string Emit(ThemeableType type)
    {
        const string keyType = "global::ZGF.Gui.Immediate.ThemeColorKey";
        const string colorsType = "global::System.Collections.Generic.IReadOnlyDictionary<string, uint>";
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        if (type.Namespace != null)
            sb.AppendLine($"namespace {type.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"partial {type.Keyword} {type.Name}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>The keys a theme file sets this type's colors by, in the order they are declared.</summary>");
        sb.AppendLine($"    public static global::System.Collections.Generic.IReadOnlyList<{keyType}> ColorKeys {{ get; }} = new {keyType}[]");
        sb.AppendLine("    {");
        foreach (var color in type.Colors)
            sb.AppendLine($"        new(\"{color.Key}\", {(color.IsRequired ? "true" : "false")}),");
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>An instance with the colors <paramref name=\"colors\"/> holds by key. A color left out");
        sb.AppendLine("    /// is 0, which a derived color takes as unset, and a shadow left out keeps its default color.</summary>");
        sb.AppendLine($"    public static {type.Name} FromColors({colorsType} colors)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var made = new {type.Name}");
        sb.AppendLine("        {");
        foreach (var color in type.Colors.Where(static c => !c.IsShadow))
            sb.AppendLine($"            {color.Name} = ThemeColorOrUnset(colors, \"{color.Key}\"),");
        sb.AppendLine("        };");
        foreach (var color in type.Colors.Where(static c => c.IsShadow))
        {
            sb.AppendLine($"        if (colors.TryGetValue(\"{color.Key}\", out var {color.Key}))");
            sb.AppendLine($"            made = made with {{ {color.Name} = made.{color.Name} with {{ Color = {color.Key} }} }};");
        }
        sb.AppendLine("        return made;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    private static uint ThemeColorOrUnset({colorsType} colors, string key)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (colors.TryGetValue(key, out var color))");
        sb.AppendLine("            return color;");
        sb.AppendLine("        return 0u;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
