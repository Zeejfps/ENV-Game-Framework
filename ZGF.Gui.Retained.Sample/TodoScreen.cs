using ZGF.Gui.Desktop.Components.Controls;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Retained.Sample.Controls;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Retained.Sample;

public sealed record TodoScreen : Widget
{
    protected override IWidget Build(Context ctx) => Layout(ctx.Require<TodoViewModel>());

    private static IWidget Layout(TodoViewModel vm) => new Box
    {
        Background = Palette.Background,
        Children =
        [
            new Padding
            {
                Amount = PaddingStyle.All(16),
                Children =
                [
                    new Column
                    {
                        Gap = 8,
                        CrossAxis = CrossAxisAlignment.Stretch,
                        Children =
                        [
                            new Text { Value = "Todos", FontSize = 21, Weight = FontWeight.Bold, Color = Palette.Text },
                            new Text
                            {
                                Value = Prop.Bind<string?>(() => $"{vm.Remaining()} of {vm.Tasks.Count} remaining"),
                                FontSize = Palette.FontSize,
                                Color = Palette.TextMuted,
                            },
                            new ProgressBar
                            {
                                Fraction = Prop.Bind(() => vm.Tasks.Count == 0 ? 0f : 1f - vm.Remaining() / (float)vm.Tasks.Count),
                            },
                            DraftRow(vm),
                            FilterRow(vm),
                            Settings(vm),
                            new Box { Height = 1, Background = Palette.PanelBorder },
                            new Grow
                            {
                                Child = new ScrollArea
                                {
                                    FillParent = true,
                                    AutoHide = true,
                                    Style = new ScrollBarStyle
                                    {
                                        TrackBackground = 0x00000000,
                                        ThumbIdleBackground = Palette.Control,
                                        ThumbHoverBackground = Palette.ControlBorder,
                                        ThumbRadius = BorderRadiusStyle.All(3),
                                    },
                                    Gap = 8,
                                    Children =
                                    [
                                        Each.Of(vm.Tasks, new TaskRow(), gap: 8) with { CrossAxis = CrossAxisAlignment.Stretch },
                                        new Text
                                        {
                                            Value = "Nothing to do — add a task.",
                                            FontSize = Palette.FontSize,
                                            Color = Palette.TextMuted,
                                            Visible = Prop.Bind(() => vm.Tasks.Count == 0),
                                        },
                                    ],
                                },
                            },
                        ],
                    },
                ],
            },
        ],
    };

    private static IWidget DraftRow(TodoViewModel vm) => new Row
    {
        Gap = 8,
        CrossAxis = CrossAxisAlignment.Center,
        Children =
        [
            new Grow
            {
                Child = new Box
                {
                    Height = Palette.ControlHeight,
                    Background = Palette.FieldBackground,
                    BorderSize = BorderSizeStyle.All(1),
                    BorderColor = BorderColorStyle.All(Palette.ControlBorder),
                    BorderRadius = BorderRadiusStyle.All(6),
                    Children =
                    [
                        new Padding
                        {
                            Amount = new PaddingStyle { Left = 8, Right = 8 },
                            Children =
                            [
                                new TextInput
                                {
                                    Value = vm.Draft,
                                    Placeholder = "What needs doing?",
                                    OnSubmit = vm.AddDraft,
                                    Background = 0x00000000,
                                    FontSize = Palette.FontSize,
                                    Color = Palette.Text,
                                    CaretColor = Palette.Text,
                                    PlaceholderColor = Palette.TextMuted,
                                    VAlign = TextAlignment.Center,
                                },
                            ],
                        },
                    ],
                },
            },
            ActionButton("Add", vm.AddDraft, vm.CanAdd),
        ],
    };

    private static IWidget FilterRow(TodoViewModel vm) => new Row
    {
        Gap = 8,
        CrossAxis = CrossAxisAlignment.Center,
        Children =
        [
            new Checkbox { Label = "Hide completed", Checked = vm.HideCompleted },
            new Spacer(),
            ActionButton("Clear completed", vm.ClearCompleted),
        ],
    };

    private static IWidget Settings(TodoViewModel vm) => new Box
    {
        Background = Palette.Panel,
        BorderSize = BorderSizeStyle.All(1),
        BorderColor = BorderColorStyle.All(Palette.PanelBorder),
        BorderRadius = BorderRadiusStyle.All(6),
        Children =
        [
            new Padding
            {
                Amount = PaddingStyle.All(12),
                Children =
                [
                    new Column
                    {
                        Gap = 8,
                        CrossAxis = CrossAxisAlignment.Stretch,
                        Children =
                        [
                            new Text { Value = "Settings", FontSize = Palette.FontSize, Weight = FontWeight.Bold, Color = Palette.Text },
                            new Row
                            {
                                Gap = 8,
                                CrossAxis = CrossAxisAlignment.Center,
                                Children =
                                [
                                    new Text { Value = "Volume", FontSize = Palette.FontSize, Color = Palette.TextMuted },
                                    new Grow { Child = new Slider { Value = vm.Volume } },
                                    new Text
                                    {
                                        Value = vm.Volume.Bind<float, string?>(v => $"{v * 100:0}%"),
                                        FontSize = Palette.FontSize,
                                        Color = Palette.Text,
                                    },
                                ],
                            },
                        ],
                    },
                ],
            },
        ],
    };

    internal static IWidget ActionButton(string label, Action onClick, Prop<bool>? enabled = null) => new Button
    {
        Label = label,
        OnClick = onClick,
        Enabled = enabled ?? true,
        Height = Palette.ControlHeight,
        FontSize = Palette.FontSize,
        Background = Palette.Control,
        HoverBackground = Palette.ControlHovered,
        TextColor = Palette.Text,
        DisabledBackground = Palette.ControlActive,
        DisabledTextColor = Palette.TextMuted,
        Padding = new PaddingStyle { Left = 12, Right = 12, Top = 6, Bottom = 6 },
    };
}

public sealed record TaskRow : Widget
{
    protected override IWidget Build(Context ctx)
    {
        var vm = ctx.Require<TodoViewModel>();
        var task = ctx.Require<TaskViewModel>();
        return new Row
        {
            Gap = 8,
            CrossAxis = CrossAxisAlignment.Center,
            Visible = Prop.Bind(() => !(vm.HideCompleted.Value && task.Done.Value)),
            Children =
            [
                new Checkbox
                {
                    Label = task.Title,
                    Checked = task.Done,
                    TextColor = task.Done.Bind(done => done ? Palette.TextMuted : Palette.Text),
                },
                new Spacer(),
                TodoScreen.ActionButton("Delete", () => vm.Remove(task)),
            ],
        };
    }
}
