# ILSight Icon

The app icon for ILSight. Needed by Phase 4 (`vpk pack --icon`) and Phase 5 (About page) of [ilsight-migration.md](ilsight-migration.md).

## Concept

A glass prism with a short block of code coming out of one face: opaque IL goes in, readable code comes out. The silhouette is **a triangle with an indented code block**.

- No incoming beam and no spectrum. A beam in, an upright triangle and a rainbow out is the *Dark Side of the Moon* cover.
- Nothing that resembles ILSpy, dotPeek (monogram) or dnSpy (bug), and no `</>` or magnifier.

## Elements

| Element | Spec |
|---|---|
| Prism | Rounded triangle, indigo `#4B55C8`, left of centre. One lit edge, no other effects. |
| Code lines | Three horizontal strokes with rounded caps, leaving the prism's right face. Indents 0, 1, 0; lengths long, medium, short. One accent colour. |
| Accent | Cyan `#3FD0E0` on the macOS tile. On Windows and Linux, a darker teal that keeps 3:1 contrast on both white and `#202020`. Pick it by measuring. |
| macOS tile | Ink navy, `#1C2244` → `#0D1124` top to bottom. |

The line arrangement is the same at every size. Small sizes lose detail, never structure.

## Rules

- **The 16px and 24px silhouettes come first.** Approve them in black on white before any colour, then build the large art from them.
- **No baked lighting on macOS.** Icon Composer adds the glass, highlights and shadow; painted glows and caustics fight it.
- **No outlines around the code lines.** A 1px stroke around a 2px bar eats the colour or fills the gaps. Contrast comes from the accent colour.
- **Monochrome must work.** macOS tinted and clear modes, and colourblind users, get only the shape.

## Platforms

- **macOS 26:** Icon Composer file with three layers: tile, code lines, prism. Check what Velopack's `--icon` accepts for `.icns` and older macOS versions.
- **Windows:** transparent, no tile. `.ico` with 16, 20, 24, 30, 32, 36, 40, 48, 64 and 256. Windows scales the nearest larger size for the in-between ones, so each is checked.
- **Linux:** PNGs at 16, 24, 32, 48, 64, 128, 256 and 512, not one 512 resampled.

## Open

- **Prism orientation.** Point up, point down, or lying on its side. Pointing right risks reading as a Play button; pointing down as a funnel or warning sign. Decide from the 16px silhouettes.
- **Who draws it.** Hand-pixelled small sizes need a pixel editor; the large art needs a vector tool.

## Steps

1. Draw 16px and 24px black silhouettes of each orientation. Pick one.
2. Add the prism colour and the accent. Measure the Windows teal against white and `#202020`.
3. Draw the 1024px art from the chosen silhouette.
4. Hand-tune 16, 24 and 32; check every other size at its scale.
5. Export `.ico`, the macOS icon and the Linux PNGs.
