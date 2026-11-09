using ZGF.Gui.Immediate.Testing;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class SemanticSnapshotTests
{
    [Fact]
    public void Paths_follow_named_and_keyed_scopes_and_skip_numbered_ones()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Id("toolbar"))
            using (ui.Id(5))
                ui.Button("Save");
            using (ui.KeyedId("row-3"))
            using (ui.Id("actions"))
                ui.Button("Delete");
        });

        var paths = frames.Snapshot().Nodes.Where(n => n.Role == SemanticRole.Button).Select(n => n.Path?.ToString()).ToList();

        Assert.Equal(["toolbar/Save", "#row-3/actions/Delete"], paths);
    }

    [Fact]
    public void A_snapshot_line_shows_a_path_as_the_literal_a_selector_takes()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.KeyedId("1"))
                ui.Button("row");
            using (ui.Id("a/b"))
                ui.Button("Go");
        });

        var snapshot = frames.Snapshot();
        var text = snapshot.ToText();

        Assert.Contains("button path=\"#1/row\" \"row\"", text);
        Assert.Contains("button path=\"a\\\\/b/Go\" \"Go\"", text);
        var found = Assert.IsType<TargetResult.Found>(snapshot.Resolve(Selector.AtPath("a\\/b/Go")));
        Assert.Equal("Go", found.Node.Label);
        Assert.Equal("path=\"#1/row\"", Selector.AtPath("#1/row").ToString());
    }

    [Fact]
    public void A_name_with_a_slash_is_one_segment_and_never_reads_as_two()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Row())
            {
                ui.Button("one", id: "a/b");
                using (ui.Id("a"))
                    ui.Button("two", id: "b");
            }
        });

        Assert.Equal("one", frames.Find(Selector.AtPath(@"a\/b")).Label);
        Assert.Equal("two", frames.Find(Selector.AtPath("a/b")).Label);
        Assert.Equal([new TestSegment.Named("a/b")], TestPath.Parse(@"a\/b").Segments);
        Assert.Equal(@"a\/b/\#c/#k\\", new TestPath([new TestSegment.Named("a/b"), new TestSegment.Named("#c"), new TestSegment.Keyed(@"k\")]).ToString());
        Assert.Equal(TestPath.Parse(@"a\/b/\#c/#k\\"), new TestPath([new TestSegment.Named("a/b"), new TestSegment.Named("#c"), new TestSegment.Keyed(@"k\")]));
    }

    [Fact]
    public void Rows_keep_their_keyed_paths_when_they_are_reordered()
    {
        var rows = new List<string> { "alpha", "beta", "gamma" };
        var deleted = new List<string>();
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            foreach (var row in rows)
            {
                using (ui.KeyedId(row))
                using (ui.Row())
                {
                    ui.Label(row);
                    if (ui.Button("Delete"))
                        deleted.Add(row);
                }
            }
        });

        frames.ClickOn(Selector.AtPath("#beta/Delete"));
        rows.Reverse();
        frames.ClickOn(Selector.AtPath("#beta/Delete"));
        frames.ClickOn(Selector.AtPath("#alpha/Delete"));

        Assert.Equal(["beta", "beta", "alpha"], deleted);
    }

    [Fact]
    public void Labels_that_repeat_are_ambiguous_and_every_match_is_listed()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            for (var i = 0; i < 2; i++)
            {
                using (ui.Id(i))
                    ui.Button("Delete");
            }
        });

        var error = Assert.Throws<UiTargetException>(() => frames.ClickOn(Selector.OfRole(SemanticRole.Button, "Delete")));

        var ambiguous = Assert.IsType<TargetResult.Ambiguous>(error.Result);
        Assert.Equal(2, ambiguous.Matches.Count);
        Assert.Contains("2 nodes match button \"Delete\"", error.Message);
    }

    [Fact]
    public void A_selector_that_matches_nothing_names_the_nearest_and_shows_the_snapshot()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Button("Save"));

        var error = Assert.Throws<UiTargetException>(() => frames.ClickOn(Selector.AtPath("Sav")));

        var notFound = Assert.IsType<TargetResult.NotFound>(error.Result);
        Assert.Equal("Save", notFound.Nearest[0].Path!.ToString());
        Assert.Contains("# frame", error.Message);
    }

    [Fact]
    public void A_button_under_a_popup_is_reported_occluded_and_not_clicked()
    {
        var clicks = 0;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            if (ui.Button("under"))
                clicks++;
            using (ui.Popup(ui.GetId("cover"), new UiRect(0f, 0f, 0f, 0f), new Box { Width = 200f, Height = 100f, Background = 0xFF000000 }))
                ui.Label("covering");
        });

        var error = Assert.Throws<UiTargetException>(() => frames.ClickOn(Selector.AtPath("under")));

        var occluded = Assert.IsType<TargetResult.Occluded>(error.Result);
        Assert.Equal(SemanticRole.Popup, occluded.By!.Role);
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void A_button_behind_a_modal_is_reported_blocked_and_what_is_in_the_modal_is_not()
    {
        var clicks = 0;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            ui.Button("behind");
            using (ui.Modal(ui.GetId("modal"), new Box { AlignX = Align.Center, AlignY = Align.Center }))
            {
                if (ui.Button("OK"))
                    clicks++;
            }
        });

        var error = Assert.Throws<UiTargetException>(() => frames.ClickOn(Selector.AtPath("behind")));
        frames.ClickOn(Selector.AtPath("OK"));

        Assert.Equal(SemanticRole.Modal, Assert.IsType<TargetResult.Blocked>(error.Result).Modal.Role);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Rows_past_a_scroll_are_scrolled_out_and_rows_across_its_edge_are_clipped_but_reachable()
    {
        var clicked = -1;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.Scroll("list", new Box { Height = Sizing.Fixed(70f), Gap = 0f }))
            {
                for (var i = 0; i < 10; i++)
                {
                    if (ui.Button($"Row {i}"))
                        clicked = i;
                }
            }
        });
        frames.Frame();

        var snapshot = frames.Snapshot();
        var visibility = snapshot.Nodes.Where(n => n.Role == SemanticRole.Button).Select(n => n.Visibility).ToList();
        var error = Assert.Throws<UiTargetException>(() => frames.ClickOn(Selector.OfRole(SemanticRole.Button, "Row 9")));
        frames.ClickOn(Selector.WithText("Row 2"));

        Assert.Equal(Visibility.Visible, visibility[0]);
        Assert.Equal(Visibility.Clipped, visibility[2]);
        Assert.Equal(Visibility.ScrolledOut, visibility[9]);
        Assert.IsType<TargetResult.Clipped>(error.Result);
        Assert.Equal(2, clicked);
    }

    [Fact]
    public void Tabs_are_found_by_their_id_or_label_and_so_are_the_tabs_their_list_shows()
    {
        var selected = "one";
        var frames = new UiFrames(new DefaultUiState(), width: 260f, body: ui =>
        {
            using var strip = ui.TabStrip("tabs");
            foreach (var (label, id) in new[] { ("One", "one"), ("Two", "two"), ("Three", "three"), ("Four", "four") })
            {
                if (strip.Tab(label, selected == id, closable: false, id: id).Selected)
                    selected = id;
            }
        });
        frames.Frame(2);

        frames.ClickOn(Selector.AtPath("two"));
        var tab = frames.Find(Selector.AtPath("two"));
        frames.ClickOn(Selector.AtPath("tabs/tab-list"));
        var listed = frames.Find(Selector.AtPath("tabs/tab-list/four"));

        Assert.Equal("two", selected);
        Assert.Equal(SemanticRole.Tab, tab.Role);
        Assert.True(tab.Has(SemanticStates.Selected));
        Assert.Equal((SemanticRole.MenuItem, "Four"), (listed.Role, listed.Label));
    }

    private sealed record Node(string Name, params Node[] Children);

    private sealed class Model : ITreeModel<Node>
    {
        public int KeyCalls;
        public bool HasChildren(Node node) => node.Children.Length > 0;
        public TreeChildren<Node> GetChildren(Node node) => TreeChildren<Node>.Of(node.Children);
        public TreeRow Row(Node node) => new(node.Name);

        public string KeyOf(Node node)
        {
            KeyCalls++;
            return node.Name;
        }
    }

    [Fact]
    public void Tree_rows_are_found_by_their_keys_under_the_tree()
    {
        var leaf = new Node("leaf");
        var root = new Node("root", leaf);
        var state = new TreeViewState<Node>();
        state.SetExpanded(root, true);
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.TreeView("tree", [root], new Model(), state));

        frames.ClickOn(Selector.AtPath("tree/#root/#leaf"));
        var row = frames.Find(Selector.AtPath("tree/#root"));
        var clicked = frames.Snapshot().Resolve(Selector.AtPath("tree/#root/#leaf"));

        Assert.Same(leaf, state.Selected);
        Assert.Equal(SemanticRole.TreeItem, row.Role);
        Assert.True(row.Has(SemanticStates.Expanded));
        Assert.True(Assert.IsType<TargetResult.Found>(clicked).Node.Has(SemanticStates.Selected));
    }

    [Fact]
    public void Tree_rows_with_the_same_key_under_different_parents_have_different_paths()
    {
        var first = new Node("file");
        var second = new Node("file");
        Node[] roots = [new Node("dir1", first), new Node("dir2", second)];
        var state = new TreeViewState<Node>(ReferenceEqualityComparer.Instance);
        foreach (var root in roots)
            state.SetExpanded(root, true);
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.TreeView("tree", roots, new Model(), state));

        frames.ClickOn(Selector.AtPath("tree/#dir2/#file"));

        Assert.Same(second, state.Selected);
    }

    [Fact]
    public void Frames_nobody_inspects_never_ask_the_model_for_keys()
    {
        var root = new Node("root", new Node("leaf"));
        var state = new TreeViewState<Node>();
        state.SetExpanded(root, true);
        var model = new Model();
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.TreeView("tree", [root], model, state));

        frames.Frame(3);
        var before = model.KeyCalls;
        frames.Snapshot();

        Assert.Equal(0, before);
        Assert.NotEqual(0, model.KeyCalls);
    }

    [Fact]
    public void After_a_click_the_snapshot_shows_what_the_app_now_holds()
    {
        var on = false;
        var lit = false;
        var view = "list";
        var selection = new ListSelection();
        var frames = new UiFrames(new DefaultUiState(), width: 400f, height: 300f, body: ui =>
        {
            ui.Checkbox("Sync", ref on);
            ui.Switch("lights", ref lit);
            ui.SegmentedControl("view", ref view, [new Segment<string>("list", "List"), new Segment<string>("grid", "Grid")]);
            using var list = ui.SelectableList(ui.GetId("rows"), 3, selection, box: new Box { Height = Sizing.Fixed(100f) });
            for (var i = list.First; i < list.End; i++)
            {
                using (list.Row(i, ui.GetId($"row{i}")))
                    ui.Label($"Row {i}");
            }
        });

        frames.ClickOn(Selector.AtPath("Sync"));
        var sync = frames.Snapshot().Resolve(Selector.AtPath("Sync"));
        frames.ClickOn(Selector.AtPath("lights"));
        var lights = frames.Snapshot().Resolve(Selector.AtPath("lights"));
        frames.ClickOn(Selector.AtPath("view/Grid"));
        var grid = frames.Snapshot().Resolve(Selector.AtPath("view/Grid"));
        frames.ClickOn(Selector.AtPath("row2"));
        var rows = frames.Snapshot().Nodes.Where(n => n.Role == SemanticRole.ListItem).Select(n => n.Has(SemanticStates.Selected)).ToList();

        Assert.True(on && lit && view == "grid" && selection.Index == 2);
        Assert.True(Assert.IsType<TargetResult.Found>(sync).Node.Has(SemanticStates.Checked));
        Assert.True(Assert.IsType<TargetResult.Found>(lights).Node.Has(SemanticStates.Checked));
        Assert.True(Assert.IsType<TargetResult.Found>(grid).Node.Has(SemanticStates.Selected));
        Assert.Equal([false, false, true], rows);
    }

    [Fact]
    public void Text_nothing_takes_the_pointer_for_and_a_tooltip_are_unreachable()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            ui.Label("plain");
            using (ui.Popup(ui.GetId("tip"), new UiRect(100f, 100f, 0f, 0f), passThrough: true))
                ui.Label("tip text");
        });

        Assert.IsType<TargetResult.Unreachable>(frames.Target(Selector.WithText("plain")));
        Assert.IsType<TargetResult.Unreachable>(frames.Target(Selector.WithText("tip text")));
    }

    [Fact]
    public void A_button_covered_but_for_a_narrow_edge_is_clicked_on_that_edge()
    {
        var clicks = 0;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            if (ui.Button("edge", width: Sizing.Fixed(100f)))
                clicks++;
            using (ui.Popup(ui.GetId("cover"), new UiRect(0f, 0f, 0f, 0f), new Box { Width = 95f, Height = 100f }))
            {
            }
        });

        var target = Assert.IsType<TargetResult.Hittable>(frames.Target(Selector.AtPath("edge")));
        frames.ClickOn(Selector.AtPath("edge"));

        Assert.True(target.At.X >= 95f);
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Every_exposed_piece_counts_however_many_regions_cut_the_target()
    {
        var target = new SemanticNode
        {
            Path = TestPath.Parse("target"),
            Role = SemanticRole.Button,
            Bounds = new UiRect(0f, 0f, 1000f, 10f),
            VisibleBounds = new UiRect(0f, 0f, 1000f, 10f),
            HitId = 1,
            Layer = -1,
        };
        var hits = new List<HitRegion> { new(1, target.Bounds, 0, 0) };
        for (var i = 0; i < 257; i++)
            hits.Add(new HitRegion(2 + i, new UiRect(2f + 3f * i, 0f, 1f, 10f), 0, hits.Count));
        hits.Add(new HitRegion(1000, new UiRect(0f, 0f, 771f, 10f), 0, hits.Count));
        var snapshot = new SemanticSnapshot(1, target, [], [], hits.ToArray(), [], new Dictionary<int, SemanticNode>(), pointerLocked: false);

        var hittable = Assert.IsType<TargetResult.Hittable>(snapshot.Target(Selector.AtPath("target")));

        Assert.True(hittable.At.X >= 771f);
    }

    [Fact]
    public void Overflowing_tabs_with_one_label_in_different_scopes_keep_their_scopes_in_the_list()
    {
        var frames = new UiFrames(new DefaultUiState(), width: 200f, body: ui =>
        {
            using var strip = ui.TabStrip("tabs");
            foreach (var doc in new[] { "doc-a", "doc-b", "doc-c" })
            {
                using (ui.Id(doc))
                    strip.Tab("README", doc == "doc-a", closable: false);
            }
        });
        frames.Frame(2);

        frames.ClickOn(Selector.AtPath("tabs/tab-list"));
        var a = frames.Find(Selector.AtPath("tabs/tab-list/doc-a/README"));
        var b = frames.Find(Selector.AtPath("tabs/tab-list/doc-b/README"));

        Assert.NotSame(a, b);
        Assert.Equal("doc-b/README", frames.Find(Selector.AtPath("doc-b/README")).Path!.ToString());
    }

    [Fact]
    public void A_wait_for_a_node_that_is_there_while_frames_never_settle_times_out_with_the_snapshot()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            ui.Button("Here");
            ui.RequestUpdate();
        });

        var error = Assert.Throws<TimeoutException>(() => frames.WaitFor(Selector.AtPath("Here"), TimeSpan.FromMilliseconds(50)));

        Assert.Contains("never became ready", error.Message);
        Assert.Contains("# frame", error.Message);
    }

    [Fact]
    public void A_wait_for_a_node_that_is_there_while_it_animates_finds_it_and_reports_the_animation()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            ui.Button("Here");
            ui.RequestAnimationFrame();
        });

        var node = frames.WaitFor(Selector.AtPath("Here"), TimeSpan.FromSeconds(10));

        Assert.Equal("Here", node.Label);
        Assert.Equal(FramePending.Animation, frames.Animating & FramePending.Animation);
    }

    [Fact]
    public void Every_path_reads_back_as_written()
    {
        string[] names = ["", "/", "#", "\\", "\\e", "e", "a/b", "#a", "a#", "\\/", "//", "üé😀", " ", "\\\\#/"];
        var random = new Random(4);
        for (var round = 0; round < 500; round++)
        {
            var segments = new List<TestSegment>();
            var count = random.Next(1, 5);
            for (var i = 0; i < count; i++)
            {
                var name = names[random.Next(names.Length)] + names[random.Next(names.Length)];
                segments.Add(random.Next(2) == 0 ? new TestSegment.Named(name) : new TestSegment.Keyed(name));
            }
            var path = new TestPath(segments);

            Assert.Equal(path, TestPath.Parse(path.ToString()));
        }
        Assert.NotEqual(new TestPath([new TestSegment.Named("")]).ToString(), new TestPath([new TestSegment.Keyed("")]).ToString());
    }

    [Fact]
    public void Every_shown_path_is_a_one_line_string_literal_that_decodes_to_the_path()
    {
        string[] names = ["a", "\"", "\\", "/", "#", "\n", "\r", "\t", "\0", "\u0085", "\u2028", "\u2029", "\u001b", "\u007f", " ", "é"];
        var random = new Random(7);
        for (var round = 0; round < 500; round++)
        {
            var segments = new List<TestSegment>();
            var count = random.Next(1, 4);
            for (var i = 0; i < count; i++)
            {
                var name = names[random.Next(names.Length)] + names[random.Next(names.Length)];
                segments.Add(random.Next(2) == 0 ? new TestSegment.Named(name) : new TestSegment.Keyed(name));
            }
            var path = new TestPath(segments);

            var shown = Selector.AtPath(path.ToString()).ToString();

            Assert.StartsWith("path=\"", shown);
            Assert.EndsWith("\"", shown);
            var literal = shown["path=\"".Length..^1];
            Assert.DoesNotContain(literal, c => char.IsControl(c) || c is '\u2028' or '\u2029');
            Assert.Equal(path, TestPath.Parse(System.Text.RegularExpressions.Regex.Unescape(literal)));
        }
    }

    [Fact]
    public void Menu_items_are_found_by_the_labels_that_lead_to_them()
    {
        var picked = "";
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using (ui.MenuBar("menu"))
            {
                using var file = ui.Menu("File");
                if (!file.IsOpen) return;
                if (ui.MenuItem("Open"))
                    picked = "open";
                using var recent = ui.SubMenu("Recent");
                if (recent.IsOpen && ui.MenuItem("notes.md"))
                    picked = "notes";
            }
        });

        frames.ClickOn(Selector.AtPath("File"));
        frames.MoveTo(Selector.AtPath("File/Recent"));
        frames.ClickOn(Selector.AtPath("File/Recent/notes.md"));

        Assert.Equal("notes", picked);
    }

    [Fact]
    public void A_wait_for_a_selector_returns_once_its_node_is_there()
    {
        var shown = false;
        Ui? last = null;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            last = ui;
            if (shown)
                ui.Button("Later");
        });
        frames.Frame();
        _ = Task.Run(async () =>
        {
            await Task.Delay(20);
            last!.Post(() => shown = true);
        });

        var node = frames.WaitFor(Selector.AtPath("Later"), TimeSpan.FromSeconds(5));

        Assert.Equal("Later", node.Label);
    }

    [Fact]
    public void Text_without_semantics_is_a_text_node_and_a_label_it_repeats_is_folded_into_its_widget()
    {
        var on = true;
        var name = "Ada";
        var frames = new UiFrames(new DefaultUiState(), width: 300f, height: 200f, body: ui =>
        {
            using (ui.Column(new Box { Padding = 8f }))
            {
                ui.Heading("Settings");
                ui.Checkbox("Sync", ref on);
                ui.TextField("name", ref name, placeholder: "Name");
                ui.Button("Save", enabled: false);
            }
        });

        var text = frames.Snapshot().ToText();

        Assert.Equal("""
            # frame 1: [x,y wxh] in main-window UI points, origin top-left, y down
            # surface main [0,0 300x200] @1x
            window [0,0 300x200]
              text "Settings" [8,8 64x16]
              checkbox path="Sync" "Sync" [8,32 56x32] checked
              textfield path="name" "Name" [8,72 284x32] value="Ada"
              button path="Save" "Save" [8,112 56x32] disabled

            """.ReplaceLineEndings("\n"), text);
    }

    [Fact]
    public void The_snapshot_is_also_json()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Button("Save"));

        var json = System.Text.Json.JsonDocument.Parse(frames.Snapshot().ToJson()).RootElement;
        var button = json.GetProperty("root").GetProperty("children")[0];

        Assert.Equal("button", button.GetProperty("role").GetString());
        Assert.Equal("Save", button.GetProperty("path").GetString());
        Assert.Equal(2, json.GetProperty("surfaces")[0].GetProperty("rect").GetProperty("w").GetInt32() / 200);
    }

    [Fact]
    public void Frames_build_no_snapshot_until_one_is_asked_for()
    {
        Ui? last = null;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            last = ui;
            using (ui.KeyedId("row"))
                ui.Button("Save");
            ui.Semantics(SemanticRole.Group, "custom");
        });

        frames.Frame(3);

        Assert.Null(last!.Snapshot);
        Assert.Equal(0, last.SemanticNodesBuilt);
        frames.Snapshot();
        Assert.NotEqual(0, last.SemanticNodesBuilt);
    }
}
