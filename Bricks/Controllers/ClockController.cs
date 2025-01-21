namespace Bricks.Controllers;

public sealed class ClockController
{
    private StopwatchClock Clock { get; }
    private IKeyboard Keyboard { get; }

    public ClockController(StopwatchClock clock, IKeyboard keyboard)
    {
        Clock = clock;
        Keyboard = keyboard;
    }

    public void Update()
    {
        var clock = Clock;
        // if (Keyboard.WasKeyPressedThisFrame(KeyCode.P))
        // {
        //     if (clock.IsRunning)
        //         clock.Stop();
        //     else
        //         clock.Start();
        // }
    
        if (Keyboard.WasKeyPressedThisFrame(KeyCode.L))
        {
            clock.StepForward();
        }
    }
}