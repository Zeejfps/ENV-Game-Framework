using ZGF.Gui;
using ZGF.Gui.Prototype.Components;
using ZGF.Gui.Views;

namespace ZGF.Gui.Prototype;

/// <summary>
/// Composite component: ViewModel in, structure out. Never touches a View — the single
/// BuildView call at the window recurses through every nested component.
/// </summary>
public sealed record TodoScreen(TodoViewModel Vm) : Component
{
    protected override IComponent Build(Context ctx) => new Box
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
                    Header(),
                    new Text
                    {
                        FontSize = 13,
                        Color = 0xFF9CA3AF,
                        Bind = () => $"{Vm.RemainingCount()} of {Vm.Tasks.Count} remaining",
                    },
                    new Text("Nothing to do — add a task.")
                    {
                        FontSize = 13,
                        Color = 0xFF6B7280,
                        BindVisible = () => Vm.Tasks.Count == 0,
                    },
                    new Each<TaskViewModel>(Vm.Tasks, task => new TaskRow(Vm, task))
                    {
                        Gap = 4,
                    },
                ],
            },
        ],
    };

    private IComponent Header() => new Row
    {
        Gap = 8,
        CrossAxis = CrossAxisAlignment.Center,
        Children =
        [
            new Text("Tasks") { FontSize = 20, Color = 0xFFE0E0E0 },
            new Spacer(),
            new Button("Clear done", Vm.ClearDone)
            {
                Background = 0xFF374151,
                HoverBackground = 0xFF4B5563,
                FontSize = 13,
            },
            new Button("Add", Vm.AddTask) { FontSize = 13 },
        ],
    };
}

public sealed record TaskRow(TodoViewModel Vm, TaskViewModel Task) : Component
{
    protected override IComponent Build(Context ctx) => new Box
    {
        Padding = PaddingStyle.All(8),
        BorderRadius = BorderRadiusStyle.All(4),
        BindBackground = () => Task.IsDone.Value ? 0xFF232A23 : 0xFF2A2A2A,
        Children =
        [
            new Row
            {
                Gap = 8,
                CrossAxis = CrossAxisAlignment.Center,
                Children =
                [
                    new Button("✓", Task.Toggle)
                    {
                        FontSize = 12,
                        Background = 0xFF374151,
                        HoverBackground = 0xFF4B5563,
                        Padding = new PaddingStyle { Left = 7, Right = 7, Top = 2, Bottom = 2 },
                    },
                    new Text(Task.Title)
                    {
                        FontSize = 14,
                        BindColor = () => Task.IsDone.Value ? 0xFF6B7280 : 0xFFE0E0E0,
                    },
                    new Spacer(),
                    new Button("✕", () => Vm.Remove(Task))
                    {
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
