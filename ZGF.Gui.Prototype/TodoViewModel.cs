using ZGF.Observable;

namespace ZGF.Gui.Prototype;

public sealed class TaskViewModel
{
    public TaskViewModel(string title) => Title = title;

    public string Title { get; }
    public State<bool> IsDone { get; } = new(false);

    public void Toggle() => IsDone.Value = !IsDone.Value;
}

public sealed class TodoViewModel
{
    private int _counter;

    public ObservableList<TaskViewModel> Tasks { get; } = new();

    public State<string> NewTitle { get; } = new("");

    public void AddTask()
    {
        var title = NewTitle.Value.Trim();
        Tasks.Add(new TaskViewModel(title.Length > 0 ? title : $"Task #{++_counter}"));
        NewTitle.Value = "";
    }

    public void Remove(TaskViewModel task) => Tasks.Remove(task);

    public void ClearDone()
    {
        for (var i = Tasks.Count - 1; i >= 0; i--)
        {
            if (Tasks[i].IsDone.Value)
                Tasks.RemoveAt(i);
        }
    }

    public int RemainingCount()
    {
        var remaining = 0;
        foreach (var task in Tasks)
        {
            if (!task.IsDone.Value)
                remaining++;
        }
        return remaining;
    }
}
