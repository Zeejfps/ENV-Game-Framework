using ZGF.Gui.Immediate;

namespace ZGF.Gui.Immediate.Sample;

public sealed class TodoItem
{
    public required int Id { get; init; }
    public required string Title { get; set; }
    public bool Done;
}

public sealed class TodoState
{
    public string Draft = "";
    public bool HideCompleted;
    public float Volume = 0.4f;
    public int NextId = 1;
    public List<TodoItem> Items { get; } = new();

    public void Add(string title) => Items.Add(new TodoItem { Id = NextId++, Title = title });
}

public sealed class TodoApp(TodoState state) : Gui<TodoState>(state)
{
    protected override void Render(Ui ui, TodoState s)
    {
        ui.Heading("Todos");

        var remaining = s.Items.Count(item => !item.Done);
        ui.Label($"{remaining} of {s.Items.Count} remaining", ui.Theme.TextMuted);
        ui.ProgressBar(s.Items.Count == 0 ? 0f : 1f - remaining / (float)s.Items.Count);

        using (ui.Row())
        {
            var add = ui.TextField("draft", ref s.Draft, "What needs doing?");
            var canAdd = !string.IsNullOrWhiteSpace(s.Draft);
            add |= ui.Button("Add", enabled: canAdd);
            if (add && canAdd)
            {
                s.Add(s.Draft.Trim());
                s.Draft = "";
            }
        }

        using (ui.Row())
        {
            ui.Checkbox("Hide completed", ref s.HideCompleted);
            ui.Spacer();
            if (ui.Button("Clear completed"))
                s.Items.RemoveAll(item => item.Done);
        }

        using (ui.Panel())
        {
            ui.Label("Settings", bold: true);
            using (ui.Row())
            {
                ui.Label("Volume", ui.Theme.TextMuted);
                ui.Slider("volume", ref s.Volume, 0f, 1f);
                ui.Label($"{s.Volume * 100:0}%");
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
                    ui.Checkbox(item.Title + "##done", ref item.Done, item.Done ? ui.Theme.TextMuted : null);
                    ui.Spacer();
                    if (ui.Button("Delete"))
                        removed = item;
                }
            }

            if (s.Items.Count == 0)
                ui.Label("Nothing to do — add a task.", ui.Theme.TextMuted);
        }

        if (removed != null)
            s.Items.Remove(removed);
    }
}
