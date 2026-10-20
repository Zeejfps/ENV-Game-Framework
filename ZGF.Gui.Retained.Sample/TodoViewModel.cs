using ZGF.Observable;

namespace ZGF.Gui.Retained.Sample;

public sealed class TaskViewModel(string title)
{
    public string Title { get; } = title;
    public State<bool> Done { get; } = new(false);
}

public sealed class TodoViewModel
{
    public TodoViewModel()
    {
        CanAdd = new Derived<bool>(() => !string.IsNullOrWhiteSpace(Draft.Value));
    }

    public ObservableList<TaskViewModel> Tasks { get; } = new();
    public State<string> Draft { get; } = new("");
    public State<bool> HideCompleted { get; } = new(false);
    public State<float> Volume { get; } = new(0.4f);
    public Derived<bool> CanAdd { get; }

    public void Add(string title) => Tasks.Add(new TaskViewModel(title));

    public void AddDraft()
    {
        if (!CanAdd.Value) return;
        Add(Draft.Value.Trim());
        Draft.Value = "";
    }

    public void Remove(TaskViewModel task) => Tasks.Remove(task);

    public void ClearCompleted()
    {
        for (var i = Tasks.Count - 1; i >= 0; i--)
        {
            if (Tasks[i].Done.Value)
                Tasks.RemoveAt(i);
        }
    }

    public int Remaining()
    {
        var remaining = 0;
        foreach (var task in Tasks)
        {
            if (!task.Done.Value)
                remaining++;
        }
        return remaining;
    }
}
