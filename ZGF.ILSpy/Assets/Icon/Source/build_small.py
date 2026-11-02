from build_icon import ACCENT, BLADES, INTERIOR, LID, SCLERA, Aperture, almond

BRACE_8 = [".XX", ".X.", "XX.", "X..", "X..", "XX.", ".X.", ".XX"]
BRACE_6 = [".X", ".X", "X.", "X.", ".X", ".X"]

ART = {
    32: dict(height=20, iris=11.5, gap=None, brace=BRACE_8, brace_left=11),
    30: dict(height=18, iris=10.8, gap=None, brace=BRACE_6, brace_left=11),
    24: dict(height=15, iris=7.6, gap=None, brace=None),
    20: dict(height=13, iris=6.4, gap=None, brace=None),
    16: dict(height=11, iris=5.2, gap=None, brace=None),
}


def contains(polygon, point):
    x, y = point
    inside = False
    for i in range(len(polygon)):
        (x0, y0), (x1, y1) = polygon[i - 1], polygon[i]
        if (y0 > y) != (y1 > y) and x < x0 + (y - y0) * (x1 - x0) / (y1 - y0):
            inside = not inside
    return inside


def colors(size):
    spec = ART[size]
    center = (size / 2, size / 2)
    outline = almond(center, size, spec["height"])
    iris = Aperture(size, spec["iris"])
    blades = [iris.blade(k, 0) for k in range(BLADES)]
    gapped = [iris.blade(k, spec["gap"]) for k in range(BLADES)] if spec["gap"] else blades
    eye = {(x, y) for y in range(size) for x in range(size) if contains(outline, (x + 0.5, y + 0.5))}
    painted = {}
    for x, y in eye:
        point = (x + 0.5, y + 0.5)
        rim = any(n not in eye for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
        if rim:
            painted[(x, y)] = LID
        elif contains(iris.opening, point):
            painted[(x, y)] = ACCENT if spec["brace"] is None else INTERIOR
        elif not contains(iris.outline, point):
            painted[(x, y)] = SCLERA
        else:
            blade = next((k for k in range(BLADES) if contains(gapped[k], point)), None)
            painted[(x, y)] = LID if blade is None else iris.tone(blade)
    if spec["brace"]:
        top = (size - len(spec["brace"])) // 2
        for j, row in enumerate(spec["brace"]):
            for i, c in enumerate(row):
                if c == "X":
                    x, y = spec["brace_left"] + i, top + j
                    for cell in ((x, y), (size - 1 - x, y)):
                        assert painted.get(cell) == INTERIOR, (size, cell)
                        painted[cell] = ACCENT
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
