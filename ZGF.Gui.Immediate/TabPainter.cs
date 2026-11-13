namespace ZGF.Gui.Immediate;

/// <summary>What a tab's behavior found this frame, for its painter to draw.</summary>
/// <param name="Id">The id the painter records the tab's element with, which makes it hit-testable.</param>
/// <param name="Selected">It is the selected tab of its strip.</param>
/// <param name="Hovered">The pointer is over the tab or its close button.</param>
/// <param name="FocusVisible">It holds keyboard focus while <see cref="Ui.FocusVisible"/>, so it draws a focus ring.</param>
/// <param name="Close">Its close button's behavior, which the painter records with <see cref="ButtonState.Id"/>; null
/// for a tab that can't be closed.</param>
public readonly record struct TabState(int Id, bool Selected, bool Hovered, bool FocusVisible, ButtonState? Close);

/// <summary>What a tab shows: <see cref="Label"/>, led by <see cref="Icon"/> when there is one, in the font
/// <see cref="Font"/> names, else the theme's. A selected tab that is not <see cref="Current"/> is marked in a muted
/// color instead of the accent.</summary>
public readonly record struct TabFace(string Label, bool Current = true, UiIcon? Icon = null, string? Font = null);

/// <summary>Records a tab: its element, hit-testable as <see cref="TabState.Id"/>, its close button, when it has one,
/// hit-testable as that button's id, and everything drawn in them.</summary>
public delegate void TabPainter(Ui ui, in TabState state, in TabFace face);
