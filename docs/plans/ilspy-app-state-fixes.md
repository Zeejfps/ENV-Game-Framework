# ILSpy app-state fixes

What is left to fix in how `ZGF.ILSpy` keeps its app state
(review of 28 Oct 2026, each claim checked against the code). Each item breaks a rule in
[coding_rules.md](../coding_rules.md), mostly "make illegal states unrepresentable". Zultree
(`G:\Dev\zultree`) runs on the same app skeleton and already does most of these, so its files are
named as working examples. Ordered by priority.

## Structure

1. **`AppState` lives in the app project and has public mutable fields.** `Theme`, `IconTheme`,
   `ApiVisibility`, `PreviewTabs`, `Locale`, `ViewSettingsChanges` and `TabPanels` (`AppState.cs:53-93`)
   can be set from anywhere, and nothing stops `AppState` from referencing the GUI. Fix: make them
   properties with setters only where a feature needs one, and over time move what does not need the
   screen into `ZGF.ILSpy.Core`, which cannot reference `ZGF.Gui.*`. Zultree keeps `AppState` in
   `Zultree.Core` for this reason.
