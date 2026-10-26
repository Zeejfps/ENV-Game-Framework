namespace ZGF.ILSpy;

/// <summary>What the settings keep of the screen: taking it when it changed, to be saved with the app state's settings.</summary>
public static class ScreenSettings
{
    /// <summary>What the settings keep of the screen, when that changed since it was last taken; else null.</summary>
    public static ViewSettings? TakeChangedViewSettings(UiState uiState)
    {
        var sources = CaptureViewSources(uiState);
        if (!uiState.KeptView.HasChanged(sources))
            return null;
        return TakeViewSettings(uiState);
    }

    /// <summary>What the settings keep of the screen as it is now, changed or not, as before the last save.</summary>
    public static ViewSettings TakeViewSettings(UiState uiState)
    {
        uiState.KeptView.Keep(CaptureViewSources(uiState));
        return CreateViewSettings(uiState);
    }

    internal static ViewSources CaptureViewSources(UiState uiState)
    {
        return new ViewSources(
            uiState.BookmarkColumns.Changes,
            uiState.SearchColumns.Changes,
            uiState.ExportColumns.Changes,
            uiState.FoldedOptionGroups.Revision,
            uiState.Search.Kinds,
            uiState.MetadataFilters.Revision,
            uiState.ExportSetup.Window.Changes,
            uiState.Layout.Changes,
            uiState.Display,
            uiState.NuGetPicker.SearchChanges,
            uiState.NuGetPicker.Window.Changes);
    }

    private static ViewSettings CreateViewSettings(UiState uiState)
    {
        return new ViewSettings(
            uiState.BookmarkColumns.Weights.ToList(),
            uiState.SearchColumns.Weights.ToList(),
            uiState.ExportColumns.Weights.ToList(),
            uiState.FoldedOptionGroups.Save(),
            uiState.Search.Kinds,
            uiState.MetadataFilters.Save(),
            uiState.ExportSetup.Window.Bounds,
            uiState.Layout.CopyArrangement(),
            uiState.Display,
            uiState.NuGetPicker.Save(),
            uiState.NuGetPicker.Window.Bounds);
    }
}
