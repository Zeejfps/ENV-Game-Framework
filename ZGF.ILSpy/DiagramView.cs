using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Diagrams;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy;

/// <summary>A diagram's tab: the cards and the links between them, under the bar over the document, which
/// holds its zoom, fit and member filter.
/// Dragging pans, the wheel scrolls and with Ctrl zooms; a click selects a card or folds a part of it, a
/// double-click opens the code of the type or member under the pointer, and resting on one shows its
/// documentation. A card's menu grows the diagram along its relations or takes it off.</summary>
internal static class DiagramView
{
    private const string CanvasId = "diagram";
    private const string MenuId = "diagram-menu";
    private const float PanThreshold = 4f;
    private const float WheelZoomStep = 1.1f;

    public static void Render(Ui<UiState> ui, AppState appState, DocumentTab tab, TypeDiagram diagram)
    {
        var view = ui.State.Documents.ViewOf(tab).Diagram;
        using var pane = ui.Column(new Box { Height = Sizing.Grow(), Gap = 0f, Background = ui.Theme.PaneBackground });
        if (!ui.State.Display.ShowOmnibar)
            OwnToolbar(ui, tab);
        if (diagram.Cards.Count == 0)
        {
            EmptyHint(ui, diagram);
            return;
        }
        DiagramFaces.MeasureIfChanged(ui, view, diagram);
        Canvas(ui, appState, view, diagram);
        CardMenu(ui, appState, view, diagram);
    }

    /// <summary>The tools in a bar of the tab's own, while the bar over documents, where they sit otherwise, is hidden.</summary>
    private static void OwnToolbar(Ui<UiState> ui, DocumentTab tab)
    {
        using (ui.PaneToolbar())
            Tools(ui, tab);
        ui.Separator();
    }

    /// <summary>The diagram's tools, for the bar over the document: zoom, fit and the member filter.</summary>
    public static void Tools(Ui<UiState> ui, DocumentTab tab)
    {
        var t = ui.State.Strings;
        var icons = ui.State.Icons;
        var view = ui.State.Documents.ViewOf(tab).Diagram;
        var camera = view.Camera;
        var area = FindCanvasRect(ui);
        using (ui.Row(new Box { Gap = ui.Theme.Gap, AlignY = Align.Center }))
        {
            if (ui.IconButton("zoom-out", icons.ResourceZoomOut, t.ResourceZoomOut, camera.CanZoomOut))
                camera.ZoomOut(area);
            ui.Text(t.ResourceZoomPercent(camera.Percent), color: ui.Theme.TextMuted);
            if (ui.IconButton("zoom-in", icons.ResourceZoomIn, t.ResourceZoomIn, camera.CanZoomIn))
                camera.ZoomIn(area);
            if (ui.Button(t.DiagramFit, id: "fit"))
                FitToView(view, area);
            ui.Separator(ui.Theme.IconSize);
            ShowHiddenMembersCheckbox(ui, view);
        }
    }

    private static void ShowHiddenMembersCheckbox(Ui<UiState> ui, DiagramViewState view)
    {
        var shows = view.ShowsHiddenMembers;
        if (ui.Checkbox(ui.State.Strings.DiagramShowHiddenMembers, ref shows, id: "show-hidden-members"))
            view.ShowHiddenMembers(shows);
    }

    /// <summary>Where the cards were drawn on the last frame.</summary>
    private static UiRect FindCanvasRect(Ui<UiState> ui)
    {
        ui.TryGetRect(ui.GetId(CanvasId), out var rect);
        return rect;
    }

    private static void FitToView(DiagramViewState view, UiRect area)
    {
        var layout = view.Transition.Layout;
        if (layout == null)
            return;
        view.Camera.Fit(layout.Width, layout.Height, area);
    }

    private static void EmptyHint(Ui<UiState> ui, TypeDiagram diagram)
    {
        var t = ui.State.Strings;
        var text = diagram.IsBusy ? t.DiagramReading : t.DiagramEmpty;
        DocumentsView.Hint(ui, text);
    }

    private static void Canvas(Ui<UiState> ui, AppState appState, DiagramViewState view, TypeDiagram diagram)
    {
        var id = ui.GetId(CanvasId);
        var i = ui.Interact(id);
        var hasRect = ui.TryGetRect(id, out var rect);
        var selectedBefore = FindSelectedPlace(view);
        FollowLayout(ui, view, diagram);
        HoldSelectedCard(view, selectedBefore);
        if (hasRect)
            BringIntoView(view, diagram, rect);
        var hit = FindHit(ui, view, diagram, i, hasRect, rect);
        Scroll(ui, view, rect, i.Hovered);
        Press(view, i, hit);
        Pan(ui, view, i);
        SettleCamera(view, i);
        Click(ui, appState, view, diagram, i, hit);
        ShowHandOverClickable(ui, view, i, hit);
        FillFrame(ui, view, diagram, hit);
        using (ui.Box(id, new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Background = ui.Theme.EditorBackground, Clip = true }))
            ui.Paint(view.Frame, DiagramFrame.Paint);
        if (hasRect)
            MemberTooltip(ui, appState, view, diagram, id, IsPanning(view, i), hit);
    }

    /// <summary>Where the selected card is drawn now, before the layout moves on; null when none is.</summary>
    private static PointF? FindSelectedPlace(DiagramViewState view)
    {
        var selected = view.Selected;
        if (selected == null)
            return null;
        return view.Transition.PlaceOf(selected);
    }

    /// <summary>Moves toward the diagram's newest layout, drawing again until the move ends; a new layout
    /// is of the diagram as it was last measured.</summary>
    private static void FollowLayout(Ui<UiState> ui, DiagramViewState view, TypeDiagram diagram)
    {
        if (view.Transition.Follow(diagram.Layout))
            view.LaidOutVersion = view.MeasuredVersion;
        view.Transition.Advance(ui.DeltaTime);
        if (view.Transition.IsMoving)
            ui.RequestUpdate();
    }

    /// <summary>Moves the view along with the selected card, which was drawn at <paramref name="before"/>, so
    /// that it stays where it is on the screen while the others move around it.</summary>
    private static void HoldSelectedCard(DiagramViewState view, PointF? before)
    {
        if (before == null)
            return;
        var after = FindSelectedPlace(view);
        if (after == null)
            return;
        var corner = view.Camera.Corner;
        view.Camera.LookFrom(new PointF(corner.X + after.Value.X - before.Value.X, corner.Y + after.Value.Y - before.Value.Y));
    }

    /// <summary>Fits the first layout that places cards to <paramref name="rect"/>, then brings the card
    /// focused into it once a layout of the diagram as it is now places it.</summary>
    private static void BringIntoView(DiagramViewState view, TypeDiagram diagram, UiRect rect)
    {
        var layout = view.Transition.Layout;
        if (layout == null)
            return;
        FitFirstLayout(view, layout, rect);
        if (view.LaidOutVersion != diagram.Version)
            return;
        FocusPendingCard(view, layout, rect);
    }

    private static void FitFirstLayout(DiagramViewState view, TypeLayout layout, UiRect rect)
    {
        if (!view.IsFitPending)
            return;
        if (layout.Places.Count == 0)
            return;
        view.Camera.Fit(layout.Width, layout.Height, rect);
        view.IsFitPending = false;
    }

    private static void FocusPendingCard(DiagramViewState view, TypeLayout layout, UiRect rect)
    {
        var type = view.PendingFocus;
        if (type == null)
            return;
        view.PendingFocus = null;
        if (!layout.Places.TryGetValue(type, out var place))
            return;
        var face = view.Faces.Find(type);
        if (face == null)
            return;
        if (view.Camera.Shows(place, face.Width, face.Height, rect))
            return;
        view.Camera.CenterOn(new PointF(place.X + face.Width / 2, place.Y + face.Height / 2), rect);
    }

    /// <summary>The card and row under the pointer, while it is over the cards or holds a press on them.</summary>
    private static DiagramHit FindHit(Ui<UiState> ui, DiagramViewState view, TypeDiagram diagram, Interaction i, bool hasRect, UiRect rect)
    {
        if (!hasRect)
            return DiagramHit.Nothing;
        if (!i.Hovered && !i.Held)
            return DiagramHit.Nothing;
        var point = view.Camera.ToDiagram(ui.Input.Pointer, rect);
        return HitAt(view, diagram, point);
    }

    /// <summary>The topmost card drawn at <paramref name="point"/> of the diagram, and its row there.</summary>
    private static DiagramHit HitAt(DiagramViewState view, TypeDiagram diagram, PointF point)
    {
        for (var i = diagram.Cards.Count - 1; i >= 0; i--)
        {
            var hit = HitCard(view, diagram.Cards[i], point);
            if (hit.Face != null)
                return hit;
        }
        return DiagramHit.Nothing;
    }

    private static DiagramHit HitCard(DiagramViewState view, TypeCard card, PointF point)
    {
        var face = view.Faces.Find(card.Type);
        if (face == null)
            return DiagramHit.Nothing;
        var place = view.Transition.PlaceOf(card.Type);
        if (place == null)
            return DiagramHit.Nothing;
        if (!Covers(place.Value, face, point))
            return DiagramHit.Nothing;
        return new DiagramHit(face, face.RowAt(point.Y - place.Value.Y));
    }

    private static bool Covers(PointF place, CardFace face, PointF point)
    {
        var across = point.X >= place.X && point.X < place.X + face.Width;
        var down = point.Y >= place.Y && point.Y < place.Y + face.Height;
        return across && down;
    }

    /// <summary>The wheel scrolls the diagram while the pointer is over it, and zooms it about the pointer
    /// with Ctrl.</summary>
    private static void Scroll(Ui<UiState> ui, DiagramViewState view, UiRect rect, bool hovered)
    {
        if (!hovered)
            return;
        var input = ui.Input;
        if (!HasScrolled(input))
            return;
        if (IsCommandHeld(input.Modifiers))
        {
            view.Camera.ZoomAt(input.Pointer, rect, MathF.Pow(WheelZoomStep, input.ScrollY));
            return;
        }
        var step = ui.Theme.ScrollWheelStep;
        view.Camera.Pan(input.ScrollX * step, input.ScrollY * step);
    }

    private static bool HasScrolled(UiInput input)
    {
        if (input.ScrollY != 0f)
            return true;
        return input.ScrollX != 0f;
    }

    private static bool IsCommandHeld(KeyModifiers modifiers) => (modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;

    /// <summary>Keeps what a press landed on.</summary>
    private static void Press(DiagramViewState view, Interaction i, DiagramHit hit)
    {
        if (!i.Pressed)
            return;
        view.PressedOn = hit;
        view.PressMoved = false;
    }

    private static void OpenCode(AppState appState, TypeDiagram diagram, CardRow? row, bool inNewTab)
    {
        if (row == null)
            return;
        var target = row.Value.Target;
        if (target == null)
            return;
        OpenFromDiagram(appState, diagram, target, inNewTab);
    }

    /// <summary>A press dragged further than a click pans the diagram along with the pointer.</summary>
    private static void Pan(Ui<UiState> ui, DiagramViewState view, Interaction i)
    {
        if (!ui.TryGetDragStart(i, view.Camera.Corner, out var start))
            return;
        var pointer = ui.Input.Pointer;
        var dx = pointer.X - ui.PressPosition.X;
        var dy = pointer.Y - ui.PressPosition.Y;
        if (!view.PressMoved)
            view.PressMoved = dx * dx + dy * dy >= PanThreshold * PanThreshold;
        if (!view.PressMoved)
            return;
        var zoom = view.Camera.Zoom;
        view.Camera.LookFrom(new PointF(start.X - dx / zoom, start.Y - dy / zoom));
    }

    /// <summary>Counts the camera's move as done once no press drags it and the cards stopped moving it along.</summary>
    private static void SettleCamera(DiagramViewState view, Interaction i)
    {
        if (i.Held)
            return;
        if (view.Transition.IsMoving)
            return;
        view.Camera.FinishMoving();
    }

    private static bool IsPanning(DiagramViewState view, Interaction i)
    {
        if (!i.Held)
            return false;
        return view.PressMoved;
    }

    /// <summary>A right click opens the menu of the card under the pointer, a middle click opens its code in
    /// a new tab, and a left click that did not pan picks what it was pressed on and opens the code of the
    /// type or member there, in a new tab with Ctrl held.</summary>
    private static void Click(Ui<UiState> ui, AppState appState, DiagramViewState view, TypeDiagram diagram, Interaction i, DiagramHit hit)
    {
        if (i.ClickedWith(PointerButton.Right))
        {
            OpenMenu(ui, view, hit);
            return;
        }
        if (i.ClickedWith(PointerButton.Middle))
        {
            OpenCode(appState, diagram, hit.FindRow(), inNewTab: true);
            return;
        }
        if (!i.Clicked)
            return;
        if (view.PressMoved)
            return;
        Pick(view, view.PressedOn);
        OpenCode(appState, diagram, view.PressedOn.FindRow(), IsCommandHeld(ui.Input.Modifiers));
    }

    private static void OpenMenu(Ui<UiState> ui, DiagramViewState view, DiagramHit hit)
    {
        var face = hit.Face;
        if (face == null)
            return;
        view.Selected = face.Card.Type;
        view.MenuCard = face.Card;
        ui.OpenContextMenu(MenuId);
    }

    /// <summary>Selects the card <paramref name="hit"/> is on, or none off the cards, and folds or unfolds the
    /// part whose row it is on.</summary>
    private static void Pick(DiagramViewState view, DiagramHit hit)
    {
        var face = hit.Face;
        if (face == null)
        {
            view.Selected = null;
            return;
        }
        view.Selected = face.Card.Type;
        var row = hit.FindRow();
        if (row == null)
            return;
        var fold = row.Value.Fold;
        if (fold == null)
            return;
        view.Folds.Toggle(fold.Value);
    }

    /// <summary>A hand over a row a click acts on: one that opens code or folds a part; none while panning.</summary>
    private static void ShowHandOverClickable(Ui<UiState> ui, DiagramViewState view, Interaction i, DiagramHit hit)
    {
        if (IsPanning(view, i))
            return;
        var row = hit.FindRow();
        if (row == null)
            return;
        if (!IsClickable(row.Value))
            return;
        ui.SetCursor(MouseCursor.Hand);
    }

    private static bool IsClickable(CardRow row)
    {
        if (row.IsLink)
            return true;
        return row.Fold != null;
    }

    private static void FillFrame(Ui<UiState> ui, DiagramViewState view, TypeDiagram diagram, DiagramHit hit)
    {
        var frame = view.Frame;
        frame.Corner = view.Camera.Corner;
        frame.Zoom = view.Camera.Zoom;
        frame.Theme = ui.Theme;
        frame.Palette = ui.State.NodeIconPalette;
        frame.Icons = ui.State.Icons;
        FillLinks(ui, view, diagram, frame);
        FillCards(view, diagram, hit, frame);
    }

    /// <summary>The links of the layout drawn whose cards the diagram still shows, each as it is drawn now.</summary>
    private static void FillLinks(Ui<UiState> ui, DiagramViewState view, TypeDiagram diagram, DiagramFrame frame)
    {
        frame.Links.Clear();
        var layout = view.Transition.Layout;
        if (layout == null)
            return;
        foreach (var route in layout.Routes)
        {
            if (!ConnectsShownCards(diagram, route.Link))
                continue;
            frame.Links.Add(DrawnLinkOf(ui, view, route));
        }
    }

    private static bool ConnectsShownCards(TypeDiagram diagram, TypeLink link)
    {
        if (!diagram.Contains(link.From))
            return false;
        return diagram.Contains(link.To);
    }

    private static DrawnLink DrawnLinkOf(Ui<UiState> ui, DiagramViewState view, LinkRoute route)
    {
        var shape = view.Transition.Shape(route);
        var label = DiagramFaces.LabelOf(route.Link);
        if (label == null)
            return new DrawnLink(route.Link.Kind, shape, null, 0f, 0f);
        var size = DiagramFaces.MeasureLabel(ui, label);
        return new DrawnLink(route.Link.Kind, shape, label, size.Width, size.Height);
    }

    /// <summary>The cards the layout drawn places, each where it is drawn now.</summary>
    private static void FillCards(DiagramViewState view, TypeDiagram diagram, DiagramHit hit, DiagramFrame frame)
    {
        frame.Cards.Clear();
        foreach (var card in diagram.Cards)
        {
            var face = view.Faces.Find(card.Type);
            if (face == null)
                continue;
            var place = view.Transition.PlaceOf(card.Type);
            if (place == null)
                continue;
            var isSelected = card.Type == view.Selected;
            frame.Cards.Add(new DrawnCard(face, place.Value, isSelected, HoveredRowOf(face, hit)));
        }
    }

    /// <summary>The row of <paramref name="face"/> the pointer is on when it does something, else -1.</summary>
    private static int HoveredRowOf(CardFace face, DiagramHit hit)
    {
        if (hit.Face != face)
            return -1;
        var row = hit.FindRow();
        if (row == null)
            return -1;
        if (row.Value.Kind == CardRowKind.Stereotype)
            return -1;
        return hit.Row;
    }

    /// <summary>The declaration and documentation of the type or member under the pointer, once it has
    /// rested there, under the pointer; none while the diagram pans.</summary>
    private static void MemberTooltip(Ui<UiState> ui, AppState appState, DiagramViewState view, TypeDiagram diagram, int canvasId,
        bool isPanning, DiagramHit hit)
    {
        var tooltip = FindTooltip(appState, isPanning, hit);
        var part = PartOf(tooltip, hit);
        using var popup = ui.TooltipContentAtPointer(canvasId, part, out var shownPart, CodeTooltipView.Look);
        RememberTooltip(view, tooltip, part, shownPart);
        if (!popup.IsOpen)
            return;
        if (view.TooltipPart != shownPart)
            return;
        var click = CodeTooltipView.Render(ui, view.Tooltip!);
        FollowLink(ui, appState, diagram, click);
    }

    private static CodeTooltip? FindTooltip(AppState appState, bool isPanning, DiagramHit hit)
    {
        if (isPanning)
            return null;
        var row = hit.FindRow();
        if (row == null)
            return null;
        var target = row.Value.Target;
        if (target == null)
            return null;
        appState.Tooltips.TryGet(target, out var tooltip);
        return tooltip;
    }

    private static int PartOf(CodeTooltip? tooltip, DiagramHit hit)
    {
        if (tooltip == null)
            return 0;
        return HashCode.Combine(hit.Face!.Card.Type, hit.Row) | 1;
    }

    /// <summary>Where the row <paramref name="hit"/> is on is drawn, from its text on, widened to the left by
    /// the tooltip's padding so the tooltip's text starts where the row's does.</summary>
    /// <summary>Keeps the tooltip of the row under the pointer, unless another row's is still shown while the
    /// pointer crosses over to it.</summary>
    private static void RememberTooltip(DiagramViewState view, CodeTooltip? tooltip, int part, int shownPart)
    {
        if (tooltip == null)
            return;
        if (shownPart != 0 && shownPart != part)
            return;
        view.Tooltip = tooltip;
        view.TooltipPart = part;
    }

    /// <summary>A link clicked in the tooltip opens its code, in a new tab with a middle click or Ctrl, and
    /// closes the tooltip.</summary>
    private static void FollowLink(Ui<UiState> ui, AppState appState, TypeDiagram diagram, RichTextClick click)
    {
        if (click.Link is not CodeReference reference)
            return;
        ui.HideTooltip();
        var inNewTab = click.Button == PointerButton.Middle || IsCommandHeld(ui.Input.Modifiers);
        OpenFromDiagram(appState, diagram, reference, inNewTab);
    }

    private static void CardMenu(Ui<UiState> ui, AppState appState, DiagramViewState view, TypeDiagram diagram)
    {
        using var menu = ui.ContextMenu(MenuId);
        if (!menu.IsOpen)
            return;
        var card = view.MenuCard;
        if (card == null)
            return;
        if (!diagram.Contains(card.Type))
            return;
        CardMenuItems(ui, appState, view, diagram, card);
    }

    private static void CardMenuItems(Ui<UiState> ui, AppState appState, DiagramViewState view, TypeDiagram diagram, TypeCard card)
    {
        var t = ui.State.Strings;
        if (ui.MenuItem(t.TreeOpen, id: "open"))
            OpenFromDiagram(appState, diagram, card.Type, inNewTab: false);
        if (ui.MenuItem(t.TreeOpenInNewTab, id: "open-in-new-tab"))
            OpenFromDiagram(appState, diagram, card.Type, inNewTab: true);
        ui.MenuSeparator();
        GrowItems(ui, appState, diagram, card);
        ui.MenuSeparator();
        if (ui.MenuItem(t.DiagramRemove, id: "remove"))
            Remove(appState, view, diagram, card);
    }

    /// <summary>The items that add the types <paramref name="card"/> is related to, each offered when it has
    /// such relations, and enabled while the diagram lacks some of them.</summary>
    private static void GrowItems(Ui<UiState> ui, AppState appState, TypeDiagram diagram, TypeCard card)
    {
        var t = ui.State.Strings;
        if (HasLinks(card, IsBaseLink) && ui.MenuItem(t.DiagramAddBases, enabled: HasMissingBases(diagram, card), id: "add-bases"))
            ExpandBases(appState, diagram, card);
        if (HasLinks(card, IsInterfaceLink) && ui.MenuItem(t.DiagramAddInterfaces, enabled: HasMissingInterfaces(diagram, card), id: "add-interfaces"))
            ExpandInterfaces(appState, diagram, card);
        if (card.CanHaveDerived && ui.MenuItem(DerivedItemOf(ui, card), id: "add-derived"))
            ExpandDerived(appState, diagram, card);
        if (HasLinks(card, IsHeldLink) && ui.MenuItem(t.DiagramAddHeld, enabled: HasMissingHeld(diagram, card), id: "add-held"))
            ExpandHeld(appState, diagram, card);
    }

    private static bool HasLinks(TypeCard card, Func<TypeLink, bool> isKind) => card.Links.Any(isKind);

    /// <summary>Implementations for an interface, derived types for a class.</summary>
    private static string DerivedItemOf(Ui<UiState> ui, TypeCard card)
    {
        var t = ui.State.Strings;
        if (card.Stereotype == TypeStereotype.Interface)
            return t.DiagramAddImplementations;
        return t.DiagramAddDerived;
    }

    private static void Remove(AppState appState, DiagramViewState view, TypeDiagram diagram, TypeCard card)
    {
        RemoveFromDiagram(appState, diagram, card.Type);
        view.MenuCard = null;
        if (view.Selected == card.Type)
            view.Selected = null;
    }
}
