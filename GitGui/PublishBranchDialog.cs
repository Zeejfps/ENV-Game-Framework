using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Observable;

namespace GitGui;

public sealed class PublishBranchDialog : MultiChildView, IPublishBranchView
{
    private readonly Action _onClose;
    private readonly DialogButton _publishButton;
    private readonly TextView _errorView;
    private readonly CheckboxView _trackCheckbox;
    private readonly RemoteDropdown _remoteDropdown;

    public event Action? PublishRequested;

    public PublishBranchDialog(PublishBranchRequest request, Action onClose)
    {
        _onClose = onClose;

        var subtitle = new TextView
        {
            Text = "First push — choose a remote and set the upstream",
            TextColor = DialogPalette.RowTextMissing,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var branchRow = BuildLabeledRow("Branch:", BuildBranchChip(request.LocalBranch));

        _remoteDropdown = new RemoteDropdown();
        var remoteRow = BuildLabeledRow("To:", _remoteDropdown);

        _trackCheckbox = new CheckboxView("Track this remote branch (set upstream)")
        {
            PreferredHeight = 24,
            IsChecked = { Value = true },
        };
        var trackRow = BuildLabeledRow("", _trackCheckbox);

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight, PreferredWidth = 96 };
        _publishButton = new DialogButton("Publish", RaisePublishRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight, PreferredWidth = 96 };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new FlexItem { Grow = 1, Child = new MultiChildView() },
                cancelButton,
                _publishButton,
            },
        };

        AddChildToSelf(DialogFrame.Build("Publish branch", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                subtitle,
                branchRow,
                remoteRow,
                trackRow,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                buttonsRow,
            },
        }));

        this.UseController(_ => new DiscardChangesKbmController(RaisePublishRequested, onClose));

        this.UsePresenter(ctx => new PublishBranchPresenter(
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

    private void RaisePublishRequested() => PublishRequested?.Invoke();

    public string SelectedRemote => _remoteDropdown.Selected;
    public bool SetUpstream => _trackCheckbox.IsChecked.Value;

    public bool PublishEnabled
    {
        set => _publishButton.IsEnabled.Value = value;
    }

    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    public void SetRemotes(IReadOnlyList<string> remotes)
    {
        _remoteDropdown.SetOptions(remotes);
    }

    public void Close() => _onClose();
}

internal sealed class RemoteDropdown : HoverableButton
{
    private readonly TextView _labelView;
    private readonly TextView _chevron;
    private IReadOnlyList<string> _options = Array.Empty<string>();

    public State<string> SelectedState { get; } = new(string.Empty);
    public string Selected => SelectedState.Value;

    public RemoteDropdown()
    {
        PreferredHeight = 30;

        var icon = new TextView
        {
            Text = LucideIcons.Branch,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            TextColor = DialogPalette.BodyText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _labelView = new TextView
        {
            Text = "(no remotes)",
            TextColor = DialogPalette.RowTextMissing,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _chevron = new TextView
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
            Gap = 6,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                icon,
                new FlexItem { Grow = 1, Child = _labelView },
                _chevron,
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
            _labelView.Text = string.IsNullOrEmpty(s) ? "(no remotes)" : s;
            _labelView.TextColor = string.IsNullOrEmpty(s)
                ? DialogPalette.RowTextMissing
                : DialogPalette.TitleText;
        });
    }

    public void SetOptions(IReadOnlyList<string> options)
    {
        _options = options;
        if (options.Count == 0)
        {
            SelectedState.Value = string.Empty;
            IsEnabled.Value = false;
            _chevron.Text = string.Empty;
            return;
        }
        IsEnabled.Value = true;
        _chevron.Text = options.Count > 1 ? LucideIcons.ChevronDown : string.Empty;
        var preferred = options.FirstOrDefault(o => o == "origin") ?? options[0];
        SelectedState.Value = preferred;
    }

    protected override void OnClicked()
    {
        if (_options.Count <= 1) return;
        var ctx = Context;
        if (ctx == null) return;
        var items = new List<RepoBarContextMenu.Item>(_options.Count);
        foreach (var opt in _options)
        {
            var captured = opt;
            items.Add(new RepoBarContextMenu.Item(
                captured,
                () => SelectedState.Value = captured));
        }
        RepoBarContextMenu.Show(ctx, Position.BottomLeft, items);
    }
}
