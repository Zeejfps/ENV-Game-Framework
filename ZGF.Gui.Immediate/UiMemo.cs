namespace ZGF.Gui.Immediate;

public static class UiMemo
{
    private const int MemoSalt = 0x4D454D4F;

    private sealed class MemoState<TArgs>
    {
        public bool HasValue;
        public TArgs Args = default!;
        public string Value = "";
    }

    /// <summary>
    /// <c>format(args)</c>, re-run only when <paramref name="args"/> differ from the last call with
    /// this <paramref name="key"/> — so text formatted every frame allocates only when it changes.
    /// Pass a tuple of everything the text depends on (including the active <c>Strings</c> for
    /// localized text) and a <c>static</c> lambda.
    /// </summary>
    public static string Memo<TArgs>(this Ui ui, string key, TArgs args, Func<TArgs, string> format)
    {
        var state = ui.GetState<MemoState<TArgs>>(HashCode.Combine(ui.GetId(key), MemoSalt));
        if (state.HasValue && EqualityComparer<TArgs>.Default.Equals(state.Args, args))
            return state.Value;

        state.Args = args;
        state.Value = format(args);
        state.HasValue = true;
        return state.Value;
    }
}
