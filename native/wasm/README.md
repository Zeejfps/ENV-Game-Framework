# native/wasm

Self-built `browser-wasm` native libraries for the .NET WebAssembly host.

`libfreetype.a` is produced by [`tools/build-freetype-wasm.sh`](../../tools/build-freetype-wasm.sh)
and is **not** committed (see `.gitignore`). Build it locally before a web
publish, or restore it from a CI artifact. HarfBuzz is supplied separately by the
`HarfBuzzSharp.NativeAssets.WebAssembly` NuGet package and does not live here.

See [`docs/web-font-rendering.md`](../../docs/web-font-rendering.md) for the full
plan, the emsdk-version-matching requirement, and how the archive is wired into
the host via `<NativeFileReference>`.
