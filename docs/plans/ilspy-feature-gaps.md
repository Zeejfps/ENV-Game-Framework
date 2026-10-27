# ILSpy feature gaps

What upstream ILSpy has that our app (`ZGF.ILSpy`) doesn't, as of ILSpy 11.1 (23 Sep 2026). 11.0
ported upstream from WPF to Avalonia; the feature set carried over.

Sources: https://github.com/icsharpcode/ILSpy/releases and the `v11.1` source.

- **ReadyToRun disassembly** (a plugin upstream).
- **Windows PDBs:** local names come only from portable PDBs; upstream also reads Windows PDBs
  (through Mono.Cecil).
- **Word wrap** in the code view.
- **Installed code fonts on macOS and Linux:** `ISystemFonts` lists them on Windows only, so elsewhere
  the font picker offers just the bundled font.
- **Open from the GAC** (Windows).
- **Open from a running process.**
- **Compare assemblies** (API diff).
- **Create Diagram.**
- **Check for updates.**
- **Plugins** (MEF upstream).
