using ZGF.Gui;
using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Prototype;

/// <summary>
/// Composite component: ViewModel in, structure out. Never touches a View — the single
/// BuildView call at the window recurses through every nested component.
/// Screen-level VMs resolve from the context (registered or container-constructed);
/// per-item VMs (<see cref="TaskRow"/>) are passed down as plain data.
/// </summary>
public sealed record TodoScreen : Widget
{
    protected override IWidget Build(Context ctx)
    {
        var vm = ctx.Require<TodoViewModel>();
        return Layout(vm);
    }

    private static IWidget Layout(TodoViewModel Vm) => new Box
    {
        Background = 0xFF1E1E1E,
        Padding = PaddingStyle.All(16),
        Children =
        [
            new Column
            {
                Gap = 10,
                CrossAxis = CrossAxisAlignment.Stretch,
                Children =
                [
                    Header(Vm),
                    new Text
                    {
                        FontSize = 13,
                        Color = 0xFF9CA3AF,
                        Bind = () => $"{Vm.RemainingCount()} of {Vm.Tasks.Count} remaining",
                    },
                    new Text
                    {
                        Value = "Nothing to do — add a task.",
                        FontSize = 13,
                        Color = 0xFF6B7280,
                        Visible = Prop.Bind(() => Vm.Tasks.Count == 0),
                    },
                    new Grow
                    {
                        Child = new ScrollArea
                        {
                            Gap = 4,
                            Children = [Each.Of(Vm.Tasks, new TaskRow(), gap: 4)],
                        },
                    },
                ],
            },
        ],
    };

    private static IWidget Header(TodoViewModel Vm) => new Row
    {
        Gap = 8,
        CrossAxis = CrossAxisAlignment.Center,
        Children =
        [
            new Text { Value = "Tasks", FontSize = 20, Color = 0xFFE0E0E0 },
            new Spacer(),
            new TextInput
            {
                Value = Vm.NewTitle,
                Placeholder = "New task…",
                Width = 200,
                Height = 24,
                FontSize = 13,
                Color = 0xFFE0E0E0,
                CaretColor = 0xFFE0E0E0,
            },
            new Button
            {
                Label = "Clear done",
                OnClick = Vm.ClearDone,
                Background = 0xFF374151,
                HoverBackground = 0xFF4B5563,
                FontSize = 13,
            },
            new Button { Label = "Add", OnClick = Vm.AddTask, FontSize = 13 },
        ],
    };
}

/// <summary>
/// Item template: built once per task against the scoped context <see cref="Each{T}"/> creates,
/// so the task VM resolves from the scope and the list VM chains up to the window context.
/// </summary>
public sealed record TaskRow : Widget
{
    protected override IWidget Build(Context ctx)
    {
        var list = ctx.Require<TodoViewModel>();
        var task = ctx.Require<TaskViewModel>();
        return Layout(list, task);
    }

    private static IWidget Layout(TodoViewModel Vm, TaskViewModel Task) => new Box
    {
        Padding = PaddingStyle.All(8),
        BorderRadius = BorderRadiusStyle.All(4),
        Background = Task.IsDone.Bind(done => done ? 0xFF232A23 : 0xFF2A2A2Au),
        Children =
        [
            new Row
            {
                Gap = 8,
                CrossAxis = CrossAxisAlignment.Center,
                Children =
                [
                    new Button
                    {
                        Label = "✓",
                        OnClick = Task.Toggle,
                        FontSize = 12,
                        Background = 0xFF374151,
                        HoverBackground = 0xFF4B5563,
                        Padding = new PaddingStyle { Left = 7, Right = 7, Top = 2, Bottom = 2 },
                    },
                    new Text
                    {
                        Value = Task.Title,
                        FontSize = 14,
                        BindColor = () => Task.IsDone.Value ? 0xFF6B7280 : 0xFFE0E0E0,
                    },
                    new Spacer(),
                    new Button
                    {
                        Label = "✕",
                        OnClick = () => Vm.Remove(Task),
                        FontSize = 12,
                        Background = 0x00000000,
                        HoverBackground = 0xFF7F1D1D,
                        Padding = new PaddingStyle { Left = 7, Right = 7, Top = 2, Bottom = 2 },
                    },
                ],
            },
        ],
    };
}
