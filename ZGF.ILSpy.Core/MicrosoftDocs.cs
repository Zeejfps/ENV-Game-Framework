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
        if (node is NamespaceNode ns)
            return FindNamespacePage(ns);
        if (node is EntityNode entity)
            return FindPage(entity.Entity);
        return null;
    }

    /// <summary>The reference page of <paramref name="entity"/>, a type or member visible outside its
    /// assembly. Null for any other entity.</summary>
    internal static string? FindPage(IEntity entity)
    {
        if (!EntityText.IsPublicApi(entity))
            return null;
        var name = FindPageName(entity);
        if (name == null)
            return null;
        return PageAt(name);
    }

    private static string? FindNamespacePage(NamespaceNode ns)
    {
        if (!ns.IsPublicApi)
            return null;
        if (ns.Name.Length == 0)
            return null;
        return PageAt(ns.Name);
    }

    private static string PageAt(string name) => ApiReference + name.ToLowerInvariant();

    private static string? FindPageName(IEntity entity)
    {
        if (entity is ITypeDefinition type)
            return TypePageName(type);
        if (entity is IMember member)
            return MemberPageName(member);
        return null;
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
