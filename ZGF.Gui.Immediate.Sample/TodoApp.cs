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

    private readonly List<TodoItem> _shown = new();

    protected override void Render(Ui ui, TodoState s)
    {
        using var screen = ui.Screen(s.DarkMode ? UiTheme.Dark : UiTheme.Light);
        var theme = ui.Theme;
        var t = Strings.For(s.Locale);
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
            add |= ui.Button(t.ActionAdd, enabled: canAdd, id: "add");
            if (add && canAdd)
            {
                s.Add(s.Draft.Trim());
                s.Draft = "";
            }
            if (ui.Button(t.ActionAddMany, id: "add-many"))
            {
                for (var i = 0; i < 1000; i++)
                    s.Add(t.TasksGenerated(s.NextId));
            }
        }

        using (ui.Row())
        {
            ui.Checkbox(t.FilterHideCompleted, ref s.HideCompleted, id: "hide-completed");
            ui.Spacer();
            if (ui.Button(t.ActionClearCompleted, id: "clear-completed"))
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
            using (ui.Row())
            {
                foreach (var (locale, endonym) in Languages)
                {
                    if (ui.Button(endonym, enabled: s.Locale != locale))
                        s.Locale = locale;
                }
            }
        }

        ui.Separator();

        _shown.Clear();
        foreach (var item in s.Items)
        {
            if (!s.HideCompleted || !item.Done)
                _shown.Add(item);
        }

        TodoItem? removed = null;
        using (var list = ui.VirtualScroll("items", _shown.Count, theme.ControlHeight))
        {
            for (var i = list.First; i < list.End; i++)
            {
                var item = _shown[i];
                using (ui.Id(item.Id))
                using (ui.Row())
                {
                    ui.Checkbox(item.Title, ref item.Done, item.Done ? theme.TextMuted : null, id: "done");
                    ui.Spacer();
                    if (ui.Button(t.ActionDelete, id: "delete"))
                        removed = item;
                }
            }

            if (s.Items.Count == 0)
                ui.Label(t.TasksEmpty, theme.TextMuted);
        }

        if (removed != null)
            s.Items.Remove(removed);
    }
}
