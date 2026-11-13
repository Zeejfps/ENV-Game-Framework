namespace ZGF.Gui.Immediate.Widgets;

/// <summary>
/// The header of one group of a <see cref="UiWidgets.DockSpace"/>, as its <see cref="DockHeaderPainter"/> records it.
/// The painter lays the header out and calls the parts it shows where it shows them. Each part takes its input as it
/// is called, so a part the painter leaves out takes none: no Tab stop, no drag, no menu. The parts act on the
/// group's front panel, <see cref="Panel"/>. A group of a panel that is neither movable nor alone gets no header.
/// </summary>
public readonly struct DockHeader
{
    private readonly DockWalker _walker;
    private readonly DockGroup _group;
    private readonly int _elementId;
    private readonly int _stripId;

    internal DockHeader(DockWalker walker, DockGroup group, int elementId, int stripId, DockTab tab, bool floating)
    {
        _walker = walker;
        _group = group;
        _elementId = elementId;
        _stripId = stripId;
        Tab = tab;
        Floating = floating;
    }

    /// <summary>The group's front panel, which the grip drags and the menu and close buttons act on.</summary>
    public string Panel => _group.Active ?? _group.Panels[0];

    /// <summary>How <see cref="Panel"/>'s tab is named.</summary>
    public DockTab Tab { get; }

    /// <summary>How many panels the group holds; more than one is shown by <see cref="Tabs"/>.</summary>
    public int PanelCount => _group.Panels.Count;

    /// <summary>The group is in a floating window.</summary>
    public bool Floating { get; }

    /// <summary>An id for a part of this group's header, distinct from every other group's.</summary>
    public int IdOf(string part) => HashCode.Combine(_elementId, part);

    /// <summary>Records the group's tab strip, one tab per panel, as the stock header does: picking, closing and
    /// dragging a tab, its context menu (<see cref="DockSpace.TabMenu"/>), and dropping a dragged panel among the
    /// tabs. Without it, a panel dragged onto this group docks beside or into it, not between tabs.</summary>
    public void Tabs(Box box = default) => _walker.HeaderTabs(_group, _elementId, _stripId, box);

    /// <summary>The behavior of a handle to drag <see cref="Panel"/> by, recorded by the painter with
    /// <paramref name="id"/>: dragging it moves the panel as dragging its tab does; right-clicking it asks for its
    /// menu, and double-clicking it is <see cref="DockSpace.TabDoubleClicked"/>. Inert for a panel that can't move.
    /// It takes no focus.</summary>
    public Interaction Grip(int id) => _walker.HeaderGrip(_group, _elementId, id);

    /// <summary>The behavior of a button that opens <see cref="Panel"/>'s menu below it
    /// (<see cref="DockSpace.TabMenu"/>), recorded by the painter with <see cref="ButtonState.Id"/>; it is read by
    /// assistive tools as <paramref name="label"/>.</summary>
    public ButtonState MenuButton(int id, string label) => _walker.HeaderMenuButton(_group, id, label);

    /// <summary>The behavior of a button that closes <see cref="Panel"/>, recorded by the painter with
    /// <see cref="ButtonState.Id"/>, as with <see cref="UiWidgets.PaintCloseButton"/>; null for a panel that can't
    /// close.</summary>
    public ButtonState? CloseButton(int id) => _walker.HeaderCloseButton(_group, id);
}

/// <summary>Records a dock group's header, calling the <see cref="DockHeader"/> parts it shows. The stock header,
/// <see cref="UiWidgets.PaintDockHeader"/>, is the group's tabs alone.</summary>
public delegate void DockHeaderPainter(Ui ui, in DockHeader header);

public static partial class UiWidgets
{
    /// <summary>The stock header of a dock group: its <see cref="DockHeader.Tabs"/>, as a strip across the group's
    /// top — for a painter of an app's own to fall back to.</summary>
    public static void PaintDockHeader(this Ui ui, in DockHeader header)
    {
        header.Tabs();
    }
}
