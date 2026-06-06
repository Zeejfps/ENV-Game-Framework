# ZGF.Gui.iOS.SmokeTest

A **compile-only** check that the platform-independent toolkit can target iOS.

This project targets `net10.0-ios` and references `ZGF.Gui` and `ZGF.Metal`. It
contains no app — just a probe that forces the compiler to resolve those assemblies
under the iOS target. (It deliberately does **not** reference `ZGF.Core`: the
desktop windowing abstractions live behind `ZGF.Gui.Desktop`, and the toolkit no
longer depends on them.) Its purpose is to catch problems *early*, before any real
iOS app exists:

- **Platform-compatibility analysis (CA1416):** desktop-only API usage in the
  referenced code is flagged when compiled for `ios`.
- **Restore-time issues:** a transitive dependency with no iOS runtime asset
  (e.g. `HarfBuzzSharp` — only Linux/macOS/Win32 native assets are referenced today)
  shows up here first.

## Building

Requires the .NET iOS workload:

```sh
dotnet workload install ios
dotnet build ZGF.Gui.iOS.SmokeTest/ZGF.Gui.iOS.SmokeTest.csproj
```

The managed compile runs on any OS with the workload installed; a full iOS *link*
still requires a Mac with Xcode.

## Why it's not in the solution

`ZGF.Gui.iOS.SmokeTest` is intentionally **excluded** from `ENV Game Framework.sln`
so that a normal desktop `dotnet build` of the solution does not require the iOS
workload. Build it explicitly (as above), or wire it into a dedicated CI job.

## Expected follow-up

When this project fails to restore/compile, that failure is the to-do list for the
iOS port (e.g. "add `HarfBuzzSharp.NativeAssets.iOS`", "provide a FreeType iOS
native"). See the mobile-rendering notes for the full sequence.
