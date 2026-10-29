namespace ZGF.ILSpy.Core;

/// <summary>
/// A type named by the file of the assembly that defines it and its full name as metadata writes it,
/// nested types after a '+' and generic ones with their arity, such as <c>Outer+Inner`1</c>. Unlike a
/// <see cref="CodeReference"/> it names the type by what it is rather than where its row is, so it
/// finds the type again after the app restarts or the assembly is rebuilt.
/// </summary>
public readonly record struct TypeAnchor(AssemblyPath AssemblyPath, string FullName);
