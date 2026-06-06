# iOS Port — Handoff

Resume point for getting `ZGF.Gui` running on the iOS simulator, after upgrading macOS + Xcode.

_Last updated: 2026-06-06._

## TL;DR

All the **code** is done and committed; the **toolchain** is the blocker. The installed
.NET iOS workload (`26.5.10284`) requires **Xcode 26.5**, but you have Xcode 16.2, and the
App Store won't offer Xcode 26.5 because your macOS (15.0) is too old. So: update macOS →
install Xcode 26.5 → install an iOS simulator runtime → build & run.

## Where the work lives

- Branch: **`ios_integration`** (commit `108ea0b3 "First pass on iOS"`). Working tree clean —
  the OS update cannot lose anything.
- Nothing is on `master` yet.

## What's done (and how verified)

1. **Extracted the platform-neutral Metal canvas backend** into a new package `ZGF.Gui.Metal`
   (`MetalRenderedCanvas`, `MetalSharedResources`, `MetalImageManager`, `ShaderAssets`,
   `.gen.metal` shaders). It multi-targets `net10.0;net10.0-ios` (the `-ios` target only when
   built with `-p:BuildIos=true`). `ZGF.Gui.Desktop` now references it and keeps only the
   desktop glue (`MetalRenderBackend`). Dependency chain: `ZGF.Gui.Metal → ZGF.Rendering.Metal`
   (the latter stays dependency-free interop).
   - ✅ **Verified**: `ZGF.Gui.Desktop` builds clean (desktop unaffected); the iOS smoke test
     (`ZGF.Gui.iOS.SmokeTest -p:BuildIos=true`) compiles the new package under `net10.0-ios`
     with 0 warnings / 0 errors (incl. 0 CA1416 — no desktop-only API leaked in).

2. **Font native libraries for iOS** (`ZGF.Fonts`):
   - FreeType: its iOS native ships inside the `FreeTypeSharp` package (an xcframework) — no
     action needed.
   - HarfBuzz: added `HarfBuzzSharp.NativeAssets.iOS`, scoped to the `net10.0-ios` target; the
     desktop natives (Linux/macOS/Win32) are scoped to non-iOS.
   - ✅ **Verified at restore/compile level** only. The natives actually load when the real app
     runs — that's gated on the toolchain below.

3. **The iOS app** `ZGF.Gui.iOS.App` (intentionally **not** in `ENV Game Framework.sln`):
   - `Program.cs` / `AppDelegate.cs` — UIKit entry point + window.
   - `MetalUiView.cs` — `UIView` whose backing layer is a `CAMetalLayer`.
   - `IosMetalSurface.cs` — iOS implementation of the shared `IMetalSurface` seam.
   - `MetalViewController.cs` — creates `MTLDevice`/command queue via Microsoft.iOS bindings,
     hands their native handles to the shared `MetalSharedResources` / `MetalSurfaceRenderer`,
     and drives `BeginFrame → DrawScene → EndFrame` from a `CADisplayLink`. Draws a rounded
     card + centered text as a first-light scene. Mirrors desktop
     `PlatformBackend.ResolveMetal`, minus GLFW; same DPI model (logical-point geometry,
     pixel-sized `drawableSize`, glyphs baked at device pixels).
   - ⚠️ **NOT compiler-verified.** The Xcode gate blocks the app build, so this C# has never
     been through a compiler. Restore + project-reference resolution succeed; only the Xcode
     check fails. Expect to fix a Microsoft.iOS binding detail or two on first real build
     (most likely suspects: the `NativeHandle → IntPtr` casts, and the exact enum names
     `MTLPixelFormat.BGRA8Unorm` / `NSRunLoopMode.Common`).

## What you need to do: update the Mac

Two macOS updates are currently offered:

| Update | Size | Notes |
|---|---|---|
| macOS **Sequoia 15.7.7** | ~6.9 GB | Minor; stays on Sequoia. May or may not be new enough for Xcode 26.5. |
| macOS **Tahoe 26.5.1** | ~10.9 GB | Major OS upgrade; matches Xcode 26.5's generation — guaranteed to run it. |

**Decision:** Xcode 26.5 is a "26"-series (Tahoe-era) Xcode. It *may* still run on a late
Sequoia, but I can't confirm 15.7.7 is sufficient. Two routes:

- **Low-risk first try:** update to **Sequoia 15.7.7**, then attempt to install Xcode 26.5
  (see below). If the installer/`xcodes` says the OS is too old, then update to Tahoe.
- **Guaranteed:** update straight to **macOS Tahoe 26.5.1**. This definitely runs Xcode 26.5.

System Settings → General → Software Update. (Tahoe is a major upgrade — expect the usual
"review your apps for compatibility" caveats.)

## Then: install Xcode 26.5

Once macOS is updated, pick one:

- **App Store** — Xcode should now appear/offer the 26.5 update (it was hidden only because
  the OS was too old).
- **Apple Developer downloads** — https://developer.apple.com/download/applications/ →
  download the Xcode 26.5 `.xip`, expand, move to `/Applications`. (Free Apple ID works.)
- **`xcodes` CLI (recommended — keeps 16.2 side-by-side):**
  ```sh
  brew install xcodesorg/made/xcodes
  xcodes list            # shows installable versions + whether your OS supports them
  xcodes install 26.5
  xcodes select 26.5     # or: sudo xcode-select -s /Applications/Xcode-26.5.app
  ```

Verify: `xcodebuild -version` should report 26.5.

## Then: install an iOS simulator runtime

None is installed yet (`xcrun simctl list runtimes` is empty):

```sh
xcodebuild -downloadPlatform iOS
xcrun simctl list devices        # note a booted/available iPhone's UDID
```

## Then: build & run the app

From the repo root, on branch `ios_integration`:

```sh
# Managed compile first (fastest way to surface any binding fixes):
dotnet build ZGF.Gui.iOS.App/ZGF.Gui.iOS.App.csproj -p:BuildIos=true

# Build + deploy + launch on a simulator:
dotnet build ZGF.Gui.iOS.App/ZGF.Gui.iOS.App.csproj -p:BuildIos=true -t:Run \
    -p:_DeviceName=:v2:udid=<sim-udid>
```

`-p:BuildIos=true` is required so the referenced toolkit projects expose their `net10.0-ios`
target (and `ZGF.Fonts` pulls the iOS HarfBuzz native instead of the desktop ones).

**Expected first result:** a teal-free screen with a blue rounded card reading
"Hello from ZGF.Gui / rendering on iOS via Metal". If the C# needs a binding tweak, the first
`dotnet build` will point right at it.

## After it renders

1. Replace the placeholder `DrawScene` in `MetalViewController.cs` with a real View tree
   (`Context` + `MultiChildView` + content), matching how `ZGF.Gui.Desktop/GuiApp.cs` lays out
   and draws (`_root.LayoutSelf(); _root.DrawSelf()`).
2. Wire `UITouch` events → the toolkit's input system (desktop reference:
   `DesktopInputSystem`).

## Fallback (if you'd rather NOT update the OS)

The alternative to upgrading Xcode is pinning the **.NET iOS workload** to a version
compatible with Xcode 16.2. That likely forces the app to `net9.0-ios`, which then can't
reference our `net10.0` libraries unless they also multi-target `net9.0-ios` — messy, and
feasibility is uncertain. Updating macOS + Xcode is the cleaner path; this is only here as a
documented escape hatch.

## Shader regeneration note

The `.gen.metal` files are Slang-generated. Sources live in `ZGF.Gui.Tests/Assets/Shaders/`;
the tool is `tools/CompileCanvasShaders`. After regenerating, copy the refreshed
`canvas_*.gen.metal` into **`ZGF.Gui.Metal/Assets/Shaders/`** (the copy target moved here from
`ZGF.Gui.Desktop` during the extraction).
