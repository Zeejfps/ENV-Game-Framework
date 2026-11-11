namespace ZGF.Gui.Immediate;

/// <summary>The words the framework's own widgets show, such as a text field's menu, in English unless
/// the app's UI state gives them in its language. A new word is a property here with its English default,
/// so an app that sets some keeps building.</summary>
public sealed record UiWords
{
    public static readonly UiWords English = new();

    public string Cut { get; init; } = "Cut";

    public string Copy { get; init; } = "Copy";

    public string Paste { get; init; } = "Paste";

    public string SelectAll { get; init; } = "Select All";
}
