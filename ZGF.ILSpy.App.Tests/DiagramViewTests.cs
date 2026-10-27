using ZGF.Desktop;
using ZGF.Gui.Testing;
using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Docking;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.App.Tests.BuiltInGlyphs;

namespace ZGF.ILSpy.App.Tests;

public sealed class DiagramViewTests
{
    private const string VehicleDiagram = "Vehicle diagram";
    private const double PollSeconds = 0.05;
    private const float Near = 0.5f;

    /// <summary>How far in from the diagram's left its toolbar draws its first icon.</summary>
    private const float ToolbarInset = 20f;

    /// <summary>An app that picked <paramref name="item"/> from the tree menu of the fixture
    /// <paramref name="type"/>, with the diagram it shows laid out.</summary>
    private static AppDriver PickFromTree(string type, string item)
    {
        var app = AppDriver.Opened();
        app.Ui.Tree.Reveal(app.Fixture(type));
        app.Run(2);
        app.PickFromTreeMenu(type, item);
        SettleDiagram(app);
        return app;
    }

    /// <summary>Runs frames until the active tab's diagram is laid out as it is now and its cards stopped moving.</summary>
    private static void SettleDiagram(AppDriver app)
    {
        app.RunUntil(() => IsLaidOut(app));
        app.Run();
    }

    private static bool IsLaidOut(AppDriver app)
    {
        if (app.AppState.IsDiagramming)
            return false;
        var tab = app.AppState.Tabs.Active;
        if (tab == null)
            return false;
        var diagram = tab.Diagram;
        if (diagram == null)
            return false;
        if (!app.Ui.Documents.Contains(tab))
            return false;
        var view = ViewOf(app, tab);
        if (view.LaidOutVersion != diagram.Version)
            return false;
        if (view.IsFitPending)
            return false;
        return !view.Transition.IsMoving;
    }

    private static DiagramViewState ViewOf(AppDriver app, DocumentTab tab) => app.Ui.Documents.ViewOf(tab).Diagram;

    private static List<DocumentTab> ListDiagramTabs(AppDriver app)
    {
        return app.AppState.Tabs
            .Where(static t => t.Diagram != null)
            .ToList();
    }

    private static DocumentTab DiagramTab(AppDriver app) => ListDiagramTabs(app).Single();

    /// <summary>The texts drawn below the diagram's toolbar and right of where it starts: the diagram's,
    /// not the tree's.</summary>
    private static List<(string Text, UiRect Rect)> ListDiagramTexts(AppDriver app)
    {
        var zoomOut = app.TextRect(BuiltInGlyphs.Icons.ResourceZoomOut);
        var left = zoomOut.X - ToolbarInset;
        return app.Frames.TextRects()
            .Where(t => t.Rect.X >= left)
            .Where(t => t.Rect.Y > zoomOut.Bottom)
            .ToList();
    }

    private static bool DrewOnDiagram(AppDriver app, string text) => ListDiagramTexts(app).Any(t => t.Text == text);

    /// <summary>Where <paramref name="text"/> was last drawn on the diagram.</summary>
    private static UiRect CardText(AppDriver app, string text) => ListDiagramTexts(app).Last(t => t.Text == text).Rect;

    private static void RightClick(AppDriver app, UiRect rect)
    {
        app.Frames.Click(rect.X + 2f, rect.Y + rect.Height / 2, PointerButton.Right).Frame();
    }

    private static void PickFromCardMenu(AppDriver app, string card, string item)
    {
        RightClick(app, CardText(app, card));
        app.ClickMenuItem(item).Frame();
        SettleDiagram(app);
    }

    private static PointF CenterOf(UiRect rect) => new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

    /// <summary>Clicks a little in from the left of the text drawn in <paramref name="rect"/>.</summary>
    private static void ClickAt(AppDriver app, UiRect rect)
    {
        app.Frames.Click(rect.X + 4f, rect.Y + rect.Height / 2).Frame();
    }

    [Fact]
    public void Show_in_diagram_from_the_tree_draws_the_type_and_its_relations_as_cards_and_links()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");

        Assert.True(app.Frames.Drew(VehicleDiagram));
        Assert.True(DrewOnDiagram(app, "Car"));
        Assert.True(DrewOnDiagram(app, "Bike"));
        Assert.True(DrewOnDiagram(app, "Garage"));
        Assert.True(DrewOnDiagram(app, "«abstract»"));
        Assert.True(DrewOnDiagram(app, "Wheels : int"));
        Assert.True(DrewOnDiagram(app, "Vehicles *"));
        Assert.True(app.Frames.Canvas.CubicBeziers.Count >= 4);
    }

    [Fact]
    public void Showing_another_type_adds_it_to_the_diagram_and_show_in_new_diagram_opens_another()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");
        app.Ui.Tree.Reveal(app.Fixture("Greeter"));
        app.Ui.Tree.Reveal(app.Fixture("Counter"));
        app.Run(2);

        app.PickFromTreeMenu("Greeter", "Show in diagram");
        SettleDiagram(app);
        Assert.Single(ListDiagramTabs(app));
        Assert.True(DrewOnDiagram(app, "Greeter"));

        app.PickFromTreeMenu("Counter", "Show in new diagram");
        SettleDiagram(app);
        Assert.Equal(2, ListDiagramTabs(app).Count);
        Assert.True(app.Frames.Drew("Counter diagram"));
    }

    [Fact]
    public void Clicking_a_card_s_name_or_a_member_opens_its_code_beside_the_diagram()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");

        ClickAt(app, CardText(app, "Bike"));
        app.Settle(2);
        Assert.Equal("Bike", app.AppState.Tabs.Active!.Node!.Text);

        app.Click(VehicleDiagram).Frame();
        ClickAt(app, CardText(app, "Mileage() : int"));
        app.Settle(2);
        Assert.Equal("Mileage() : int", app.AppState.Tabs.Active!.Node!.Text);
        Assert.NotNull(DiagramTab(app).Diagram);
    }

    [Fact]
    public void Links_saying_what_a_type_is_take_the_type_color_and_links_saying_what_it_has_the_data_color()
    {
        var app = PickFromTree("Car", "Show in diagram");
        PickFromCardMenu(app, "Vehicle", "Add the types its properties hold");
        var palette = app.Ui.NodeIconPalette;

        var colors = app.Frames.Canvas.CubicBeziers
            .Select(static b => b.Inputs.Color)
            .ToHashSet();

        Assert.Contains(palette.Type, colors);
        Assert.Contains(palette.Data, colors);
    }

    [Fact]
    public void Going_back_from_code_opened_on_a_diagram_returns_to_the_diagram_and_forward_to_the_code()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");
        var diagram = DiagramTab(app);
        ClickAt(app, CardText(app, "Mileage() : int"));
        app.Settle(2);
        var code = app.AppState.Tabs.Active!;
        Assert.NotEqual(diagram, code);

        GoBack(app.AppState);
        app.Settle(2);
        Assert.Equal(diagram, app.AppState.Tabs.Active);

        GoForward(app.AppState);
        app.Settle(2);
        Assert.Equal("Mileage() : int", app.AppState.Tabs.Active!.Node!.Text);
    }

    [Fact]
    public void Closing_a_diagram_drops_it_from_the_history()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");
        var diagram = DiagramTab(app);
        Assert.Contains(app.AppState.History.Entries, static e => e is HistoryEntry.Diagram);

        CloseTab(app.AppState, diagram);

        Assert.DoesNotContain(app.AppState.History.Entries, static e => e is HistoryEntry.Diagram);
    }

    [Fact]
    public void A_diagram_s_tab_shows_the_diagram_icon()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");

        Assert.True(app.Frames.Drew(Text(Icons.TabDiagram)));
    }

    [Fact]
    public void Hovering_a_member_underlines_it_with_a_hand_cursor()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");
        var member = CardText(app, "Mileage() : int");
        Assert.DoesNotContain(app.Frames.Canvas.Lines, l => IsUnderOf(app, l, member));

        app.Frames.MovePointer(CenterOf(member)).Frame();

        Assert.Equal(MouseCursor.Hand, app.Frames.Cursor);
        Assert.Contains(app.Frames.Canvas.Lines, l => IsUnderOf(app, l, member));
    }

    [Fact]
    public void Hovering_between_cards_shows_no_hand_cursor()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");
        var member = CardText(app, "Mileage() : int");

        app.Frames.MovePointer(member.X - 200f, member.Y).Frame();

        Assert.NotEqual(MouseCursor.Hand, app.Frames.Cursor);
    }

    /// <summary>Whether <paramref name="line"/> is a level rule starting at the left of the text drawn in
    /// <paramref name="text"/>, within its row.</summary>
    private static bool IsUnderOf(AppDriver app, RecordedLine line, UiRect text)
    {
        var start = line.Inputs.Start;
        var end = line.Inputs.End;
        var y = app.Frames.Height - start.Y;
        if (Math.Abs(start.Y - end.Y) > Near)
            return false;
        if (Math.Abs(start.X - text.X) > Near)
            return false;
        if (end.X - start.X < text.Height)
            return false;
        return y > text.Y && y <= text.Bottom;
    }

    [Fact]
    public void A_card_s_menu_adds_the_types_it_holds_while_the_card_stays_where_it_is()
    {
        var app = PickFromTree("Car", "Show in diagram");
        var vehicle = CardText(app, "Vehicle");
        Assert.False(DrewOnDiagram(app, "Vehicles *"));

        PickFromCardMenu(app, "Vehicle", "Add the types its properties hold");

        Assert.Contains(DiagramTab(app).Diagram!.Cards, static c => c.Name == "Garage");
        var moved = CardText(app, "Vehicle");
        Assert.Equal(vehicle.X, moved.X, Near);
        Assert.Equal(vehicle.Y, moved.Y, Near);
        app.Click("Fit to view").Frame();
        Assert.True(DrewOnDiagram(app, "Vehicles *"));
    }

    [Fact]
    public void A_card_s_menu_takes_it_off_the_diagram()
    {
        var app = PickFromTree("Car", "Show in diagram");
        Assert.True(DrewOnDiagram(app, "IDriven"));

        PickFromCardMenu(app, "IDriven", "Remove from diagram");

        Assert.False(DrewOnDiagram(app, "IDriven"));
        Assert.Equal(2, DiagramTab(app).Diagram!.Cards.Count);
    }

    [Fact]
    public void Clicking_a_part_of_a_card_folds_it_and_clicking_again_unfolds_it()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");
        var fields = CardText(app, "Fields (2)");

        app.Frames.Click(fields);
        SettleDiagram(app);
        Assert.False(DrewOnDiagram(app, "Plate : string"));

        app.Frames.Click(fields);
        SettleDiagram(app);
        Assert.True(DrewOnDiagram(app, "Plate : string"));
    }

    [Fact]
    public void Members_other_assemblies_cannot_reach_show_only_on_the_diagram_s_own_types_until_the_filter_shows_them()
    {
        var app = PickFromTree("Breakdown", "Show in diagram");
        PickFromCardMenu(app, "Breakdown", "Add base types");
        var tab = DiagramTab(app);
        var view = ViewOf(app, tab);
        var exception = tab.Diagram!.Cards[1];
        Assert.Equal("Exception", exception.Name);
        Assert.DoesNotContain(view.Faces.Find(exception.Type)!.Rows, IsHiddenMember);

        app.Click("Show private and internal members");
        SettleDiagram(app);

        Assert.Contains(view.Faces.Find(exception.Type)!.Rows, IsHiddenMember);
    }

    private static bool IsHiddenMember(CardRow row)
    {
        if (row.Kind != CardRowKind.Member)
            return false;
        return row.Accessibility is NodeAccessibility.Private or NodeAccessibility.Internal;
    }

    [Fact]
    public void Resting_on_a_member_shows_its_documentation()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");

        app.Frames.MovePointer(CardText(app, "Mileage() : int"));

        Poll.Until(() => app.Frames.Drew("How far the vehicle has gone."), () => app.Frames.Wait(PollSeconds).Frame());
    }

    [Fact]
    public void Dragging_pans_the_diagram_and_the_wheel_scrolls_it()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");
        var camera = ViewOf(app, DiagramTab(app)).Camera;
        var corner = camera.Corner;
        var from = CenterOf(CardText(app, "Garage"));

        app.Frames.Drag(from, new PointF(from.X + 20f, from.Y), new PointF(from.X + 40f, from.Y + 30f));
        app.Frames.Release().Frame();

        Assert.Equal(corner.X - 40f / camera.Zoom, camera.Corner.X, 0.01f);
        Assert.Equal(corner.Y - 30f / camera.Zoom, camera.Corner.Y, 0.01f);
        var panned = camera.Corner;
        app.Frames.Wheel(-1).Frame();
        Assert.True(camera.Corner.Y > panned.Y);
    }

    [Fact]
    public void The_zoom_buttons_zoom_and_fit_brings_the_whole_diagram_into_view()
    {
        var app = PickFromTree("Vehicle", "Show in diagram");
        var camera = ViewOf(app, DiagramTab(app)).Camera;
        var fitted = camera.Zoom;

        app.Click(BuiltInGlyphs.Icons.ResourceZoomIn).Frame();
        Assert.True(camera.Zoom > fitted);

        app.Click("Fit to view").Frame();
        Assert.Equal(fitted, camera.Zoom, 0.001f);
    }
}
