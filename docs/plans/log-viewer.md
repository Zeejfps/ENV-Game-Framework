# Log viewer on the immediate GUI

A cross-platform viewer for large log files, built on `ZGF.Gui.Immediate`: klogg's speed on
multi-GB files, lnav's understanding of log structure (formats, timelines, facets), in a docked GUI
an agent can drive through the MCP server.

Why a desktop app: memory-mapped files far past WebAssembly's memory limit, a reliable live tail
across rotation, child processes for SSH / Docker / kubectl streams, OS file associations and
multiple windows.

Why now: klogg's last stable release is 22.06 (June 2022), LogExpert is Windows-only WinForms, and
lnav, the most capable, runs only in a terminal.

## Projects

| Project | Depends on | Holds |
|---|---|---|
| `ZGF.LogViewer.Core` | — | Sources, line index, search, formats, time index, merge, templates. No GUI. |
| `ZGF.LogViewer.Tests` | Core | xunit tests for Core against generated logs and checked-in format samples. |
| `ZGF.LogViewer.Benchmarks` | Core | BenchmarkDotNet suites for indexing, line access and search, plus a log generator. |
| `ZGF.LogViewer` | Core, `ZGF.Gui.Immediate`, `ZGF.Gui.Immediate.Widgets` | The app. |

Framework gaps the app needs are filled in `ZGF.Gui.Immediate` / `ZGF.Gui.Immediate.Widgets`, with
tests in `ZGF.Gui.Immediate.Tests`, and are general-purpose — nothing log-specific there. The app
publishes with native AOT, as ILSpy does.

## Features

### Baseline (klogg parity)

- Open multi-GB files from the command line, the native file picker and drag and drop; the file is
  readable while it indexes, with progress in the status bar.
- Plain and regex search; matches go to a filtered pane whose selection stays in sync with the main
  view. Several filters combine (and / or / not).
- Follow mode (tail): appended lines show up live; truncation and rotation reopen the file.
- Highlight rules: color a line or a span by regex, saved per user.
- Marks on lines, next / previous mark, marks listed in the filtered pane.
- Encoding detection (UTF-8, UTF-16 LE/BE with BOM, Latin-1) with a manual override.
- Minimap scrollbar with ticks for errors, warnings, marks and search hits over the whole file.

### Structure (lnav in a GUI)

- Format detection over the first lines: JSON lines, syslog, Serilog / NLog / log4net default
  layouts, nginx / Apache access logs, Unity and Unreal logs, logcat. A format yields timestamp,
  level, logger, thread and message as columns.
- User formats in a file (regex with named groups + field mapping), loaded at startup.
- Facets panel: levels, loggers and threads with counts, one click to filter.
- Time histogram above the view, stacked by level; click jumps, drag filters to a range. Go to
  timestamp.
- Inline expansion of JSON payloads and multi-line entries (stack traces fold under their first
  line).

### Insight (new)

- Merged timeline: several files interleaved by timestamp, each source in its own color.
- Templates: lines clustered into message templates (numbers, hex, GUIDs, IPs, timestamps masked)
  with counts; selecting a template filters to its lines.
- Run diff: two logs aligned by template — templates only in one run, and templates whose counts
  differ most.
- MCP tools: `open`, `search`, `get_lines`, `facets`, `templates`, `diff_runs`, `jump_to_time`.

### Sources (later)

- `.gz` and `.zst` files, a folder of logs, `ssh host tail -F`, `docker logs -f`,
  `kubectl logs -f`. Saved sessions: open files, filters, highlights, marks, layout.

### Layout

```
┌─ Menu bar · command palette (Ctrl+P) ───────────────────────────┐
├─ Tabs: [api.log] [worker.log] [merged: api+worker] ─────────────┤
├─ Time histogram (stacked by level, drag to select) ─────────────┤
│ ┌─ Main view (virtualized, columns) ────┐ ┌─ Facets ───────────┐│
│ │                                       │ ├─ Templates ────────┤│
│ ├─ Filters + filtered view ─────────────┤ │                    ││
│ └───────────────────────────────────────┘ └────────────────────┘│
├─ Status: size · lines · indexing % · following ● · encoding ────┤
```

Panes live in `UiDockSpace`. Keyboard: `/` search, `n` / `N` next / previous match, `m` mark,
`g` / `G` top / bottom, `Ctrl+G` go to line or time, `F` toggle follow.

## Core design

- **Sources.** `ILineSource` exposes bytes by range, length and change notifications.
  `FileSource` reads through `MemoryMappedFile`, remapping as the file grows; later sources
  (gzip, process) spool to a temp file and reuse the same path.
- **Line index.** Built in the background in chunks, one per core, with vectorized
  `Span<byte>.IndexOf((byte)'\n')`. Stores an absolute offset every 64 lines and the line
  lengths in between as varints — about 1–2 bytes a line instead of 8. `GetLine(n)` seeks the
  checkpoint and walks at most 63 lengths. Readers see the lines indexed so far.
- **Tail.** `FileSystemWatcher` as a hint plus a 250 ms poll of length and write time. A shorter
  file or a new file id (inode / file index) means truncation or rotation: reopen and reindex.
- **Search.** Plain text through `SearchValues<string>`; regex with `RegexOptions.NonBacktracking`
  so no pattern can hang. Runs per chunk on worker threads, streams sorted line-number arrays,
  cancels when the query changes. Filters combine by merging sorted arrays.
- **Formats.** A detector scores each known format on a sample; a format parses fields lazily per
  visible or scanned line. Built-in formats use `[GeneratedRegex]` (AOT-safe).
- **Time index.** Timestamp parsed for every line once, in the indexing pass when a format is
  known, kept sparsely (every 64 lines, monotonic runs noted). Binary search for jumps; the
  histogram is buckets over it. Merged views k-way merge the per-file time indexes into a
  (source, line) map.
- **Templates.** Drain-style clustering: mask variable tokens, group by token count and leading
  tokens, merge by similarity threshold. One pass, run on demand, cancellable.
- **Long lines.** Lines over a cap (e.g. 64 KB) render truncated with a size chip that expands
  them; layout never measures text that is off screen.

## Performance targets

Measured on the benchmark machine against generated logs, and compared with klogg on the same files.

| Measure | Target |
|---|---|
| Index a 10 GB file from page cache | ≤ 5 s, first screen readable < 200 ms |
| Line index memory | ≤ 2 bytes / line |
| `GetLine(n)` anywhere | < 5 µs |
| Plain search, 10 GB, page cache | ≤ 5 s |
| Scroll frame, 4K window full of text | < 4 ms, no per-frame allocation |
| Tail latency (append → visible) | < 300 ms |

## Phases

At most two agents run at once. Agents do not commit; each phase is reviewed and committed before the
next one starts.

### Phase 0 — engine

**Core agent** — `ZGF.LogViewer.Core`, `ZGF.LogViewer.Tests`, `ZGF.LogViewer.Benchmarks`:

1. Log generator: sizes from 1 MB to 20 GB, mixed formats, configurable line length distribution
   including multi-MB lines, CRLF and LF, UTF-8 and UTF-16.
2. `FileSource` and the line index, with tail (growth, truncation, rotation).
3. Plain and regex search streaming line-number results; filter combination.
4. Encoding detection.
5. Benchmarks for the targets above; klogg timings recorded alongside in the plan.

### Phase 1 (parallel) — viewer MVP

**Framework agent** — whatever the viewer needs that the framework lacks, e.g. a monospace
text-run cache for dense rows, column headers with resizable widths, a minimap scrollbar.

**App agent** — `ZGF.LogViewer`: `Program`, open flows (args, file picker, drag and drop), tabs,
main view over the line index, filtered pane, highlight rules, marks, follow mode, status bar,
settings persistence, theme.

Done when it replaces klogg for daily use.

### Phase 2 — structure

Formats (built-in + user file), columns, facets, time histogram, go to time, multi-line entries and
JSON expansion.

### Phase 3 — insight

Merged timeline, templates, run diff, MCP tools.

### Phase 4 — sources

Compressed files, folders, process sources (SSH, Docker, kubectl), saved sessions.

## Risks

- **Text throughput.** Fast scrolling through screens of dense monospace text is the renderer's
  hardest case; cache glyph runs per visible row and measure it in the frame benchmarks from
  phase 1.
- **Format edge cases** never end; the user format file keeps built-ins small.
- **Scope.** Phases 3–4 are the differentiators but wait until phase 1 is in daily use.
