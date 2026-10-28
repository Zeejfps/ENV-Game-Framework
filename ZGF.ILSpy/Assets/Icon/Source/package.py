"""Builds every app icon file from the vector sources with vectorcraft-cli and Pillow.

    python package.py <path to vectorcraft-cli>

Writes, next to this folder: app.ico (Windows), app_icon.png and Linux/ (Linux), app_icon.rgba (the
window icon the app embeds) and AppIcon.icon (macOS, compiled by actool when the app is bundled).
Keeps the editable .vectorcraft documents here.
"""

import json
import struct
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

from build_icon import BLADE_PARTS, LENS_PARTS, SIZE, TILE_BOTTOM, TILE_TOP, build
from build_small import build as build_small

SOURCE = Path(__file__).parent
OUTPUT = SOURCE.parent
HAND_PIXELLED_SIZES = (32, 30, 24, 20, 16)
VECTOR_SIZES = (512, 256, 128, 64, 48, 40, 36, 30)
ICO_SIZES = (16, 20, 24, 30, 32, 36, 40, 48, 64, 256)
LINUX_SIZES = (16, 24, 32, 48, 64, 128, 256, 512)
WINDOW_ICON_SIZES = (16, 32, 48, 64, 128, 256)
MAC_LAYERS = {
    "blades": BLADE_PARTS,
    "lens": LENS_PARTS,
}


def main():
    cli = sys.argv[1]
    with tempfile.TemporaryDirectory() as scratch:
        work = Path(scratch)
        icon = render(cli, work, "master", build(), keep="ilsight")
        frames = {}
        for size in HAND_PIXELLED_SIZES:
            frames[size] = render(cli, work, f"hand-{size}", build_small(size), keep=f"ilsight-{size}")
        for size in VECTOR_SIZES:
            frames[size] = render(cli, work, f"vector-{size}", build(size))
        layers = {}
        for name, parts in MAC_LAYERS.items():
            layers[name] = render(cli, work, f"layer-{name}", build(SIZE, plated=True, parts=parts))
        icon.save(OUTPUT / "app_icon.png")
        write_ico(frames)
        write_linux_icons(frames)
        write_window_icon(frames)
        write_mac_icon(layers)


def render(cli, work, name, steps, keep=None):
    commands = work / f"{name}.json"
    commands.write_text(json.dumps(steps), encoding="utf-8")
    png = work / f"{name}.png"
    arguments = [cli, "run", "--commands", str(commands), "--export", str(png)]
    if keep is not None:
        arguments += ["--export", str(SOURCE / f"{keep}.vectorcraft")]
    subprocess.run(arguments, check=True, capture_output=True)
    return Image.open(png).convert("RGBA")


def write_ico(frames):
    largest = frames[ICO_SIZES[-1]]
    smaller = [frames[size] for size in ICO_SIZES[:-1]]
    largest.save(OUTPUT / "app.ico", sizes=[(size, size) for size in ICO_SIZES], append_images=smaller)


def write_linux_icons(frames):
    folder = OUTPUT / "Linux"
    folder.mkdir(exist_ok=True)
    for size in LINUX_SIZES:
        frames[size].save(folder / f"app_icon_{size}.png")


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
        "fill": {"linear-gradient": [srgb(TILE_TOP), srgb(TILE_BOTTOM)]},
        "groups": [
            mac_group("Blades", "blades", glass=True, specular=True, shadow=0.25),
            mac_group("Lens", "lens", glass=False, specular=False, shadow=0),
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
