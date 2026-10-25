# Icon themes

Put your own icon themes for ILSpy in this folder, with the fonts and SVG files they draw with. ILSpy
lists them under "Icon theme" in Display options, after the built-in ones. An icon theme only says what
each icon draws; its colors come from the color theme.

## An icon theme file

Each icon theme is one `.json` file. The file's name without `.json` is the theme's id, which `extends`
and the settings use. Comments and trailing commas are allowed.

```jsonc
{
  "$schema": "./icon-theme.schema.json",
  "name": "My icons",              // or one name per language: { "en": "...", "de": "..." }
  "extends": "lucide",             // optional: a built-in or user icon theme id
  "fonts": { "symbols": "fonts/Symbols.ttf" },
  "icons": {
    "node.class": { "font": "symbols", "glyph": "E0F2" },
    "toolbar.back": { "svg": "svg/back.svg" }
  }
}
```

- `name`: what the theme is listed as. Without one, the id is used.
- `extends`: the icon theme to build on. What this file leaves out comes from it.
- `fonts`: TrueType or OpenType font files by id, for the icons to name.
- `icons`: what each icon key draws: a font by id and a glyph in it, or an SVG file.

Every icon key is listed in `icon-theme.schema.json`, with what it is for. A key no theme in the chain
sets is drawn as the built-in `lucide` theme draws it, and every theme can use its font, `lucide`.

## Glyphs

Write a glyph as the hex digits of its codepoint, such as `E0F2`, as a font's character map shows it.

## Pictures

An SVG picture that uses `currentColor` is drawn in the color ILSpy gives the icon, so it follows the
color theme. One that does not keeps its own colors.

## Files

Paths are relative to the theme file and must stay inside this folder: a path that starts at a drive or
leaves the folder with `..` is refused.

## Editing

- Saving a theme file, a font or a picture here redraws ILSpy with it at once. Problems in a file show in
  the error dialog.
- "Customize current icon theme", beside the icon theme picker in Display options, makes a starting
  file: it extends the icon theme shown and lists every icon, commented out as drawn now. Uncomment a line
  to change that icon.
- `icon-theme.schema.json` describes every key, so an editor that reads `$schema` completes and checks
  them. ILSpy writes this file and this README; changes to them are replaced.
