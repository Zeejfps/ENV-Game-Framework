# ZGF.Gui.Web.SmokeTest

A **compile-only** check that the platform-independent toolkit and the font backend can
target the browser (`net10.0-browser`) — step 1 of the web-rendering prep. It mirrors
`ZGF.Gui.iOS.SmokeTest`.

It contains no app — just a probe (`WebCompatibilityProbe`) that forces the compiler to
resolve `ZGF.Gui` and `ZGF.Fonts` under the browser target. Its purpose is to catch
problems *early*, before any real web host exists:

- **Restore-time issues:** a transitive dependency with no browser-wasm asset shows up here.
- **API-compatibility:** browser-incompatible API usage in the referenced toolkit is flagged
  when its source is compiled for `browser`.

## Building

Requires the .NET WASM build tooling:

```sh
dotnet workload install wasm-tools
dotnet build ZGF.Gui.Web.SmokeTest/ZGF.Gui.Web.SmokeTest.csproj -p:BuildWasm=true
```

`-p:BuildWasm=true` makes the referenced toolkit projects (`ZGF.Gui`, `ZGF.Fonts`, and
their deps) expose their `net10.0-browser` target; the global property propagates across the
project references. The project also sets `<BuildWasm>true</BuildWasm>` so a bare
`dotnet build` of *this* csproj still resolves the browser targets.

## Scope: compile + restore probe only

This project deliberately does **not** wire up the browser-wasm **native** libraries. Those
— our self-built `libfreetype.a` (see `tools/build-freetype-wasm.sh`) and the
`HarfBuzzSharp.NativeAssets.WebAssembly` asset — plus AOT are concerns of the real web host
(`ZGF.Gui.Web`, not yet created). Here, the managed FreeType/HarfBuzz wrappers compile and
restore; nothing native is linked or executed.

## Why it's not in the solution

`ZGF.Gui.Web.SmokeTest` is intentionally **excluded** from `ENV Game Framework.sln` so a
normal desktop `dotnet build` of the solution does not require the wasm-tools workload.
Build it explicitly (as above), or wire it into a dedicated CI job.

## Expected follow-up

When this project fails to restore/compile, that failure is the to-do list for the web port
(e.g. "expose net10.0-browser from dependency X", "provide a browser shim for Y"). See
`docs/web-font-rendering.md` for the full sequence.
