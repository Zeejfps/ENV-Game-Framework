using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

public static class BindToThemeExtension
{
    extension(View view)
    {
        /// <summary>
        /// Subscribes the view to <paramref name="service"/>.Tokens. The current value fires
        /// <paramref name="rebuild"/> immediately, and every theme swap fires it again. Used
        /// by canvas-draw views that can't express per-call colors through the style sheet —
        /// they keep a cached tokens snapshot and refresh it from this callback.
        /// </summary>
        public void BindToTheme(IThemeService service, Action<ThemeTokens> rebuild)
        {
            view.Behaviors.Add(new ThemeBindingBehavior(service, rebuild));
        }

        /// <summary>
        /// Context-resolving variant — resolves <see cref="IThemeService"/> from the view's
        /// <see cref="View.Context"/> on attach. Use this from static view helpers that build a
        /// subtree but don't have the service in scope at construction time. If no service is
        /// registered, the binding is silently inert.
        /// </summary>
        public void BindToTheme(Action<ThemeTokens> rebuild)
        {
            view.Behaviors.Add(new ContextThemeBindingBehavior(rebuild));
        }
    }

    extension(RectView view)
    {
        /// <summary>
        /// Background color binding that resolves the value from the current theme each tick.
        /// Wraps <c>BindToTheme(t =&gt; view.BackgroundColor = project(t))</c> — saves the
        /// boilerplate at the call site.
        /// </summary>
        public void BindBackgroundColorFromTheme(Func<ThemeTokens, uint> project)
        {
            view.BindToTheme(t => view.BackgroundColor = project(t));
        }

        public void BindBorderColorFromTheme(Func<ThemeTokens, BorderColorStyle> project)
        {
            view.BindToTheme(t => view.BorderColor = project(t));
        }
    }

    extension(TextView view)
    {
        public void BindTextColorFromTheme(Func<ThemeTokens, uint> project)
        {
            view.BindToTheme(t => view.TextColor = project(t));
        }
    }

    private sealed class ThemeBindingBehavior : IViewBehavior
    {
        private readonly IThemeService _service;
        private readonly Action<ThemeTokens> _rebuild;
        private IDisposable? _subscription;

        public ThemeBindingBehavior(IThemeService service, Action<ThemeTokens> rebuild)
        {
            _service = service;
            _rebuild = rebuild;
        }

        public void AttachToContext(View view, Context context)
        {
            _subscription = _service.Tokens.Subscribe(_rebuild);
        }

        public void DetachFromContext(View view, Context context)
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }

    private sealed class ContextThemeBindingBehavior : IViewBehavior
    {
        private readonly Action<ThemeTokens> _rebuild;
        private IDisposable? _subscription;

        public ContextThemeBindingBehavior(Action<ThemeTokens> rebuild)
        {
            _rebuild = rebuild;
        }

        public void AttachToContext(View view, Context context)
        {
            var service = context.Get<IThemeService>();
            if (service == null) return;
            _subscription = service.Tokens.Subscribe(_rebuild);
        }

        public void DetachFromContext(View view, Context context)
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
