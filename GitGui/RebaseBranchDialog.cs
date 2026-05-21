using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class RebaseBranchDialog : MultiChildView, IRebaseBranchView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly DialogButton _rebaseButton;
    private readonly TextView _errorView;
    private readonly CheckboxView _autostashCheckbox;
    private readonly TextView _previewIcon;
    private readonly TextView _previewText;

    public event Action? RebaseRequested;

    public RebaseBranchDialog(RebaseBranchRequest request, Action onClose)
    {
        _onClose = onClose;

        var title = new TextView
        {
            Text = "Rebase",
            TextColor = DialogPalette.TitleText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var headerRow = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 28,
            Children =
            {
                new MultiChildView { PreferredWidth = CloseButtonSize },
                new FlexItem { Grow = 1, Child = title },
                new DialogCloseButton(onClose),
            },
        };

        var subtitle = new TextView
        {
            Text = "Copy commits from one branch to another",
            TextColor = DialogPalette.RowTextMissing,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var rebaseRow = BuildLabeledRow("Rebase:", BuildBranchChip(request.SourceBranch));
        var ontoRow = BuildLabeledRow("On:", BuildBranchChip(request.TargetDisplay));

        _autostashCheckbox = new CheckboxView("Stash and reapply local changes")
        {
            PreferredHeight = 24,
        };
        var autostashRow = BuildLabeledRow("", _autostashCheckbox);

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

        _errorView = new TextView
        {
            Text = string.Empty,
            TextColor = 0xFFE06C75,
            TextWrap = TextWrap.Wrap,
        };

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = 32, PreferredWidth = 96 };
        _rebaseButton = new DialogButton("Rebase", RaiseRebaseRequested) { PreferredHeight = 32, PreferredWidth = 96 };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new FlexItem { Grow = 1, Child = previewChip },
                cancelButton,
                _rebaseButton,
            },
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = BorderColorStyle.All(DialogPalette.Border),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(10),
            Padding = PaddingStyle.All(20),
            Children =
            {
                new FlexColumnView
                {
                    Gap = 12,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        headerRow,
                        subtitle,
                        new RectView
                        {
                            BackgroundColor = DialogPalette.Separator,
                            PreferredHeight = 1,
                        },
                        rebaseRow,
                        ontoRow,
                        autostashRow,
                        _errorView,
                        new MultiChildView { PreferredHeight = 4 },
                        buttonsRow,
                    },
                },
            },
        });

        this.UseController(_ => new DiscardChangesKbmController(RaiseRebaseRequested, onClose));

        this.UsePresenter(ctx => new RebaseBranchPresenter(
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

    private void RaiseRebaseRequested() => RebaseRequested?.Invoke();

    public bool Autostash => _autostashCheckbox.IsChecked.Value;

    public bool RebaseEnabled
    {
        set => _rebaseButton.IsEnabled.Value = value;
    }

    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    public RebasePreviewState PreviewState
    {
        set
        {
            switch (value)
            {
                case RebasePreviewState.Clean:
                    _previewIcon.Text = LucideIcons.CheckSquare;
                    _previewIcon.TextColor = 0xFF9DD17B;
                    _previewText.Text = "Rebase can be done without conflicts";
                    _previewText.TextColor = 0xFF9DD17B;
                    break;
                case RebasePreviewState.Conflicts:
                    _previewIcon.Text = LucideIcons.CloudOff;
                    _previewIcon.TextColor = 0xFFE6A85C;
                    _previewText.Text = "Rebase will produce conflicts";
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
