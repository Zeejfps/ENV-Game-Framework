import math

from build_icon import ACCENT, BLADE_DARK, BLADE_LIGHT, BLADE_MID, INTERIOR, LIGHT_FROM

BODY, OPENING, SEAM = "body", "opening", "seam"
FRAGMENT = 4

BRACE_10 = [".XX", ".X.", ".X.", "XX.", "X..", "X..", "XX.", ".X.", ".X.", ".XX"]
BRACE_8 = [".XX", ".X.", "XX.", "X..", "X..", "XX.", ".X.", ".XX"]

ART = {
    32: dict(height=28, tips=(2, 4, 6), half=8, flat=12, seams=True, brace=BRACE_10, brace_left=11),
    30: dict(height=28, tips=(1, 3, 5), half=7, flat=10, seams=True, brace=BRACE_8, brace_left=10),
    24: dict(height=22, tips=(1, 3), half=5, flat=8, seams=True, brace=None),
    20: dict(height=18, tips=(1, 3), half=4, flat=6, seams=True, brace=None),
    16: dict(height=14, tips=(1,), half=3, flat=6, seams=False, brace=None),
}


def body(size, height):
    top = (size - height) // 2
    cells = set()
    for row in range(height):
        inset = min(row, height - 1 - row)
        inset = (height // 2 - 1 - inset) // 2
        for x in range(inset, size - inset):
            cells.add((x, top + row))
    first, last = top, top + height - 1
    for y in (first, last):
        xs = sorted(x for x, cy in cells if cy == y)
        cells -= {(xs[0], y), (xs[-1], y)}
    return cells


def opening(size, tips, half, flat):
    rows = list(tips) + [half] * flat + list(reversed(tips))
    top = (size - len(rows)) // 2
    centre = size // 2
    return {(x, top + j): None for j, h in enumerate(rows) for x in range(centre - h, centre + h)}, top


def seams(size, tips, half, flat, body_cells):
    _, top = opening(size, tips, half, flat)
    centre = size // 2
    flat_top = top + len(tips)
    cells = set()
    for y in range(0, flat_top):
        cells.add((centre - half, y))
    x, y = centre - tips[0] + 2, top - 1
    while (x, y) in body_cells or (x + 1, y) in body_cells:
        cells |= {(x, y), (x + 1, y)}
        x, y = x + 2, y - 1
    x, y = centre + half, flat_top
    while (x, y) in body_cells or (x + 1, y) in body_cells:
        cells |= {(x, y), (x + 1, y)}
        x, y = x + 2, y + 1
    return cells | {(size - 1 - cx, size - 1 - cy) for cx, cy in cells}


def cells(size):
    spec = ART[size]
    shape = body(size, spec["height"])
    kind = {cell: BODY for cell in shape}
    hole, top = opening(size, spec["tips"], spec["half"], spec["flat"])
    for cell in hole:
        kind[cell] = OPENING
    if spec["seams"]:
        for cell in seams(size, spec["tips"], spec["half"], spec["flat"], shape):
            if kind.get(cell) == BODY:
                kind[cell] = SEAM
    braces = set()
    if spec["brace"]:
        brace_top = (size - len(spec["brace"])) // 2
        for j, row in enumerate(spec["brace"]):
            for i, c in enumerate(row):
                if c == "X":
                    x, y = spec["brace_left"] + i, brace_top + j
                    braces |= {(x, y), (size - 1 - x, y)}
    return kind, braces


def regions(kind):
    remaining = {cell for cell, value in kind.items() if value == BODY}
    found = []
    while remaining:
        seed = remaining.pop()
        region, stack = {seed}, [seed]
        while stack:
            x, y = stack.pop()
            for neighbour in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if neighbour in remaining:
                    remaining.remove(neighbour)
                    region.add(neighbour)
                    stack.append(neighbour)
        found.append(region)
    return found


def shade(size, x, y):
    centre = (size - 1) / 2
    light = math.cos(math.atan2(y - centre, x - centre) - LIGHT_FROM)
    return BLADE_LIGHT if light > 0.34 else BLADE_DARK if light < -0.34 else BLADE_MID


def colors(size):
    kind, braces = cells(size)
    lit = ART[size]["brace"] is None
    found = [region for region in regions(kind) if len(region) >= FRAGMENT]
    assert len(found) == (6 if ART[size]["seams"] else 1), (size, len(found))
    painted = {}
    for region in found:
        if len(found) == 1:
            for cell in region:
                painted[cell] = shade(size, *cell)
            continue
        x = sum(cx for cx, _ in region) / len(region)
        y = sum(cy for _, cy in region) / len(region)
        for cell in region:
            painted[cell] = shade(size, x, y)
    for cell, value in kind.items():
        if value == OPENING:
            painted[cell] = ACCENT if lit or cell in braces else INTERIOR
    return painted


def build(size):
    steps = []

    def add(command, **params):
        steps.append({"command": command, "params": params})
        return f"${len(steps) - 1}.id"

    add("file.new", width=size, height=size, units="Pixels", backgroundContents="transparent", created=None)
    painted = colors(size)
    for y in range(size):
        x = 0
        while x < size:
            color = painted.get((x, y))
            start = x
            while x < size and painted.get((x, y)) == color:
                x += 1
            if color is None:
                continue
            item = add("path.create", d=f"M {start} {y} L {x} {y} L {x} {y + 1} L {start} {y + 1} Z")
            add("paint.setFill", ids=[item], color=color)
            add("paint.setStroke", ids=[item], none=True)
    add("select.none")
    return steps
