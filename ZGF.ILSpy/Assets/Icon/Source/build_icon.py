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
BEAM = "#fff1cc"
BEAM_WARM = "#ffe3a2"
AMBER_TOP = "#ffcb63"
AMBER_BOTTOM = "#f08a1c"
OUTLINE = "#140e30"
GLASS_ROCK_TOP = "#8f6de8"
GLASS_ROCK_BOTTOM = "#6747c6"
GLASS_AMBER = "#f9a83a"
FLASHLIGHT_BODY = "#d4d3df"
FLASHLIGHT_BODY_HIGHLIGHT = "#eeedf5"
FLASHLIGHT_TAIL_CAP = "#aaa7bf"
FLASHLIGHT_BEZEL = "#f0a12e"
FLASHLIGHT_TAIL_BUTTON = "#1f1a3d"

ARCH_POINTS = [(110, 870), (128, 560), (205, 360), (285, 270), (420, 175), (505, 150),
               (600, 170), (735, 285), (830, 400), (895, 590), (914, 870)]
SHADE_POINTS = [(110, 870), (128, 560), (205, 360), (285, 270), (420, 175), (505, 150),
                (440, 260), (360, 420), (330, 870)]
HIGHLIGHT_POINTS = [(505, 150), (600, 170), (735, 285), (575, 212)]
HIGHLIGHT_CORNER_RADIUS = 12
HOLLOW_PATH = ("M 305 870 L 305 520 C 305 420 370 340 448 326 L 578 314 "
               "C 660 318 719 420 719 520 L 719 870 Z")
BRACE_TERMINAL_RADIUS = 8
LEFT_BRACE = ("M 405 358 L 390 358 C 345 358 325 378 325 425 L 325 540 C 325 561 316 577 292 595 "
              "C 316 613 325 629 325 650 L 325 771 C 325 818 345 838 390 838 L 405 838")
BEAM_ORIGIN = (710, 735)
AIM_DEGREES = 214
FLASHLIGHT_SCALE = 1.2
FREEFORM_BEAM_ORIGIN = (710, 735)
FREEFORM_FLASHLIGHT_SCALE = 1.25
FLASHLIGHT_OUTLINE = 12
BEAM_SPREAD_DEGREES = 15
BEAM_LENGTH = 540
BEAM_STRENGTH = 0.72
BEAM_NEAR_HALF_WIDTH = 38
BEAM_FEATHER = 30
BEAM_FEATHER_STEPS = 64
FLASHLIGHT_HEAD = [(0, -30), (52, -24), (52, 24), (0, 30)]
FLASHLIGHT_BARREL = [(48, -21), (206, -33), (206, 33), (48, 21)]
FLASHLIGHT_BARREL_HIGHLIGHT = [(54, -21), (202, -33), (202, -23), (54, -14)]
FLASHLIGHT_SWITCH = [(118, -33), (152, -35), (152, -27), (118, -25)]
FLASHLIGHT_TAIL_FACE = (212, 0, 17, 34)
FLASHLIGHT_TAIL_BUTTON_FACE = (215, 0, 7, 14)
FLASHLIGHT_GLOW = (-14, 0, 66, 66)
FLASHLIGHT_GLOW_STRENGTH = 0.5


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


ALL_PARTS = ("background", "rock", "opening", "beam", "braces", "flashlight")
FREEFORM_PARTS = ("rock", "opening", "beam", "braces", "flashlight")
FLAT = "flat"
GLASS = "glass"


def build(parts=ALL_PARTS, style=FLAT, beam_origin=BEAM_ORIGIN, flashlight_scale=FLASHLIGHT_SCALE):
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
        draw_opening(filled)
    if "beam" in parts:
        draw_beam(add, filled, beam_origin)
    if "braces" in parts:
        draw_braces(add, style)
    if "flashlight" in parts:
        draw_flashlight(add, filled, style, beam_origin, flashlight_scale)

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


def draw_opening(filled):
    filled(HOLLOW_PATH, color=HOLLOW)


def draw_beam(add, filled, center=BEAM_ORIGIN, unit=1, strength=BEAM_STRENGTH):
    far = toward(center, AIM_DEGREES, BEAM_LENGTH * unit)
    # Nested low-opacity cones feather both edges in exported PNGs as well as
    # editable documents. A live feather effect alone leaves a hard export edge.
    opacity = 1 - (1 - strength) ** (1 / BEAM_FEATHER_STEPS)
    for index in range(BEAM_FEATHER_STEPS):
        inset = BEAM_FEATHER * (2 * index / (BEAM_FEATHER_STEPS - 1) - 1)
        beam = filled(beam_cone(center, unit, inset), gradient=linear(list(center), list(far),
                      [stop(0, BEAM_WARM, opacity), stop(0.55, BEAM, opacity * 0.42), stop(1, BEAM, 0)]))
        add("object.setProps", id=f"{beam}.id", blend="Screen")


def beam_cone(center, unit, inset=0):
    across = AIM_DEGREES + 90
    near_left = toward(center, across, (BEAM_NEAR_HALF_WIDTH - inset) * unit)
    near_right = toward(center, across, -(BEAM_NEAR_HALF_WIDTH - inset) * unit)
    far_left = toward(center, AIM_DEGREES + BEAM_SPREAD_DEGREES, BEAM_LENGTH * unit)
    far_right = toward(center, AIM_DEGREES - BEAM_SPREAD_DEGREES, BEAM_LENGTH * unit)
    far_left = toward(far_left, across, -inset * unit)
    far_right = toward(far_right, across, inset * unit)
    return f"M {fmt(near_left)} L {fmt(far_left)} L {fmt(far_right)} L {fmt(near_right)} Z"


def toward(origin, degrees, length):
    angle = math.radians(degrees)
    return origin[0] + math.cos(angle) * length, origin[1] + math.sin(angle) * length


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


def draw_flashlight(add, filled, style, center=BEAM_ORIGIN, scale=FLASHLIGHT_SCALE, outline=FLASHLIGHT_OUTLINE):
    def part(points, radius, **fill):
        shape = filled(flashlight_polygon(points, radius * scale, center, scale), **fill)
        if style == GLASS:
            return
        add("paint.setStroke", ids=[f"{shape}.id"], color=OUTLINE)
        add("stroke.set", ids=[f"{shape}.id"], weight=outline, align="outside", join="round")

    if style == FLAT:
        glow = flashlight_polygon(ellipse_points(*FLASHLIGHT_GLOW), 0, center, scale)
        filled(glow, gradient={"kind": "radial", "stops": [stop(0, BEAM, FLASHLIGHT_GLOW_STRENGTH), stop(1, BEAM, 0)]})
    part(FLASHLIGHT_HEAD, 8, color=FLASHLIGHT_BEZEL)
    part(FLASHLIGHT_BARREL, 10, color=FLASHLIGHT_BODY)
    if style == FLAT:
        highlight = flashlight_polygon(FLASHLIGHT_BARREL_HIGHLIGHT, 6 * scale, center, scale)
        filled(highlight, color=FLASHLIGHT_BODY_HIGHLIGHT)
    part(FLASHLIGHT_SWITCH, 4, color=FLASHLIGHT_BEZEL)
    part(ellipse_points(*FLASHLIGHT_TAIL_FACE), 0, color=FLASHLIGHT_TAIL_CAP)
    button = flashlight_polygon(ellipse_points(*FLASHLIGHT_TAIL_BUTTON_FACE), 0, center, scale)
    filled(button, color=FLASHLIGHT_TAIL_BUTTON)


def flashlight_polygon(points, radius, center, scale):
    return rounded_polygon(placed_on_flashlight(points, center, scale), radius)


def placed_on_flashlight(points, center, scale):
    angle = math.radians(AIM_DEGREES - 180)
    cos = math.cos(angle)
    sin = math.sin(angle)
    placed = []
    for x, y in points:
        scaled_x = x * scale
        scaled_y = y * scale
        placed.append((center[0] + scaled_x * cos - scaled_y * sin,
                       center[1] + scaled_x * sin + scaled_y * cos))
    return placed


def ellipse_points(center_x, center_y, radius_x, radius_y, count=32):
    points = []
    for i in range(count):
        angle = 2 * math.pi * i / count
        points.append((center_x + radius_x * math.cos(angle), center_y + radius_y * math.sin(angle)))
    return points
