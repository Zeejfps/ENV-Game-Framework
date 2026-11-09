using Xunit.Abstractions;
using ZGF.Gui.Immediate.Sample;

namespace ZGF.Gui.Immediate.Scratch;

/// <summary>An example to copy: drive an app, then read scratch/&lt;name&gt;.txt and open the .png only when the
/// question is visual. Run it with <c>dotnet test ZGF.Gui.Immediate.Scratch --filter AddTodo</c>.</summary>
[Collection(GpuCollection.Name)]
public sealed class TodoExample(GpuDeviceFixture fixture, ITestOutputHelper output)
{
    [GpuFact]
    public void AddTodo()
    {
        var state = new TodoState { DarkMode = true };
        state.Add("Sketch the immediate-mode API");
        using var app = GpuFrames.Running(fixture.Device, new TodoApp(state), width: 520, height: 640, output: output.WriteLine);

        app.ClickOn(Selector.AtPath("draft"));
        app.Type("Write a scratch test");
        app.ClickOn(Selector.AtPath("add"));
        app.Settle();
        app.Look("todo-added");

        Assert.Contains(state.Items, item => item.Title == "Write a scratch test");
    }
}
