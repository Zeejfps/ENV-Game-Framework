using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class ReferenceTreeTests
{
    [Fact]
    public void A_reference_of_a_reference_shown_is_revealed_in_the_tree_though_its_assembly_is_not_listed()
    {
        var app = AppDriver.Opened();
        var coreLib = app.Node("References", "System.Runtime", "System.Private.CoreLib");

        app.Show(coreLib, 2);
        app.Settle(2);

        Assert.False(coreLib.Assembly.IsListed);
        Assert.Same(coreLib, app.Ui.Tree.View.Selected);
        Assert.Same(coreLib, app.AppState.Tabs.Active!.Node);
    }
}
