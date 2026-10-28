import math

SIZE = 1024

BLADE_LIGHT = "#8c96f0"
BLADE_MID = "#6670e0"
BLADE_DARK = "#4a56ef"
INTERIOR = "#0b0e1f"
ACCENT = "#3fd0e0"
TILE_TOP = "#1c2244"
TILE_BOTTOM = "#0d1124"

BLADES = 6
OPENING = 0.56
BODY_CORNER = 0.28
LIGHT_FROM = math.radians(-135)
BRACE_HEIGHT = 0.2
BRACE_OFFSET = 0.085
BRACE_WEIGHT = 0.034

TILE_MARGIN = 0.146
GAP = 0.02

ALL_PARTS = ("interior", "braces", "blades")
LENS_PARTS = ("interior", "braces")
BLADE_PARTS = ("blades",)


def regular(n, radius, center, rotation):
    return [(center[0] + radius * math.cos(rotation + 2 * math.pi * k / n),
             center[1] + radius * math.sin(rotation + 2 * math.pi * k / n)) for k in range(n)]


def rounded_regular(n, radius, corner, center, rotation, steps=12):
    points = []
    inner = radius - corner / math.cos(math.pi / n)
    for k in range(n):
        angle = rotation + 2 * math.pi * k / n
        cx, cy = center[0] + inner * math.cos(angle), center[1] + inner * math.sin(angle)
        for s in range(steps + 1):
            a = angle - math.pi / n + 2 * math.pi / n * s / steps
            points.append((cx + corner * math.cos(a), cy + corner * math.sin(a)))
    return points


def sub(a, b):
    return (a[0] - b[0], a[1] - b[1])


def cross(a, b):
    return a[0] * b[1] - a[1] * b[0]


def unit(v):
    length = math.hypot(*v)
    return (v[0] / length, v[1] / length)


def intersect(p, d, q, e):
    t = cross(sub(q, p), e) / cross(d, e)
    return (p[0] + d[0] * t, p[1] + d[1] * t)


def ray_hit(p, d, outline):
    best = None
    for i in range(len(outline)):
        a, b = outline[i], outline[(i + 1) % len(outline)]
        e = sub(b, a)
        denominator = cross(d, e)
        if abs(denominator) < 1e-12:
            continue
        t = cross(sub(a, p), e) / denominator
        u = cross(sub(a, p), d) / denominator
        if t > 1e-9 and 0 <= u <= 1 and (best is None or t < best[0]):
            best = (t, i, (p[0] + d[0] * t, p[1] + d[1] * t))
    return best[1], best[2]


def shifted(point, direction, probe, amount):
    normal = (-direction[1], direction[0])
    if (probe[0] - point[0]) * normal[0] + (probe[1] - point[1]) * normal[1] < 0:
        normal = (-normal[0], -normal[1])
    return (point[0] + normal[0] * amount, point[1] + normal[1] * amount)


def body_radius(size, plated):
    if plated:
        return size / 2 - size * TILE_MARGIN
    return size / 2 / (1 - BODY_CORNER * (1 / math.cos(math.pi / BLADES) - 1))


class Aperture:
    def __init__(self, size, radius):
        self.center = (size / 2, size / 2)
        self.opening = regular(BLADES, radius * OPENING, self.center, -math.pi / 2)
        self.outline = rounded_regular(BLADES, radius, radius * BODY_CORNER, self.center,
                                       -math.pi / 2 + math.pi / BLADES)

    def edge(self, k):
        a, b = self.opening[k % BLADES], self.opening[(k + 1) % BLADES]
        return a, unit(sub(b, a))

    def blade(self, k, gap):
        (a0, d0), (a1, d1) = self.edge(k), self.edge(k + 1)
        if gap:
            inside = self.opening[(k + 1) % BLADES]
            probe = (inside[0] + (d0[0] + d1[0]) * 3, inside[1] + (d0[1] + d1[1]) * 3)
            a0 = shifted(a0, d0, probe, gap / 2)
            a1 = shifted(a1, d1, probe, gap / 2)
        corner = intersect(a0, d0, a1, d1)
        i0, h0 = ray_hit(corner, d0, self.outline)
        i1, h1 = ray_hit(corner, d1, self.outline)
        count = len(self.outline)
        forward = [self.outline[(i0 + 1 + s) % count] for s in range((i1 - i0) % count)]
        backward = [self.outline[(i0 - s) % count] for s in range((i0 - i1) % count)]
        return [corner, h0] + (forward if len(forward) <= len(backward) else backward) + [h1]

    def tone(self, k):
        polygon = self.blade(k, 0)
        cx = sum(p[0] for p in polygon) / len(polygon) - self.center[0]
        cy = sum(p[1] for p in polygon) / len(polygon) - self.center[1]
        light = math.cos(math.atan2(cy, cx) - LIGHT_FROM)
        return BLADE_LIGHT if light > 0.34 else BLADE_DARK if light < -0.34 else BLADE_MID


def path_d(points):
    return "M " + " L ".join(f"{x:.3f} {y:.3f}" for x, y in points) + " Z"


def brace_path(cx, cy, height, facing):
    sign = 1 if facing == "open" else -1
    tip = cx + sign * height * 0.16
    nose = cx - sign * height * 0.16
    top, bottom = cy - height / 2, cy + height / 2
    r = height * 0.12
    return (f"M {tip} {top} C {cx} {top} {cx} {top} {cx} {top + r} L {cx} {cy - r} "
            f"C {cx} {cy - r * 0.3} {nose} {cy} {nose} {cy} C {nose} {cy} {cx} {cy + r * 0.3} {cx} {cy + r} "
            f"L {cx} {bottom - r} C {cx} {bottom} {cx} {bottom} {tip} {bottom}")


def build(size=SIZE, plated=False, parts=ALL_PARTS):
    radius = body_radius(size, plated)
    aperture = Aperture(size, radius)
    gap = max(1.0, size * GAP)
    steps = []

    def add(command, **params):
        steps.append({"command": command, "params": params})
        return f"${len(steps) - 1}.id"

    def filled(points, color):
        item = add("path.create", d=path_d(points))
        add("paint.setFill", ids=[item], color=color)
        add("paint.setStroke", ids=[item], none=True)

    add("file.new", width=size, height=size, units="Pixels", backgroundContents="transparent", created=None)
    if "interior" in parts:
        filled(aperture.opening, INTERIOR)
    if "braces" in parts:
        draw_braces(add, aperture.center, 2 * radius)
    if "blades" in parts:
        for k in range(BLADES):
            filled(aperture.blade(k, gap), aperture.tone(k))
    add("select.none")
    return steps


def draw_braces(add, center, span):
    height = span * BRACE_HEIGHT
    offset = span * BRACE_OFFSET
    for facing, dx in (("open", -offset), ("close", offset)):
        item = add("path.create", d=brace_path(center[0] + dx, center[1], height, facing))
        add("paint.setFill", ids=[item], none=True)
        add("paint.setStroke", ids=[item], color=ACCENT)
        add("stroke.set", ids=[item], weight=span * BRACE_WEIGHT, cap="round", join="round")
