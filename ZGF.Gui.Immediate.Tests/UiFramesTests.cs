using ZGF.Gui.Immediate.Testing;

namespace ZGF.Gui.Immediate.Tests;

public sealed class UiFramesTests
{
    [Fact]
    public void Frames_can_only_be_made_with_the_state_they_carry()
    {
        var type = typeof(UiFrames<>);
        var state = type.GetGenericArguments()[0];

        var constructors = type.GetConstructors();

        Assert.NotEmpty(constructors);
        Assert.All(constructors, c => Assert.Equal(state, c.GetParameters()[0].ParameterType));
        Assert.All(constructors, c => Assert.False(c.GetParameters()[0].HasDefaultValue));
    }
}
