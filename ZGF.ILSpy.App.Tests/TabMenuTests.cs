using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.DocumentPanels;

namespace ZGF.ILSpy.App.Tests;

public sealed class TabMenuTests
{
    private static readonly string[] Types = ["Greeter", "Farewell", "Outline"];

    /// <summary>The fixture assembly's tab, then a tab for each of <see cref="Types"/> beside it, in one group.</summary>
    private static (AppDriver App, BrowseNode Assembly) OpenTabs()
    {
        var app = AppDriver.Opened(width: 1600f);
        app.Settle(2);
        var assembly = app.AppState.Tabs.Active!.Node!;
        foreach (var type in Types)
            app.Show(app.Fixture(type), 2, inNewTab: true);
        app.Settle(2);
        return (app, assembly);
    }

    private static List<string> TitlesInGroup(AppDriver app)
    {
        var tab = app.AppState.Tabs.Active!;
        return ListTabsInGroup(app.Ui, tab)
            .Select(t => NodeText.Name(t.Node!))
            .ToList();
    }

    private static void PickFromTabMenu(AppDriver app, string title, string item)
    {
        var tab = app.LastTextRect(title);
        app.Frames.Click(tab.X + 4f, tab.Y + tab.Height / 2, PointerButton.Right).Frame();
        app.ClickMenuItem(item).Frame(2);
        app.Settle(2);
    }

    [Fact]
    public void Closing_to_the_right_then_the_others_leaves_only_the_tab_right_clicked()
    {
        var (app, assembly) = OpenTabs();
        Assert.Equal([NodeText.Name(assembly), .. Types], TitlesInGroup(app));

        PickFromTabMenu(app, "Greeter", "Close all to the right");

        Assert.Equal([NodeText.Name(assembly), "Greeter"], TitlesInGroup(app));
        Assert.Equal(2, app.AppState.Tabs.Count);

        PickFromTabMenu(app, "Greeter", "Close others");

        Assert.Equal(["Greeter"], TitlesInGroup(app));
        Assert.Single(app.AppState.Tabs);
    }

    [Fact]
    public void Closing_to_the_left_keeps_the_tab_right_clicked_and_those_after_it()
    {
        var (app, _) = OpenTabs();

        PickFromTabMenu(app, "Farewell", "Close all to the left");

        Assert.Equal(["Farewell", "Outline"], TitlesInGroup(app));
    }

    [Fact]
    public void Closing_all_closes_the_documents_of_the_group_and_leaves_the_other_panels_docked_there()
    {
        var (app, _) = OpenTabs();
        var tab = app.AppState.Tabs.Active!;
        var group = app.Ui.Layout.GroupOf(app.Ui.Documents.PanelOf(tab))!;
        group.Panels.Add(DockPanels.Bookmarks);
        app.Run(2);

        CloseTabsInGroup(app.Ui, tab);
        app.Settle(2);

        Assert.Empty(app.AppState.Tabs);
        Assert.Same(group, app.Ui.Layout.GroupOf(DockPanels.Bookmarks));
    }

    [Fact]
    public void The_menu_offers_nothing_to_close_beside_a_tab_alone_in_its_group()
    {
        var app = AppDriver.Opened(width: 1600f);
        app.Settle(2);
        var title = NodeText.Name(app.AppState.Tabs.Active!.Node!);
        var tab = app.LastTextRect(title);

        app.Frames.Click(tab.X + 4f, tab.Y + tab.Height / 2, PointerButton.Right).Frame();

        Assert.True(app.Frames.Drew("Close others"));
        Assert.True(app.Frames.Drew("Ctrl+W"));
        app.ClickMenuItem("Close others").Frame(2);
        Assert.Single(app.AppState.Tabs);
    }
}
