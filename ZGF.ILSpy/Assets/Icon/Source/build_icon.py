import math

SIZE = 1024
BRACE_WEIGHT = 57
BRACE_OUTLINE = 10
ARCH_CORNER_RADIUS = 46
FACET_CORNER_RADIUS = 30

BACKGROUND_TOP = "#2c2468"
BACKGROUND_BOTTOM = "#161236"
ROCK_TOP = "#9d7bf0"
ROCK_BOTTOM = "#5636b0"
ROCK_SHADE = "#24125e"
ROCK_HIGHLIGHT = "#ffffff"
HOLLOW = "#120d2c"
BEAM = "#ffe9c2"
AMBER_TOP = "#ffcb63"
AMBER_BOTTOM = "#f08a1c"
OUTLINE = "#140e30"
GLASS_ROCK_TOP = "#8f6de8"
GLASS_ROCK_BOTTOM = "#6747c6"
GLASS_AMBER = "#f9a83a"

ARCH_POINTS = [(110, 870), (128, 560), (205, 360), (285, 270), (420, 175), (505, 150),
               (600, 170), (735, 285), (830, 400), (895, 590), (914, 870)]
SHADE_POINTS = [(110, 870), (128, 560), (205, 360), (285, 270), (420, 175), (505, 150),
                (440, 260), (360, 420), (330, 870)]
HIGHLIGHT_POINTS = [(505, 150), (600, 170), (735, 285), (575, 212)]
HIGHLIGHT_CORNER_RADIUS = 12
HOLLOW_PATH = ("M 305 870 L 305 520 C 305 420 370 340 448 326 L 578 314 "
               "C 660 318 719 420 719 520 L 719 870 Z")
BEAM_PATH = "M 616 378 L 654 392 L 552 772 L 408 772 Z"
BEAM_FADE_END = 0.8
BRACE_TERMINAL_RADIUS = 8
LEFT_BRACE = ("M 405 358 L 390 358 C 345 358 325 378 325 425 L 325 540 C 325 561 316 577 292 595 "
              "C 316 613 325 629 325 650 L 325 771 C 325 818 345 838 390 838 L 405 838")


def rounded_polygon(points, radius):
    count = len(points)
    commands = []
    for i in range(count):
        previous = points[i - 1]
        current = points[i]
        following = points[(i + 1) % count]
        into = unit(current, previous)
        out_of = unit(current, following)
        cut = min(radius, distance(current, previous) / 2, distance(current, following) / 2)
        start = (current[0] + into[0] * cut, current[1] + into[1] * cut)
        end = (current[0] + out_of[0] * cut, current[1] + out_of[1] * cut)
        verb = "M" if i == 0 else "L"
        commands.append(f"{verb} {fmt(start)} Q {fmt(current)} {fmt(end)}")
    return " ".join(commands) + " Z"


def unit(origin, target):
    length = distance(origin, target)
    return ((target[0] - origin[0]) / length, (target[1] - origin[1]) / length)


def distance(a, b):
    return math.hypot(b[0] - a[0], b[1] - a[1])


def fmt(point):
    return f"{point[0]:.1f} {point[1]:.1f}"


def mirror(d):
    out = []
    expect_x = True
    for token in d.split():
        if token.isalpha():
            out.append(token)
            expect_x = True
            continue
        if expect_x:
            out.append(f"{SIZE - float(token):g}")
        else:
            out.append(token)
        expect_x = not expect_x
    return " ".join(out)


def linear(start, end, stops):
    return {"kind": "linear", "start": start, "end": end, "stops": stops}


def stop(offset, color, opacity=1):
    return {"offset": offset, "color": color, "opacity": opacity}


ALL_PARTS = ("background", "rock", "opening", "braces")
FLAT = "flat"
GLASS = "glass"


def build(parts=ALL_PARTS, style=FLAT):
    steps = []

    def add(command, **params):
        steps.append({"command": command, "params": params})
        return f"${len(steps) - 1}"

    def filled(d, **fill):
        shape = add("path.create", d=d)
        add("paint.setFill", ids=[f"{shape}.id"], **fill)
        add("paint.setStroke", ids=[f"{shape}.id"], none=True)
        return shape

    def clipped_to(shape, clip_d):
        clip = add("path.create", d=clip_d)
        add("object.clippingMask.make", ids=[f"{shape}.id", f"{clip}.id"])

    arch = rounded_polygon(ARCH_POINTS, ARCH_CORNER_RADIUS)

    add("file.new", width=SIZE, height=SIZE, units="Pixels", backgroundContents="transparent", created=None)

    if "background" in parts:
        draw_background(filled)
    if "rock" in parts:
        draw_rock(add, filled, clipped_to, arch, style)
    if "opening" in parts:
        draw_opening(add, filled, clipped_to, style)
    if "braces" in parts:
        draw_braces(add, style)

    add("select.none")
    return steps


def draw_background(filled):
    filled(f"M 0 0 L {SIZE} 0 L {SIZE} {SIZE} L 0 {SIZE} Z",
           gradient=linear([512, 0], [512, SIZE], [stop(0, BACKGROUND_TOP), stop(1, BACKGROUND_BOTTOM)]))


def draw_rock(add, filled, clipped_to, arch, style):
    top = GLASS_ROCK_TOP if style == GLASS else ROCK_TOP
    bottom = GLASS_ROCK_BOTTOM if style == GLASS else ROCK_BOTTOM
    filled(arch, gradient=linear([512, 150], [512, 870], [stop(0, top), stop(1, bottom)]))

    shade = filled(rounded_polygon(SHADE_POINTS, FACET_CORNER_RADIUS), color=ROCK_SHADE)
    add("object.setProps", id=f"{shade}.id", opacity=38)
    clipped_to(shade, arch)

    highlight = filled(rounded_polygon(HIGHLIGHT_POINTS, HIGHLIGHT_CORNER_RADIUS), color=ROCK_HIGHLIGHT)
    add("object.setProps", id=f"{highlight}.id", opacity=16)
    clipped_to(highlight, arch)


def draw_opening(add, filled, clipped_to, style):
    filled(HOLLOW_PATH, color=HOLLOW)
    if style == GLASS:
        return

    beam = filled(BEAM_PATH, gradient=linear([635, 385], [480, 800],
                                             [stop(0, BEAM, 0.28), stop(BEAM_FADE_END, BEAM, 0)]))
    add("object.setProps", id=f"{beam}.id", blend="Screen")
    clipped_to(beam, HOLLOW_PATH)


def draw_braces(add, style):
    for d in (LEFT_BRACE, mirror(LEFT_BRACE)):
        brace = add("path.create", d=d)
        add("paint.setFill", ids=[f"{brace}.id"], none=True)
        add("paint.setStroke", ids=[f"{brace}.id"], color=AMBER_BOTTOM)
        add("stroke.set", ids=[f"{brace}.id"], weight=BRACE_WEIGHT, cap="butt", join="miter", miterLimit=10)
        add("select.set", ids=[f"{brace}.id"])
        outlined = add("object.path.outlineStroke")
        add("effect.apply", ids=[f"{outlined}.ids.0"], effect="stylize.roundCorners",
            params={"radius": BRACE_TERMINAL_RADIUS})
        if style == GLASS:
            add("paint.setFill", ids=[f"{outlined}.ids.0"], color=GLASS_AMBER)
            add("paint.setStroke", ids=[f"{outlined}.ids.0"], none=True)
            continue
        add("paint.setFill", ids=[f"{outlined}.ids.0"],
            gradient=linear([512, 330], [512, 866], [stop(0, AMBER_TOP), stop(1, AMBER_BOTTOM)]))
        add("paint.setStroke", ids=[f"{outlined}.ids.0"], color=OUTLINE)
        add("stroke.set", ids=[f"{outlined}.ids.0"], weight=BRACE_OUTLINE, align="outside", join="round")
