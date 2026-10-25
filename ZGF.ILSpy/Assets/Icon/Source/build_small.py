from build_icon import (BEAM, FLASHLIGHT_BEZEL, FLASHLIGHT_BODY_HIGHLIGHT, FLAT, FREEFORM_BEAM_ORIGIN,
                        FREEFORM_FLASHLIGHT_SCALE, HOLLOW, OUTLINE, ROCK_BOTTOM, ROCK_SHADE, ROCK_TOP, beam_cone,
                        draw_flashlight, linear, rounded_polygon, stop)

AMBER = "#ffb53f"
ROCK_FLAT = "#7656d4"

ZOOM_32 = 7 / 6
ZOOM_CENTER_32 = (16, 16.5)
ZOOM_SHIFT_32 = (-1, -1)


def zoomed(point):
    return (ZOOM_CENTER_32[0] + (point[0] - ZOOM_CENTER_32[0]) * ZOOM_32 + ZOOM_SHIFT_32[0],
            ZOOM_CENTER_32[1] + (point[1] - ZOOM_CENTER_32[1]) * ZOOM_32 + ZOOM_SHIFT_32[1])


def zoomed_points(points):
    return [zoomed(point) for point in points]


def zoomed_path(d):
    out = []
    pending = []
    for token in d.split():
        if token.isalpha():
            out.append(token)
            continue
        pending.append(float(token))
        if len(pending) == 2:
            x, y = zoomed(pending)
            out.append(f"{x:.3f} {y:.3f}")
            pending = []
    return " ".join(out)


ARCH_32 = zoomed_points([(3.5, 27.5), (4, 18), (6.5, 11.5), (9, 8.5), (13, 5.5), (16, 4.5), (19, 5.5), (23, 9),
                         (26, 12.5), (28, 18.5), (28.5, 27.5)])
SHADE_32 = zoomed_points([(3.5, 27.5), (4, 18), (6.5, 11.5), (9, 8.5), (13, 5.5), (16, 4.5), (14, 8), (11.5, 13),
                          (10.5, 27.5)])
HOLLOW_32 = zoomed_path("M 10 27.5 L 10 15 C 10 12 12 10 14.5 10 L 18 9.5 C 20.5 9.5 22 12 22 15 L 22 27.5 Z")
BRACE_MIRROR_32 = 2 * zoomed((16, 0))[0]
LEFT_BRACE_32 = zoomed_path("M 13 11 C 11 11 10 11.8 10 13.5 L 10 15.6 C 10 17 9.2 17.7 7.2 18 "
                            "C 9.2 18.3 10 19 10 20.4 L 10 22.5 C 10 24.2 11 25 13 25")
BRACE_WEIGHT_32 = 2
BRACE_SEPARATION_32 = 1
BEAM_ORIGIN_32 = zoomed((FREEFORM_BEAM_ORIGIN[0] / 32 + 0.4, FREEFORM_BEAM_ORIGIN[1] / 32 + 0.4))
UNIT_32 = ZOOM_32 / 32
FLASHLIGHT_SCALE_32 = FREEFORM_FLASHLIGHT_SCALE * UNIT_32
BEAM_TONES_32 = ((0.85, 45), (0.5, 50))
SMALL_BEAM = "#ffe08a"

ARCH_16 = [(1.5, 14), (2, 9), (3.5, 5.5), (5.5, 3.5), (8, 2.5), (10.5, 3.5), (12.5, 5.5), (14, 9), (14.5, 14)]
HOLLOW_16 = "M 5 14 L 5 7 C 5 5.9 5.9 5 7 5 L 9 5 C 10.1 5 11 5.9 11 7 L 11 14 Z"
LEFT_BRACE_16_PIXELS = {
    (6, 5): 100, (5, 5): 50,
    (5, 6): 100, (5, 7): 100,
    (4, 8): 100, (5, 8): 40, (4, 9): 100, (5, 9): 40,
    (5, 10): 100, (5, 11): 100,
    (5, 12): 50, (6, 12): 100,
}
BEAM_16_PIXELS = {(8, 11): 70, (8, 10): 70, (8, 9): 70, (7, 9): 70,
                  (7, 10): 40, (7, 8): 40, (8, 8): 40, (7, 7): 40}
FLASHLIGHT_GLOW_16_PIXELS = {(11, 11): 60}
FLASHLIGHT_HEAD_16_PIXELS = {(12, 12): 100, (13, 12): 100, (12, 13): 100}
FLASHLIGHT_BODY_16_PIXELS = {(13, 13): 100, (14, 14): 100, (15, 15): 100,
                             (14, 13): 45, (13, 14): 45, (15, 14): 45, (14, 15): 45}


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
        draw_16(add, filled, pixels)
    add("select.none")
    return steps


def draw_32(add, filled, stroked):
    arch = rounded_polygon(ARCH_32, 1.5)
    filled(arch, gradient=linear(list(zoomed((16, 4.5))), list(zoomed((16, 27.5))),
                                 [stop(0, ROCK_TOP), stop(1, ROCK_BOTTOM)]))
    shade = filled(rounded_polygon(SHADE_32, 1), color=ROCK_SHADE)
    add("object.setProps", id=f"{shade}.id", opacity=38)
    clip = add("path.create", d=arch)
    add("object.clippingMask.make", ids=[f"{shade}.id", f"{clip}.id"])
    filled(HOLLOW_32, color=HOLLOW)
    draw_beam_32(add, filled)
    braces = (LEFT_BRACE_32, mirrored_path(LEFT_BRACE_32, BRACE_MIRROR_32))
    for d in braces:
        stroked(d, OUTLINE, BRACE_WEIGHT_32 + 2 * BRACE_SEPARATION_32)
    for d in braces:
        stroked(d, AMBER, BRACE_WEIGHT_32)
    draw_flashlight(add, filled, FLAT, BEAM_ORIGIN_32, FLASHLIGHT_SCALE_32, BRACE_SEPARATION_32)


def draw_beam_32(add, filled):
    for length, strength in BEAM_TONES_32:
        beam = filled(beam_cone(BEAM_ORIGIN_32, UNIT_32 * length), color=SMALL_BEAM)
        add("object.setProps", id=f"{beam}.id", opacity=strength, blend="Screen")


def draw_16(add, filled, pixels):
    filled(rounded_polygon(ARCH_16, 1), color=ROCK_FLAT)
    filled(HOLLOW_16, color=HOLLOW)
    pixels(BEAM_16_PIXELS, SMALL_BEAM)
    pixels(LEFT_BRACE_16_PIXELS | mirrored_pixels(LEFT_BRACE_16_PIXELS, 16), AMBER)
    pixels(FLASHLIGHT_GLOW_16_PIXELS, BEAM)
    pixels(FLASHLIGHT_HEAD_16_PIXELS, FLASHLIGHT_BEZEL)
    pixels(FLASHLIGHT_BODY_16_PIXELS, FLASHLIGHT_BODY_HIGHLIGHT)
