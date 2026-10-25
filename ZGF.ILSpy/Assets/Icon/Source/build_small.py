from build_icon import (BACKGROUND_BOTTOM, BACKGROUND_TOP, HOLLOW, OUTLINE, ROCK_BOTTOM, ROCK_SHADE, ROCK_TOP,
                        linear, rounded_polygon, stop)

AMBER = "#ffb53f"
ROCK_FLAT = "#7656d4"

TILE_32 = {"inset": 1, "radius": 6}
ARCH_32 = [(3.5, 27.5), (4, 18), (6.5, 11.5), (9, 8.5), (13, 5.5), (16, 4.5), (19, 5.5), (23, 9),
           (26, 12.5), (28, 18.5), (28.5, 27.5)]
SHADE_32 = [(3.5, 27.5), (4, 18), (6.5, 11.5), (9, 8.5), (13, 5.5), (16, 4.5), (14, 8), (11.5, 13), (10.5, 27.5)]
HOLLOW_32 = "M 10 27.5 L 10 15 C 10 12 12 10 14.5 10 L 18 9.5 C 20.5 9.5 22 12 22 15 L 22 27.5 Z"
LEFT_BRACE_32_ROWS = {
    10: (9, 12), 11: (9, 12),
    12: (9, 10), 13: (9, 10), 14: (9, 10), 15: (9, 10),
    16: (8, 9), 17: (7, 8), 18: (7, 8), 19: (8, 9),
    20: (9, 10), 21: (9, 10), 22: (9, 10), 23: (9, 10),
    24: (9, 12), 25: (9, 12),
}

TILE_16 = {"inset": 0.5, "radius": 3}
ARCH_16 = [(1.5, 14), (2, 9), (3.5, 5.5), (5.5, 3.5), (8, 2.5), (10.5, 3.5), (12.5, 5.5), (14, 9), (14.5, 14)]
HOLLOW_16 = "M 5 14 L 5 7 C 5 5.9 5.9 5 7 5 L 9 5 C 10.1 5 11 5.9 11 7 L 11 14 Z"
LEFT_BRACE_16_PIXELS = [(5, 5), (6, 5), (5, 6), (5, 7), (4, 8), (5, 8), (4, 9), (5, 9),
                        (5, 10), (5, 11), (5, 12), (6, 12)]


def rows_to_pixels(rows):
    pixels = []
    for y, (first, last) in rows.items():
        for x in range(first, last + 1):
            pixels.append((x, y))
    return pixels


def mirrored(pixels, size):
    return [(size - 1 - x, y) for x, y in pixels]


def exterior_ring(pixels):
    occupied = set(pixels)
    ring = set()
    for x, y in pixels:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            neighbour = (x + dx, y + dy)
            if neighbour not in occupied:
                ring.add(neighbour)
    return sorted(ring)


def pixel_path(x, y):
    return f"M {x} {y} L {x + 1} {y} L {x + 1} {y + 1} L {x} {y + 1} Z"


def tile_path(size, tile):
    inset = tile["inset"]
    side = size - 2 * inset
    corners = [(inset, inset), (inset + side, inset), (inset + side, inset + side), (inset, inset + side)]
    return rounded_polygon(corners, tile["radius"])


def build(size):
    steps = []

    def add(command, **params):
        steps.append({"command": command, "params": params})
        return f"${len(steps) - 1}"

    def filled(d, **fill):
        shape = add("path.create", d=d)
        add("paint.setFill", ids=[f"{shape}.id"], **fill)
        add("paint.setStroke", ids=[f"{shape}.id"], none=True)
        return shape

    def pixels(cells, color):
        for x, y in cells:
            filled(pixel_path(x, y), color=color)

    add("file.new", width=size, height=size, units="Pixels", backgroundContents="transparent")
    if size == 32:
        draw_32(add, filled, pixels)
    else:
        draw_16(filled, pixels)
    add("select.none")
    return steps


def draw_32(add, filled, pixels):
    filled(tile_path(32, TILE_32),
           gradient=linear([16, 1], [16, 31], [stop(0, BACKGROUND_TOP), stop(1, BACKGROUND_BOTTOM)]))
    arch = rounded_polygon(ARCH_32, 1.5)
    filled(arch, gradient=linear([16, 4.5], [16, 27.5], [stop(0, ROCK_TOP), stop(1, ROCK_BOTTOM)]))
    shade = filled(rounded_polygon(SHADE_32, 1), color=ROCK_SHADE)
    add("object.setProps", id=f"{shade}.id", opacity=38)
    clip = add("path.create", d=arch)
    add("object.clippingMask.make", ids=[f"{shade}.id", f"{clip}.id"])
    filled(HOLLOW_32, color=HOLLOW)
    left = rows_to_pixels(LEFT_BRACE_32_ROWS)
    braces = left + mirrored(left, 32)
    pixels(exterior_ring(braces), OUTLINE)
    pixels(braces, AMBER)


def draw_16(filled, pixels):
    filled(tile_path(16, TILE_16),
           gradient=linear([8, 0.5], [8, 15.5], [stop(0, BACKGROUND_TOP), stop(1, BACKGROUND_BOTTOM)]))
    filled(rounded_polygon(ARCH_16, 1), color=ROCK_FLAT)
    filled(HOLLOW_16, color=HOLLOW)
    pixels(LEFT_BRACE_16_PIXELS + mirrored(LEFT_BRACE_16_PIXELS, 16), AMBER)
