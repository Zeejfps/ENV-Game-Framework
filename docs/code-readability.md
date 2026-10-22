# Code readability rules

Code should be digestible at a glance: no decoding lines, details one jump away.

## Lines and levels

- Each line does one thing. Split lines that mix a call with `??`, a ternary, or a transformation.
- Each method stays at one level of abstraction. Entry points read as a list of named steps.
- `Program.cs` holds only the top-level steps; no helper functions. Helpers live on the type that owns the data they work with.
- Method names are verbs (`CreateWindowConfig`, `ListExistingAssemblies`), not nouns (`WindowConfig`, `StartupAssemblies`).
- Extract small intent-named helpers even when only a few lines long (`ReportOpenFailure(error)`, `IsStale(ticket)`), so a caller never mixes plumbing or lookup details with intent.
- Name the intent and hide the mechanism: `settings.RememberWindow(bounds)`, not `settings.Window = AppSettings.Sanitize(bounds)`. Prefer `app.OpenOnStartup(paths)` over `app.Post(() => ...)` in top-level code.
- Replace tricks with intent-named calls (`node.LoadChildren()`, not `_ = node.Children`).
- Magic strings and numbers become named constants next to their use.

## Ordering

- Methods follow the call chain: each method is followed by the methods it calls, in the order it calls them. A callback comes right after the method that passes it, so reading top-down follows execution.
- Shared helpers called from many places can live anywhere.

## Syntax

- Avoid uncommon syntax sugar: property patterns as null checks, collection-expression spreads, range slicing, ternaries inside arguments.
- Null checks use `x != null`, not `is { } x`. Patterns only when they do real work.
- Guard clauses with early return, one check per `if`.
- No chained ternaries. A single ternary only for trivial `a ? x : y` picks.
- Conditions with more than two parts get a name: a method (`IsCopyShortcut(stroke)`) or a local `bool`.
- Switch expressions are fine as flat lookup tables, one mapping per line.
- No increments inside expressions (`=> ++_x`); increment on its own line.
- `var` is fine everywhere. `with { }` is fine.

## Members

- Expression-bodied members (`=>`) only when the body does exactly one thing and the line is short. Otherwise use a block body, one step per line.

## LINQ

- One operator per line:
  ```csharp
  var bookmarks = found
      .Where(f => f.Place != null)
      .Select(f => Bookmark.FromRecord(f.Record, f.Place))
      .ToList();
  ```
- Lambdas inside stay small: one condition or one call.

## Lambdas

- Inline only when the body is a single expression with no nesting.
- Multi-statement bodies become named methods.
- Never nest lambdas; restructure with `async`/`await` instead.
