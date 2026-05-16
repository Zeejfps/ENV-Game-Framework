using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

public sealed class GroupSectionController : KeyboardMouseController
{
    private readonly Guid _groupId;
    private IDragController? _dragController;

    public GroupSectionController(Guid groupId)
    {
        _groupId = groupId;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _dragController = context.Get<IDragController>();
        _dragController?.RegisterGroupSection(view, _groupId);
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _dragController?.Unregister(view);
        _dragController = null;
    }
}
