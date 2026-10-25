# Themes

Put your own color themes for ILSpy in this folder. ILSpy lists them in Display options, after the
built-in themes.

## A theme file

Each theme is one `.json` file. The file's name without `.json` is the theme's id, which `extends`
and the settings use. Comments and trailing commas are allowed.

```jsonc
{
  "$schema": "./theme.schema.json",
  "name": "My Dracula",            // or one name per language: { "en": "...", "de": "..." }
  "type": "dark",                  // dark or light
  "extends": "dracula",            // optional: a built-in or user theme id
  "palette": { "pink": "#FF79C6" },
  "ui": { "accent": "#BD93F9", "rowHover": "#2C2E3B" },
  "code": { "keyword": "pink" },
  "icons": { "type": "#8BE9FD" }
}
```

- `name`: what the theme is listed as. Without one, the id is used.
- `type`: whether the theme is dark or light.
- `extends`: the theme to build on. What this file leaves out comes from it.
- `palette`: named colors the other sections can use by name.
- `ui`, `code`, `icons`: the colors of the interface, the code view and the tree's icons.

A theme that extends no other must set `type` and the base `ui` colors: `background`, `surface`,
`control`, `field`, `border`, `text`, `textMuted`, `accent` and `onAccent`. The other `ui` colors are
derived from those when left out, and `code` and `icons` come from the built-in dark or light theme.

## Colors

Write a color as `#RRGGBB`, as `#RRGGBBAA` with alpha, or as the name of a palette color.
`#00000000` is not allowed, because it reads as unset; for an invisible color write another
transparent one, such as `#FFFFFF00`.

## Editing

- Saving a file here redraws ILSpy with it at once. Problems in a file show in the error dialog.
- "Customize current theme", beside the theme picker in Display options, makes a starting file: it
  extends the theme shown and lists every color, commented out at its current value. Uncomment a
  line to change that color.
- `theme.schema.json` describes every key, so an editor that reads `$schema` completes and checks
  them. ILSpy writes this file and this README; changes to them are replaced.
