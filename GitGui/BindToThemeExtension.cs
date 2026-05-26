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
}
