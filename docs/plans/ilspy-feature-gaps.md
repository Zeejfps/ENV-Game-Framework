# ILSpy feature gaps

What upstream ILSpy has that our app (`ZGF.ILSpy`) doesn't, as of ILSpy 11.1 (23 Sep 2026). 11.0
ported upstream from WPF to Avalonia; the feature set carried over.

Sources: https://github.com/icsharpcode/ILSpy/releases and the `v11.1` source.

## Biggest gaps

1. **Save and export.** Save code (Ctrl+S), whole-project export as an SDK-style `.csproj`
   (`WholeProjectDecompiler`, which we already reference), portable PDB generation (single and
   batch).
2. **More ways to open.** Drag-and-drop files, `.nupkg` / zip packages, open from a NuGet feed,
   GAC (Windows), open from a running process, single-file bundles, WebCIL `.wasm`.
3. **Saved assembly lists.** Create, switch, and delete named lists. We keep one list.

## Medium

- **Languages:** IL with C# (interleaved); ReadyToRun disassembly (plugin upstream).
- **Debug symbols:** local names come only from portable PDBs; upstream also reads Windows PDBs
  (through Mono.Cecil). The load-time `AutoLoadAssemblyReferences` and
  `ApplyWindowsRuntimeProjections` are left out of the options panel.
- **Resources:** BAML → XAML, `!AvaloniaResources` unpacked into the tree.
- **Display options:** font and size, zoom, word wrap, line-number toggle, bracket matching,
  current-line highlight.
- **Tree:** module nodes, nested-namespace view, exported (forwarded) types, types/members used
  from each reference, optional metadata tokens (hex or decimal).
- **Per-assembly menu:** Load dependencies, Set target framework, Pick PDB, Open containing
  folder, Remove assemblies that failed to load, sort list, collapse all.
- **Metadata explorer:** "Show in metadata" from code, "Decompile metadata row in new tab", hide
  empty tables, decoded blob signatures in tooltips. EncLog, EncMap, AssemblyOS/Processor and
  AssemblyRefOS/Processor only show row counts.
- **Navigation:** preview tabs, Close all documents, Search MSDN, omnibar (breadcrumb + search).
- **Analyzer:** properties and events lack Used By; Applied To skips DeclSecurity attributes
  (TODO in `ZGF.ILSpy.Core/Analysis/AttributeAppliedToAnalyzer.cs`).

## Smaller

- Compare assemblies (API diff) and Create Diagram.
- Command line: `/navigateTo:`, `/language:`, `/list:`, `/clearList`, single-instance handling.
- Help/About and Check for updates.
- Plugins (MEF upstream).
- Copy code with formatting (HTML on the clipboard).

## Suggested order

1. Save code and project export.
