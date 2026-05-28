using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

// Modal shown when the user clicks Stash in the actions toolbar. Lets the user name the
// stash, pick the files to stash, and optionally keep the index (--keep-index) so staged
// hunks stay around after stashing. --include-untracked is derived from the row checks:
// passed iff any selected row is an untracked file.
public sealed class StashDialog : MultiChildView, IStashView
{
    private readonly Action _onClose;
    private readonly TextInputView _messageInput;
    private readonly CheckoutDialogKbmController _messageController;
    private readonly CheckboxView _keepStagedCheckbox;
    private readonly DialogButton _stashButton;
    private readonly TextView _errorView;
    private readonly ColumnView _fileListColumn;
    private readonly TextView _fileListHeader;
    private readonly TextView _fileListEmpty;
    private readonly List<FileRow> _rows = new();

    public event Action? StashRequested;
    public event Action? SelectionChanged;

    public StashDialog(Repo repo, Action onClose)
    {
        PreferredWidth = 520f;
        PreferredHeight = 520f;

        _onClose = onClose;

        var messageLabel = new TextView { Text = "Message" };
        messageLabel.BindThemedTextColor(s => s.DialogBody.SectionHeaderText);

        _messageInput = DialogFrame.TextInput();
        var messageBox = DialogFrame.WrapInput(_messageInput);

        _keepStagedCheckbox = new CheckboxView("Keep staged changes in index")
        {
            PreferredHeight = 22,
        };

        _fileListHeader = new TextView { Text = "Files" };
        _fileListHeader.BindThemedTextColor(s => s.DialogBody.SectionHeaderText);

        _fileListEmpty = new TextView
        {
            Text = "No local changes.",
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _fileListEmpty.BindThemedTextColor(s => s.FileChangesSection.EmptyPlaceholderText);

        _fileListColumn = new ColumnView { Gap = 1 };

        var scrollPane = new VerticalScrollPane();
        scrollPane.Children.Add(_fileListColumn);
        scrollPane.UseController(_ => new VerticalScrollPaneWheelController(scrollPane));

        var vScrollBar = ScrollBars.CreateVertical();

        var fileScrollHost = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(4),
            Children =
            {
                new BorderLayoutView
                {
                    Center = scrollPane,
                    East = vScrollBar,
                },
            },
        };
        fileScrollHost.BindThemedBackgroundColor(s => s.DialogFrame.InsetBackground);
        fileScrollHost.BindThemedBorderColor(s => BorderColorStyle.All(s.DialogFrame.Border));
        fileScrollHost.UsePresenter(_ => new VerticalScrollBarSyncController(scrollPane, vScrollBar));

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _stashButton = new DialogButton("Stash", RaiseStashRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build("Stash changes", onClose, new FlexColumnView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                messageLabel,
                messageBox,
                _fileListHeader,
                new FlexItem { Grow = 1, Child = fileScrollHost },
                _keepStagedCheckbox,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                DialogFrame.ButtonsRow(cancelButton, _stashButton),
            },
        }));

        // Same reason as CreateBranchDialog: text-input controllers consume clicks across
        // the view they're on, so attach to the input itself, not the outer dialog.
        _messageController = new CheckoutDialogKbmController(_messageInput, RaiseStashRequested, onClose);
        _messageInput.UseController(_ => _messageController);

        var request = new StashRequest(repo);
        this.UsePresenter(ctx => new StashPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>(),
            ctx.Require<LocalChangesSelectionStore>()));
    }

    private void RaiseStashRequested() => StashRequested?.Invoke();

    public string Message => new(_messageInput.Text);
    public bool KeepStaged => _keepStagedCheckbox.IsChecked.Value;

    public IReadOnlyList<string> SelectedPaths
    {
        get
        {
            var list = new List<string>(_rows.Count);
            foreach (var r in _rows)
                if (r.Checkbox.IsChecked.Value) list.Add(r.Row.Path);
            return list;
        }
    }

    public bool StashEnabled
    {
        set => _stashButton.IsEnabled.Value = value;
    }
    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }
    public event Action MessageChanged
    {
        add => _messageInput.TextChanged += value;
        remove => _messageInput.TextChanged -= value;
    }

    public void SetFiles(IReadOnlyList<StashFileRow> files, IReadOnlyList<string> preChecked)
    {
        _fileListColumn.Children.Clear();
        _rows.Clear();

        if (files.Count == 0)
        {
            _fileListColumn.Children.Add(_fileListEmpty);
            UpdateHeader(0, 0);
            return;
        }

        var preCheckedSet = new HashSet<string>(preChecked);
        foreach (var file in files)
        {
            var row = BuildRow(file, preCheckedSet.Contains(file.Path));
            _rows.Add(row);
            _fileListColumn.Children.Add(row.View);
        }

        UpdateHeader(SelectedCount(), _rows.Count);
    }

    public void FocusMessage()
    {
        _messageController.BeginEditing();
    }
    public void Close() => _onClose();

    private FileRow BuildRow(StashFileRow file, bool initialChecked)
    {
        var checkbox = new CheckboxView(string.Empty);
        checkbox.IsChecked.Value = initialChecked;

        var badge = FileChangesUI.CreateStatusBadge(file.Display);

        var pathText = new TextView
        {
            Text = FileChangeFormatting.FormatPath(file.Display),
            VerticalTextAlignment = TextAlignment.Center,
        };
        pathText.BindThemedTextColor(s => s.FileChangeRow.RowText);

        var rowContent = new FlexRowView
        {
            Gap = 8f,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 22,
            Children =
            {
                checkbox,
                badge,
                new FlexItem { Grow = 1, Child = pathText },
            },
        };

        var row = new FileRow(file, checkbox, rowContent);
        checkbox.IsChecked.Subscribe(_ =>
        {
            UpdateHeader(SelectedCount(), _rows.Count);
            SelectionChanged?.Invoke();
        });
        return row;
    }

    private int SelectedCount()
    {
        var n = 0;
        foreach (var r in _rows) if (r.Checkbox.IsChecked.Value) n++;
        return n;
    }

    private void UpdateHeader(int selected, int total)
    {
        _fileListHeader.Text = total == 0 ? "Files" : $"Files ({selected}/{total})";
    }

    private sealed record FileRow(StashFileRow Row, CheckboxView Checkbox, View View);
}
