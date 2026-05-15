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
            view.Behaviors.Add(new TextBindingBehavior<string?>(source, s => s));
        }

        public void BindText<T>(IReadable<T> source, Func<T, string?> format)
        {
            view.Behaviors.Add(new TextBindingBehavior<T>(source, format));
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
        /// <param name="onCreated">Optional callback invoked after each child is created and
        /// inserted. Use for per-child setup that depends on the parent (e.g. setting flex
        /// styles on a flex parent).</param>
        /// <param name="onRemoved">Optional callback invoked when a child is removed. Use for
        /// per-child teardown (disposing per-item resources tied to the child View).</param>
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
        /// invalidates, the function re-runs and the children are reseeded. Use this for
        /// filtered/projected views where the source isn't directly an
        /// <see cref="ObservableList{T}"/>.
        /// </summary>
        public void BindChildren<TItem, TChild>(Func<IEnumerable<TItem>> compute,
            Func<TItem, TChild> create)
            where TChild : View
        {
            parent.Behaviors.Add(new ComputedChildrenBindingBehavior<TItem, TChild>(
                parent, compute, create));
        }
    }
}
