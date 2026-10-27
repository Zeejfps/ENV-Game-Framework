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

    /// <summary>Whether <paramref name="property"/> holds a generic enumerable, giving the type of its
    /// elements.</summary>
    public static bool TryGetElementType(IProperty property, [MaybeNullWhen(false)] out IType elementType)
    {
        elementType = property.ReturnType.GetElementTypeFromIEnumerable(property.Compilation, true, out var isGeneric);
        return isGeneric == true;
    }

    /// <summary>Whether <paramref name="method"/> overrides a method object declares.</summary>
    public static bool OverridesObject(IMethod method)
    {
        if (!method.IsOverride)
            return false;
        var baseMember = InheritanceHelper.GetBaseMember(method);
        if (baseMember == null)
            return false;
        return IsObject(baseMember.DeclaringType);
    }

    /// <summary>Whether <paramref name="other"/>, one of <paramref name="type"/>'s base classes or the
    /// type itself, is a base class of it other than object.</summary>
    public static bool IsAncestor(ITypeDefinition type, IType other)
    {
        if (other.Equals(type))
            return false;
        return !IsObject(other);
    }
}
