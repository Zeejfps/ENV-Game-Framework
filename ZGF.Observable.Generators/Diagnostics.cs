using Microsoft.CodeAnalysis;

namespace ZGF.Observable.Generators;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor PropertyNotPartial = new(
        id: "ZGFOBS001",
        title: "[Observable] property must be partial",
        messageFormat: "Property '{0}' is decorated with [Observable] but is not declared partial",
        category: "ZGF.Observable",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyMissingAccessors = new(
        id: "ZGFOBS002",
        title: "[Observable] property must have both getter and setter",
        messageFormat: "Property '{0}' must declare both 'get' and 'set' accessors. Use Derived<T> for read-only computed observables.",
        category: "ZGF.Observable",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingTypeNotPartial = new(
        id: "ZGFOBS003",
        title: "[Observable] property's containing type must be partial",
        messageFormat: "Type '{0}' contains [Observable] properties but is not declared partial",
        category: "ZGF.Observable",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingTypeNotView = new(
        id: "ZGFOBS004",
        title: "[Observable] property must be declared on a View subclass",
        messageFormat: "Type '{0}' is not a subclass of ZGF.Gui.View. [Observable] requires the protected Property<T>() helper from View to wire SetDirty.",
        category: "ZGF.Observable",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AmbiguousInitialValue = new(
        id: "ZGFOBS005",
        title: "[Observable] property has both attribute initial value and Default method",
        messageFormat: "Property '{0}' specifies [Observable(...)] initial value and also has a 'Default{0}()' method. Choose one.",
        category: "ZGF.Observable",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DefaultMethodTypeMismatch = new(
        id: "ZGFOBS006",
        title: "Default method return type does not match property type",
        messageFormat: "Method 'Default{0}()' returns '{1}' but property '{0}' has type '{2}'",
        category: "ZGF.Observable",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
