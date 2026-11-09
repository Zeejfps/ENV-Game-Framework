namespace ZGF.Gui.Immediate.Testing;

/// <summary>A <see cref="Selector"/> named no node, more than one, or one a press would not reach;
/// <see cref="Result"/> says which. The message holds the snapshot it was looked for in.</summary>
public sealed class UiTargetException : Exception
{
    private UiTargetException(TargetResult result, string message, Exception? inner = null) : base(message, inner)
    {
        Result = result;
    }

    public TargetResult Result { get; }

    internal static UiTargetException For(TargetResult result, SemanticSnapshot snapshot) => new(result, Describe(result, snapshot));

    /// <summary>This failure with <paramref name="note"/> put first in its message.</summary>
    internal UiTargetException WithNote(string note) => new(Result, note + "\n" + Message, this);

    internal static string Describe(TargetResult result, SemanticSnapshot snapshot) =>
        TargetText.Problem(result) + "\n\n" + snapshot.ToText();
}
