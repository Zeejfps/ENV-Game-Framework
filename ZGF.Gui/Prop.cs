using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace ZGF.Gui;

/// <summary>
/// A widget authoring input: a property sourced as either a constant value, a live observable,
/// or an auto-tracked compute. <see cref="Apply{TView}"/> turns whichever form was supplied into
/// the right wiring on a built view — set-once for a constant, a subscription for a source, a
/// tracked binding for a compute. This is the single uniform channel that makes every widget
/// property reactive, replacing the bespoke <c>Bind*</c> companion prop per property.
/// <para>
/// A constant converts implicitly (<c>Padding = PaddingStyle.All(16)</c>); a reactive value is
/// explicit via <see cref="Prop.Bind{T}(System.Func{T})"/> (<c>Padding = Prop.Bind(() =&gt; …)</c>).
/// The explicit form is forced by the language — C# will not flow a bare lambda through a
/// user-defined conversion — and it reads as a deliberate "this one is live."
/// </para>
/// </summary>
public readonly struct Prop<T>
{
    private readonly T _value;
    private readonly IReadable<T>? _source;
    private readonly Func<T>? _compute;

    public bool IsSet { get; }

    public Prop(T value)
    {
        _value = value;
        _source = null;
        _compute = null;
        IsSet = true;
    }

    internal Prop(IReadable<T> source)
    {
        _value = default!;
        _source = source;
        _compute = null;
        IsSet = true;
    }

    internal Prop(Func<T> compute)
    {
        _value = default!;
        _source = null;
        _compute = compute;
        IsSet = true;
    }

    /// <summary>The constant snapshot. Meaningful only when this prop was given a constant.</summary>
    public T Value => _value;

    public static implicit operator Prop<T>(T value) => new(value);

    /// <summary>
    /// Applies this prop onto a freshly built view. A constant is written immediately; a source or
    /// compute attaches a binding behavior whose lifetime follows the view's mounted state. A
    /// no-op when unset, so an absent prop leaves the view's own default untouched.
    /// </summary>
    public void Apply<TView>(TView view, Action<TView, T> set) where TView : View
    {
        if (!IsSet)
            return;
        if (_compute != null)
            view.Behaviors.Add(new DerivedPropertyBindingBehavior<TView, T>(view, _compute, set));
        else if (_source != null)
            view.Behaviors.Add(new PropertyBindingBehavior<TView, T, T>(view, _source, static x => x, set));
        else
            set(view, _value);
    }

    public static Prop<T> Unset => default;
}

/// <summary>
/// Inference-friendly constructors for the reactive forms of <see cref="Prop{T}"/> — constructors
/// can't infer generic arguments, so <c>Prop.Bind(() =&gt; vm.X.Value)</c> beats spelling out
/// <c>Prop&lt;PaddingStyle&gt;</c>. The constant form needs no helper: it converts implicitly.
/// </summary>
public static class Prop
{
    /// <summary>A prop driven by an auto-tracked compute; every observable it reads is a dependency.</summary>
    public static Prop<T> Bind<T>(Func<T> compute) => new(compute);

    /// <summary>
    /// A prop bound to an observable source (a <see cref="State{T}"/>, <see cref="Derived{T}"/>, or
    /// any <see cref="IReadable{T}"/>). Explicit on purpose: a bare <c>State&lt;T&gt;</c> assigned to
    /// a constant prop would collapse through its implicit <c>T</c> conversion and silently lose
    /// reactivity.
    /// </summary>
    public static Prop<T> Bind<T>(IReadable<T> source) => new(source);
}
