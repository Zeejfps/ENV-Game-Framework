"""Builds every app icon file from the vector sources with vectorcraft-cli and Pillow.

    python package.py <path to vectorcraft-cli>

Writes, next to this folder: app.ico (Windows), app_icon.png (Linux), app_icon.rgba (the window
icon the app embeds) and AppIcon.icon (macOS, compiled by actool when the app is bundled). Keeps
the editable .vectorcraft documents here.
"""

import json
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

from build_icon import (BACKGROUND_BOTTOM, BACKGROUND_TOP, FLAT, FREEFORM_BEAM_ORIGIN, FREEFORM_FLASHLIGHT_SCALE,
                        FREEFORM_PARTS, GLASS, build)
from build_small import build as build_small

SOURCE = Path(__file__).parent
OUTPUT = SOURCE.parent
MASTER = 1024
FREEFORM_MARGIN = 20
DEDICATED_SIZES = (32, 16)
BEAM_ONLY_SIZE = 24
MASTER_SIZES = (256, 128, 64, 48)
ICO_SIZES = (16, 24, 32, 48, 64, 128, 256)
WINDOW_ICON_SIZES = (16, 32, 48, 64, 128, 256)
MAC_LAYERS = {
    "flashlight": ("flashlight",),
    "braces": ("braces",),
    "opening": ("opening", "beam"),
    "rock": ("rock",),
}


def main():
    cli = sys.argv[1]
    with tempfile.TemporaryDirectory() as scratch:
        work = Path(scratch)
        freeform = build(FREEFORM_PARTS, FLAT, FREEFORM_BEAM_ORIGIN, FREEFORM_FLASHLIGHT_SCALE)
        master = render(cli, work, "master", freeform, keep="ilspelunk")
        dedicated = {}
        for size in DEDICATED_SIZES:
            dedicated[size] = render(cli, work, f"small-{size}", build_small(size), keep=f"ilspelunk-{size}")
        beam_only = build_small(32, flashlight=False)
        dedicated[BEAM_ONLY_SIZE] = render(cli, work, "beam-only", beam_only, scale=BEAM_ONLY_SIZE / 32)
        layers = {}
        for name, parts in MAC_LAYERS.items():
            layers[name] = render(cli, work, f"layer-{name}", build(parts, GLASS))
        icon = fit_to_canvas(master)
        frames = collect_frames(icon, dedicated)
        icon.save(OUTPUT / "app_icon.png")
        write_ico(frames)
        write_window_icon(frames)
        write_mac_icon(layers)


def render(cli, work, name, steps, keep=None, scale=1):
    commands = work / f"{name}.json"
    commands.write_text(json.dumps(steps), encoding="utf-8")
    png = work / f"{name}.png"
    arguments = [cli, "run", "--commands", str(commands), "--export", str(png), "--scale", str(scale)]
    if keep is not None:
        arguments += ["--export", str(SOURCE / f"{keep}.vectorcraft")]
    subprocess.run(arguments, check=True, capture_output=True)
    return Image.open(png).convert("RGBA")


def fit_to_canvas(art):
    shape = art.crop(art.getchannel("A").getbbox())
    room = MASTER - 2 * FREEFORM_MARGIN
    scale = room / max(shape.size)
    size = (round(shape.width * scale), round(shape.height * scale))
    canvas = Image.new("RGBA", (MASTER, MASTER), (0, 0, 0, 0))
    canvas.alpha_composite(shape.resize(size, Image.LANCZOS), ((MASTER - size[0]) // 2, (MASTER - size[1]) // 2))
    return canvas


def collect_frames(icon, dedicated):
    frames = dict(dedicated)
    for size in MASTER_SIZES:
        frames[size] = icon.resize((size, size), Image.LANCZOS)
    return frames


def write_ico(frames):
    largest = frames[ICO_SIZES[-1]]
    smaller = [frames[size] for size in ICO_SIZES[:-1]]
    largest.save(OUTPUT / "app.ico", sizes=[(size, size) for size in ICO_SIZES], append_images=smaller)


def write_window_icon(frames):
    packed = bytearray(struct.pack("<i", len(WINDOW_ICON_SIZES)))
    for size in WINDOW_ICON_SIZES:
        packed += struct.pack("<ii", size, size)
        packed += frames[size].tobytes()
    (OUTPUT / "app_icon.rgba").write_bytes(packed)


def write_mac_icon(layers):
    package = OUTPUT / "AppIcon.icon"
    assets = package / "Assets"
    assets.mkdir(parents=True, exist_ok=True)
    for name, image in layers.items():
        image.save(assets / f"{name}.png")
    (package / "icon.json").write_text(json.dumps(mac_icon_manifest(), indent=2), encoding="utf-8")


def mac_icon_manifest():
    return {
        "fill": {"linear-gradient": [srgb(BACKGROUND_TOP), srgb(BACKGROUND_BOTTOM)]},
        "groups": [
            mac_group("Flashlight", "flashlight", glass=True, specular=True, shadow=0.25),
            mac_group("Braces", "braces", glass=True, specular=True, shadow=0.2),
            mac_group("Opening", "opening", glass=False, specular=False, shadow=0),
            mac_group("Rock", "rock", glass=False, specular=False, shadow=0.15),
        ],
        "supported-platforms": {"squares": ["macOS"]},
    }


def mac_group(title, layer, glass, specular, shadow):
    return {
        "name": title,
        "layers": [{"image-name": f"{layer}.png", "name": layer, "glass": glass}],
        "shadow": {"kind": "neutral", "opacity": shadow},
        "specular": specular,
    }


def srgb(hex_color):
    channels = [int(hex_color[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    return "srgb:" + ",".join(f"{channel:.5f}" for channel in channels) + ",1.00000"


main()
