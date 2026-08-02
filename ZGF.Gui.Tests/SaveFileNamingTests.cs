using ZGF.Gui.Desktop;

namespace ZGF.Gui.Tests;

/// <summary>
/// The three save dialogs disagree about extensions — Windows appends its default, macOS appends
/// nothing, zenity depends on the desktop — so every implementation routes its result through
/// <c>SaveFileNaming</c>. These pin what "the same keystrokes give the same path" means.
/// </summary>
public sealed class SaveFileNamingTests
{
    [Fact]
    public void ANameTypedWithoutAnExtensionGetsTheSuggestedOne()
    {
        Assert.Equal(
            "/plans/kitchen.plankbench",
            SaveFileNaming.ApplyDefaultExtension("/plans/kitchen", "plan.plankbench"));
    }

    [Fact]
    public void ANameTypedWithAnExtensionIsLeftAlone()
    {
        // Typing ".txt" is a decision, not an omission.
        Assert.Equal(
            "/plans/kitchen.txt",
            SaveFileNaming.ApplyDefaultExtension("/plans/kitchen.txt", "plan.plankbench"));
    }

    [Fact]
    public void TheSuggestedExtensionIsReappliedWhenItIsAlreadyRight()
    {
        Assert.Equal(
            "/plans/kitchen.plankbench",
            SaveFileNaming.ApplyDefaultExtension("/plans/kitchen.plankbench", "plan.plankbench"));
    }

    [Fact]
    public void ATrailingDotMeansTheUserWantsNoExtension()
    {
        // Path.HasExtension is false for a trailing dot, so without a special case this would
        // become "kitchen..plankbench".
        Assert.Equal(
            "/plans/kitchen.",
            SaveFileNaming.ApplyDefaultExtension("/plans/kitchen.", "plan.plankbench"));
    }

    [Fact]
    public void NoSuggestionMeansNothingIsAppended()
    {
        Assert.Equal("/plans/kitchen", SaveFileNaming.ApplyDefaultExtension("/plans/kitchen", null));
        Assert.Equal("/plans/kitchen", SaveFileNaming.ApplyDefaultExtension("/plans/kitchen", ""));
    }

    [Fact]
    public void ASuggestionWithoutAnExtensionAppendsNothing()
    {
        Assert.Equal("/plans/kitchen", SaveFileNaming.ApplyDefaultExtension("/plans/kitchen", "plan"));
    }

    [Fact]
    public void ACancelledDialogStaysCancelled()
    {
        // The implementations pass "" through on cancel; it must not become ".plankbench".
        Assert.Equal("", SaveFileNaming.ApplyDefaultExtension("", "plan.plankbench"));
    }

    [Fact]
    public void ADottedFolderDoesNotCountAsTheFileHavingAnExtension()
    {
        // Path.GetExtension looks only past the last separator, so the folder's dot is ignored
        // and the file still gets its extension.
        Assert.Equal(
            "/my.plans/kitchen.plankbench",
            SaveFileNaming.ApplyDefaultExtension("/my.plans/kitchen", "plan.plankbench"));
    }
}
