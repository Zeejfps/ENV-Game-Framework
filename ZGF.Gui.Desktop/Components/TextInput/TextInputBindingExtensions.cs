using ZGF.Observable;

namespace ZGF.Gui.Bindings;

public static class TextInputBindingExtensions
{
    extension(TextInputView input)
    {
        /// <summary>
        /// Two-way sync between the input and a string <see cref="State{T}"/>. Convenience
        /// overload of <see cref="BindTwoWay(IReadable{string}, System.Action{string})"/> for
        /// the common case where the view model exposes the field as mutable state.
        /// </summary>
        public void BindTwoWay(State<string> state)
            => input.BindTwoWay(state, value => state.Value = value);

        /// <summary>
        /// Two-way sync between the input and a view model: <paramref name="source"/> drives
        /// the input, and the user's edits are pushed back through <paramref name="sink"/>.
        /// This second form fits view models that expose text as a read-only observable plus
        /// a setter (e.g. an immutable-state-record VM with an <c>IReadable</c> slice and a
        /// <c>SetX</c> reducer), where there is no <see cref="State{T}"/> to hand out.
        ///
        /// A buffer-equality guard on the source side skips no-op rewrites (so echoing the
        /// user's own edit back doesn't disturb the caret), and a reentrancy guard suppresses
        /// the writeback while a programmatic <see cref="TextInputView.SetText"/> is in flight
        /// — so a wholesale source-driven replacement (async load, scheme switch) never feeds
        /// its own value back into the sink.
        /// </summary>
        public void BindTwoWay(IReadable<string> source, Action<string> sink)
        {
            var suppressWriteback = false;
            source.Subscribe(s =>
            {
                if (input.Text.SequenceEqual(s.AsSpan())) return;
                suppressWriteback = true;
                input.SetText(s.AsSpan());
                suppressWriteback = false;
            });
            input.TextValue.Subscribe(s =>
            {
                if (suppressWriteback) return;
                sink(s);
            });
        }
    }
}
