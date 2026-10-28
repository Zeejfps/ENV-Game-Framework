# ILSight Icon

The app icon for ILSight. Needed by Phase 4 (`vpk pack --icon`) and Phase 5 (About page) of [ilsight-migration.md](ilsight-migration.md).
Sources and the build live in `Assets/Icon/Source/`; `python package.py <vectorcraft-cli.exe>` regenerates every output.

## Concept

A camera aperture opening onto `{ }`: the app lets you see into compiled code.

- Six chunky blades around a hexagonal opening, in a rounded-hexagon body. Not the eight thin blades and round body of the Aperture Science logo from *Portal*.
- Nothing that resembles ILSpy, dotPeek (monogram) or dnSpy (bug), and no `</>` or magnifier.

## Elements

| Element | Spec |
|---|---|
| Blades | Flat colour, lit from the top left: two light `#8C96F0`, two mid `#6670E0`, two dark `#4A56EF`. |
| Opening | Hexagon, near-black navy `#0B0E1F`. |
| Braces | Cyan `#3FD0E0` on every platform. They only sit on the opening, never on the page background. |
| macOS tile | Ink navy, `#1C2244` → `#0D1124` top to bottom. |

The design is judged at 32px and up. That is the smallest size macOS draws on Retina and Windows draws at 150% scaling or more. 16 and 24 only appear on Windows at 100% scaling (title bar, context menus, taskbar), and get a simplified small mark instead of a reduced app icon: the app icon's colours and main shape, with any detail dropped or redrawn that doesn't hold. Here that is the blades around a solid cyan opening, with no braces.

## Rules

- **No baked lighting on macOS.** Icon Composer adds the glass, highlights and shadow; painted glows fight it.
- **32 is hand-pixelled.** 30 and 20 are exact crops of the hand-drawn 32 and 24; 36 and up are vector.
- **Monochrome must work.** macOS tinted and clear modes, and colourblind users, get only the shape.

## Platforms

- **macOS 26:** `AppIcon.icon` with the tile as the background gradient and two layers: `blades` (glass, specular, shadow) over `lens` (opening and braces, no glass).
- **Windows:** `app.ico`, transparent, no tile, with 16, 20, 24, 30, 32, 36, 40, 48, 64 and 256.
- **Linux:** `Linux/app_icon_<size>.png` at 16, 24, 32, 48, 64, 128, 256 and 512.

## Open

- **Liquid Glass.** The macOS result has only been approximated on Windows. Check `AppIcon.icon` in Icon Composer on a Mac, and what Velopack's `--icon` accepts for `.icns` and older macOS versions.
- **36 and 40** are slightly soft at the blade seams. Hand-pixel them if they look soft in the real taskbar.
