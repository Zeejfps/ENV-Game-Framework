# ILSpy feature gaps

What upstream ILSpy has that our app (`ZGF.ILSpy`) doesn't, as of ILSpy 11.1 (23 Sep 2026). 11.0
ported upstream from WPF to Avalonia; the feature set carried over.

Sources: https://github.com/icsharpcode/ILSpy/releases and the `v11.1` source.

## Medium

- **Languages:** ReadyToRun disassembly (plugin upstream).
- **Debug symbols:** local names come only from portable PDBs; upstream also reads Windows PDBs
  (through Mono.Cecil). The load-time `AutoLoadAssemblyReferences` and
  `ApplyWindowsRuntimeProjections` are left out of the options panel.
- **Display options:** word wrap; installed code fonts on macOS and Linux (`ISystemFonts` lists
  them on Windows only, so elsewhere the picker offers just the bundled font).
- **Metadata explorer:** decoded blob signatures in tooltips.

## Smaller

- More ways to open: the GAC (Windows), a running process.
- Compare assemblies (API diff) and Create Diagram.
- Check for updates.
- Plugins (MEF upstream).
- Copy code with formatting (HTML on the clipboard).
- Module nodes in the tree, for assemblies made of several modules.
