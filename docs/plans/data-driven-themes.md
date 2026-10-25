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

## Phase 1: color themes (this branch)

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

## Phase 2: icon themes (later branch)

- A reference `lucide.json` maps meaning-based keys (`node.class`, `overlay.private`, `toolbar.back`) to
  `{ "font": "lucide", "char": "" }` and declares its fonts.
- A generator builds a typed `Icons` class from it, the way `Strings` is built: `ui.State.Icons.NodeClass` instead
  of `LucideIcons.Box`.
- A user icon theme fills the same keys with its own font; missing keys fall back to Lucide.
- Icon colors stay in color themes.
- Framework: a shared key-to-`UiIcon` map with fallback, and registering a font from a file path at runtime. The
  framework's own line-drawn chevrons and check marks stay as they are.

## Later

- Follow the OS light/dark setting with a preferred dark and light theme.
- Italic or bold per syntax kind.
- Import VS Code themes by mapping `editor.*` keys and common scopes to our roles.
