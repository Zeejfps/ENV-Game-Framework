using System.Runtime.Versioning;
using ZGF.Geometry;
using ZGF.Gui.VerticalScrollBar;
using ZGF.Gui.Views;
using ZGF.Gui.Web.Files;
using ZGF.Gui.Web.Input;

namespace ZGF.Gui.Web.Demo;

/// A full-screen showcase built from a real ZGF.Gui view tree (header / sidebar /
/// content / footer), driven by the WebGL2 canvas. The web host has no view-level
/// input system yet, so hover/press highlighting and clicks are polled from
/// <see cref="WebInput"/> against each control's laid-out <see cref="View.Position"/>.
[SupportedOSPlatform("browser")]
internal sealed class DemoScreen
{
    private static class Palette
    {
        public const uint PageBg = 0xFF0E1014;
        public const uint HeaderBg = 0xFF1B2030;
        public const uint SidebarBg = 0xFF141822;
        public const uint CardBg = 0xFF1A1F2B;
        public const uint Accent = 0xFF4C8DFF;
        public const uint AccentHover = 0xFF6AA1FF;
        public const uint AccentPress = 0xFF3A6FD0;
        public const uint Subtle = 0xFF222838;
        public const uint SubtleHover = 0xFF2C3447;
        public const uint SubtlePress = 0xFF1A2030;
        public const uint Text = 0xFFE6EAF2;
        public const uint TextDim = 0xFF9AA4B2;
        public const uint White = 0xFFFFFFFF;
    }

    public MultiChildView Root { get; }

    /// Raised when the upload control is clicked. Program handles it so the file
    /// picker opens inside the live DOM click gesture.
    public Action? UploadRequested;

    private readonly List<DemoButton> _buttons = new();
    private readonly TextView _statusLabel;
    private readonly TextView _outputLabel;
    private readonly VerticalScrollPane _contentScroll = new() { Gap = 16 };
    private DemoButton? _activeNav;

    public DemoScreen(Context context, int width, int height)
    {
        _statusLabel = Label("Move the mouse over the controls — hover and click are live.", 12, Palette.TextDim,
            vCenter: true);
        _outputLabel = Label("No files yet. Click “Upload files” or drop files anywhere on the window.",
            14, Palette.TextDim, wrap: true);

        Root = new MultiChildView
        {
            Width = width,
            Height = height,
            Context = context,
            Children =
            {
                new RectView
                {
                    BackgroundColor = Palette.PageBg,
                    Children =
                    {
                        new ColumnView
                        {
                            Gap = 0,
                            Children =
                            {
                                BuildHeader(),
                                new FlexItem { Grow = 1, Child = BuildBody() },
                                BuildFooter(),
                            },
                        },
                    },
                },
            },
        };
    }

    // ---- regions ----

    private MultiChildView BuildHeader() => new RectView
    {
        Height = 60,
        BackgroundColor = Palette.HeaderBg,
        Children =
        {
            new PaddingView
            {
                Padding = new PaddingStyle { Left = 24, Right = 24 },
                Children =
                {
                    new RowView
                    {
                        MainAxisAlignment = MainAxisAlignment.SpaceBetween,
                        CrossAxisAlignment = CrossAxisAlignment.Center,
                        Children =
                        {
                            Label("ZGF GUI", 22, Palette.White, bold: true, vCenter: true),
                            Label("WebGL2 · FreeType · HarfBuzz", 13, Palette.TextDim, vCenter: true),
                        },
                    },
                },
            },
        },
    };

    private MultiChildView BuildBody() => new RowView
    {
        Gap = 0,
        Children =
        {
            BuildSidebar(),
            new FlexItem { Grow = 1, Child = BuildContent() },
        },
    };

    private MultiChildView BuildSidebar()
    {
        var nav = new ColumnView { Gap = 8 };
        nav.Children.Add(Label("NAVIGATION", 11, Palette.TextDim));

        string[] items = { "Home", "Widgets", "Typography", "About" };
        for (var i = 0; i < items.Length; i++)
        {
            var selected = i == 1;
            var btn = NavButton(items[i], selected);
            if (selected) _activeNav = btn;
            nav.Children.Add(btn.View);
        }

        return new RectView
        {
            Width = 220,
            BackgroundColor = Palette.SidebarBg,
            Children =
            {
                new PaddingView { Padding = PaddingStyle.All(16), Children = { nav } },
            },
        };
    }

    private MultiChildView BuildContent()
    {
        var col = _contentScroll;
        col.Children.Add(Label("Widgets", 26, Palette.Text, bold: true));
        col.Children.Add(Label(
            "This screen is a live ZGF.Gui view tree — RectView, TextView, ColumnView/RowView, " +
            "FlexItem growth, rounded corners and box shadows — rendered through the WebGL2 canvas " +
            "with FreeType rasterization and HarfBuzz shaping running in WebAssembly.",
            15, Palette.TextDim, wrap: true));

        col.Children.Add(Label("Buttons", 13, Palette.TextDim));
        col.Children.Add(new RowView
        {
            Gap = 12,
            MainAxisAlignment = MainAxisAlignment.Start,
            CrossAxisAlignment = CrossAxisAlignment.Start,
            Children =
            {
                ActionButton("Primary", primary: true, width: 130).View,
                ActionButton("Secondary", primary: false, width: 130).View,
                UploadButton().View,
            },
        });

        col.Children.Add(Label("Color swatches", 13, Palette.TextDim));
        col.Children.Add(Swatches());

        col.Children.Add(Label("Cards", 13, Palette.TextDim));
        col.Children.Add(new RowView
        {
            Gap = 16,
            CrossAxisAlignment = CrossAxisAlignment.Start,
            Children =
            {
                Card("Shadows", "RectView with a soft box shadow and rounded corners."),
                Card("Output", "The card on the right mirrors file picker / drop results."),
            },
        });

        col.Children.Add(_outputLabel);

        return new RectView
        {
            BackgroundColor = Palette.PageBg,
            Children =
            {
                new PaddingView { Padding = PaddingStyle.All(24), Children = { _contentScroll } },
            },
        };
    }

    private MultiChildView BuildFooter() => new RectView
    {
        Height = 28,
        BackgroundColor = Palette.HeaderBg,
        Children =
        {
            new PaddingView
            {
                Padding = new PaddingStyle { Left = 16, Right = 16 },
                Children = { _statusLabel },
            },
        },
    };

    // ---- pieces ----

    private MultiChildView Swatches()
    {
        var row = new RowView { Gap = 10, CrossAxisAlignment = CrossAxisAlignment.Start };
        uint[] colors =
        {
            0xFF4C8DFF, 0xFF36C275, 0xFFE0B341, 0xFFE0556B, 0xFFB069E8, 0xFF45C7D6,
        };
        foreach (var c in colors)
        {
            row.Children.Add(new RectView
            {
                Width = 48,
                Height = 48,
                BackgroundColor = c,
                BorderRadius = BorderRadiusStyle.All(8),
            });
        }
        return row;
    }

    private MultiChildView Card(string title, string body) => new RectView
    {
        Width = 240,
        Height = 120,
        BackgroundColor = Palette.CardBg,
        BorderRadius = BorderRadiusStyle.All(12),
        BoxShadow = new BoxShadowStyle { OffsetY = -6, Blur = 24, Spread = 0, Color = 0x66000000 },
        Children =
        {
            new PaddingView
            {
                Padding = PaddingStyle.All(16),
                Children =
                {
                    new ColumnView
                    {
                        Gap = 8,
                        Children =
                        {
                            Label(title, 16, Palette.Text, bold: true),
                            Label(body, 13, Palette.TextDim, wrap: true),
                        },
                    },
                },
            },
        },
    };

    private DemoButton NavButton(string label, bool selected)
    {
        var btn = new DemoButton(label, height: 36, width: null,
            baseColor: selected ? Palette.Accent : Palette.SidebarBg,
            hoverColor: selected ? Palette.AccentHover : Palette.Subtle,
            pressColor: selected ? Palette.AccentPress : Palette.SubtlePress,
            textColor: selected ? Palette.White : Palette.Text,
            radius: 8, center: false)
        {
            OnClick = SelectNav,
        };
        _buttons.Add(btn);
        return btn;
    }

    private DemoButton ActionButton(string label, bool primary, float width)
    {
        var btn = new DemoButton(label, height: 40, width: width,
            baseColor: primary ? Palette.Accent : Palette.Subtle,
            hoverColor: primary ? Palette.AccentHover : Palette.SubtleHover,
            pressColor: primary ? Palette.AccentPress : Palette.SubtlePress,
            textColor: Palette.White, radius: 8, center: true)
        {
            OnClick = b => SetOutput($"Clicked “{label}” at {DescribeMouse()}."),
        };
        _buttons.Add(btn);
        return btn;
    }

    private DemoButton UploadButton()
    {
        var btn = new DemoButton("Upload files", height: 40, width: 150,
            baseColor: Palette.Accent, hoverColor: Palette.AccentHover, pressColor: Palette.AccentPress,
            textColor: Palette.White, radius: 8, center: true)
        {
            OnClick = _ => UploadRequested?.Invoke(),
        };
        _buttons.Add(btn);
        return btn;
    }

    private void SelectNav(DemoButton clicked)
    {
        if (_activeNav == clicked) return;
        _activeNav?.SetSelected(false, Palette.SidebarBg, Palette.Subtle, Palette.SubtlePress, Palette.Text);
        clicked.SetSelected(true, Palette.Accent, Palette.AccentHover, Palette.AccentPress, Palette.White);
        _activeNav = clicked;
        SetOutput($"Navigated to “{clicked.Label}”.");
    }

    // ---- per-frame interactivity ----

    public void Sync()
    {
        var (_, wheelY) = WebInput.TakeWheel();
        if (wheelY != 0f && WebInput.IsOver(_contentScroll.Position))
            _contentScroll.Scroll(wheelY);

        foreach (var b in _buttons)
            b.Sync();

        var target = HitTest(WebInput.MousePoint.X, WebInput.MousePoint.Y);
        var where = WebFileDrop.IsDragOver ? "drop to upload" : DescribeMouse();
        var hover = target is null ? "" : $"  •  over “{target.Label}”";
        _statusLabel.Text = $"pointer: {where}{hover}";
    }

    public void HandleClick(float x, float y)
    {
        var hit = HitTest(x, y);
        if (hit is not null)
            hit.OnClick?.Invoke(hit);
    }

    /// The button hit at a point, or null. Used for clicks and the status readout.
    public DemoButton? HitTest(float x, float y)
    {
        foreach (var b in _buttons)
        {
            var p = b.View.Position;
            if (x >= p.Left && x < p.Right && y >= p.Bottom && y < p.Top)
                return b;
        }
        return null;
    }

    public void SetOutput(string text) => _outputLabel.Text = text;

    private static string DescribeMouse()
    {
        if (!WebInput.MouseInside) return "outside";
        var p = WebInput.MousePoint;
        return $"x {p.X:0} y {p.Y:0}";
    }

    // ---- helpers ----

    private static TextView Label(string text, float size, uint color,
        bool bold = false, bool wrap = false, bool vCenter = false) => new()
    {
        Text = text,
        FontSize = size,
        TextColor = color,
        FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
        TextWrap = wrap ? TextWrap.Wrap : TextWrap.NoWrap,
        VerticalTextAlignment = vCenter ? TextAlignment.Center : TextAlignment.Start,
    };
}
