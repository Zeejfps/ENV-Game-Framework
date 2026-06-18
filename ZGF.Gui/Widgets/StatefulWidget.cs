namespace ZGF.Gui.Widgets;

/// <summary>
/// A widget that owns a long-lived mutable <typeparamref name="TState"/> — interaction state, a view
/// model, anything that must outlive the authoring record. <see cref="CreateState"/> builds it once,
/// <see cref="Build"/> composes the view tree with that state in hand, and the state is exposed via
/// <see cref="State"/> so a parent combinator (e.g. <c>WithController</c>) can reach the same instance
/// the visuals bind to.
/// <para>
/// This is the retained-mode analogue of Flutter's <c>StatefulWidget</c>/<c>State</c> split, with one
/// deliberate difference: <see cref="Build"/> runs <b>once</b>, not per frame — state changes
/// propagate through reactive bindings, not by re-running build. So the state is created once for the
/// view's whole life; it is not torn down and recreated across an unmount/remount cycle. (Reopening a
/// dialog builds a fresh widget, hence a fresh state; that is a new build, not a remount.)
/// </para>
/// </summary>
public abstract record StatefulWidget<TState> : Widget, IStatefulWidget<TState> where TState : class
{
    private TState? _state;

    /// <summary>The state for this build. Throws until <see cref="CreateView"/> has run, since
    /// <see cref="CreateState"/> needs the build context and so cannot run at construction.</summary>
    public TState State => _state ?? throw new InvalidOperationException(
        $"{GetType().Name}.State is not available until the widget has been built.");

    /// <summary>Builds the state object once, before the view tree. <paramref name="ctx"/> resolves
    /// any services or authoring props (e.g. <c>SomeProp.ToReadable(ctx)</c>) the state needs.</summary>
    protected abstract TState CreateState(Context ctx);

    /// <summary>Composes the view tree against <paramref name="state"/> — bind visuals to its
    /// observables, etc. Replaces <see cref="Widget.Build(Context)"/>.</summary>
    protected abstract IWidget Build(Context ctx, TState state);

    protected sealed override View CreateView(Context ctx)
    {
        var state = _state = CreateState(ctx);
        var view = Build(ctx, state).BuildView(ctx);
        if (state is IDisposable disposable)
            view.Use(() => disposable);
        return view;
    }
}
