using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
/// themeable. A property whose getter has no body is required; one that derives its color is not. Each
/// key carries its property's summary as plain text, for a schema to show what the color is for.
/// </summary>
[Generator]
public sealed class ThemeColorsGenerator : IIncrementalGenerator
{
    private const string AttributeName = "ZGF.Gui.Immediate.ThemeColorsAttribute";
    private const string ShadowTypeName = "ZGF.Gui.Immediate.UiShadow";
    private const string ShadowKeySuffix = "Color";
    private const string DocCommentPrefix = "///";
    private const string DerivedDescription = "Derived from other colors, or a default, when unset.";

    private static readonly Regex DocLinePrefix = new(@"^\s*///", RegexOptions.Multiline);
    private static readonly Regex Summary = new(@"<summary>(.*?)</summary>", RegexOptions.Singleline);
    private static readonly Regex SeeCref = new(@"<see\s+cref=""([^""]+)""\s*/>");
    private static readonly Regex NamedReference = new(@"<(?:paramref|typeparamref)\s+name=""([^""]+)""\s*/>");
    private static readonly Regex AnyTag = new(@"<[^>]+>");
    private static readonly Regex Whitespace = new(@"\s+");

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

    private sealed record ColorProperty(string Name, string Key, bool IsRequired, bool IsShadow, string Description);

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
        var properties = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(IsSettable)
            .Where(IsColor)
            .ToList();
        var keysByName = properties.ToDictionary(static p => p.Name, KeyOf);
        var colors = properties
            .Select(p => DescribeColor(p, keysByName))
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

    private static bool IsColor(IPropertySymbol property)
    {
        if (property.Type.SpecialType == SpecialType.System_UInt32)
            return true;
        return IsShadow(property);
    }

    private static bool IsShadow(IPropertySymbol property) => property.Type.ToDisplayString() == ShadowTypeName;

    private static string KeyOf(IPropertySymbol property)
    {
        if (IsShadow(property))
            return CamelCase(property.Name) + ShadowKeySuffix;
        return CamelCase(property.Name);
    }

    private static ColorProperty DescribeColor(IPropertySymbol property, Dictionary<string, string> keysByName)
    {
        var isShadow = IsShadow(property);
        var isRequired = !isShadow && IsRequired(property);
        var description = DescribeRole(property, keysByName, isRequired, isShadow);
        return new ColorProperty(property.Name, keysByName[property.Name], isRequired, isShadow, description);
    }

    /// <summary>The property's summary as plain text, a color it names written as its key; for a derived
    /// color without one, that it is derived.</summary>
    private static string DescribeRole(IPropertySymbol property, Dictionary<string, string> keysByName, bool isRequired, bool isShadow)
    {
        var summary = ReadSummary(property);
        if (summary.Length > 0)
            return ToPlainText(summary, keysByName);
        if (isRequired || isShadow)
            return "";
        return DerivedDescription;
    }

    /// <summary>The inside of the property's <c>&lt;summary&gt;</c>, read from its comment trivia so it
    /// is found whether or not the compilation parses documentation comments.</summary>
    private static string ReadSummary(IPropertySymbol property)
    {
        foreach (var reference in property.DeclaringSyntaxReferences)
        {
            var comment = ReadDocComment(reference.GetSyntax());
            var match = Summary.Match(comment);
            if (match.Success)
                return match.Groups[1].Value;
        }
        return "";
    }

    private static string ReadDocComment(SyntaxNode declaration)
    {
        var text = new StringBuilder();
        foreach (var trivia in declaration.GetLeadingTrivia())
        {
            if (IsDocComment(trivia))
                text.AppendLine(trivia.ToFullString());
        }
        return DocLinePrefix.Replace(text.ToString(), "");
    }

    private static bool IsDocComment(SyntaxTrivia trivia)
    {
        if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            return true;
        if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            return false;
        return trivia.ToString().StartsWith(DocCommentPrefix, StringComparison.Ordinal);
    }

    /// <summary>Doc comment XML as one line of text: a <c>see cref</c> to a color becomes its key, to
    /// anything else its name; other tags are dropped and whitespace collapsed.</summary>
    private static string ToPlainText(string xml, Dictionary<string, string> keysByName)
    {
        var text = SeeCref.Replace(xml, m => NameOfReference(m.Groups[1].Value, keysByName));
        text = NamedReference.Replace(text, static m => m.Groups[1].Value);
        text = AnyTag.Replace(text, "");
        text = DecodeEntities(text);
        return Whitespace.Replace(text, " ").Trim();
    }

    private static string NameOfReference(string cref, Dictionary<string, string> keysByName)
    {
        var name = cref.Split('(')[0];
        var member = name.Substring(name.LastIndexOf('.') + 1);
        if (keysByName.TryGetValue(member, out var key))
            return key;
        return member;
    }

    private static string DecodeEntities(string text)
    {
        return text
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&amp;", "&");
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
            sb.AppendLine($"        new(\"{color.Key}\", {(color.IsRequired ? "true" : "false")}, {SymbolDisplay.FormatLiteral(color.Description, true)}),");
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>Every color by its key as this instance's getters give it, so a derived color as");
        sb.AppendLine("    /// derived, and a shadow's color.</summary>");
        sb.AppendLine("    public global::System.Collections.Generic.Dictionary<string, uint> ToColors()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new global::System.Collections.Generic.Dictionary<string, uint>");
        sb.AppendLine("        {");
        foreach (var color in type.Colors)
            sb.AppendLine($"            [\"{color.Key}\"] = {ColorExpression(color)},");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
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

    private static string ColorExpression(ColorProperty color)
    {
        if (color.IsShadow)
            return color.Name + ".Color";
        return color.Name;
    }
}
