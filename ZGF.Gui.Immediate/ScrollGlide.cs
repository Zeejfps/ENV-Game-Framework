namespace ZGF.Gui.Immediate;

/// <summary>
/// A scroll gliding along one axis to where the wheel sent it, on a spring that carries its speed from
/// one turn of the wheel to the next. Anything else that moves the scroll, as a drag of its bar or
/// <see cref="Ui.ScrollTo"/>, stops it where it is.
/// </summary>
internal sealed class ScrollGlide
{
    private const float MaxStep = 1f / 240f;

    /// <summary>How near its target, in points, and how slow, in points a second, a glide may be and
    /// end there.</summary>
    private const float SettledGap = 0.1f;
    private const float SettledSpeed = 1f;

    private bool _gliding;
    private float _target;
    private float _velocity;
    private float _shown;

    /// <summary>Where the next turn of the wheel moves from: the target of a glide under way, so turns
    /// add up, or <paramref name="offset"/>.</summary>
    public float From(float offset) => _gliding ? _target : offset;

    public void Retarget(float offset, float target)
    {
        if (!_gliding)
        {
            _gliding = true;
            _shown = offset;
        }
        _target = target;
    }

    /// <summary>Stops the glide when the offset is no longer where it left it.</summary>
    public void StopIfMoved(float offset)
    {
        if (!_gliding || offset == _shown)
            return;
        _gliding = false;
        _velocity = 0f;
    }

    /// <summary>Moves <paramref name="offset"/> a frame along the glide, asking for the next frame until
    /// it arrives.</summary>
    public float Step(Ui ui, float offset, Spring spring)
    {
        if (!_gliding)
            return offset;
        var dt = ui.DeltaTime;
        var steps = Math.Max(1, (int)MathF.Ceiling(dt / MaxStep));
        var h = dt / steps;
        for (var i = 0; i < steps; i++)
        {
            var acceleration = (-spring.Stiffness * (offset - _target) - spring.Damping * _velocity) / spring.Mass;
            _velocity += acceleration * h;
            offset += _velocity * h;
        }
        if (MathF.Abs(offset - _target) <= SettledGap && MathF.Abs(_velocity) <= SettledSpeed)
        {
            offset = _target;
            _gliding = false;
            _velocity = 0f;
        }
        else
        {
            ui.RequestUpdate();
        }
        _shown = MathF.Max(0f, offset);
        return _shown;
    }
}
