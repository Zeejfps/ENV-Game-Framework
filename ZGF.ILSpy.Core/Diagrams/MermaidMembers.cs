// Derived from ILSpy (MIT): see LICENSE-ILSpy.txt

using ICSharpCode.Decompiler.TypeSystem;

namespace ZGF.ILSpy.Core.Diagrams;

/// <summary>Members as lines of a mermaid class definition: visibility, then type and name, with *
/// marking an abstract member and $ a static one (https://mermaid.js.org/syntax/classDiagram.html).</summary>
internal sealed class MermaidMembers(DiagramNames names)
{
    public string FormatMethod(IMethod method)
    {
        var parameters = string.Join(", ", method.Parameters.Select(FormatParameter));
        var typeArguments = TypeArgumentsOf(method);
        var returnType = names.NameOf(method.ReturnType);
        return $"{VisibilityOf(method.Accessibility)}{NameOf(method)}{typeArguments}({parameters}){ModifierOf(method)} {returnType}";
    }

    private string FormatParameter(IParameter parameter) => names.NameOf(parameter.Type) + " " + parameter.Name;

    private string TypeArgumentsOf(IMethod method)
    {
        if (method.TypeArguments.Count == 0)
            return "";
        var arguments = method.TypeArguments.Select(names.NameOf);
        return "❰" + string.Join(", ", arguments) + "❱";
    }

    // An explicit interface implementation is named after the interface member it implements.
    private string NameOf(IMethod method)
    {
        if (!method.IsExplicitInterfaceImplementation)
            return method.Name;
        var implemented = method.ExplicitlyImplementedInterfaceMembers.FirstOrDefault();
        if (implemented == null)
            return method.Name;
        return names.NameOf(implemented.DeclaringType) + '.' + implemented.Name;
    }

    public string FormatProperty(IProperty property)
    {
        var type = names.NameOf(property.ReturnType);
        return $"{VisibilityOf(property.Accessibility)}{type} {property.Name}{ModifierOf(property)}";
    }

    public string FormatField(IField field)
    {
        var type = names.NameOf(field.ReturnType);
        return $"{VisibilityOf(field.Accessibility)}{type} {field.Name}{ModifierOf(field)}";
    }

    private static string ModifierOf(IMember member)
    {
        if (member.IsAbstract)
            return "*";
        if (member.IsStatic)
            return "$";
        return "";
    }

    private static string VisibilityOf(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Private => "-",
        Accessibility.ProtectedAndInternal => "~",
        Accessibility.Internal => "~",
        Accessibility.Protected => "#",
        Accessibility.ProtectedOrInternal => "#",
        Accessibility.Public => "+",
        _ => "",
    };
}
