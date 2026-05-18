using ZGF.Gui;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;

namespace GitGui;

public sealed class GroupRenameField : MultiChildView
{
    public GroupRenameField(Group group, IRepoRegistry registry)
    {
        PreferredHeight = 22;

        var input = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            FontSize = 18f,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextVerticalAlignment = TextAlignment.Center,
        };
        input.Enter(group.Name);
        input.SelectAll();

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorderHover),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 4, Right = 4 },
            Children = { input }
        });

        Behaviors.Add(new GroupRenameKbmController(input, group.Id, registry));
    }
}

internal sealed class GroupRenameKbmController : BaseTextInputKbmController
{
    private readonly TextInputView _input;
    private readonly Guid _groupId;
    private readonly IRepoRegistry _registry;
    private bool _finished;

    public GroupRenameKbmController(TextInputView input, Guid groupId, IRepoRegistry registry) : base(input)
    {
        _input = input;
        _groupId = groupId;
        _registry = registry;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _input.StartEditing();
        context.StealFocus(this);
    }

    protected override void OnKeyboardKeyPressed(ref KeyboardKeyEvent e)
    {
        if (_finished) return;

        if (e.Key == KeyboardKey.Enter || e.Key == KeyboardKey.NumpadEnter)
        {
            e.Consume();
            Commit();
            return;
        }
        if (e.Key == KeyboardKey.Escape)
        {
            e.Consume();
            Cancel();
            return;
        }
        base.OnKeyboardKeyPressed(ref e);
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (_finished) return;

        if (e.Phase == EventPhase.Bubbling
            && e.State == InputState.Pressed
            && e.Button == MouseButton.Left
            && _input.IsEditing
            && !_input.Position.ContainsPoint(e.Mouse.Point))
        {
            Commit();
            return;
        }
        base.OnMouseButtonStateChanged(ref e);
    }

    public override void OnFocusLost()
    {
        if (_finished) return;
        Commit();
    }

    private void Commit()
    {
        if (_finished) return;
        _finished = true;
        var newName = new string(_input.Text);
        _input.StopEditing();
        _registry.RenameGroup(_groupId, newName);
        _registry.EndRenameGroup();
    }

    private void Cancel()
    {
        if (_finished) return;
        _finished = true;
        _input.StopEditing();
        _registry.EndRenameGroup();
    }
}
