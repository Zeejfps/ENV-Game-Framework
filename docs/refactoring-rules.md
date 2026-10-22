# Refactoring rules

How to restructure code so each piece has one job and sits with what it depends on. These apply on top of
`docs/code-readability.md`, which covers how each line and method reads.

## Responsibility

- **One reason to change.** Name the one thing a type, file or method is responsible for. If the description
  needs an "and", move the second part to whoever owns it.
- **Behaviour lives with the owner of its data.** A caller that runs a sequence of steps on another object's
  state is doing that object's job: give the object an intent-named method and call it.
- **Decide things where they vary.** Per-run inputs (command line, settings) are decided at the boundary.
  What a type intrinsically is (its fonts, its columns) belongs to the type. Expensive work happens where its
  owner controls the timing.
- **Whoever owns a mechanism wires it.** Expose an intent-named call (`host.InstallSynchronizationContext()`)
  instead of handing internals (queues, delegates, handles) to callers to assemble.

## Ownership and lifetime

- **Ownership follows lifetime.** An object that has to cope with a dependency not existing yet or already
  being gone is owned the wrong way round. Symptoms: `_x?.Call()` forwarding, buffering calls until something
  starts, caching values after something stops, `_stopped` flags, nullable fields assigned "later". Let the
  longer-lived side pass the shorter-lived one in, or give the dependency to a constructor so it is never null.
- **Symmetry.** What one place sets up, its mirror tears down: subscribe in startup, unsubscribe in shutdown.
- **Extension points are template methods.** A public non-virtual method calls a protected virtual `OnX`.
  Callers only call the public one.

## Passing things around

- **Pass context consistently.** Calls at the same level get the same context object. Don't hand out a raw
  piece (the bounds) at one moment and the whole (the host) at another.
- **One way to do a thing.** Don't pass two objects that offer the same capability.
- **Don't pass what the receiver can already reach.** If a callee has the object, it can derive the value.
- **Name data.** A tuple that crosses a method or type boundary becomes a record.

## Leftovers

- **Remove what the old design needed.** Indirections that no longer serve a purpose (posting what can be
  called directly, wrappers that only forward), members with no callers, unused assets. Search for callers
  before deleting.
- **Keep docs true.** After every move, fix XML docs and `cref`s that describe the old shape. Don't add
  rationale comments.

## Naming

- Types are nouns for what they hold or are, methods are verbs, and a new name never collides with an
  existing type elsewhere in the repo.

## Process

- Behaviour-preserving: no new features, no speculative extension points.
- Prefer the long-term structure over a local patch, even when the refactor is bigger.
- Small commits, each one building with the tests passing.
