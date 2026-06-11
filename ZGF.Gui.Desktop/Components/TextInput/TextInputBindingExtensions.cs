using ZGF.Gui;
using ZGF.Observable;

namespace ZGF.Gui.Desktop.Components.TextInput;

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
            // Tie the subscriptions to the view's context lifecycle so they're disposed on
            // detach. Subscribing inline here would leak: `source` (often a longer-lived VM
            // observable) retains a closure capturing `input`, so a recreated input is never
            // collected while its source lives.
            input.Behaviors.Add(new TwoWayBindingBehavior(input, source, sink));
        }
    }

    private sealed class TwoWayBindingBehavior : IViewBehavior
    {
        private readonly TextInputView _input;
        private readonly IReadable<string> _source;
        private readonly Action<string> _sink;
        private bool _suppressWriteback;
        private IDisposable? _sourceSub;
        private IDisposable? _inputSub;

        public TwoWayBindingBehavior(TextInputView input, IReadable<string> source, Action<string> sink)
        {
            _input = input;
            _source = source;
            _sink = sink;
        }

        public void Attach(View view)
        {
            _sourceSub = _source.Subscribe(s =>
            {
                if (_input.Text.SequenceEqual(s.AsSpan())) return;
                _suppressWriteback = true;
                _input.SetText(s.AsSpan());
                _suppressWriteback = false;
            });
            _inputSub = _input.TextValue.Subscribe(s =>
            {
                if (_suppressWriteback) return;
                _sink(s);
            });
        }

        public void Detach(View view)
        {
            _sourceSub?.Dispose();
            _sourceSub = null;
            _inputSub?.Dispose();
            _inputSub = null;
        }
    }
}
