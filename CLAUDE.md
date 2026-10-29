# Project instructions

Follow the coding rules in @docs/coding_rules.md

## C#

- Check for null with a null check, and for a type with a type check:

  ```csharp
  // YES
  if (module.Place == null) return null;
  return module.Place.Container;

  if (assembly is not LoadedModule module) return null;

  // NO: property and empty patterns used as null checks
  if (assembly is LoadedModule { Place: { } place }) ...
  if (Find(path) is not { } node) return;
  ```

  A pattern like `{ Prop: { } x }` or `is { } x` hides that the check is a null check. Put the value
  in a local if needed, then write `== null` / `!= null`; use `is T x` only to test for a type.
