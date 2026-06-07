using System.Globalization;
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
/// </summary>
public sealed class NumberFieldView : MultiChildView, ITextInputClient
{
    private readonly TextView _label;
    private ITextInputService? _service;
    private Context? _context;

    private float _value;
    private string _buffer = string.Empty;
    private bool _editing;

    public NumberFieldView()
    {
        _label = new TextView
        {
            Text = string.Empty,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        Children.Add(_label);

        // Tap to focus: raise the keyboard and start routing keystrokes to this field.
        this.UsePointerController(_ => new ButtonPointerController(this) { Clicked = BeginEditing });
    }

    public float Min { get; set; }
    public float Max { get; set; } = 100f;
    public string Format { get; set; } = "0";
    public string Suffix { get; set; } = string.Empty;

    /// <summary>Which keyboard to present; also satisfies <see cref="ITextInputClient.Keyboard"/>.</summary>
    public TextInputKeyboard Keyboard { get; set; } = TextInputKeyboard.Number;

    public Action<float>? ValueChanged { get; set; }

    public uint TextColor { get; set; } = 0xFFF2F5FB;
    public float FontSize { get; set; } = 14f;

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
                UpdateLabel();
        }
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        _context = context;
        _service = context.Get<ITextInputService>();
        UpdateLabel();
    }

    protected override void OnDetachedFromContext(Context context)
    {
        base.OnDetachedFromContext(context);
        if (_editing)
            EndEditing();
        _service = null;
        _context = null;
    }

    private void BeginEditing()
    {
        if (_service == null)
            return;

        _editing = true;
        _buffer = _value.ToString(Format, CultureInfo.InvariantCulture);
        UpdateLabel();
        _service.BeginEdit(this);
        _context?.Get<KeyboardAvoidanceController>()?.Focus(this);
    }

    private void EndEditing()
    {
        if (!_editing)
            return;

        _editing = false;
        if (float.TryParse(_buffer, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            _value = Math.Clamp(v, Min, Max);
        UpdateLabel();
        ValueChanged?.Invoke(_value);
        _context?.Get<KeyboardAvoidanceController>()?.Blur(this);
    }

    // --- ITextInputClient ---------------------------------------------------------------------

    bool ITextInputClient.HasText => _buffer.Length > 0;

    void ITextInputClient.InsertText(string text)
    {
        _buffer += text;
        AfterEdit();
    }

    void ITextInputClient.DeleteBackward()
    {
        if (_buffer.Length > 0)
            _buffer = _buffer[..^1];
        AfterEdit();
    }

    void ITextInputClient.OnEditingEnded() => EndEditing();

    private void AfterEdit()
    {
        UpdateLabel();
        // Live preview: clamp so a paired graph stays in range, but keep showing the raw keystrokes.
        if (float.TryParse(_buffer, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
        {
            _value = Math.Clamp(v, Min, Max);
            ValueChanged?.Invoke(_value);
        }
    }

    // --- Rendering ----------------------------------------------------------------------------

    private void UpdateLabel()
    {
        _label.TextColor = TextColor;
        _label.FontSize = FontSize;
        _label.Text = _editing
            ? _buffer + "|"
            : _value.ToString(Format, CultureInfo.InvariantCulture) + Suffix;
    }

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
