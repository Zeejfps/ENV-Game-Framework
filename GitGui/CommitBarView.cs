using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

/// <summary>
/// The bottom strip of the Local Changes view: commit title input, growing description
/// field, amend checkbox, commit button, and an inline error banner. <see cref="Bind"/>
/// wires the controls two-way to a <see cref="LocalChangesViewModel"/>; there are no
/// pass-through properties or events.
/// </summary>
internal sealed class CommitBarView : MultiChildView
{
    private const int Padding = 10;
    private const float CommitButtonWidth = 120f;
    private const float DescriptionMinHeight = 0f;
    private const float DescriptionMaxHeight = 240f;

    private readonly TextInputView _titleInput;
    private readonly GrowingDescriptionField _descriptionField;
    private readonly CheckboxView _amendCheckbox;
    private readonly DialogButton _commitButton;
    private readonly ColumnView _column;
    private readonly ErrorBar _errorBar;

    public CommitBarView()
    {
        _titleInput = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextWrap = TextWrap.NoWrap,
            PlaceholderText = "Commit title",
            PlaceholderTextColor = DialogPalette.RowTextMissing,
        };
        _titleInput.UseController(_ => new TextInputViewKbmController(_titleInput));

        // No PreferredHeight — let the box size to one line of text plus padding/border.
        // The input itself reports MeasureHeight = lineHeight (single line, NoWrap), and the
        // RectView adds its own padding+border on top.
        var titleBox = new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 4, Bottom = 4 },
            Children = { _titleInput },
        };

        _descriptionField = new GrowingDescriptionField(DescriptionMinHeight, DescriptionMaxHeight)
        {
            PlaceholderText = "Commit description",
        };

        _commitButton = new DialogButton("Commit", OnCommitClicked)
        {
            PreferredWidth = CommitButtonWidth,
            PreferredHeight = 28,
        };

        _amendCheckbox = new CheckboxView("Amend");

        var buttonRow = new FlexRowView
        {
            MainAxisAlignment = MainAxisAlignment.SpaceBetween,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { _amendCheckbox, _commitButton },
        };

        // Error bar is left out of the column until OpError adds it — that way the
        // column gap doesn't reserve space for an absent banner.
        _column = new ColumnView
        {
            Gap = 8,
            Children = { titleBox, _descriptionField, buttonRow },
        };
        _errorBar = new ErrorBar(_column, insertAt: 0);

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.HeaderBg,
            BorderColor = new BorderColorStyle { Top = CommitsPalette.Border },
            BorderSize = new BorderSizeStyle { Top = 1 },
            Padding = new PaddingStyle
            {
                Left = Padding,
                Right = Padding,
                Top = Padding,
                Bottom = Padding,
            },
            Children = { _column },
        });
    }

    private LocalChangesViewModel? _vm;

    public void Bind(LocalChangesViewModel vm)
    {
        _vm = vm;

        // Title: VM → input (with feedback guard) and input → VM. The State<string>
        // equality check stops the feedback loop on its own — writing the same string
        // back is a no-op — but checking the span first skips the input Clear+Enter churn.
        vm.Title.Subscribe(s =>
        {
            if (_titleInput.Text.SequenceEqual(s.AsSpan())) return;
            _titleInput.Clear();
            if (s.Length > 0) _titleInput.Enter(s.AsSpan());
        });
        _titleInput.TextChanged += () => vm.Title.Value = _titleInput.Text.ToString();

        vm.Description.Subscribe(s =>
        {
            if (_descriptionField.Text.SequenceEqual(s.AsSpan())) return;
            _descriptionField.SetText(s.AsSpan());
        });
        _descriptionField.TextChanged += () => vm.Description.Value = _descriptionField.Text.ToString();

        // Amend checkbox is two-way against vm.Amend; State.Value equality stops the loop.
        vm.Amend.Subscribe(b => _amendCheckbox.IsChecked.Value = b);
        _amendCheckbox.IsChecked.Changed += b => vm.Amend.Value = b;

        vm.CommitEnabled.Subscribe(b => _commitButton.IsEnabled.Value = b);
        vm.OpError.Subscribe(msg => _errorBar.Message = msg);
    }

    private void OnCommitClicked() => _vm?.Commit();
}
