using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Bindings;

public static class BindingExtensions
{
    extension(TextView view)
    {
        /// <summary>
        /// Binds <see cref="TextView.Text"/> to the source observable. Fires immediately
        /// with the current value, then on every change. Subscription is tied to the View's
        /// context lifecycle — bindings clean up automatically on detach.
        /// </summary>
        public void BindText(IReadable<string?> source)
        {
            view.Behaviors.Add(new PropertyBindingBehavior<TextView, string?, string?>(
                view, source, s => s, (v, s) => v.Text = s));
        }

        public void BindText<T>(IReadable<T> source, Func<T, string?> format)
        {
            view.Behaviors.Add(new PropertyBindingBehavior<TextView, T, string?>(
                view, source, format, (v, s) => v.Text = s));
        }

        /// <summary>
        /// Binds <see cref="TextView.Text"/> to a derived value defined by a compute
        /// function. The function's observable reads are auto-tracked.
        /// </summary>
        public void BindText(Func<string?> compute)
        {
            view.Behaviors.Add(new DerivedPropertyBindingBehavior<TextView, string?>(
                view, compute, (v, s) => v.Text = s));
        }

        /// <summary>
        /// Binds <see cref="TextView.TextColor"/> to the source observable.
        /// </summary>
        public void BindTextColor<T>(IReadable<T> source, Func<T, uint> project)
        {
            view.Behaviors.Add(new PropertyBindingBehavior<TextView, T, uint>(
                view, source, project, (v, c) => v.TextColor = c));
        }

        /// <summary>
        /// Binds <see cref="TextView.TextColor"/> to a derived value. The function's
        /// observable reads are auto-tracked.
        /// </summary>
        public void BindTextColor(Func<uint> compute)
        {
            view.Behaviors.Add(new DerivedPropertyBindingBehavior<TextView, uint>(
                view, compute, (v, c) => v.TextColor = c));
        }

        /// <summary>
        /// Binds <see cref="TextView.TextColor"/> to a value selected from the active
        /// <see cref="IThemeService{TStyles}"/> styles. Theme swaps and any observable reads
        /// inside <paramref name="select"/> are auto-tracked.
        /// </summary>
        public void BindThemedTextColor<TStyles>(IThemeService<TStyles> theme, Func<TStyles, uint> select)
        {
            view.Behaviors.Add(new ThemedDerivedPropertyBindingBehavior<TextView, TStyles, uint>(
                view, theme, select, (v, c) => v.TextColor = c));
        }

        /// <summary>
        /// Binds <see cref="TextView.Rotation"/> to the source observable.
        /// </summary>
        public void BindRotation(IReadable<float> source)
        {
            view.Behaviors.Add(new PropertyBindingBehavior<TextView, float, float>(
                view, source, r => r, (v, r) => v.Rotation = r));
        }
    }

    extension(View view)
    {
        /// <summary>
        /// Binds <see cref="View.IsVisible"/> to the source observable.
        /// </summary>
        public void BindIsVisible(IReadable<bool> source)
        {
            view.Behaviors.Add(new PropertyBindingBehavior<View, bool, bool>(
                view, source, b => b, (v, b) => v.IsVisible = b));
        }

        /// <summary>
        /// Binds <see cref="View.IsVisible"/> to a projected source value.
        /// </summary>
        public void BindIsVisible<T>(IReadable<T> source, Func<T, bool> project)
        {
            view.Behaviors.Add(new PropertyBindingBehavior<View, T, bool>(
                view, source, project, (v, b) => v.IsVisible = b));
        }

        /// <summary>
        /// Binds <see cref="View.IsVisible"/> to a derived value. The function's observable
        /// reads are auto-tracked.
        /// </summary>
        public void BindIsVisible(Func<bool> compute)
        {
            view.Behaviors.Add(new DerivedPropertyBindingBehavior<View, bool>(
                view, compute, (v, b) => v.IsVisible = b));
        }

        /// <summary>
        /// Runs an arbitrary side-effect whenever <paramref name="source"/> changes, tied to the
        /// view's context lifecycle (subscribes on attach, disposes on detach). The generalized
        /// escape hatch for reactions that aren't a single property assignment — list re-notify,
        /// scroll reveal, repaint — so a view never has to track subscriptions by hand.
        /// </summary>
        public void Bind<T>(IReadable<T> source, Action<T> apply)
        {
            view.Behaviors.Add(new PropertyBindingBehavior<View, T, T>(
                view, source, static x => x, (_, v) => apply(v)));
        }

        /// <summary>
        /// Subscribes to the active <see cref="IThemeService{TStyles}"/> for painted views
        /// that can't express their colors as discrete property bindings. The callback fires
        /// once when the view attaches to a context and again on every theme swap; typical
        /// use is to mutate cached <c>TextStyle</c>/colour fields and call <c>SetDirty()</c>.
        /// </summary>
        public void BindThemed<TStyles>(IThemeService<TStyles> theme, Action<TStyles> onChange)
        {
            view.Behaviors.Add(new ThemedDerivedPropertyBindingBehavior<View, TStyles, TStyles>(
                view, theme, s => s, (v, s) => onChange(s)));
        }
    }

    extension(RectView view)
    {
        /// <summary>
        /// Binds <see cref="RectView.BackgroundColor"/> to the source observable.
        /// </summary>
        public void BindBackgroundColor<T>(IReadable<T> source, Func<T, uint> project)
        {
            view.Behaviors.Add(new PropertyBindingBehavior<RectView, T, uint>(
                view, source, project, (v, c) => v.BackgroundColor = c));
        }

        /// <summary>
        /// Binds <see cref="RectView.BackgroundColor"/> to a derived value. The function's
        /// observable reads are auto-tracked.
        /// </summary>
        public void BindBackgroundColor(Func<uint> compute)
        {
            view.Behaviors.Add(new DerivedPropertyBindingBehavior<RectView, uint>(
                view, compute, (v, c) => v.BackgroundColor = c));
        }

        /// <summary>
        /// Binds <see cref="RectView.BackgroundColor"/> to a value selected from the active
        /// <see cref="IThemeService{TStyles}"/> styles. Theme swaps and any observable reads
        /// inside <paramref name="select"/> are auto-tracked.
        /// </summary>
        public void BindThemedBackgroundColor<TStyles>(IThemeService<TStyles> theme, Func<TStyles, uint> select)
        {
            view.Behaviors.Add(new ThemedDerivedPropertyBindingBehavior<RectView, TStyles, uint>(
                view, theme, select, (v, c) => v.BackgroundColor = c));
        }

        /// <summary>
        /// Binds <see cref="RectView.BorderColor"/> to the source observable.
        /// </summary>
        public void BindBorderColor<T>(IReadable<T> source, Func<T, BorderColorStyle> project)
        {
            view.Behaviors.Add(new PropertyBindingBehavior<RectView, T, BorderColorStyle>(
                view, source, project, (v, c) => v.BorderColor = c));
        }

        /// <summary>
        /// Binds <see cref="RectView.BorderColor"/> to a derived value. The function's
        /// observable reads are auto-tracked.
        /// </summary>
        public void BindBorderColor(Func<BorderColorStyle> compute)
        {
            view.Behaviors.Add(new DerivedPropertyBindingBehavior<RectView, BorderColorStyle>(
                view, compute, (v, c) => v.BorderColor = c));
        }

        /// <summary>
        /// Binds <see cref="RectView.BorderColor"/> to a value selected from the active
        /// <see cref="IThemeService{TStyles}"/> styles. Theme swaps and any observable reads
        /// inside <paramref name="select"/> are auto-tracked.
        /// </summary>
        public void BindThemedBorderColor<TStyles>(IThemeService<TStyles> theme, Func<TStyles, BorderColorStyle> select)
        {
            view.Behaviors.Add(new ThemedDerivedPropertyBindingBehavior<RectView, TStyles, BorderColorStyle>(
                view, theme, select, (v, c) => v.BorderColor = c));
        }
    }

    extension(View.ChildrenCollection children)
    {
        /// <summary>
        /// Mirrors the source list into this children collection. Add/Insert/Remove/Move/
        /// Replace/Clear events from the source produce matching mutations — no full diff.
        /// Subscription is tied to the owning view's mounted lifetime. Available only on
        /// views that expose their children publicly.
        /// </summary>
        public void BindChildren<TItem, TChild>(ObservableList<TItem> source,
            Func<TItem, TChild> create,
            Action<TChild, TItem>? onCreated = null,
            Action<TChild>? onRemoved = null)
            where TChild : View
        {
            children.Owner.Behaviors.Add(new ChildrenBindingBehavior<TItem, TChild>(
                children, source, create, onCreated, onRemoved));
        }

        /// <summary>
        /// Mirrors a derived list into this children collection. The compute function's
        /// observable reads are auto-tracked; when any dependency invalidates, the function
        /// re-runs and the children are reseeded.
        /// </summary>
        public void BindChildren<TItem, TChild>(Func<IEnumerable<TItem>> compute,
            Func<TItem, TChild> create)
            where TChild : View
        {
            children.Owner.Behaviors.Add(new DerivedChildrenBindingBehavior<TItem, TChild>(
                children, compute, create));
        }
    }
}
