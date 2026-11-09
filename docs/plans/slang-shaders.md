# Slang Shaders

Plan, written 2026-11-09. One Slang source per GUI canvas shader, with the GLSL and Metal the
backends load generated from it and checked in, and a test that regenerates them and fails on any
difference.

## Why

The canvas shaders existed as two hand-maintained copies, GLSL 410 in `ZGF.Gui.OpenGL` and Metal in
`ZGF.Gui.Metal`, after the repository split (`d47ed577`) moved the original Slang pipeline to Sandbox
and left only its outputs here. Nothing failed when an output was edited, so every change since was
made to the outputs.

## Decisions

**Slang sources live in `ZGF.Gui.Shaders/`**, rebuilt from the GLSL, which was the richest copy:
five modules and `canvas_common.slangh` for Globals, clips and color decoding.

**Generated output is checked in**, and `GeneratedShadersTests` regenerates it with the pinned
compiler and compares every file. Generating at build time was rejected: it puts a compiler
download into every consumer's build (ILSight and vesper publish NativeAOT from this repo as a
submodule on three OSes). The `tests` workflow runs the suites on push.

**One tool, `ZGF.Gui.Shaders/regenerate.cs`**, a .NET 10 file-based app, both fetches the compiler
and regenerates. It replaces the planned `fetch-slang.ps1`, `fetch-slang.sh` and a regenerate
script: one file holds the `slangc` flags for all three OSes, and the test runs the same code a
developer does. It downloads the release archive for the host OS and architecture, checks its
SHA256, and installs it under the per-user local application data folder, in `ZGF/slang/<version>/`.

**The version is pinned** in `SLANG_VERSION`, with a SHA256 per platform archive, and bumped
deliberately: a compiler bump can shift pixels. Linux and macOS use the `.tar.gz` archives.

**GLSL is `#version 450`**, emitted with `-profile glsl_450`. The OpenGL floor rose from 4.1 to 4.5
in `GlDriverCheck` (which `GlfwApp`'s context hints now read) and in `GlRequirements`. Machines whose
driver stops between 4.1 and 4.4 lose the OpenGL backend, as does choosing
`GuiRenderBackendKind.OpenGl` on macOS. macOS defaults to Metal.

**The matrix layout is `-matrix-layout-column-major`**, and both backends upload
`RenderedCanvasBase.Projection` as it is. Metal no longer transposes it.

**Warnings are errors** (`-warnings-as-errors all`), and every generated file starts with a comment
naming its source.

## Shader ABI

| | GL | Metal |
|---|---|---|
| Globals (`projection`, `canvasMetrics`) | uniform block, binding 0, one buffer per canvas | `[[buffer(0)]]` |
| ClipRects (rects, rounded rects, radii; 256 of each) | uniform block, binding 1 | `[[buffer(1)]]` |
| Unit quad | attribute location 0 | `[[buffer(2)]]` |
| Instance attributes | locations 1..N, declaration order | `[[buffer(3)]]` |
| Glyph atlas / image texture | texture unit 2 | `[[texture(0)]]`, `[[sampler(0)]]` |

The GL texture is on unit 2, not 0: Slang applies Vulkan's single binding namespace, so a sampler at
binding 0 overlaps Globals and warns, and OpenGL GLSL rejects the `set` qualifier that would separate
them. The fragment's window position flips y for Metal through a `__target_switch` in
`fragmentPixelPos`; that was the only difference between the hand-patched Metal and the GLSL.

## Baselines

`ShaderBaselineTests` draws six scenes covering all five primitives and rounded clips, at 1x and
1.5x, and compares each channel to a recorded PNG within 2. Drivers differ in their derivatives, so
baselines are per backend: `wgl` was recorded on an NVIDIA GPU, `egl` on Mesa 25.2.8 llvmpipe, which
the CI runner uses. Both sets were recorded with the hand-written shaders before the port, and the
generated shaders match them exactly on both drivers.

## Remaining

1. **Record the Metal baseline and verify Metal on a Mac.** The baseline must come from the
   hand-written shaders. On a Mac, add a worktree at the last commit before the port, copy
   `ZGF.Gui.Tests/ShaderBaselineTests.cs` into it, and run that test there with
   `ZGF_RECORD_SHADER_BASELINES=1`. Copy the resulting `Baselines/Shaders/metal` folder into the
   ported tree, then run `ShaderBaselineTests` and the rounded-corner seam tests in
   `ZGF.Gui.Immediate.Tests`. The iOS smoke project is compile-only; the macOS run checks the same
   Metal text.

2. **Launch a published consumer build once per platform.** ILSight and vesper need no workflow
   changes, but NativeAOT linking proves neither the embedded resource names nor that the driver
   accepts the shader text.
