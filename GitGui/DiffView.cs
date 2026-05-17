using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

internal static class DiffPalette
{
    // ~18% alpha blend of FileChangesPalette.StatusAdded over CommitsPalette.Background.
    public const uint LineAddedBg = 0xFF284534;
    // ~18% alpha blend of FileChangesPalette.StatusDeleted over CommitsPalette.Background.
    public const uint LineRemovedBg = 0xFF432528;

    public const uint LineAddedGlyphText = FileChangesPalette.StatusAdded;
    public const uint LineRemovedGlyphText = FileChangesPalette.StatusDeleted;
    public const uint LineContextGlyphText = FileChangesPalette.HeaderText;
    public const uint LineNumberText = 0xFF7A7C81;

    public const uint HunkSeparatorBg = FileChangesPalette.HeaderBg;
    public const uint HunkSeparatorRangeText = FileChangesPalette.HeaderText;
    public const uint HunkSeparatorContextText = CommitDetailsPalette.Secondary;

    public const uint BannerBg = HunkSeparatorBg;
    public const uint BannerText = HunkSeparatorContextText;

    public const uint LineText = CommitDetailsPalette.Primary;

    public const uint TruncatedFooterBg = HunkSeparatorBg;
    public const uint TruncatedFooterText = HunkSeparatorRangeText;
}

internal abstract record DiffViewModel
{
    public sealed record Placeholder(string Text) : DiffViewModel;
    public sealed record Loaded(DiffResult Result) : DiffViewModel;
}

/// <summary>
/// Diff panel shown below the file lists in Local Changes whenever exactly one file is
/// selected. <see cref="SetTarget"/> drives the load; the panel renders banners, hunk
/// separators, and per-line gutter+glyph+text rows for the resulting <see cref="DiffResult"/>.
/// </summary>
public sealed class DiffView : MultiChildView
{
    private const float GlyphColumnWidth = 18f;

    private IGitService? _gitService;
    private IRepoRegistry? _registry;
    private IUiDispatcher? _dispatcher;
    private IDisposable? _vmSubscription;

    private readonly State<DiffViewModel> _viewModel = new(
        new DiffViewModel.Placeholder("Select a file to view diff."));

    private string? _targetPath;
    private DiffSide _targetSide;
    private int _loadGeneration;

    private readonly ColumnView _content;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _vScrollBar;
    private readonly HorizontalScrollBarView _hScrollBar;

    public DiffView()
    {
        _content = new ColumnView { Gap = 0 };
        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(_content);
        _scrollPane.Behaviors.Add(new ScrollPaneWheelController(_scrollPane));

        _vScrollBar = ScrollBarStyles.CreateVertical();
        _hScrollBar = ScrollBarStyles.CreateHorizontal();

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            BorderColor = new BorderColorStyle { Top = CommitsPalette.Border },
            BorderSize = new BorderSizeStyle { Top = 1 },
            Children =
            {
                new BorderLayoutView
                {
                    Center = _scrollPane,
                    East = _vScrollBar,
                    South = _hScrollBar,
                },
            },
        });

        Behaviors.Add(new CommitDetailsScrollSyncController(_scrollPane, _vScrollBar, _hScrollBar));
    }

    protected override void OnAttachedToContext(Context context)
    {
        _gitService = context.Get<IGitService>();
        _registry = context.Get<IRepoRegistry>();
        _dispatcher = context.Get<IUiDispatcher>();
        _vmSubscription = _viewModel.Subscribe(Render);
        // Services were unavailable on prior SetTarget calls (e.g. before first attach);
        // now that they're here, kick off the load for whatever the current target is.
        StartLoad();
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _vmSubscription?.Dispose();
        _vmSubscription = null;
        _gitService = null;
        _registry = null;
        _dispatcher = null;
    }

    public void SetTarget(string? path, DiffSide side)
    {
        _targetPath = path;
        _targetSide = side;
        StartLoad();
    }

    private void StartLoad()
    {
        // Bumping unconditionally invalidates any in-flight load — whether we replace it
        // with a new one or fall back to a placeholder, the old result must not win.
        _loadGeneration++;

        if (_targetPath == null)
        {
            _viewModel.Value = new DiffViewModel.Placeholder("Select a file to view diff.");
            return;
        }

        if (_gitService == null || _registry == null) return;
        var repo = _registry.Active.Value;
        if (repo == null) return;

        var gen = _loadGeneration;
        var path = _targetPath;
        var side = _targetSide;
        _viewModel.Value = new DiffViewModel.Placeholder("Loading…");

        var service = _gitService;
        var dispatcher = _dispatcher;
        Task.Run(() =>
        {
            DiffViewModel result;
            try
            {
                var diff = service.GetDiff(repo, path, side);
                result = new DiffViewModel.Loaded(diff);
            }
            catch (Exception ex)
            {
                result = new DiffViewModel.Placeholder(ex.Message);
            }

            dispatcher?.Post(() =>
            {
                if (gen != _loadGeneration) return;
                _viewModel.Value = result;
            });
        });
    }

    private void Render(DiffViewModel vm)
    {
        switch (vm)
        {
            case DiffViewModel.Placeholder p:
                ShowPlaceholder(p.Text, CommitsPalette.Placeholder);
                break;
            case DiffViewModel.Loaded l:
                ShowDiff(l.Result);
                break;
        }
    }

    private void ShowPlaceholder(string text, uint color)
    {
        _content.Children.Clear();
        _content.Children.Add(new TextView
        {
            Text = text,
            TextColor = color,
            HorizontalTextAlignment = TextAlignment.Center,
        });
        _scrollPane.ScrollToOrigin();
    }

    private void ShowDiff(DiffResult result)
    {
        if (result.ErrorMessage != null)
        {
            ShowPlaceholder(result.ErrorMessage, CommitsPalette.WarningText);
            return;
        }
        if (result.IsBinary)
        {
            ShowPlaceholder("Binary file not shown", CommitsPalette.Placeholder);
            return;
        }
        if (result.Hunks.Count == 0 && !result.IsModeOnly && result.OldPath == null)
        {
            ShowPlaceholder("No textual changes", CommitsPalette.Placeholder);
            return;
        }

        _content.Children.Clear();

        if (result.OldPath != null)
            _content.Children.Add(BuildBannerStrip($"Renamed: {result.OldPath} → {result.Path}"));
        if (result.IsModeOnly)
            _content.Children.Add(BuildBannerStrip(
                $"Mode: {FormatMode(result.OldMode)} → {FormatMode(result.NewMode)}"));

        var gutterWidth = ComputeGutterWidth(result.Hunks);
        foreach (var hunk in result.Hunks)
        {
            _content.Children.Add(BuildHunkSeparator(hunk));
            foreach (var line in hunk.Lines)
                _content.Children.Add(BuildLineRow(line, gutterWidth));
        }

        if (result.Truncated)
        {
            var shown = 0;
            foreach (var h in result.Hunks) shown += h.Lines.Count;
            _content.Children.Add(BuildBannerStrip(
                $"Diff truncated — only the first {shown} lines are shown."));
        }

        _scrollPane.ScrollToOrigin();
    }

    private static View BuildBannerStrip(string text) => new RectView
    {
        BackgroundColor = DiffPalette.BannerBg,
        BorderColor = new BorderColorStyle
        {
            Top = FileChangesPalette.HeaderBorder,
            Bottom = FileChangesPalette.HeaderBorder,
        },
        BorderSize = new BorderSizeStyle { Top = 1, Bottom = 1 },
        Padding = new PaddingStyle { Left = 8, Right = 8, Top = 2, Bottom = 2 },
        Children =
        {
            new TextView
            {
                Text = text,
                TextColor = DiffPalette.BannerText,
                FontFamily = DiffOptions.MonoFontFamily,
            },
        },
    };

    private static View BuildHunkSeparator(DiffHunk hunk)
    {
        var range = $"@@ -{hunk.OldStart},{hunk.OldLines} +{hunk.NewStart},{hunk.NewLines} @@";
        var rangeText = new TextView
        {
            Text = range,
            TextColor = DiffPalette.HunkSeparatorRangeText,
            FontFamily = DiffOptions.MonoFontFamily,
        };

        var row = new FlexRowView
        {
            Gap = 12f,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { rangeText },
        };
        if (!string.IsNullOrEmpty(hunk.Header))
        {
            row.Children.Add(new FlexItem
            {
                Grow = 1,
                Child = new TextView
                {
                    Text = hunk.Header,
                    TextColor = DiffPalette.HunkSeparatorContextText,
                    FontFamily = DiffOptions.MonoFontFamily,
                },
            });
        }

        return new RectView
        {
            BackgroundColor = DiffPalette.HunkSeparatorBg,
            BorderColor = new BorderColorStyle
            {
                Top = FileChangesPalette.HeaderBorder,
                Bottom = FileChangesPalette.HeaderBorder,
            },
            BorderSize = new BorderSizeStyle { Top = 1, Bottom = 1 },
            Padding = new PaddingStyle { Left = 8, Right = 8, Top = 2, Bottom = 2 },
            Children = { row },
        };
    }

    private static View BuildLineRow(DiffLine line, float gutterWidth)
    {
        var (glyph, glyphColor) = line.Kind switch
        {
            DiffLineKind.Added => ("+", DiffPalette.LineAddedGlyphText),
            DiffLineKind.Removed => ("-", DiffPalette.LineRemovedGlyphText),
            _ => (" ", DiffPalette.LineContextGlyphText),
        };

        var bg = line.Kind switch
        {
            DiffLineKind.Added => DiffPalette.LineAddedBg,
            DiffLineKind.Removed => DiffPalette.LineRemovedBg,
            _ => CommitsPalette.Background,
        };

        var oldNumber = new TextView
        {
            Text = line.OldLineNumber?.ToString() ?? string.Empty,
            TextColor = DiffPalette.LineNumberText,
            FontFamily = DiffOptions.MonoFontFamily,
            HorizontalTextAlignment = TextAlignment.End,
            PreferredWidth = gutterWidth,
        };
        var newNumber = new TextView
        {
            Text = line.NewLineNumber?.ToString() ?? string.Empty,
            TextColor = DiffPalette.LineNumberText,
            FontFamily = DiffOptions.MonoFontFamily,
            HorizontalTextAlignment = TextAlignment.End,
            PreferredWidth = gutterWidth,
        };
        var glyphView = new TextView
        {
            Text = glyph,
            TextColor = glyphColor,
            FontFamily = DiffOptions.MonoFontFamily,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = GlyphColumnWidth,
        };
        var text = new TextView
        {
            Text = ExpandTabs(line.Text),
            TextColor = DiffPalette.LineText,
            FontFamily = DiffOptions.MonoFontFamily,
        };

        return new RectView
        {
            BackgroundColor = bg,
            Children =
            {
                new FlexRowView
                {
                    Gap = 4f,
                    CrossAxisAlignment = CrossAxisAlignment.Start,
                    Children =
                    {
                        oldNumber,
                        newNumber,
                        glyphView,
                        new FlexItem { Grow = 1, Child = text },
                    },
                },
            },
        };
    }

    // No metric access from layout-time code, so estimate from font size. The 0.6 multiplier
    // matches a typical mono advance ratio; padding gives breathing room on each side.
    private static float ComputeGutterWidth(IReadOnlyList<DiffHunk> hunks)
    {
        var maxOld = 0;
        var maxNew = 0;
        foreach (var h in hunks)
        {
            foreach (var l in h.Lines)
            {
                if (l.OldLineNumber is int o && o > maxOld) maxOld = o;
                if (l.NewLineNumber is int n && n > maxNew) maxNew = n;
            }
        }
        var digits = Math.Max(1, Math.Max(DigitCount(maxOld), DigitCount(maxNew)));
        const float assumedFontSize = 13f;
        return digits * assumedFontSize * 0.6f + 8f;
    }

    private static int DigitCount(int n)
    {
        if (n <= 0) return 1;
        var d = 0;
        while (n > 0) { d++; n /= 10; }
        return d;
    }

    private static string ExpandTabs(string s)
    {
        if (s.IndexOf('\t') < 0) return s;
        return s.Replace("\t", new string(' ', DiffOptions.TabWidth));
    }

    private static string FormatMode(int? mode)
        => mode is int m ? Convert.ToString(m, 8).PadLeft(6, '0') : "-";
}
