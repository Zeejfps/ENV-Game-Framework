namespace ZGF.ILSpy.Core;

/// <summary>
/// A type named by the file of the assembly that defines it and its full name as metadata writes it,
/// nested types after a '+' and generic ones with their arity, such as <c>Outer+Inner`1</c>. Unlike a
/// <see cref="CodeReference"/> it names the type by what it is rather than where its row is, so it
/// finds the type again after the app restarts or the assembly is rebuilt. Paths are compared ignoring
/// case, as the list compares them; names are not.
/// </summary>
public readonly record struct TypeAnchor(string AssemblyPath, string FullName)
{
    public bool Equals(TypeAnchor other) =>
        string.Equals(FullName, other.FullName, StringComparison.Ordinal)
        && string.Equals(AssemblyPath, other.AssemblyPath, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(AssemblyPath), StringComparer.Ordinal.GetHashCode(FullName));
}
