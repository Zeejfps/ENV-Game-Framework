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
    }

    extension(MultiChildView parent)
    {
        /// <summary>
        /// Binds the parent's <see cref="MultiChildView.Children"/> to the source list. Add/
        /// Insert/Remove/Move/Replace/Clear events from the source produce matching mutations
        /// on the parent's children — no full diff. Subscription is tied to the parent's
        /// context lifecycle.
        /// </summary>
        public void BindChildren<TItem, TChild>(ObservableList<TItem> source,
            Func<TItem, TChild> create,
            Action<TChild, TItem>? onCreated = null,
            Action<TChild>? onRemoved = null)
            where TChild : View
        {
            parent.Behaviors.Add(new ChildrenBindingBehavior<TItem, TChild>(
                parent, source, create, onCreated, onRemoved));
        }

        /// <summary>
        /// Binds the parent's <see cref="MultiChildView.Children"/> to a derived list. The
        /// compute function's observable reads are auto-tracked; when any dependency
        /// invalidates, the function re-runs and the children are reseeded.
        /// </summary>
        public void BindChildren<TItem, TChild>(Func<IEnumerable<TItem>> compute,
            Func<TItem, TChild> create)
            where TChild : View
        {
            parent.Behaviors.Add(new DerivedChildrenBindingBehavior<TItem, TChild>(
                parent, compute, create));
        }
    }
}
