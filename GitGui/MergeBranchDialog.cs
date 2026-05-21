using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class MergeBranchDialog : MultiChildView, IMergeBranchView
{
    private readonly Action _onClose;
    private readonly DialogButton _mergeButton;
    private readonly TextView _errorView;
    private readonly MergeOptionDropdown _optionDropdown;
    private readonly TextView _previewIcon;
    private readonly TextView _previewText;

    public event Action? MergeRequested;

    public MergeBranchDialog(MergeBranchRequest request, Action onClose)
    {
        //PreferredWidth = 680f;
        _onClose = onClose;

        var mergeRow = BuildLabeledRow("Merge:", BuildBranchChip(request.SourceDisplay));
        var intoRow = BuildLabeledRow("Into:", BuildBranchChip(request.TargetBranch));

        _optionDropdown = new MergeOptionDropdown();
        var optionRow = BuildLabeledRow("Merge Option:", _optionDropdown);

        _previewIcon = new TextView
        {
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            Text = string.Empty,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _previewText = new TextView
        {
            Text = string.Empty,
            TextColor = DialogPalette.RowTextMissing,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var previewChip = new FlexRowView
        {
            Gap = 6,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { _previewIcon, _previewText },
        };

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight, PreferredWidth = 96 };
        _mergeButton = new DialogButton("Merge", RaiseMergeRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight, PreferredWidth = 96 };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new FlexItem { Grow = 1, Child = previewChip },
                cancelButton,
                _mergeButton,
            },
        };

        AddChildToSelf(DialogFrame.Build("Merge branch", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                mergeRow,
                intoRow,
                optionRow,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                buttonsRow,
            },
        }));

        this.UseController(_ => new DiscardChangesKbmController(RaiseMergeRequested, onClose));

        this.UsePresenter(ctx => new MergeBranchPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    private static FlexRowView BuildLabeledRow(string label, MultiChildView value)
    {
        var labelText = new TextView
        {
            Text = label,
            TextColor = DialogPalette.SectionHeaderText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var labelColumn = new FlexRowView
        {
            PreferredWidth = 110,
            MainAxisAlignment = MainAxisAlignment.End,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { labelText },
        };
        return new FlexRowView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 28,
            Children =
            {
                labelColumn,
                new FlexItem { Grow = 1, Child = value },
            },
        };
    }

    private static FlexRowView BuildBranchChip(string name)
    {
        var icon = new TextView
        {
            Text = LucideIcons.Branch,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            TextColor = DialogPalette.BodyText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var label = new TextView
        {
            Text = name,
            TextColor = DialogPalette.TitleText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        return new FlexRowView
        {
            Gap = 6,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { icon, label },
        };
    }

    private void RaiseMergeRequested() => MergeRequested?.Invoke();

    public MergeStrategy Strategy => _optionDropdown.Selected;

    public bool MergeEnabled
    {
        set => _mergeButton.IsEnabled.Value = value;
    }

    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    public MergePreviewState PreviewState
    {
        set
        {
            switch (value)
            {
                case MergePreviewState.Clean:
                    _previewIcon.Text = LucideIcons.CheckSquare;
                    _previewIcon.TextColor = 0xFF9DD17B;
                    _previewText.Text = "Merge can be done without conflicts";
                    _previewText.TextColor = 0xFF9DD17B;
                    break;
                case MergePreviewState.Conflicts:
                    _previewIcon.Text = LucideIcons.CloudOff;
                    _previewIcon.TextColor = 0xFFE6A85C;
                    _previewText.Text = "Merge will produce conflicts";
                    _previewText.TextColor = 0xFFE6A85C;
                    break;
                default:
                    _previewIcon.Text = string.Empty;
                    _previewText.Text = string.Empty;
                    break;
            }
        }
    }

    public void Close() => _onClose();
}

internal sealed class MergeOptionDropdown : HoverableButton
{
    private static readonly (MergeStrategy Strategy, string Label, string Detail)[] Options =
    {
        (MergeStrategy.Default, "Default", "Fast-forward if possible"),
        (MergeStrategy.NoFastForward, "Create merge commit", "Always create a merge commit"),
        (MergeStrategy.FastForwardOnly, "Fast-forward only", "Fail if not fast-forward"),
        (MergeStrategy.Squash, "Squash", "Stage changes for a new commit"),
    };

    private readonly TextView _labelView;
    private readonly TextView _detailView;
    public State<MergeStrategy> SelectedState { get; } = new(MergeStrategy.Default);

    public MergeStrategy Selected => SelectedState.Value;

    public MergeOptionDropdown()
    {
        PreferredHeight = 30;
        _labelView = new TextView
        {
            Text = LookupLabel(MergeStrategy.Default),
            TextColor = DialogPalette.TitleText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _detailView = new TextView
        {
            Text = LookupDetail(MergeStrategy.Default),
            TextColor = DialogPalette.RowTextMissing,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var chevron = new TextView
        {
            Text = LucideIcons.ChevronDown,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 12,
            TextColor = DialogPalette.RowText,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };

        var row = new FlexRowView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                _labelView,
                new FlexItem { Grow = 1, Child = _detailView },
                chevron,
            },
        };

        var background = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 8, Right = 8, Top = 4, Bottom = 4 },
            Children = { row },
        };
        DialogPalette.BindBorderedButtonChrome(background, IsHovered);
        SetBackground(background);

        SelectedState.Subscribe(s =>
        {
            _labelView.Text = LookupLabel(s);
            _detailView.Text = LookupDetail(s);
        });
    }

    protected override void OnClicked()
    {
        var ctx = Context;
        if (ctx == null) return;
        var items = new List<RepoBarContextMenu.Item>(Options.Length);
        foreach (var opt in Options)
        {
            var strategy = opt.Strategy;
            items.Add(new RepoBarContextMenu.Item(
                $"{opt.Label} — {opt.Detail}",
                () => SelectedState.Value = strategy));
        }
        RepoBarContextMenu.Show(ctx, Position.BottomLeft, items);
    }

    private static string LookupLabel(MergeStrategy s)
    {
        foreach (var o in Options) if (o.Strategy == s) return o.Label;
        return string.Empty;
    }

    private static string LookupDetail(MergeStrategy s)
    {
        foreach (var o in Options) if (o.Strategy == s) return o.Detail;
        return string.Empty;
    }
}
