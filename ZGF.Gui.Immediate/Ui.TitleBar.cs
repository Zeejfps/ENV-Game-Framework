using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

public abstract partial class Ui
{
    private const string WindowIconImagePrefix = "window-icon-";

    private readonly List<int> _titleBarDragAreas = new();
    private readonly Dictionary<int, ShownWindowIconImage> _windowIconImages = new();

    /// <summary>The icon the platform shows for the window, which <see cref="TitleBarIcon(int, float?)"/>
    /// and <see cref="AppIcon"/> draw; null until the app sets one.</summary>
    internal WindowIcon? WindowIcon { get; set; }

    /// <summary>Whether the window lets what is recorded reach under its native title bar, with
    /// <see cref="TitleBar(int, Box)"/>; false where the platform keeps the title bar to itself.</summary>
    public bool CanDrawTitleBar { get; internal set; }

    /// <summary>Where the platform draws its caption buttons over the title bar, in UI points; null while
    /// the window's content does not reach under its title bar.</summary>
    internal UiRect? CaptionButtons { get; set; }

    /// <summary>How much of the window, from its top, lies off the screen, in UI points: the content
    /// starts below it.</summary>
    internal float HiddenTop { get; set; }

    /// <summary>The color of the title bar recorded this frame, or null when none was, and the window's
    /// title bar gets its own strip again.</summary>
    internal uint? TitleBarBackground { get; private set; }

    public Scope TitleBar(string id, Box box = default) => TitleBar(GetId(id), box);

    /// <summary>A row in the window's native title bar, as tall as it, that leaves the room of the
    /// platform's caption buttons free at its end. Record it at the top of the window, every frame the
    /// title bar should hold it, while <see cref="CanDrawTitleBar"/>: the window's content reaches under
    /// its title bar while one is recorded. The caption buttons are drawn on its background. Mark where it
    /// moves the window with <see cref="TitleBarDragArea(int, Box)"/>.</summary>
    public Scope TitleBar(int id, Box box = default)
    {
        TitleBarBackground = box.Background ?? WindowBackground;
        var padding = box.Padding ?? default;
        return Row(id, box with
        {
            Height = Sizing.Fixed(TitleBarHeight()),
            Padding = LeaveCaptionButtonsFree(padding),
        });
    }

    /// <summary>As tall as the caption buttons: a maximized window's keeps the height it has while
    /// the window is not maximized, below the part of the window off the screen.</summary>
    private float TitleBarHeight()
    {
        if (CaptionButtons == null)
            return Theme.MenuBarHeight;
        return CaptionButtons.Value.Height;
    }

    private Spacing LeaveCaptionButtonsFree(Spacing padding)
    {
        if (CaptionButtons == null)
            return padding;
        var buttonsWidth = Width - CaptionButtons.Value.X;
        return padding with { Right = padding.Right + buttonsWidth };
    }

    public void TitleBarDragArea(string id, Box box = default) => TitleBarDragArea(GetId(id), box);

    /// <summary>An empty part of a <see cref="TitleBar(int, Box)"/>, growing to the room it is given
    /// unless sized, that moves the window when dragged and maximizes it when double-clicked, as the
    /// native title bar does.</summary>
    public void TitleBarDragArea(int id, Box box = default)
    {
        _titleBarDragAreas.Add(id);
        Leaf(id, box with
        {
            Width = box.Width ?? Sizing.Grow(),
            Height = box.Height ?? Sizing.Grow(),
        });
    }

    public bool TitleBarIcon(string id, float? size = null) => TitleBarIcon(GetId(id), size);

    /// <summary>The window's icon in a <see cref="TitleBar(int, Box)"/>, <paramref name="size"/> points
    /// square or as large as the theme's icons, as a button: true the frame it is pressed, for the app to
    /// open its window menu. Records nothing while the window has no icon.</summary>
    public bool TitleBarIcon(int id, float? size = null)
    {
        if (WindowIcon == null)
            return false;
        var side = size ?? Theme.IconSize;
        var imageId = ShowWindowIconImage(side);
        if (imageId == null)
            return false;
        var i = Interact(id);
        var background = i.Hovered ? Theme.ControlHovered : 0u;
        using (Box(i.Id, new Box { Padding = new Spacing(Theme.GapTight), Radius = Theme.CornerRadius, Background = background }))
            Image(imageId, side, side);
        return i.Pressed;
    }

    /// <summary>The window's icon, <paramref name="size"/> points square, drawn in the content as the
    /// app's own picture, such as in an About dialog. Records nothing while the window has no icon.</summary>
    public void AppIcon(float size, Box box = default)
    {
        if (WindowIcon == null)
            return;
        var imageId = ShowWindowIconImage(size);
        if (imageId == null)
            return;
        Image(imageId, size, size, box);
    }

    /// <summary>The id of the canvas image of the icon that suits <paramref name="size"/> points at its
    /// scale, each size the icon is drawn at keeping an image of its own; null where the canvas holds
    /// no images.</summary>
    private string? ShowWindowIconImage(float size)
    {
        var pixels = (int)MathF.Ceiling(size * _canvas.DpiScale);
        var image = WindowIcon!.PickImage(pixels);
        if (image == null)
            return null;
        var picked = image.Value;
        if (_windowIconImages.TryGetValue(picked.Width, out var shown) && shown.Image == picked)
            return shown.Id;
        var imageId = WindowIconImagePrefix + picked.Width;
        if (!SetImage(imageId, picked.Width, picked.Height, picked.Pixels))
            return null;
        _windowIconImages[picked.Width] = new ShownWindowIconImage(picked, imageId);
        return imageId;
    }

    /// <summary>An image of the window's icon the canvas holds, under <paramref name="Id"/>.</summary>
    private readonly record struct ShownWindowIconImage(WindowIconImage Image, string Id);

    /// <summary>Puts where this frame's <see cref="TitleBarDragArea(int, Box)"/>s were laid out into
    /// <paramref name="areas"/>, in UI points.</summary>
    internal void ListTitleBarDragAreas(List<UiRect> areas)
    {
        areas.Clear();
        foreach (var id in _titleBarDragAreas)
        {
            if (_rects.TryGetValue(id, out var rect))
                areas.Add(rect);
        }
    }

    private void BeginTitleBar()
    {
        TitleBarBackground = null;
        _titleBarDragAreas.Clear();
    }
}
