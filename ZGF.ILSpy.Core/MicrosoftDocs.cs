using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core;

/// <summary>The pages of Microsoft's .NET API reference.</summary>
public static class MicrosoftDocs
{
    private const string ApiReference = "https://learn.microsoft.com/dotnet/api/";

    /// <summary>The reference page of what <paramref name="node"/> stands for, as the reference names
    /// its pages, e.g. <c>system.collections.generic.list-1.add</c>: a namespace, or a type or member
    /// visible outside its assembly. Null for any other node.</summary>
    public static string? FindPage(BrowseNode node)
    {
        if (!node.IsPublicApi)
            return null;
        var name = FindPageName(node);
        if (name == null)
            return null;
        return ApiReference + name.ToLowerInvariant();
    }

    private static string? FindPageName(BrowseNode node)
    {
        if (node is NamespaceNode ns)
            return NamespacePageName(ns);
        if (node is not EntityNode entity)
            return null;
        if (entity.Entity is ITypeDefinition type)
            return TypePageName(type);
        if (entity.Entity is IMember member)
            return MemberPageName(member);
        return null;
    }

    private static string? NamespacePageName(NamespaceNode ns)
    {
        if (ns.Name.Length == 0)
            return null;
        return ns.Name;
    }

    /// <summary>The type's reflection name with its type parameter count after a dash, and a nested
    /// type after a dot: <c>Outer-1.Inner</c>.</summary>
    private static string TypePageName(ITypeDefinition type)
    {
        return type.ReflectionName
            .Replace('`', '-')
            .Replace('+', '.');
    }

    /// <summary>The declaring type's page, then the member's name, a constructor's as <c>-ctor</c>.</summary>
    private static string? MemberPageName(IMember member)
    {
        var declaring = member.DeclaringTypeDefinition;
        if (declaring == null)
            return null;
        var name = member.Name.Replace('.', '-');
        return TypePageName(declaring) + "." + name;
    }
}
