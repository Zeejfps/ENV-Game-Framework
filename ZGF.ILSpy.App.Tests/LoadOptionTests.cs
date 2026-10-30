using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Preferences;

namespace ZGF.ILSpy.App.Tests;

public sealed class LoadOptionTests
{
    [Fact]
    public void Turning_off_loading_references_reads_the_open_assemblies_again_without_loading_what_they_reference()
    {
        var app = AppDriver.Opened();
        app.Settle(2);
        var before = app.Node().Assembly;

        SetDecompilerToggle(app.AppState, DecompilerToggle.AutoLoadAssemblyReferences, false);
        app.Run(2);
        app.Settle(2);
        var runtime = app.Node("References", "System.Runtime");

        Assert.NotSame(before, app.Node().Assembly);
        Assert.All(runtime.Children, c => Assert.Equal(NodeIcon.ReferencedTypes, c.Icon));
    }
}
