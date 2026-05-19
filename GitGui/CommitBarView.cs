using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;

namespace GitGui;

/// <summary>
/// The bottom strip of the Local Changes view: commit title input, growing description
/// field, amend checkbox, commit button, and an inline error banner. Exposes the inputs
/// as simple properties and surfaces user actions as events; the presenter does the work.
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

    public event Action? TitleChanged;
    public event Action? AmendToggled;
    public event Action? CommitClicked;

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
        _titleInput.TextChanged += () => TitleChanged?.Invoke();

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

        _commitButton = new DialogButton("Commit", () => CommitClicked?.Invoke())
        {
            PreferredWidth = CommitButtonWidth,
            PreferredHeight = 28,
        };

        _amendCheckbox = new CheckboxView("Amend");
        _amendCheckbox.IsChecked.Changed += _ => AmendToggled?.Invoke();

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

    public string TitleText
    {
        get => _titleInput.Text.ToString();
        set
        {
            _titleInput.Clear();
            if (value.Length > 0) _titleInput.Enter(value.AsSpan());
        }
    }

    public string DescriptionText
    {
        get => _descriptionField.Text.ToString();
        set => _descriptionField.SetText(value.AsSpan());
    }

    public bool AmendChecked
    {
        get => _amendCheckbox.IsChecked.Value;
        set => _amendCheckbox.IsChecked.Value = value;
    }

    public bool CommitEnabled { set => _commitButton.IsEnabled.Value = value; }
    public string? OpError { set => _errorBar.Message = value; }
}
