using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Observable;

namespace GitGui;

public sealed class RepoRow : MultiChildView
{
    private readonly Repo _repo;
    private readonly TextView _label;

    public RepoRow(Repo repo, IRepoRegistry registry)
    {
        _repo = repo;
        PreferredHeight = 28;

        var isHovered = new State<bool>(false);

        _label = new TextView
        {
            Text = repo.DisplayName,
            HorizontalTextAlignment = TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _label.BindTextColor(() =>
        {
            if (repo.IsMissing) return DialogPalette.RowTextMissing;
            return registry.Active.Value?.Id == repo.Id
                ? DialogPalette.RowTextActive
                : DialogPalette.RowText;
        });

        var background = new RectView
        {
            BorderRadius = BorderRadiusStyle.All(4),
            Padding = new PaddingStyle { Left = 24, Right = 12 },
            Children = { _label }
        };
        background.BindBackgroundColor(() =>
        {
            var active = registry.Active.Value?.Id == repo.Id;
            return (isHovered.Value, active) switch
            {
                (_, true) => DialogPalette.RowActive,
                (true, false) => DialogPalette.RowHover,
                _ => DialogPalette.RowTransparent,
            };
        });
        AddChildToSelf(background);

        Behaviors.Add(new HoverableButtonController(
            () => registry.SetActive(repo.Id),
            h => isHovered.Value = h));
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
