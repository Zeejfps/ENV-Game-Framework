# ILSpy type diagram

Status: steps 1–5 built; step 6 open. Drafted 27 Oct 2026.

A **Diagram** document tab for exploring types: start from one type, grow the diagram along its
relations, click through to the code. It lives in the app, follows its theme and works offline.

The HTML **Create diagram…** export stays as it is: ILSpy's diagrammer, for sharing a page of an
assembly. Upstream it came from netAmermaid, a documentation tool run from the command line or CI; its
maintainers want an interactive in-app graph as a separate feature (netAmermaid#6, ILSpy#3331), which
is what this is.

## Decisions

| Axis | Choice | Consequence |
|---|---|---|
| Where | Native document tab, drawn by `ZGF.Gui.Immediate` | Navigation, tooltips and theme come for free; sharing waits for image export. |
| Tabs | One diagram grows: **Show in diagram** adds to the open diagram, **Show in new diagram** starts one | The diagram tab is a working set, not a snapshot. |
| Relations | Inheritance, interfaces (with derived types and implementations), and properties holding one or many diagram types | Field, parameter and return-type uses come later as an asked-for **Uses…** expansion; drawn always they bury the structure. |
| Layout | MSAGL (`AutomaticGraphLayout` 1.1.12, MIT, netstandard2.0, no dependencies), layered | Runs under Native AOT; 200 cards and 249 edges lay out in ~200 ms. Its only trim warnings are `BinaryFormatter` in debug dumps (`DebugCurveCollection.WriteToFile`, a dump in `Polygon.Distance`), which layout does not reach. |
| Re-layout | Whole layout on every change, cards animate from old to new places | MSAGL has no incremental layered layout; animation keeps the user's bearings. |

## What it shows

- **Cards.** A header with the type's icon, its name with generic arguments as the tree writes it, and
  *abstract*, *static* or *interface*. Collapsible sections for fields, properties, methods and events,
  in the tree's C# notation. Inherited members fold per declaring type ("from `Delegate`"), collapsed.
  Private and internal members of other assemblies are hidden; a filter shows them.
- **Edges.** Inherits: solid, hollow triangle. Implements: dashed, hollow triangle. Holds one / many:
  open arrow labelled with the property, `*` for many.
- **Growing.** Each card offers its bases, derived types, interfaces or implementations, and the types
  its properties hold; each card can be removed.
- **Moving.** Drag to pan, Ctrl+wheel to zoom, fit to view. A type's name and its members are links:
  underlined under the pointer, a click opens the code, and going back returns to the diagram. Resting
  on a member shows its documentation tooltip.

## Shape

| Layer | Pieces |
|---|---|
| Framework (`ZGF.Gui.Immediate`) | `UiPainter` gains cubic Béziers and dashed lines, which `ICanvas` already draws (`DrawCubicBezier`, `DashLength`), and hollow arrowheads from line segments. |
| Core (`ZGF.ILSpy.Core`) | `TypeCard`: a type with its members in sections, each member with its `CodeReference`, text and visibility. `TypeLink`: from, to, kind, label. Read from the loaded type system. `TypeLayout` wraps MSAGL: card sizes and links in, positions and edge curves out. |
| App state | `TabContent.Diagram` holding the diagram's types and its last layout, in a type that keeps them in step. A `Diagrams` feature file: `ShowInDiagram`, `ShowInNewDiagram`, `ExpandBases`, `ExpandDerived`, `ExpandInterfaces`, `ExpandHeld`, `RemoveFromDiagram`. Layout runs in the background; a newer layout supersedes an older one, as `UserIntent` drops stale results. |
| UiState / screen | Per tab: pan, zoom, selection, collapsed sections. `DiagramView` draws with `ui.Paint`, and navigates and shows tooltips through the existing `Navigate` and `CodeTooltips`. |

Derived types and implementations come from the loaded assemblies. `DerivedTypesNode.Find` ignores
cancellation and caches forever (`ilspy-improvements.md`, item 6); the expansion needs a cancellable,
uncached search.

## Steps

Each step ships something usable.

1. `UiPainter` curves, dashes and arrowheads, with tests.
2. Core cards, links and layout, with tests.
3. **Show in diagram** on a type: a tab with the type and its direct relations; pan, zoom, fit;
   a click opens code.
4. Growing and removing cards; re-layout with animation; **Show in new diagram**.
5. Member sections, inherited groups, the visibility filter, documentation tooltips.
6. Later: **Uses…** expansion, export to PNG / SVG, keyboard navigation between cards.
