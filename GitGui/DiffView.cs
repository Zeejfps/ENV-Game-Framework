using ZGF.Gui;

namespace GitGui;

/// <summary>
/// Diff panel shown below the file lists in Local Changes whenever exactly one file is
/// selected. Placeholder content for now — the actual hunk renderer is filled in later;
/// the selected path is surfaced so it's visible the wiring works.
/// </summary>
public sealed class DiffView : MultiChildView
{
    private readonly TextView _placeholder;

    public DiffView()
    {
        _placeholder = new TextView
        {
            Text = "Diff",
            TextColor = CommitsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            BorderColor = new BorderColorStyle { Top = CommitsPalette.Border },
            BorderSize = new BorderSizeStyle { Top = 1 },
            Children = { _placeholder },
        });
    }

    public void SetSelectedPath(string? path)
    {
        _placeholder.Text = path != null ? $"Diff for: {path}" : "Diff";
    }
}
