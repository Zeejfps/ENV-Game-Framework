using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace ZGF.Gui;

/// <summary>
/// A widget authoring input: a property sourced as a constant value, a live observable, or an
/// auto-tracked compute. <see cref="Apply{TView}"/> turns whichever form was supplied into the
/// right wiring on a built view — set-once for a constant, a subscription for a source, a tracked
/// binding for a compute — so every widget property is uniformly reactive with no bespoke
/// <c>Bind*</c> companion prop.
/// <para>Three authoring forms, cheapest first:</para>
/// <list type="bullet">
/// <item><b>Constant</b> — <c>Background = 0xFF1E1E1E</c>. Converts implicitly.</item>
/// <item><b>Observable</b> — <c>Background = vm.BgColor</c> for a concrete <see cref="State{T}"/> or
/// <see cref="Derived{T}"/> (implicit); an interface-typed <see cref="IReadable{T}"/> uses
/// <c>Prop.Bind(vm.BgColor)</c>. The binding subscribes on mount and releases on unmount and never
/// disposes the source, so pass externally-owned observables (VM/service state) that outlive the view.</item>
/// <item><b>Projection</b> — <c>Background = vm.IsDone.Bind(d =&gt; d ? A : B)</c>. See
/// <see cref="PropExtensions.Bind{TIn,TOut}"/>; leak-free.</item>
/// <item><b>Compute</b> — <c>Background = Prop.Bind(() =&gt; a.Value ? b.Value : c)</c> for ad-hoc
/// multi-source logic. Explicit because C# will not flow a bare lambda through a user-defined
/// conversion.</item>
/// <item><b>Context-deferred</b> — <c>Color = Theme.Color(s =&gt; s.X)</c>; resolved from the build
/// <see cref="Context"/> at apply time via <see cref="Prop.Deferred{T}"/>, so a value can depend on a
/// service (theme, locale) yet stay ctx-free to author.</item>
/// </list>
/// </summary>
public readonly struct Prop<T>
{
    private readonly T _value;
    private readonly IReadable<T>? _source;
    private readonly Func<T>? _compute;
    private readonly Func<Context, Prop<T>>? _deferred;

    public bool IsSet { get; }

    public Prop(T value)
    {
        _value = value;
        _source = null;
        _compute = null;
        _deferred = null;
        IsSet = true;
    }

    internal Prop(IReadable<T> source)
    {
        _value = default!;
        _source = source;
        _compute = null;
        _deferred = null;
        IsSet = true;
    }

    internal Prop(Func<T> compute)
    {
        _value = default!;
        _source = null;
        _compute = compute;
        _deferred = null;
        IsSet = true;
    }

    internal Prop(Func<Context, Prop<T>> deferred)
    {
        _value = default!;
        _source = null;
        _compute = null;
        _deferred = deferred;
        IsSet = true;
    }

    /// <summary>The constant snapshot. Meaningful only when this prop was given a constant.</summary>
    public T Value => _value;

    public static implicit operator Prop<T>(T value) => new(value);

    /// <summary>
    /// Binds directly to a <see cref="State{T}"/>. Unambiguous against State's implicit <c>T</c>
    /// conversion: reaching a constant <c>Prop</c> through it would need two user-defined conversions
    /// (illegal), so the compiler takes this single-step path and the binding stays live. (C# forbids
    /// a conversion operator over the <see cref="IReadable{T}"/> interface itself — CS0552 — so the
    /// implicit forms are spelled out per concrete observable; interface-typed sources use
    /// <see cref="Prop.Bind{T}(IReadable{T})"/>.)
    /// </summary>
    public static implicit operator Prop<T>(State<T> source) => new((IReadable<T>)source);

    /// <summary>Binds directly to a <see cref="Derived{T}"/>.</summary>
    public static implicit operator Prop<T>(Derived<T> source) => new((IReadable<T>)source);

    /// <summary>
    /// Applies this prop onto a freshly built view. A constant is written immediately; a source or
    /// compute attaches a binding behavior whose lifetime follows the view's mounted state. A
    /// no-op when unset, so an absent prop leaves the view's own default untouched.
    /// </summary>
    public void Apply<TView>(Context ctx, TView view, Action<TView, T> set) where TView : View
    {
        if (!IsSet)
            return;
        if (_deferred != null)
            _deferred(ctx).Apply(ctx, view, set);
        else if (_compute != null)
            view.Behaviors.Add(new DerivedPropertyBindingBehavior<TView, T>(view, _compute, set));
        else if (_source != null)
            view.Behaviors.Add(new PropertyBindingBehavior<TView, T, T>(view, _source, static x => x, set));
        else
            set(view, _value);
    }

    /// <summary>
    /// Resolves this prop to a value-producing function, for bindings whose target isn't a single
    /// view property (e.g. a children collection). Invoking the result inside a tracked binding
    /// registers the prop's observable dependencies, so the binding re-fires on change; a constant
    /// yields a function that never changes. <paramref name="ctx"/> resolves the deferred form.
    /// </summary>
    internal Func<T> AsCompute(Context ctx)
    {
        if (_deferred != null) return _deferred(ctx).AsCompute(ctx);
        if (_compute != null) return _compute;
        if (_source is { } source) return () => source.Value;
        var value = _value;
        return () => value;
    }

    public static Prop<T> Unset => default;
}

/// <summary>
/// Inference-friendly constructor for the compute form of <see cref="Prop{T}"/> — constructors and
/// conversions can't take a bare lambda, so <c>Prop.Bind(() =&gt; …)</c> is the entry point for
/// ad-hoc, multi-source reactive values. Constants and observables need no helper: they convert
/// implicitly.
/// </summary>
public static class Prop
{
    /// <summary>A prop driven by an auto-tracked compute; every observable it reads is a dependency.</summary>
    public static Prop<T> Bind<T>(Func<T> compute) => new(compute);

    /// <summary>
    /// A prop bound directly to any observable, including an interface-typed <see cref="IReadable{T}"/>
    /// that can't ride an implicit conversion (CS0552). The binding subscribes on mount and releases on
    /// unmount; it never disposes <paramref name="source"/>, so pass an externally-owned observable that
    /// outlives the view — for an inline projection use <see cref="PropExtensions.Bind{TIn,TOut}"/>.
    /// </summary>
    public static Prop<T> Bind<T>(IReadable<T> source) => new(source);

    /// <summary>
    /// A prop whose source is resolved from the build <see cref="Context"/> at apply time — the form
    /// that lets a value depend on a service (theme, locale, …) while staying ctx-free to author.
    /// <paramref name="resolve"/> runs during the owning view's build (where ctx is in hand) and returns
    /// any other prop form. See GitBench's <c>Theme.Color(s =&gt; …)</c> for the canonical use.
    /// </summary>
    public static Prop<T> Deferred<T>(Func<Context, Prop<T>> resolve) => new(resolve);
}

public static class PropExtensions
{
    /// <summary>
    /// Projects an observable into a bindable prop: <c>Background = vm.IsDone.Bind(d =&gt; d ? A : B)</c>.
    /// The instance counterpart to <see cref="Prop.Bind{T}(System.Func{T})"/> — same verb, one source.
    /// Leak-free: the projecting <see cref="Derived{T}"/> is created when the view mounts and disposed
    /// when it unmounts, and it never touches <paramref name="source"/>'s lifetime. Prefer this over
    /// <see cref="ReadableExtensions.Select{TIn,TOut}"/> for inline binding: <c>Select</c> eagerly
    /// builds a standalone observable that you would have to own and dispose yourself.
    /// </summary>
    public static Prop<TOut> Bind<TIn, TOut>(this IReadable<TIn> source, Func<TIn, TOut> project) =>
        Prop.Bind(() => project(source.Value));
}
