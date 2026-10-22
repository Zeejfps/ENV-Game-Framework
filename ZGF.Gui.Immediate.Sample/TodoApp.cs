using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Sample.Localization;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Sample;

public sealed class TodoItem
{
    public required int Id { get; init; }
    public required string Title { get; set; }
    public bool Done;
}

public sealed class TodoState
{
    public Locale Locale = Locale.En;
    public string Draft = "";
    public bool HideCompleted;
    public float Volume = 0.4f;
    public bool Notifications = true;
    public bool DarkMode = true;
    public int Priority = 2;
    public int NextId = 1;
    public List<TodoItem> Items { get; } = new();

    public void Add(string title) => Items.Add(new TodoItem { Id = NextId++, Title = title });
}

public sealed class TodoApp(TodoState state) : Gui<TodoState>(state)
{
    private static readonly (Locale Locale, string Endonym)[] Languages =
    [
        (Locale.En, "English"),
        (Locale.Es, "Español"),
        (Locale.Ru, "Русский"),
        (Locale.Pseudo, "Pseudo"),
    ];

    private static readonly Segment<Locale>[] LanguageSegments =
        Array.ConvertAll(Languages, language => new Segment<Locale>(language.Locale, language.Endonym));

    private readonly List<TodoItem> _shown = new();
    private readonly ListSelection _selection = new();
    private TodoItem? _menuTarget;

    protected override void Render(Ui ui, TodoState s)
    {
        var palette = s.DarkMode ? UiTheme.Dark : UiTheme.Light;
        using var screen = ui.Screen(palette with { ScreenPadding = 0f });
        var theme = ui.Theme;
        var t = Strings.For(s.Locale);
        MenuBar(ui, s, t);
        var inset = palette.ScreenPadding;
        using var content = ui.Column(new Box { Height = Sizing.Grow(), Padding = new Spacing(inset, inset - theme.Gap, inset, inset) });
        ui.Heading(t.AppTitle);

        var remaining = 0;
        foreach (var item in s.Items)
        {
            if (!item.Done) remaining++;
        }
        ui.Label(ui.Memo("remaining", (t, remaining), static a => a.t.TasksRemaining(a.remaining)), theme.TextMuted);

        var progress = s.Items.Count == 0 ? 0f : 1f - remaining / (float)s.Items.Count;
        var animatedProgress = ui.Animate("progress", progress, Tween.OutCubic(0.35f));
        ui.ProgressBar(animatedProgress);

        using (ui.Row())
        {
            var add = ui.TextField("draft", ref s.Draft, t.DraftPlaceholder);
            var canAdd = !string.IsNullOrWhiteSpace(s.Draft);
            add |= ui.Button(t.ActionAdd, enabled: canAdd, id: "add", variant: ButtonVariant.Primary);
            if (add && canAdd)
            {
                s.Add(s.Draft.Trim());
                s.Draft = "";
            }
            if (ui.Button(t.ActionAddMany, id: "add-many"))
                AddMany(s, t);
            ui.Tooltip(ui.GetId("add-many"), t.TooltipAddMany);
        }

        using (ui.Row())
        {
            ui.Checkbox(t.FilterHideCompleted, ref s.HideCompleted, id: "hide-completed");
            ui.Spacer();
            if (ui.Button(t.ActionClearCompleted, id: "clear-completed", variant: ButtonVariant.Ghost))
                s.Items.RemoveAll(item => item.Done);
        }

        using (ui.Panel())
        {
            ui.Label(t.SettingsTitle, bold: true);
            using (ui.Row())
            {
                ui.Label(t.SettingsVolume, theme.TextMuted);
                ui.Slider("volume", ref s.Volume, 0f, 1f);
                var percent = (int)MathF.Round(s.Volume * 100);
                ui.Label(ui.Memo("volume", (t, percent), static a => a.t.SettingsVolumeValue(a.percent)));
            }
            using (ui.Row())
            {
                ui.Toggle(t.SettingsNotifications, ref s.Notifications, id: "notifications");
                ui.Spacer();
                ui.Label(t.SettingsPriority, theme.TextMuted);
                ui.Stepper("priority", ref s.Priority, 1, 5);
            }
            ui.Toggle(t.SettingsDarkMode, ref s.DarkMode, id: "dark-mode");
            ui.Label(t.SettingsLanguage, theme.TextMuted);
            ui.SegmentedControl("language", ref s.Locale, LanguageSegments);
        }

        ui.Separator();

        _shown.Clear();
        foreach (var item in s.Items)
        {
            if (!s.HideCompleted || !item.Done)
                _shown.Add(item);
        }

        TodoItem? removed = null;
        using (var list = ui.SelectableList(ui.GetId("items"), _shown.Count, _selection, theme.ControlHeight, new Box { Gap = theme.Gap }))
        {
            for (var i = list.First; i < list.End; i++)
            {
                var item = _shown[i];
                using (ui.Id(item.Id))
                using (list.Row(i, ui.GetId("row"), new Box { Padding = 0f }))
                {
                    ui.Checkbox(item.Title, ref item.Done, item.Done ? theme.TextMuted : null, id: "done");
                    ui.Spacer();
                    if (ui.IconButton("delete", "×", tooltip: t.ActionDelete))
                        removed = item;
                }
            }
            if (list.ContextMenuRequested)
            {
                _menuTarget = _shown[list.Index];
                ui.OpenContextMenu("item-menu", list.ContextMenuAt);
            }

            if (s.Items.Count == 0)
                ui.Label(t.TasksEmpty, theme.TextMuted);
        }

        using (var menu = ui.ContextMenu("item-menu"))
        {
            if (menu.IsOpen && _menuTarget != null)
            {
                if (ui.MenuItem(t.ActionToggleDone))
                    _menuTarget.Done = !_menuTarget.Done;
                if (ui.MenuItem(t.ActionDelete, id: "menu-delete"))
                    removed = _menuTarget;
            }
        }

        if (removed != null)
            s.Items.Remove(removed);
    }

    private static void MenuBar(Ui ui, TodoState s, Strings t)
    {
        using (ui.MenuBar("menu"))
        {
            using (var file = ui.Menu(t.MenuFile, id: "file"))
            {
                if (file.IsOpen)
                {
                    if (ui.MenuItem(t.ActionAddMany, id: "menu-add-many"))
                        AddMany(s, t);
                    if (ui.MenuItem(t.ActionClearCompleted, id: "menu-clear-completed"))
                        s.Items.RemoveAll(item => item.Done);
                }
            }
            using (var view = ui.Menu(t.MenuView, id: "view"))
            {
                if (view.IsOpen)
                {
                    ui.MenuItem(t.SettingsDarkMode, ref s.DarkMode, id: "menu-dark-mode");
                    ui.MenuItem(t.FilterHideCompleted, ref s.HideCompleted, id: "menu-hide-completed");
                    ui.MenuSeparator();
                    using (ui.Id("languages"))
                    {
                        foreach (var (locale, endonym) in Languages)
                        {
                            if (ui.MenuItem(endonym, check: s.Locale == locale))
                                s.Locale = locale;
                        }
                    }
                }
            }
        }
    }

    private static void AddMany(TodoState s, Strings t)
    {
        for (var i = 0; i < 1000; i++)
            s.Add(t.TasksGenerated(s.NextId));
    }
}
