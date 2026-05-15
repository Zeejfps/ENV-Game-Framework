using ZGF.Gui;

namespace GitGui;

public sealed class RepoRow : MultiChildView
{
    private readonly Repo _repo;
    private readonly TextView _label;

    public RepoRow(Repo repo, bool isActive)
    {
        _repo = repo;
        PreferredHeight = 28;

        var baseBg = isActive ? DialogPalette.RowActive : DialogPalette.RowTransparent;
        var hoverBg = isActive ? DialogPalette.RowActive : DialogPalette.RowHover;
        var textColor = repo.IsMissing
            ? DialogPalette.RowTextMissing
            : (isActive ? DialogPalette.RowTextActive : DialogPalette.RowText);

        _label = new TextView
        {
            Text = repo.DisplayName,
            TextColor = textColor,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var background = new RectView
        {
            BackgroundColor = baseBg,
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = 24, Right = 12 },
            Children = { _label }
        };
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            () => Context?.Get<IRepoRegistry>()?.SetActive(repo.Id),
            isHovered =>
            {
                background.BackgroundColor = isHovered ? hoverBg : baseBg;
            }));
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        _label.Text = TruncateToFit(_repo.DisplayName, context);
    }

    private static string TruncateToFit(string text, Context context)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var available = RepoBar.RowTextAvailableWidth;
        if (available <= 0)
            return text;

        if (Measure(text, context) <= available)
            return text;

        const string ellipsis = "…";
        var ellipsisWidth = Measure(ellipsis, context);
        if (ellipsisWidth > available)
            return ellipsis;

        var lo = 0;
        var hi = text.Length;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            if (Measure(text[..mid], context) + ellipsisWidth <= available)
                lo = mid;
            else
                hi = mid - 1;
        }
        return text[..lo] + ellipsis;
    }

    private static float Measure(string s, Context context)
    {
        var probe = new TextView { Text = s, Context = context };
        return probe.MeasureWidth();
    }
}