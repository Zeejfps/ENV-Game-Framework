# ILSpy feature gaps

What upstream ILSpy has that our app (`ZGF.ILSpy`) doesn't, as of ILSpy 11.1 (23 Sep 2026). 11.0
ported upstream from WPF to Avalonia; the feature set carried over.

Sources: https://github.com/icsharpcode/ILSpy/releases and the `v11.1` source.

## Implementation order

1. **Open containing folder** and **Search MSDN:** intent-named `RevealFile` and `OpenUrl` on the
   desktop facade first.

## Biggest gaps

1. **More ways to open.** Drag-and-drop files, `.nupkg` / zip packages, open from a NuGet feed,
   GAC (Windows), open from a running process, single-file bundles, WebCIL `.wasm`.

## Medium

- **Languages:** IL with C# (interleaved); ReadyToRun disassembly (plugin upstream).
- **Debug symbols:** local names come only from portable PDBs; upstream also reads Windows PDBs
  (through Mono.Cecil). The load-time `AutoLoadAssemblyReferences` and
  `ApplyWindowsRuntimeProjections` are left out of the options panel.
- **Display options:** word wrap; installed code fonts on macOS and Linux (`ISystemFonts` lists
  them on Windows only, so elsewhere the picker offers just the bundled font).
- **Tree:** nested-namespace view.
- **Per-assembly menu:** Set target framework, Pick PDB, Open containing folder.
- **Metadata explorer:** "Decompile metadata row in new tab", hide empty tables, decoded blob
  signatures in tooltips. AssemblyOS/Processor and AssemblyRefOS/Processor only show row counts.
- **Navigation:** preview tabs, Close all documents across every tab group at once, Search MSDN,
  omnibar (breadcrumb + search).

## Smaller

- Compare assemblies (API diff) and Create Diagram.
- Command line: `/navigateTo:`, `/language:`, `/list:`, `/clearList`, single-instance handling.
- Preconfigured assembly lists (upstream offers ready-made lists of framework assemblies).
- Help/About and Check for updates.
- Plugins (MEF upstream).
- Copy code with formatting (HTML on the clipboard).
- Module nodes in the tree, for assemblies made of several modules.
- The types and members used from each assembly reference, under its node.
