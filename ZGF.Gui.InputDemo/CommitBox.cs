using ZGF.Gui.Desktop.Components.TextInput;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.InputDemo;

/// <summary>
/// A commit title and description, built the way GitBench's commit bar builds them: a
/// <see cref="TextInputView"/> paired with a <see cref="TextInputViewKbmController"/>. That pairing —
/// not a stand-in widget — is what the Cyrillic fix changed, so typing here exercises the real thing.
/// Tab moves between the two fields, since there's no mouse in a scripted run.
/// </summary>
internal sealed class CommitBox
{
    // Column doesn't stretch children across the cross axis: with no width they lay out zero-wide.
    private const float FieldWidth = 520f;

    public const int WindowWidth = (int)FieldWidth + 40;
    public const int WindowHeight = 300;

    private readonly TextInputView _title;
    private readonly TextInputView _description;
    private readonly TextInputViewKbmController _titleController;
    private readonly TextInputViewKbmController _descriptionController;

    public string Title => _title.Text.ToString();
    public string Description => _description.Text.ToString();

    public CommitBox(Context ctx)
    {
        var input = ctx.Require<InputSystem>();
        var clipboard = ctx.Get<IClipboard>();

        _title = NewField(ctx, TextWrap.NoWrap, "Summary of your changes");
        _description = NewField(ctx, TextWrap.Wrap, "Optional details");

        _titleController = new TextInputViewKbmController(_title, input, clipboard);
        _descriptionController = new TextInputViewKbmController(_description, input, clipboard)
        {
            IsMultiLine = true,
        };

        // A two-stop ring: Tab cycles forward, Shift+Tab back.
        _titleController.OnTab = () => Move(_titleController, _descriptionController);
        _titleController.OnShiftTab = () => Move(_titleController, _descriptionController);
        _descriptionController.OnTab = () => Move(_descriptionController, _titleController);
        _descriptionController.OnShiftTab = () => Move(_descriptionController, _titleController);

        _title.UseController(input, _titleController);
        _description.UseController(input, _descriptionController);
    }

    /// <summary>Puts the caret in the title, as if the user had just clicked into it.</summary>
    public void FocusTitle() => _titleController.BeginEditing();

    private static void Move(TextInputViewKbmController from, TextInputViewKbmController to)
    {
        from.EndEditing();
        to.BeginEditing();
    }

    public IWidget Build() => new Padding
    {
        Amount = PaddingStyle.All(20),
        Children =
        [
            new Column
            {
                Gap = 8,
                Children =
                [
                    Caption("Commit title"),
                    new Raw { View = _title },
                    Caption("Description"),
                    new Raw { View = _description },
                ],
            },
        ],
    };

    private static IWidget Caption(string text) => new Text
    {
        Value = text,
        FontSize = 12,
        Color = 0xFF9A9A9Au,
        Height = 14,
    };

    // No explicit height: the column measures each child and reserves that slot, but a Height set
    // from outside doesn't feed back into that measurement — the view then draws taller than its
    // slot and overlaps its neighbours, which silently steals their clicks.
    private static TextInputView NewField(Context ctx, TextWrap wrap, string placeholder) =>
        new(ctx.Canvas)
        {
            TextWrap = wrap,
            PlaceholderText = placeholder,
            Width = FieldWidth,
            FontSize = 15,
            BackgroundColor = 0xFF2A2A2Au,
            TextColor = 0xFFEDEDEDu,
            CaretColor = 0xFF4E9AF1u,
            SelectionRectColor = 0xFF2E4A6Bu,
            PlaceholderTextColor = 0xFF6A6A6Au,
        };
}
