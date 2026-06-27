using ZGF.Gui.Desktop.Inspection;

namespace ZGF.Gui.Testing;

/// <summary>Raised by a harness assertion. The message carries the failure plus the full
/// <see cref="UiSnapshot"/> at the moment of failure, so the reason is visible without re-running.</summary>
public sealed class HarnessAssertionException(string message) : Exception(message);

/// <summary>Intent-level assertions over a <see cref="GuiTestHarness"/> — the checks an LLM reaches
/// for ("is this text on screen?", "is the search box focused?"). Each searches the current
/// <see cref="UiSnapshot"/> and, on failure, throws with that snapshot attached.</summary>
public static class HarnessAssertions
{
    /// <summary>Asserts some visible <c>TextView</c> shows <paramref name="text"/> (substring,
    /// case-insensitive, unless <paramref name="exact"/>). Text inside a hidden subtree does not count.</summary>
    public static void AssertVisibleText(this GuiTestHarness harness, string text, bool exact = false)
    {
        var snap = harness.Snapshot();
        if (!snap.Root.SelfAndDescendants().Any(n => n.Visible && Matches(n.Text, text, exact)))
            throw Fail($"Expected visible text \"{text}\", but it was not found.", snap);
    }

    /// <summary>Asserts no visible <c>TextView</c> shows <paramref name="text"/> (substring,
    /// case-insensitive) — for confirming a spinner/error/label has gone away.</summary>
    public static void AssertNotVisible(this GuiTestHarness harness, string text)
    {
        var snap = harness.Snapshot();
        if (snap.Root.SelfAndDescendants().Any(n => n.Visible && Matches(n.Text, text, exact: false)))
            throw Fail($"Expected no visible text \"{text}\", but it is on screen.", snap);
    }

    /// <summary>Asserts the view with this id or accessible label currently holds keyboard focus.</summary>
    public static void AssertFocused(this GuiTestHarness harness, string idOrLabel)
    {
        var snap = harness.Snapshot();
        var node = Find(snap, idOrLabel)
            ?? throw Fail($"No view with id or label \"{idOrLabel}\" to check focus.", snap);
        if (!node.States.Contains("focused"))
            throw Fail($"Expected \"{idOrLabel}\" to be focused.", snap);
    }

    /// <summary>Asserts the view with this id or accessible label carries the given accessibility
    /// state (pass a single flag, e.g. <see cref="AccessibilityStates.Selected"/>).</summary>
    public static void AssertState(this GuiTestHarness harness, string idOrLabel, AccessibilityStates state)
    {
        var snap = harness.Snapshot();
        var node = Find(snap, idOrLabel)
            ?? throw Fail($"No view with id or label \"{idOrLabel}\" to check state.", snap);
        var name = state.ToString().ToLowerInvariant();
        if (!node.States.Contains(name))
            throw Fail($"Expected \"{idOrLabel}\" to be {name}; states were [{string.Join(", ", node.States)}].", snap);
    }

    private static UiNode? Find(UiSnapshot snap, string idOrLabel) =>
        snap.Root.SelfAndDescendants().FirstOrDefault(n =>
            string.Equals(n.Id, idOrLabel, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(n.DisplayLabel, idOrLabel, StringComparison.OrdinalIgnoreCase));

    private static bool Matches(string? text, string query, bool exact) =>
        text != null && (exact
            ? string.Equals(text, query, StringComparison.OrdinalIgnoreCase)
            : text.Contains(query, StringComparison.OrdinalIgnoreCase));

    private static HarnessAssertionException Fail(string message, UiSnapshot snap) =>
        new(message + "\n--- snapshot ---\n" + snap.ToText());
}
