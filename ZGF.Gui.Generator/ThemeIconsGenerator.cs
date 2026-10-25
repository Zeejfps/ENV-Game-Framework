using System;
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
/// Gives each partial class marked <c>[ThemeIcons("file.json")]</c> a property per key of the reference
/// icon theme's <c>icons</c> object, read from the <c>AdditionalFiles</c> item whose path ends with the
/// file named: <c>node.class</c> becomes <c>NodeClass</c>, an <c>IconGlyph</c>. A constructor takes
/// them from an <c>IconSet</c>, and a static <c>Keys</c> list holds each key with the comment written
/// above it, so code names icons by meaning and a misspelled key fails to compile.
/// </summary>
[Generator]
public sealed class ThemeIconsGenerator : IIncrementalGenerator
{
    private const string AttributeName = "ZGF.Gui.Immediate.ThemeIconsAttribute";
    private const string IconsKey = "icons";

    private static readonly DiagnosticDescriptor NestedType = new(
        id: "ICON001",
        title: "Icon class is nested",
        messageFormat: "'{0}' is nested in another type; only a top-level class can be marked [ThemeIcons]",
        category: "Theming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingReference = new(
        id: "ICON002",
        title: "Reference icon theme not found",
        messageFormat: "No AdditionalFiles item ends with '{0}', the reference icon theme of '{1}'",
        category: "Theming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnreadableReference = new(
        id: "ICON003",
        title: "Reference icon theme cannot be read",
        messageFormat: "The reference icon theme '{0}' cannot be read: {1}",
        category: "Theming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MemberClash = new(
        id: "ICON004",
        title: "Icon keys give the same member",
        messageFormat: "The icon key '{0}' gives the member '{1}', which another key or the class already has",
        category: "Theming",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>Where a diagnostic goes, held as values rather than a <see cref="Location"/> so the model
    /// compares by value.</summary>
    private sealed record Place(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
    {
        public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
    }

    private sealed record IconClass(string? Namespace, string Name, string Keyword, string Reference, Place Place, bool IsNested);

    private sealed record ReferenceFile(string Path, string Text);

    private sealed record IconEntry(string Key, string Member, string Description);

    /// <summary>What the generator emits for a marked class, or the diagnostic it reports instead. It
    /// compares by value, so an edit that leaves the keys as they were reuses the generated class.</summary>
    private sealed record IconTable(
        string? Namespace,
        string Name,
        string Keyword,
        EquatableArray<IconEntry> Icons,
        DiagnosticDescriptor? Problem,
        string ProblemSubject,
        string ProblemDetail,
        Place Place);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, _) => Describe(ctx));
        var files = context.AdditionalTextsProvider
            .Where(static t => t.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Select(static (t, ct) => new ReferenceFile(Normalize(t.Path), t.GetText(ct)?.ToString() ?? ""))
            .Collect();
        var tables = classes
            .Combine(files)
            .Select(static (pair, _) => Read(pair.Left, pair.Right));
        context.RegisterSourceOutput(tables, static (spc, table) => Generate(spc, table));
    }

    private static IconClass Describe(GeneratorAttributeSyntaxContext ctx)
    {
        var declaration = (ClassDeclarationSyntax)ctx.TargetNode;
        var symbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var reference = ReadReferenceArgument(ctx.Attributes);
        var ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
        var location = declaration.Identifier.GetLocation();
        var lines = location.GetLineSpan();
        var place = new Place(lines.Path, location.SourceSpan, lines.Span);
        return new IconClass(ns, symbol.Name, KeywordOf(declaration), reference, place, symbol.ContainingType != null);
    }

    private static string ReadReferenceArgument(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.ConstructorArguments.Length == 0)
                continue;
            if (attribute.ConstructorArguments[0].Value is string reference)
                return reference;
        }
        return "";
    }

    private static string KeywordOf(ClassDeclarationSyntax declaration)
    {
        if (declaration.Modifiers.Any(static m => m.IsKind(SyntaxKind.SealedKeyword)))
            return "sealed partial class";
        return "partial class";
    }

    private static IconTable Read(IconClass iconClass, ImmutableArray<ReferenceFile> files)
    {
        if (iconClass.IsNested)
            return Fail(iconClass, NestedType, iconClass.Name);
        var file = FindReference(iconClass.Reference, files);
        if (file == null)
            return Fail(iconClass, MissingReference, iconClass.Reference, iconClass.Name);
        List<(string Key, string Description)> keys;
        try
        {
            keys = JsoncIconKeys.Read(file.Text, IconsKey);
        }
        catch (FormatException e)
        {
            return Fail(iconClass, UnreadableReference, iconClass.Reference, e.Message);
        }
        return Describe(iconClass, keys);
    }

    private static ReferenceFile? FindReference(string reference, ImmutableArray<ReferenceFile> files)
    {
        var suffix = "/" + Normalize(reference).TrimStart('/');
        foreach (var file in files)
        {
            if (file.Path.EndsWith(suffix, StringComparison.Ordinal))
                return file;
        }
        return null;
    }

    private static IconTable Describe(IconClass iconClass, List<(string Key, string Description)> keys)
    {
        var members = new HashSet<string> { iconClass.Name, "Keys" };
        var icons = ImmutableArray.CreateBuilder<IconEntry>();
        foreach (var (key, description) in keys)
        {
            var member = Identifier(key);
            if (!members.Add(member))
                return Fail(iconClass, MemberClash, key, member);
            icons.Add(new IconEntry(key, member, description));
        }
        var entries = new EquatableArray<IconEntry>(icons.ToImmutable());
        return new IconTable(iconClass.Namespace, iconClass.Name, iconClass.Keyword, entries, null, "", "", iconClass.Place);
    }

    private static IconTable Fail(IconClass iconClass, DiagnosticDescriptor problem, string subject, string detail = "")
    {
        var none = new EquatableArray<IconEntry>(ImmutableArray<IconEntry>.Empty);
        return new IconTable(iconClass.Namespace, iconClass.Name, iconClass.Keyword, none, problem, subject, detail, iconClass.Place);
    }

    private static string Normalize(string path) => path.Replace('\\', '/').ToLowerInvariant();

    /// <summary>A key in Pascal case, its dots, dashes and underscores dropped: <c>node.recordStruct</c>
    /// becomes <c>NodeRecordStruct</c>.</summary>
    private static string Identifier(string key)
    {
        var text = new StringBuilder();
        var upper = true;
        foreach (var c in key)
        {
            if (c is '.' or '-' or '_' or ' ' or '/')
            {
                upper = true;
                continue;
            }
            if (!char.IsLetterOrDigit(c))
                continue;
            if (text.Length == 0 && !char.IsLetter(c))
                text.Append('_');
            text.Append(upper ? char.ToUpperInvariant(c) : c);
            upper = false;
        }
        if (text.Length == 0)
            return "_";
        return text.ToString();
    }

    private static void Generate(SourceProductionContext spc, IconTable table)
    {
        if (table.Problem != null)
        {
            spc.ReportDiagnostic(Diagnostic.Create(table.Problem, table.Place.ToLocation(), table.ProblemSubject, table.ProblemDetail));
            return;
        }
        spc.AddSource($"{table.Name}.ThemeIcons.g.cs", SourceText.From(Emit(table), Encoding.UTF8));
    }

    private static string Emit(IconTable table)
    {
        const string keyType = "global::ZGF.Gui.Immediate.IconKey";
        const string glyphType = "global::ZGF.Gui.Immediate.IconGlyph";
        const string setType = "global::ZGF.Gui.Immediate.IconSet";
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        if (table.Namespace != null)
            sb.AppendLine($"namespace {table.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"{table.Keyword} {table.Name}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>The keys an icon theme sets icons by, in the order the reference theme lists them.</summary>");
        sb.AppendLine($"    public static global::System.Collections.Generic.IReadOnlyList<{keyType}> Keys {{ get; }} = new {keyType}[]");
        sb.AppendLine("    {");
        foreach (var icon in table.Icons)
            sb.AppendLine($"        new({SymbolDisplay.FormatLiteral(icon.Key, true)}, {SymbolDisplay.FormatLiteral(icon.Description, true)}),");
        sb.AppendLine("    };");
        foreach (var icon in table.Icons)
        {
            sb.AppendLine();
            if (icon.Description.Length > 0)
                sb.AppendLine($"    /// <summary>{EscapeXml(icon.Description)}</summary>");
            sb.AppendLine($"    public {glyphType} {icon.Member} {{ get; }}");
        }
        sb.AppendLine();
        sb.AppendLine("    /// <summary>The icons <paramref name=\"icons\"/> draws, by key.</summary>");
        sb.AppendLine($"    public {table.Name}({setType} icons)");
        sb.AppendLine("    {");
        foreach (var icon in table.Icons)
            sb.AppendLine($"        {icon.Member} = icons.Find({SymbolDisplay.FormatLiteral(icon.Key, true)});");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}
