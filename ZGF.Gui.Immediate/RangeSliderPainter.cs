namespace ZGF.Gui.Immediate;

/// <summary>A part of a range slider the pointer can be on: the bar's start or end edge, which resize it, the bar
/// between them, which slides it, or the track outside it.</summary>
public enum RangeSliderPart
{
    None,
    Track,
    Start,
    Bar,
    End,
}

/// <summary>What a range slider's behavior found this frame, for its painter to draw: a track of whole steps from
/// <see cref="Min"/> to <see cref="Max"/>, and on it a bar from <see cref="Start"/> to <see cref="End"/>, both
/// included. The pointer and focus fields are false unless it is <see cref="Enabled"/>.</summary>
/// <param name="Id">The id the painter records the slider's element with, which makes it hit-testable.</param>
/// <param name="StartText">The start as it shows, at the bar's start.</param>
/// <param name="EndText">The end as it shows, at the bar's end.</param>
/// <param name="HoveredPart">The part under the pointer.</param>
/// <param name="PressedPart">The part being dragged, drawn pushed in.</param>
/// <param name="FocusVisible">It holds keyboard focus while <see cref="Ui.FocusVisible"/>, so it draws a focus ring.</param>
public readonly record struct RangeSliderState(int Id, int Min, int Max, int Start, int End, string StartText, string EndText,
    bool Enabled, RangeSliderPart HoveredPart, RangeSliderPart PressedPart, bool FocusVisible)
{
    /// <summary>Where the bar is across the track, as fractions of its width: from the left of
    /// <see cref="Start"/>'s step to the right of <see cref="End"/>'s.</summary>
    public (float Left, float Right) BarFractions
    {
        get
        {
            var steps = Max - Min + 1f;
            return ((Start - Min) / steps, (End - Min + 1f) / steps);
        }
    }
}

/// <summary>Records a range slider: its element, hit-testable as <see cref="RangeSliderState.Id"/>, as wide as its
/// layout gives it, and the track and bar drawn in it, the bar where <see cref="RangeSliderState.BarFractions"/>
/// puts it, which its behavior hit-tests against.</summary>
public delegate void RangeSliderPainter(Ui ui, in RangeSliderState state);
