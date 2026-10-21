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

    protected override void Render(Ui ui, TodoState s)
    {
        var t = Strings.For(s.Locale);
        var theme = WidgetTheme.For(ui);
        ui.Heading(t.AppTitle);

        var remaining = 0;
        foreach (var item in s.Items)
        {
            if (!item.Done) remaining++;
        }
        ui.Label(ui.Memo("remaining", (t, remaining), static a => a.t.TasksRemaining(a.remaining)), theme.TextMuted);
        ui.ProgressBar(ui.Animate("progress", s.Items.Count == 0 ? 0f : 1f - remaining / (float)s.Items.Count, Tween.OutCubic(0.35f)));

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

        TodoItem? removed = null;
        using (ui.Scroll("items"))
        {
            foreach (var item in s.Items)
            {
                if (s.HideCompleted && item.Done) continue;
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
