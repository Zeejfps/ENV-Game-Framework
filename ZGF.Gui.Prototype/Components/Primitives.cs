using ZGF.Gui;
using ZGF.Gui.Bindings;
using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Prototype.Components;

/// <summary>
/// Shared per-view props every primitive forwards onto the built <see cref="View"/>.
/// </summary>
public abstract record Primitive : IComponent
{
    public StyleValue<float> Width { get; init; }
    public StyleValue<float> Height { get; init; }
    public StyleValue<float> MinWidth { get; init; }
    public StyleValue<float> MinHeight { get; init; }
    public string? Id { get; init; }

    /// <summary>Auto-tracked visibility binding (e.g. <c>() =&gt; vm.Items.Count == 0</c>).</summary>
    public Func<bool>? BindVisible { get; init; }

    public View BuildView(Context ctx)
    {
        var v = CreateView(ctx);
        if (Width.IsSet) v.Width = Width;
        if (Height.IsSet) v.Height = Height;
        if (MinWidth.IsSet) v.MinWidthConstraint = MinWidth;
        if (MinHeight.IsSet) v.MinHeightConstraint = MinHeight;
        if (Id != null) v.Id = Id;
        if (BindVisible != null) v.BindIsVisible(BindVisible);
        return v;
    }

    protected abstract View CreateView(Context ctx);
}

public sealed record Text(string? Value = null) : Primitive
{
    public StyleValue<float> FontSize { get; init; }
    public StyleValue<uint> Color { get; init; }
    public StyleValue<TextAlignment> HAlign { get; init; }
    public StyleValue<TextAlignment> VAlign { get; init; }

    /// <summary>Auto-tracked text binding; overrides <see cref="Value"/> once attached.</summary>
    public Func<string?>? Bind { get; init; }

    /// <summary>Auto-tracked color binding.</summary>
    public Func<uint>? BindColor { get; init; }

    protected override View CreateView(Context ctx)
    {
        var v = new TextView { Text = Value };
        if (FontSize.IsSet) v.FontSize = FontSize;
        if (Color.IsSet) v.TextColor = Color;
        if (HAlign.IsSet) v.HorizontalTextAlignment = HAlign;
        if (VAlign.IsSet) v.VerticalTextAlignment = VAlign;
        if (Bind != null) v.BindText(Bind);
        if (BindColor != null) v.BindTextColor(BindColor);
        return v;
    }
}

public sealed record Box : Primitive
{
    public uint Background { get; init; }
    public PaddingStyle Padding { get; init; }
    public StyleValue<BorderRadiusStyle> BorderRadius { get; init; }
    public StyleValue<BorderSizeStyle> BorderSize { get; init; }
    public StyleValue<BorderColorStyle> BorderColor { get; init; }
    public IComponent[] Children { get; init; } = [];

    /// <summary>Auto-tracked background binding (hover/selection driven by VM state).</summary>
    public Func<uint>? BindBackground { get; init; }

    protected override View CreateView(Context ctx)
    {
        var v = new RectView { BackgroundColor = Background, Padding = Padding };
        if (BorderRadius.IsSet) v.BorderRadius = BorderRadius;
        if (BorderSize.IsSet) v.BorderSize = BorderSize;
        if (BorderColor.IsSet) v.BorderColor = BorderColor;
        if (BindBackground != null) v.BindBackgroundColor(BindBackground);
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}

public abstract record FlexBase : Primitive
{
    public float Gap { get; init; }
    public MainAxisAlignment MainAxis { get; init; } = MainAxisAlignment.Start;
    public CrossAxisAlignment CrossAxis { get; init; } = CrossAxisAlignment.Start;
    public IComponent[] Children { get; init; } = [];

    protected abstract Axis Axis { get; }

    protected override View CreateView(Context ctx)
    {
        var v = new FlexView
        {
            Axis = Axis,
            Gap = Gap,
            MainAxisAlignment = MainAxis,
            CrossAxisAlignment = CrossAxis,
        };
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}

public sealed record Column : FlexBase
{
    protected override Axis Axis => ZGF.Gui.Views.Axis.Vertical;
}

public sealed record Row : FlexBase
{
    protected override Axis Axis => ZGF.Gui.Views.Axis.Horizontal;
}

/// <summary>
/// Wraps a child in a <see cref="FlexItem"/> so it grows along the parent flex axis.
/// The child must build to a <see cref="MultiChildView"/> (FlexItem's requirement).
/// </summary>
public sealed record Grow(IComponent Child, float Factor = 1f) : IComponent
{
    public View BuildView(Context ctx) => new FlexItem
    {
        Grow = Factor,
        Child = (MultiChildView)Child.BuildView(ctx),
    };
}

/// <summary>Flexible empty space — <c>new Spacer()</c> pushes siblings apart.</summary>
public sealed record Spacer : IComponent
{
    public View BuildView(Context ctx) => new FlexItem { Grow = 1f, Child = new RectView() };
}

/// <summary>
/// Dynamic children: mirrors an <see cref="ObservableList{T}"/> into a flex container,
/// building one component per item via <paramref name="Template"/>. Captures <paramref name="ctx"/>
/// for late item builds — safe because the built view is already pinned to this window.
/// </summary>
public sealed record Each<T>(ObservableList<T> Items, Func<T, IComponent> Template) : FlexBase
{
    protected override Axis Axis => ZGF.Gui.Views.Axis.Vertical;

    protected override View CreateView(Context ctx)
    {
        var v = (FlexView)base.CreateView(ctx);
        v.BindChildren(Items, item => Template(item).BuildView(ctx));
        return v;
    }
}
