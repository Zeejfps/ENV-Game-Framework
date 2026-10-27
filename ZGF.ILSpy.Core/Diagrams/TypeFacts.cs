// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using System.Diagnostics.CodeAnalysis;
using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Diagrams;

internal static class TypeFacts
{
    public static bool IsObject(IType type) => type.IsKnownType(KnownTypeCode.Object);

    public static bool IsInterface(IType type) => type.Kind == TypeKind.Interface;

    /// <summary>Whether <paramref name="type"/> is <c>Nullable&lt;T&gt;</c>, giving its T.</summary>
    public static bool TryGetNullable(IType type, [MaybeNullWhen(false)] out IType underlying)
    {
        if (!type.IsKnownType(KnownTypeCode.NullableOfT))
        {
            underlying = null;
            return false;
        }
        underlying = type.TypeArguments.Single();
        return true;
    }
}
