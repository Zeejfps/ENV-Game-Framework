using System.Globalization;
using ZGF.Geometry;
using ZGF.Gui.Mobile.Controllers;
using ZGF.Gui.Mobile.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Mobile.Controls;

/// <summary>
/// A tappable numeric field: shows a formatted value as a small chip, and on tap raises the system
/// keyboard (via <see cref="ITextInputService"/>) so the value can be typed. Edits update
/// <see cref="Value"/> live and fire <see cref="ValueChanged"/>, so a paired graph or slider can
/// follow each keystroke; on dismissal the value is clamped to <see cref="Min"/>/<see cref="Max"/>
/// and reformatted. The reusable text-input parallel to <see cref="SliderView"/>.
///
/// Text editing (buffer, caret, selection, select-all) is delegated to the shared
/// <see cref="TextInputView"/>; this control only adds numeric formatting, clamping, the chip
/// chrome, and the bridge from the platform keyboard (<see cref="ITextInputClient"/>).
/// </summary>
public sealed class NumberFieldView : MultiChildView, ITextInputClient
{
    private const float PaddingX = 8f;

    private readonly TextInputView _input;
    private ITextInputService? _service;
    private MobileInputSystem? _inputSystem;
    private Context? _context;

    private float _value;
    private bool _editing;
    private bool _syncing;   // suppresses the TextValue feedback while we set the text ourselves

    public NumberFieldView()
    {
        _input = new TextInputView
        {
            BackgroundColor = 0,   // transparent; the chip itself is drawn by this view
            TextColor = 0xFFF2F5FB,
            CaretColor = 0xFFF2F5FB,
            FontSize = 14f,
            TextVerticalAlignment = TextAlignment.Center,
        };
        Children.Add(_input);

        // Live preview: each keystroke mutates TextInputView's buffer, which we parse and clamp.
        _input.TextValue.Subscribe(OnInputTextChanged);

        // Tap to focus (and select all); a tap while already editing repositions the caret instead.
        this.UsePointerController(_ => new ButtonPointerController(this) { Clicked = OnTapped });
    }

    public float Min { get; set; }
    public float Max { get; set; } = 100f;
    public string Format { get; set; } = "0";
    public string Suffix { get; set; } = string.Empty;

    /// <summary>Which keyboard to present; also satisfies <see cref="ITextInputClient.Keyboard"/>.</summary>
    public TextInputKeyboard Keyboard { get; set; } = TextInputKeyboard.Number;

    public Action<float>? ValueChanged { get; set; }

    public uint TextColor
    {
        get => _input.TextColor;
        set { _input.TextColor = value; _input.CaretColor = value; }
    }

    public float FontSize
    {
        get => _input.FontSize;
        set => _input.FontSize = value;
    }

    // A faint chip at rest so the number reads as tappable; it brightens while editing.
    public uint IdleColor { get; set; } = 0x16202A3A;
    public uint ActiveColor { get; set; } = 0x334C8DFF;
    public float CornerRadius { get; set; } = 8f;

    /// <summary>
    /// The committed numeric value. Setting it updates the display but does not fire
    /// <see cref="ValueChanged"/> (so a host can push the value in without a feedback loop); only
    /// user edits raise the event.
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, Min, Max);
            if (!_editing)
                ShowFormatted();
        }
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        _context = context;
        _service = context.Get<ITextInputService>();
        _inputSystem = context.Get<MobileInputSystem>();
        ShowFormatted();
    }

    protected override void OnDetachedFromContext(Context context)
    {
        base.OnDetachedFromContext(context);
        if (_editing)
            EndEditing();
        _service = null;
        _context = null;
    }

    // Inset the editable text from the chip edges; the chip background fills the whole view.
    protected override void OnLayoutChild(in RectF position, View child)
    {
        child.LeftConstraint = position.Left + PaddingX;
        child.BottomConstraint = position.Bottom;
        child.WidthConstraint = position.Width - PaddingX * 2f;
        child.HeightConstraint = position.Height;
        child.LayoutSelf();
    }

    private void OnTapped()
    {
        if (!_editing)
        {
            BeginEditing();
            return;
        }

        // Already editing: a second tap drops the selection and moves the caret to the tap point,
        // like a native field (so you can tap to deselect / reposition instead of re-selecting all).
        if (_inputSystem != null)
            _input.MoveCaretTo(_inputSystem.Pointer.Point);
    }

    private void BeginEditing()
    {
        if (_service == null)
            return;

        _editing = true;
        SetTextSilently(_value.ToString(Format, CultureInfo.InvariantCulture));   // raw, no suffix
        _input.StartEditing();
        _input.SelectAll();   // first keystroke replaces, like a native field
        _service.BeginEdit(this);
        _context?.Get<KeyboardAvoidanceController>()?.Focus(this);
        SetDirty();           // chip switches to the active color
    }

    private void EndEditing()
    {
        if (!_editing)
            return;

        _editing = false;
        _input.StopEditing();
        if (float.TryParse(_input.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            _value = Math.Clamp(v, Min, Max);
        ShowFormatted();
        ValueChanged?.Invoke(_value);
        _context?.Get<KeyboardAvoidanceController>()?.Blur(this);
        SetDirty();
    }

    // --- ITextInputClient ---------------------------------------------------------------------

    bool ITextInputClient.HasText => !_input.Text.IsEmpty;

    void ITextInputClient.InsertText(string text) => _input.Enter(text);

    void ITextInputClient.DeleteBackward() => _input.Delete();

    void ITextInputClient.OnEditingEnded() => EndEditing();

    private void OnInputTextChanged(string text)
    {
        // Ignore our own programmatic SetText and any change while not editing; only user keystrokes
        // drive the live, clamped value.
        if (_syncing || !_editing)
            return;
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
        {
            _value = Math.Clamp(v, Min, Max);
            ValueChanged?.Invoke(_value);
        }
    }

    private void ShowFormatted() =>
        SetTextSilently(_value.ToString(Format, CultureInfo.InvariantCulture) + Suffix);

    private void SetTextSilently(string text)
    {
        _syncing = true;
        _input.SetText(text);
        _syncing = false;
    }

    // --- Rendering ----------------------------------------------------------------------------

    protected override void OnDrawSelf(ICanvas c)
    {
        var bg = _editing ? ActiveColor : IdleColor;
        if ((bg >> 24) == 0)
            return;

        c.DrawRect(new DrawRectInputs
        {
            Position = Position,
            Style = new RectStyle { BackgroundColor = bg, BorderRadius = BorderRadiusStyle.All(CornerRadius) },
            ZIndex = GetDrawZIndex(),
        });
    }
}
