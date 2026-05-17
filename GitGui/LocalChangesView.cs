using ZGF.Geometry;
using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

internal abstract record LocalChangesViewModel
{
    public sealed record Placeholder(string Text) : LocalChangesViewModel;
    public sealed record Loaded(LocalChangesSnapshot Snapshot) : LocalChangesViewModel;
}

public sealed class LocalChangesView : MultiChildView
{
    private const int CommitBarPadding = 10;
    private const float CommitButtonWidth = 120f;
    private const float DescriptionMinHeight = 60f;
    private const float DescriptionMaxHeight = 240f;

    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IUiDispatcher? _dispatcher;
    private IDisposable? _activeSubscription;
    private IDisposable? _vmSubscription;

    private readonly State<LocalChangesViewModel> _viewModel = new(
        new LocalChangesViewModel.Placeholder("Open a repository to see local changes."));

    private int _loadGeneration;

    private readonly LocalChangesPanel _unstagedPanel;
    private readonly LocalChangesPanel _stagedPanel;
    private readonly TextView _placeholder;
    private readonly RectView _centerContainer;
    private readonly LocalChangesSplitView _splitView;

    public LocalChangesView()
    {
        _unstagedPanel = new LocalChangesPanel("Unstaged", "No unstaged changes.");
        _stagedPanel = new LocalChangesPanel("Staged", "No staged changes.");

        _placeholder = new TextView
        {
            TextColor = CommitsPalette.Placeholder,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        _splitView = new LocalChangesSplitView(_unstagedPanel, _stagedPanel);

        _centerContainer = new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children = { _splitView },
        };

        AddChildToSelf(new RectView
        {
            BackgroundColor = CommitsPalette.Background,
            Children =
            {
                new BorderLayoutView
                {
                    Center = _centerContainer,
                    South = BuildCommitBar(),
                },
            },
        });
    }

    private View BuildCommitBar()
    {
        var titleInput = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextWrap = TextWrap.NoWrap,
        };
        titleInput.Behaviors.Add(new TextInputViewKbmController(titleInput));

        // No PreferredHeight — let the box size to one line of text plus padding/border.
        // The input itself reports MeasureHeight = lineHeight (single line, NoWrap), and the
        // RectView adds its own padding+border on top.
        var titleBox = new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 6, Right = 6, Top = 4, Bottom = 4 },
            Children = { titleInput },
        };

        var descriptionField = new GrowingDescriptionField(DescriptionMinHeight, DescriptionMaxHeight);

        var commitButton = new DialogButton("Commit", OnCommitClicked)
        {
            PreferredWidth = CommitButtonWidth,
            PreferredHeight = 28,
        };

        var buttonRow = new FlexRowView
        {
            MainAxisAlignment = MainAxisAlignment.End,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { commitButton },
        };

        return new RectView
        {
            BackgroundColor = CommitsPalette.HeaderBg,
            BorderColor = new BorderColorStyle { Top = CommitsPalette.Border },
            BorderSize = new BorderSizeStyle { Top = 1 },
            Padding = new PaddingStyle
            {
                Left = CommitBarPadding,
                Right = CommitBarPadding,
                Top = CommitBarPadding,
                Bottom = CommitBarPadding,
            },
            Children =
            {
                new ColumnView
                {
                    Gap = 8,
                    Children = { titleBox, descriptionField, buttonRow },
                },
            },
        };
    }

    private void OnCommitClicked()
    {
        // UI-only for now.
    }

    protected override void OnAttachedToContext(Context context)
    {
        _registry = context.Get<IRepoRegistry>();
        _gitService = context.Get<IGitService>();
        _dispatcher = context.Get<IUiDispatcher>();
        _vmSubscription = _viewModel.Subscribe(Render);
        if (_registry != null)
            _activeSubscription = _registry.Active.Subscribe(_ => StartLoadForActiveRepo());
    }

    protected override void OnDetachedFromContext(Context context)
    {
        // Bump the generation so any in-flight worker's dispatcher.Post becomes a no-op.
        _loadGeneration++;
        _activeSubscription?.Dispose();
        _activeSubscription = null;
        _vmSubscription?.Dispose();
        _vmSubscription = null;
        _registry = null;
        _gitService = null;
        _dispatcher = null;
    }

    private void StartLoadForActiveRepo()
    {
        if (_registry == null || _gitService == null) return;
        var active = _registry.Active.Value;

        _loadGeneration++;
        var gen = _loadGeneration;

        if (active == null)
        {
            _viewModel.Value = new LocalChangesViewModel.Placeholder("Open a repository to see local changes.");
            return;
        }

        _viewModel.Value = new LocalChangesViewModel.Placeholder("Loading…");

        var repo = active;
        var service = _gitService;
        var dispatcher = _dispatcher;
        Task.Run(() =>
        {
            LocalChangesViewModel result;
            try
            {
                var snap = service.GetLocalChanges(repo);
                result = snap.ErrorMessage != null
                    ? new LocalChangesViewModel.Placeholder(snap.ErrorMessage)
                    : new LocalChangesViewModel.Loaded(snap);
            }
            catch (Exception ex)
            {
                result = new LocalChangesViewModel.Placeholder(ex.Message);
            }

            dispatcher?.Post(() =>
            {
                if (gen != _loadGeneration) return;
                _viewModel.Value = result;
            });
        });
    }

    private void Render(LocalChangesViewModel vm)
    {
        switch (vm)
        {
            case LocalChangesViewModel.Placeholder p:
                ShowPlaceholder(p.Text);
                break;
            case LocalChangesViewModel.Loaded l:
                ShowSnapshot(l.Snapshot);
                break;
        }
    }

    private void ShowPlaceholder(string text)
    {
        _placeholder.Text = text;
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_placeholder);
    }

    private void ShowSnapshot(LocalChangesSnapshot snap)
    {
        _unstagedPanel.SetFiles(snap.Unstaged);
        _stagedPanel.SetFiles(snap.Staged);
        _centerContainer.Children.Clear();
        _centerContainer.Children.Add(_splitView);
    }
}

internal sealed class LocalChangesSplitView : MultiChildView
{
    private const float DividerThickness = 1f;
    private const float DividerHitWidth = 6f;
    private const float MinPanelWidth = 200f;

    private readonly LocalChangesPanel _left;
    private readonly LocalChangesPanel _right;
    private float _leftRatio = 0.5f;
    private bool _dividerHovered;

    public LocalChangesSplitView(LocalChangesPanel left, LocalChangesPanel right)
    {
        _left = left;
        _right = right;
        AddChildToSelf(_left);
        AddChildToSelf(_right);
        Behaviors.Add(new LocalChangesSplitViewController(this));
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        if (pos.Width <= 0f) return;

        var leftWidth = ClampLeftWidth(pos.Width * _leftRatio, pos.Width);
        // Persist the clamped ratio so subsequent layouts stay consistent.
        _leftRatio = leftWidth / pos.Width;
        var rightWidth = pos.Width - leftWidth;

        _left.LeftConstraint = pos.Left;
        _left.BottomConstraint = pos.Bottom;
        _left.MinWidthConstraint = leftWidth;
        _left.MaxWidthConstraint = leftWidth;
        _left.MaxHeightConstraint = pos.Height;
        _left.LayoutSelf();

        _right.LeftConstraint = pos.Left + leftWidth;
        _right.BottomConstraint = pos.Bottom;
        _right.MinWidthConstraint = rightWidth;
        _right.MaxWidthConstraint = rightWidth;
        _right.MaxHeightConstraint = pos.Height;
        _right.LayoutSelf();
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        var pos = Position;
        if (pos.Width <= 0f) return;

        var leftWidth = ClampLeftWidth(pos.Width * _leftRatio, pos.Width);
        var dividerX = pos.Left + leftWidth;
        var z = GetDrawZIndex();

        // Always paint the thin static line so the panels look separated.
        c.DrawRect(new DrawRectInputs
        {
            Position = new RectF(dividerX - DividerThickness * 0.5f, pos.Bottom, DividerThickness, pos.Height),
            Style = new RectStyle
            {
                BackgroundColor = _dividerHovered ? CommitsPalette.DividerHoverLine : CommitsPalette.Border,
            },
            ZIndex = z + 1000,
        });

        if (_dividerHovered)
        {
            c.DrawRect(new DrawRectInputs
            {
                Position = new RectF(dividerX - DividerHitWidth * 0.5f, pos.Bottom, DividerHitWidth, pos.Height),
                Style = new RectStyle { BackgroundColor = CommitsPalette.DividerHoverBg },
                ZIndex = z + 999,
            });
        }
    }

    internal bool HitTestDivider(PointF point)
    {
        var pos = Position;
        if (pos.Width <= 0f) return false;
        if (point.Y < pos.Bottom || point.Y > pos.Top) return false;
        var leftWidth = ClampLeftWidth(pos.Width * _leftRatio, pos.Width);
        var dividerX = pos.Left + leftWidth;
        return Math.Abs(point.X - dividerX) <= DividerHitWidth * 0.5f;
    }

    internal void SetDividerHovered(bool hovered)
    {
        if (_dividerHovered == hovered) return;
        _dividerHovered = hovered;
        SetDirty();
    }

    internal void DragDivider(float mouseDeltaX)
    {
        var pos = Position;
        if (pos.Width <= 0f) return;
        var leftWidth = ClampLeftWidth(pos.Width * _leftRatio, pos.Width);
        var newLeftWidth = ClampLeftWidth(leftWidth + mouseDeltaX, pos.Width);
        var newRatio = newLeftWidth / pos.Width;
        if (Math.Abs(newRatio - _leftRatio) < 0.0001f) return;
        _leftRatio = newRatio;
        SetDirty();
    }

    private static float ClampLeftWidth(float desired, float total)
    {
        var minLeft = MinPanelWidth;
        var maxLeft = total - MinPanelWidth;
        if (maxLeft < minLeft)
        {
            // Not enough room for both minimums; just split evenly.
            return total * 0.5f;
        }
        return Math.Clamp(desired, minLeft, maxLeft);
    }
}

internal sealed class LocalChangesSplitViewController : KeyboardMouseController
{
    private readonly LocalChangesSplitView _view;
    private bool _dragging;
    private float _lastDragX;

    public LocalChangesSplitViewController(LocalChangesSplitView view)
    {
        _view = view;
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button != MouseButton.Left) return;

        if (e.State == InputState.Pressed)
        {
            if (_view.HitTestDivider(e.Mouse.Point))
            {
                _dragging = true;
                _lastDragX = e.Mouse.Point.X;
                _view.Context?.Get<InputSystem>()?.RequestFocus(this);
                e.Consume();
            }
            return;
        }

        if (e.State == InputState.Released && _dragging)
        {
            _dragging = false;
            _view.Context?.Get<InputSystem>()?.Blur(this);
            e.Consume();
        }
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (_dragging)
        {
            var dx = e.Mouse.Point.X - _lastDragX;
            _lastDragX = e.Mouse.Point.X;
            _view.DragDivider(dx);
            e.Consume();
            return;
        }
        _view.SetDividerHovered(_view.HitTestDivider(e.Mouse.Point));
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (!_dragging)
            _view.SetDividerHovered(false);
    }
}

internal sealed class LocalChangesPanel : MultiChildView
{
    private const int ContentPadding = 10;
    private const int HeaderPadding = 4;
    private const int RowGap = 2;

    private readonly string _title;
    private readonly TextView _headerText;
    private readonly ColumnView _rows;
    private readonly TextView _emptyPlaceholder;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _scrollBar;

    public LocalChangesPanel(string title, string emptyText)
    {
        _title = title;

        _headerText = new TextView
        {
            Text = FormatHeader(0),
            TextColor = FileChangesPalette.HeaderText,
        };
        _rows = new ColumnView { Gap = RowGap };
        _emptyPlaceholder = new TextView
        {
            Text = emptyText,
            TextColor = FileChangesPalette.HeaderText,
        };
        _rows.Children.Add(_emptyPlaceholder);

        var headerBar = new RectView
        {
            BackgroundColor = FileChangesPalette.HeaderBg,
            BorderColor = new BorderColorStyle
            {
                Top = FileChangesPalette.HeaderBorder,
                Bottom = FileChangesPalette.HeaderBorder,
            },
            BorderSize = new BorderSizeStyle { Top = 1, Bottom = 1 },
            Padding = new PaddingStyle
            {
                Left = HeaderPadding,
                Right = HeaderPadding,
                Top = HeaderPadding,
                Bottom = HeaderPadding,
            },
            Children = { _headerText },
        };

        var paddedRows = new PaddingView
        {
            Padding = new PaddingStyle
            {
                Left = ContentPadding,
                Right = ContentPadding,
                Top = ContentPadding,
                Bottom = ContentPadding,
            },
            Children = { _rows },
        };

        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(paddedRows);
        _scrollPane.Behaviors.Add(new ScrollPaneWheelController(_scrollPane));

        _scrollBar = new VerticalScrollBarView
        {
            TrackBackgroundColor = CommitsPalette.ScrollTrackBg,
            TrackBorderColor = new BorderColorStyle
            {
                Left = CommitsPalette.ScrollTrackBorder,
                Top = CommitsPalette.ScrollTrackBorder,
                Right = CommitsPalette.ScrollTrackBorder,
                Bottom = CommitsPalette.ScrollTrackBorder,
            },
            TrackBorderSize = new BorderSizeStyle { Left = 1 },
        };
        _scrollBar.Thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        _scrollBar.Thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        _scrollBar.Thumb.BorderColor = new BorderColorStyle
        {
            Left = CommitsPalette.ScrollThumbBorder,
            Top = CommitsPalette.ScrollThumbBorder,
            Right = CommitsPalette.ScrollThumbBorder,
            Bottom = CommitsPalette.ScrollThumbBorder,
        };
        _scrollBar.Thumb.BorderSize = BorderSizeStyle.All(1);
        _scrollBar.Behaviors.Add(new VerticalScrollBarViewController(_scrollBar));

        AddChildToSelf(new BorderLayoutView
        {
            North = headerBar,
            Center = _scrollPane,
            East = _scrollBar,
        });

        Behaviors.Add(new LocalChangesScrollSyncController(_scrollPane, _scrollBar));
    }

    public void SetFiles(IReadOnlyList<FileChange> files)
    {
        _headerText.Text = FormatHeader(files.Count);
        _rows.Children.Clear();
        if (files.Count == 0)
        {
            _rows.Children.Add(_emptyPlaceholder);
        }
        else
        {
            foreach (var file in files)
                _rows.Children.Add(new FileChangeRowView(file));
        }
        _scrollPane.ScrollToOrigin();
    }

    private string FormatHeader(int count) => $"{_title} ({count})";
}

internal sealed class LocalChangesScrollSyncController : KeyboardMouseController
{
    private const float ScrollBarThickness = 12f;

    private readonly ScrollPane _pane;
    private readonly VerticalScrollBarView _vScrollBar;

    public LocalChangesScrollSyncController(ScrollPane pane, VerticalScrollBarView vScrollBar)
    {
        _pane = pane;
        _vScrollBar = vScrollBar;
    }

    protected override void OnAttachedToContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged += OnPaneVerticalScroll;
        _vScrollBar.ScrollPositionChanged += OnVScrollBarScroll;
    }

    protected override void OnDetachedFromContext(View view, Context context)
    {
        _pane.VerticalScrollPositionChanged -= OnPaneVerticalScroll;
        _vScrollBar.ScrollPositionChanged -= OnVScrollBarScroll;
    }

    private void OnPaneVerticalScroll(float normalized)
    {
        _vScrollBar.PreferredWidth = _pane.VerticalScale < 1f ? ScrollBarThickness : 0f;
        _vScrollBar.Scale = _pane.VerticalScale;
        _vScrollBar.SetNormalizedScrollPosition(normalized);
    }

    private void OnVScrollBarScroll(float normalized)
    {
        _pane.SetVerticalNormalizedScrollPosition(normalized);
    }
}

/// <summary>
/// A multi-line text input that auto-grows with its content between <c>min</c> and <c>max</c>.
/// Once content exceeds <c>max</c>, the field caps at that height and a vertical scroll bar
/// is shown so the rest is reachable by scrolling.
///
/// The desired height is recomputed in <see cref="OnLayoutChildren"/> (after the inner input
/// has been laid out — at which point its width is known and its <c>MeasureHeight</c> is
/// reliable) and stored as a <c>PreferredHeight</c>. The next layout pass reads that as the
/// desired size. This avoids the "measure before width is known" problem that would otherwise
/// make the field report a runaway height (every char treated as its own wrapped line).
/// </summary>
internal sealed class GrowingDescriptionField : MultiChildView
{
    private const float BoxBorderThickness = 1f;
    private const float BoxPaddingHorizontal = 6f;
    private const float BoxPaddingVertical = 4f;

    private readonly float _minHeight;
    private readonly float _maxHeight;

    private readonly TextInputView _input;
    private readonly ScrollPane _scrollPane;
    private readonly VerticalScrollBarView _scrollBar;

    public GrowingDescriptionField(float minHeight, float maxHeight)
    {
        _minHeight = minHeight;
        _maxHeight = maxHeight;

        _input = new TextInputView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            TextColor = DialogPalette.TitleText,
            CaretColor = DialogPalette.TitleText,
            SelectionRectColor = DialogPalette.RowActive,
            TextVerticalAlignment = TextAlignment.Start,
            TextWrap = TextWrap.Wrap,
        };
        _input.Behaviors.Add(new TextInputViewKbmController(_input) { IsMultiLine = true });

        _scrollPane = new ScrollPane();
        _scrollPane.Children.Add(_input);
        _scrollPane.Behaviors.Add(new ScrollPaneWheelController(_scrollPane));

        _scrollBar = new VerticalScrollBarView
        {
            TrackBackgroundColor = CommitsPalette.ScrollTrackBg,
            TrackBorderColor = new BorderColorStyle
            {
                Left = CommitsPalette.ScrollTrackBorder,
                Top = CommitsPalette.ScrollTrackBorder,
                Right = CommitsPalette.ScrollTrackBorder,
                Bottom = CommitsPalette.ScrollTrackBorder,
            },
            TrackBorderSize = new BorderSizeStyle { Left = 1 },
        };
        _scrollBar.Thumb.IdleBackgroundColor = CommitsPalette.ScrollThumbBg;
        _scrollBar.Thumb.HoveredBackgroundColor = CommitsPalette.ScrollThumbHoverBg;
        _scrollBar.Thumb.BorderColor = new BorderColorStyle
        {
            Left = CommitsPalette.ScrollThumbBorder,
            Top = CommitsPalette.ScrollThumbBorder,
            Right = CommitsPalette.ScrollThumbBorder,
            Bottom = CommitsPalette.ScrollThumbBorder,
        };
        _scrollBar.Thumb.BorderSize = BorderSizeStyle.All(1);
        _scrollBar.Behaviors.Add(new VerticalScrollBarViewController(_scrollBar));

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.ButtonNormal,
            BorderColor = BorderColorStyle.All(DialogPalette.ButtonBorder),
            BorderSize = BorderSizeStyle.All((int)BoxBorderThickness),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle
            {
                Left = (int)BoxPaddingHorizontal,
                Right = (int)BoxPaddingHorizontal,
                Top = (int)BoxPaddingVertical,
                Bottom = (int)BoxPaddingVertical,
            },
            Children =
            {
                new BorderLayoutView
                {
                    Center = _scrollPane,
                    East = _scrollBar,
                },
            },
        });

        Behaviors.Add(new LocalChangesScrollSyncController(_scrollPane, _scrollBar));

        // Start at the min size; the first OnLayoutChildren pass will refine this.
        PreferredHeight = _minHeight;
    }

    protected override void OnLayoutChildren()
    {
        base.OnLayoutChildren();

        // Now that the input has been laid out, its MaxWidthConstraint reflects the actual
        // viewport width — so its MeasureHeight is reliable. Cache the clamped desired height
        // as PreferredHeight; the next layout pass will pick it up.
        var chrome = 2f * (BoxBorderThickness + BoxPaddingVertical);
        var contentHeight = _input.MeasureHeight();
        var desired = Math.Clamp(contentHeight + chrome, _minHeight, _maxHeight);
        if (Math.Abs(desired - (float)PreferredHeight) > 0.5f)
        {
            // Setting PreferredHeight via SetField marks us IsSelfDirty, so the next frame's
            // layout re-runs OnLayoutSelf with the new value.
            PreferredHeight = desired;
        }
    }
}
