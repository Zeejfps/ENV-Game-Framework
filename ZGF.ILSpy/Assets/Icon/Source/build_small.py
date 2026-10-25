from build_icon import (BACKGROUND_BOTTOM, BACKGROUND_TOP, HOLLOW, OUTLINE, ROCK_BOTTOM, ROCK_SHADE, ROCK_TOP,
                        linear, rounded_polygon, stop)

AMBER = "#ffb53f"
ROCK_FLAT = "#7656d4"

TILE_32 = {"inset": 1, "radius": 6}
ARCH_32 = [(3.5, 27.5), (4, 18), (6.5, 11.5), (9, 8.5), (13, 5.5), (16, 4.5), (19, 5.5), (23, 9),
           (26, 12.5), (28, 18.5), (28.5, 27.5)]
SHADE_32 = [(3.5, 27.5), (4, 18), (6.5, 11.5), (9, 8.5), (13, 5.5), (16, 4.5), (14, 8), (11.5, 13), (10.5, 27.5)]
HOLLOW_32 = "M 10 27.5 L 10 15 C 10 12 12 10 14.5 10 L 18 9.5 C 20.5 9.5 22 12 22 15 L 22 27.5 Z"
LEFT_BRACE_32 = ("M 13 11 C 11 11 10 11.8 10 13.5 L 10 15.6 C 10 17 9.2 17.7 7.2 18 "
                 "C 9.2 18.3 10 19 10 20.4 L 10 22.5 C 10 24.2 11 25 13 25")
BRACE_WEIGHT_32 = 2
BRACE_SEPARATION_32 = 1

TILE_16 = {"inset": 0.5, "radius": 3}
ARCH_16 = [(1.5, 14), (2, 9), (3.5, 5.5), (5.5, 3.5), (8, 2.5), (10.5, 3.5), (12.5, 5.5), (14, 9), (14.5, 14)]
HOLLOW_16 = "M 5 14 L 5 7 C 5 5.9 5.9 5 7 5 L 9 5 C 10.1 5 11 5.9 11 7 L 11 14 Z"
LEFT_BRACE_16_PIXELS = {
    (6, 5): 100, (5, 5): 50,
    (5, 6): 100, (5, 7): 100,
    (4, 8): 100, (5, 8): 40, (4, 9): 100, (5, 9): 40,
    (5, 10): 100, (5, 11): 100,
    (5, 12): 50, (6, 12): 100,
}


def mirrored_path(d, size):
    out = []
    expect_x = True
    for token in d.split():
        if token.isalpha():
            out.append(token)
            expect_x = True
            continue
        out.append(f"{size - float(token):g}" if expect_x else token)
        expect_x = not expect_x
    return " ".join(out)


def mirrored_pixels(pixels, size):
    return {(size - 1 - x, y): strength for (x, y), strength in pixels.items()}


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

    def stroked(d, color, weight):
        shape = add("path.create", d=d)
        add("paint.setFill", ids=[f"{shape}.id"], none=True)
        add("paint.setStroke", ids=[f"{shape}.id"], color=color)
        add("stroke.set", ids=[f"{shape}.id"], weight=weight, cap="round", join="miter", miterLimit=4)

    def pixels(cells, color):
        for (x, y), strength in cells.items():
            pixel = filled(pixel_path(x, y), color=color)
            if strength < 100:
                add("object.setProps", id=f"{pixel}.id", opacity=strength)

    add("file.new", width=size, height=size, units="Pixels", backgroundContents="transparent", created=None)
    if size == 32:
        draw_32(add, filled, stroked)
    else:
        draw_16(filled, pixels)
    add("select.none")
    return steps


def draw_32(add, filled, stroked):
    filled(tile_path(32, TILE_32),
           gradient=linear([16, 1], [16, 31], [stop(0, BACKGROUND_TOP), stop(1, BACKGROUND_BOTTOM)]))
    arch = rounded_polygon(ARCH_32, 1.5)
    filled(arch, gradient=linear([16, 4.5], [16, 27.5], [stop(0, ROCK_TOP), stop(1, ROCK_BOTTOM)]))
    shade = filled(rounded_polygon(SHADE_32, 1), color=ROCK_SHADE)
    add("object.setProps", id=f"{shade}.id", opacity=38)
    clip = add("path.create", d=arch)
    add("object.clippingMask.make", ids=[f"{shade}.id", f"{clip}.id"])
    filled(HOLLOW_32, color=HOLLOW)
    braces = (LEFT_BRACE_32, mirrored_path(LEFT_BRACE_32, 32))
    for d in braces:
        stroked(d, OUTLINE, BRACE_WEIGHT_32 + 2 * BRACE_SEPARATION_32)
    for d in braces:
        stroked(d, AMBER, BRACE_WEIGHT_32)


def draw_16(filled, pixels):
    filled(tile_path(16, TILE_16),
           gradient=linear([8, 0.5], [8, 15.5], [stop(0, BACKGROUND_TOP), stop(1, BACKGROUND_BOTTOM)]))
    filled(rounded_polygon(ARCH_16, 1), color=ROCK_FLAT)
    filled(HOLLOW_16, color=HOLLOW)
    pixels(LEFT_BRACE_16_PIXELS | mirrored_pixels(LEFT_BRACE_16_PIXELS, 16), AMBER)
