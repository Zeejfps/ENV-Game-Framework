# Data-Driven Themes

Make ILSpy's color themes data: the built-in themes become embedded JSON files, and users add their own by dropping
files into a folder. Icon themes follow later on the same pattern.

## Starting point

- A theme is three C# palettes: `UiTheme` (framework), `CodeViewPalette` and `NodeIconPalette` (ILSpy), each with
  nine static presets that `AppThemes.All` joins by the `AppThemeId` enum.
- `UiTheme` already derives every unset state role (hover, pressed, selection, scrollbars) from its base roles, so a
  theme can be small.
- Settings store the theme as the enum's name (`"Monokai"`).
- ILSpy is published with native AOT: no reflection-based JSON.

## What we take from other editors

| From | Idea |
|---|---|
| VS Code | A JSON theme with `type: dark/light` and a flat map of color keys; unset keys fall back to defaults |
| VS Code | `include` to build on another theme (here `extends`), and a `$schema` so editors autocomplete keys |
| Rider, Helix | A named `palette` the color keys refer to |
| Zed | Watch the themes folder and redraw on save |
| VS Code product icon themes | A font plus a map from icon id to glyph (phase 2) |

Not taken: VS Code's TextMate `tokenColors`. Our syntax kinds are already named by role.

## Decisions

1. **Theme ids are kebab-case file stems** (`github-dark`). Old enum names in settings are migrated on load.
2. **Color key tables are source-generated** in `ZGF.Gui.Generator`, so every new color is themeable and the loader
   stays AOT-safe.
3. **The named presets leave the framework.** `UiTheme` keeps only `Dark` and `Light` as defaults for
   `DefaultUiState`, the sample and the framework tests. The rest are ILSpy content.
4. **A user theme with a built-in's id is reported as a clash and skipped.** To change a built-in, a user theme
   `extends` it.
5. **This branch is colors only.** Icon themes are a later branch.
6. **Fonts, density and sizes stay settings**, not theme values.

## Theme file

```jsonc
// %AppData%/ZGF.ILSpy/themes/my-dracula.json — comments and trailing commas allowed
{
  "$schema": "./theme.schema.json",
  "name": { "en": "My Dracula", "de": "Mein Dracula" },   // or a plain string
  "type": "dark",
  "extends": "dracula",                                  // optional: a built-in or user theme id
  "palette": { "purple": "#BD93F9", "bg": "#21222C" },
  "ui":    { "background": "bg", "accent": "purple", "rowHover": "#2C2E3B" },
  "code":  { "keyword": "#FF79C6", "comment": "#6272A4", "currentLine": "#44475A80" },
  "icons": { "type": "#8BE9FD", "method": "#50FA7B" }
}
```

- Colors are `#RRGGBB` or `#RRGGBBAA` (CSS order, converted to our ARGB), or a palette name.
- Keys are the C# property names in camelCase, so the types stay the source of truth.
- `ui` also takes the two shadows' colors (`popupShadowColor`, `dialogShadowColor`); their geometry stays in code.
- Resolution merges raw keys along the `extends` chain, child over parent, and builds each palette once at the end.
  Roles nobody set are derived from the final base roles, so a child that changes `control` gets matching hover
  colors.
- A theme with no `extends` implicitly extends the built-in `dark` or `light` by its `type`, for `code` and `icons`
  only. Its `ui` section starts empty and must set the base roles.
- The base `ui` roles are required after merging: `background`, `surface`, `control`, `field`, `border`, `text`,
  `textMuted`, `accent`, `onAccent`. Every `code` and `icons` role is required after merging.

## Phase 1: color themes

### Framework (`ZGF.Gui.Immediate`, `ZGF.Gui.Generator`)

1. `UiColor.TryParse` for `#RRGGBB` and `#RRGGBBAA`.
2. A generator for color key tables: for a type marked as themeable, the list of its color keys and a way to build
   an instance from a key-to-color map. Used by `UiTheme`, `CodeViewPalette` and `NodeIconPalette`.
3. Remove the named presets from `UiTheme`. Keep `Dark` and `Light`.

### ILSpy

4. The nine themes become `Assets/Themes/*.json`, embedded. Delete the C# presets in `CodeViewPalette` and
   `NodeIconPalette`, `AppThemeId`, and `AppThemes`' table.
5. `ThemeFile` reads one file: JSONC-tolerant, AOT-safe, and reports errors with file, line and key.
6. `ThemeCatalog` holds the built-in and user themes by id, resolves `extends` (detecting cycles), and keeps each
   file's errors. It lives on `AppState`.
7. A `Theming` feature file: `LoadThemes`, `ReloadUserThemes`, `ResolveTheme`. `AppState.Theme` becomes a string id.
   The app hands the resolved `AppTheme` to `UiState` as it does today.
8. User themes live in `%AppData%/ZGF.ILSpy/themes/`, next to `settings.json`. A file watcher reloads the folder and
   the open theme redraws at once.
9. Errors go to the error dialog: bad JSON, unknown keys, unknown palette names, `extends` cycles, missing required
   roles, id clashes. A broken theme falls back to its parent, or to `dark` or `light` by its `type`.
10. The Display options picker lists the catalog: built-ins first, then user themes. It gains "Open themes folder"
    and "Reload".
11. `AppSettings.Load` maps old enum names (`"GitHubDark"`) to new ids (`"github-dark"`). A saved id that no longer
    resolves falls back for display but stays in settings, so it comes back with its file.

### Tests

- Contrast and completeness run over every embedded theme (moved from the framework's `ThemeTests`).
- The loader: partial themes, `extends`, palette names, bad input, clashes, cycles, the settings migration.
- A snapshot test generates the JSON schema and fails when it differs from the checked-in
  `Assets/Themes/theme.schema.json`.

## Phase 2: icon themes (this branch)

Icon themes are a second, separate theme: a color theme says how things are colored, an icon theme only what
is drawn. The built-in `lucide` theme becomes an embedded JSON file, and users add their own in a folder beside
the color themes.

### Decisions

1. **Icon keys name meaning, not glyph**, one key per distinct use, dotted by area: `node.class`,
   `overlay.private`, `toolbar.back`, `options.reloadThemes`. A theme can draw "back" differently from the other
   left arrows.
2. **The icon theme is its own setting**, `AppSettings.IconTheme`, default `lucide`, picked under "Icon theme" in
   Display options, beside the color theme. Icon colors stay in color themes.
3. **An icon is a font glyph or an SVG file.** An SVG is drawn tinted through `currentColor` with the color the
   caller asks for, the one a font glyph would get; an SVG without `currentColor` keeps its own colors.
4. **"Customize current icon theme"** writes `<current>-custom.json`, extending the icon theme shown, with every
   key listed commented out at its current value, switches to it and opens it.

### Icon theme file

```jsonc
// %AppData%/ZGF.ILSpy/icon-themes/my-icons.json — comments and trailing commas allowed
{
  "$schema": "./icon-theme.schema.json",
  "name": { "en": "My icons", "de": "Meine Symbole" },   // or a plain string
  "extends": "lucide",                                    // optional: a built-in or user icon theme id
  "fonts": { "symbols": "fonts/Symbols.ttf" },
  "icons": {
    "node.class": { "font": "symbols", "glyph": "E0F2" },  // a codepoint in hex
    "toolbar.back": { "svg": "svg/back.svg" }
  }
}
```

- `fonts` names font files by id. `icons` names each key's font by id and its codepoint as hex text, or an SVG
  file.
- Paths are relative to the theme file and must stay inside the icon themes folder; absolute paths and paths that
  leave it are reported. Built-in themes name embedded resources.
- Fonts merge along the `extends` chain, child over parent, and every theme can use the reference theme's fonts.
  An icon's font is looked up once the chain is merged, so a child that declares a parent's font id again draws
  the parent's icons in its font.
- A key no theme in the chain sets, or one whose font or picture is broken, draws as the reference `lucide`
  theme does.
- Errors go to the error dialog with file and line: bad JSON, unknown keys, a codepoint that is not one, an icon
  that is neither a glyph nor an SVG, an unknown font id, a missing or unreadable file, a file that is no font,
  an SVG that does not parse, a path leaving the folder, `extends` cycles and unknown parents, id clashes. A
  broken theme falls back to its parent, else to `lucide`.

### Framework (`ZGF.Gui.Immediate`, `ZGF.Gui.Immediate.Widgets`, `ZGF.Gui.Generator`)

1. `IconGlyph`: what an icon draws, a font glyph (text and family) or a parsed `SvgDocument`. Widgets take it
   where they took a glyph string and a family: `UiIcon`, `IconButton`, `SplitIconButton`, `TextField`'s leading
   icon, menus, tabs, trees and table sorts.
2. `IconSet`: icons by key, falling back per key to another set.
3. `ui.Glyph(glyph, color, size)` and `UiPainter.Glyph`: a font glyph as text; an SVG rasterized at the drawn
   pixel size (size times the canvas scale) in the asked color, uploaded with the image API and cached by
   (picture, pixel size, color). A frame that draws only cached icons allocates nothing. Images not drawn for a
   while are dropped, and `ui.ReleaseUndrawnIconImages()` drops at the end of the frame every one it did not
   draw, for when the icons changed.
4. `[ThemeIcons("Assets/IconThemes/lucide.json")]` on a partial class: a generator reads the reference theme (the
   `AdditionalFiles` item whose path ends so) and gives the class a property per key, built from an `IconSet`, and a
   `Keys` list with each key's description, taken from the comment above it. A misspelled key fails to compile.
5. Registering a font at runtime already works (`ui.RegisterFont`); a font that fails to load is skipped
   instead of failing the frame, and the host keeps loaded faces by their bytes, so registering an unchanged
   font again reuses its face. `FontFace.ReadCharacters()` loads a face only to check it and list its
   characters, so a theme reports a font that cannot be loaded, or a glyph its font lacks, and falls back.
   The framework's own line-drawn chevrons, checks and crosses stay as they are.

### ILSpy

6. `Assets/IconThemes/lucide.json`, embedded, maps every key and declares `"fonts": { "lucide": "Lucide.ttf" }`.
   `LucideIcons` is deleted; its codepoints move into the file.
7. `AppIcons` is the generated class. `UiState.Icons` holds the current icon theme's, and views read
   `ui.State.Icons.NodeClass`. `NodeIcons` keeps its role-based coloring.
8. `IconThemeFile` reads one file, reading its fonts and SVGs as it goes. `IconThemeCatalog` resolves `extends`
   into an `IconTheme`: the `AppIcons`, the fonts they need and each key's value as written. It lives on
   `AppState` beside `ThemeCatalog`; both share `ThemeJson` (JSONC reading with lines), `ThemeName`, `ThemeError`
   and `ThemeFolderWatch`.
9. An `IconTheming` feature file: `LoadIconThemes`, `ReloadUserIconThemes`, `ResolveIconTheme`,
   `PrepareIconThemesFolder`, `CreateCustomIconTheme`, `WatchUserIconThemes`. The app hands the resolved theme's
   icons and fonts to `UiState` each frame, and registers the fonts when they change.
10. User icon themes live in `%AppData%/ZGF.ILSpy/icon-themes/`, beside `themes`. "Open icon themes folder" makes
    it with `icon-theme.schema.json` and a README. Saving a `.json`, `.ttf`, `.otf` or `.svg` file in it, or in a
    folder inside it, reloads the icon themes. The buttons hide while settings live in memory only.

### Tests

- Every key of the built-in theme resolves to the font and codepoint the old `LucideIcons` constant had
  (temporary, removed with `LucideIcons`).
- Parsing and errors, `extends`, cycles and clashes, fallback per key, a path leaving the folder.
- SVG icons tinted through `currentColor`, rasterized once and reused, released when undrawn after a change.
- The picker, customize, live reload of a changed SVG and font, the settings default and persistence.
- A snapshot test of `Assets/IconThemes/icon-theme.schema.json`.

## Later

- Follow the OS light/dark setting with a preferred dark and light theme.
- Italic or bold per syntax kind.
- Import VS Code themes by mapping `editor.*` keys and common scopes to our roles.
