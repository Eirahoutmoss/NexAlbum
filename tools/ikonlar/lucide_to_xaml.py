"""Lucide (ISC) SVG ikonlarını WPF Geometry kaynaklarına dönüştürür.

Kullanım:
    npm pack lucide-static && tar xzf lucide-static-*.tgz
    python tools/ikonlar/lucide_to_xaml.py package/icons > src/NexAlbum.UI/Themes/Icons.xaml

Yeni ikon: aşağıdaki ICONS sözlüğüne "Anahtar": "lucide-adı" ekleyip yeniden üretin.
Çizgi kalınlığı ve uçlar NexIcon denetiminde verilir (24 birimlik kutu).
"""

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ICONS = {
    "New": "file-plus",
    "Open": "folder-open",
    "Save": "save",
    "SaveAs": "save-all",
    "AddPhotos": "image-plus",
    "AddFolder": "folder-plus",
    "AutoLayout": "sparkles",
    "Shuffle": "shuffle",
    "Retouch": "wand-sparkles",
    "Profile": "sliders-horizontal",
    "Preflight": "shield-check",
    "Export": "file-output",
    "Settings": "settings",
    "Info": "info",
    "Search": "search",
    "Sun": "sun",
    "Moon": "moon",
    "ChevronLeft": "chevron-left",
    "ChevronRight": "chevron-right",
    "Lock": "lock",
    "Unlock": "lock-open",
    "Templates": "layout-template",
    "Grid": "layout-grid",
    "Design": "palette",
    "Book": "book-open",
    "Gallery": "images",
    "Close": "x",
    "Minimize": "minus",
    "Maximize": "square",
    "Restore": "copy",
    "ZoomIn": "zoom-in",
    "ZoomOut": "zoom-out",
    "Fit": "scan",
    "Check": "check",
    "Warning": "triangle-alert",
    "Error": "circle-alert",
    "Clock": "clock",
    "Trash": "trash-2",
    "ExternalLink": "external-link",
    "Image": "image",
    "Eye": "eye",
    "Command": "command",
    "Page": "file-text",
    "Star": "star",
    "Album": "book-image",
    "Layers": "layers",
    "Frame": "frame",
    "Present": "presentation",
    "Undo": "undo-2",
    "Redo": "redo-2",
    "PagePlus": "file-plus-2",
    "Type": "type",
    "AlignLeft": "align-left",
    "AlignCenter": "align-center",
    "AlignRight": "align-right",
    "Bold": "bold",
    "Italic": "italic",
    "Plus": "plus",
    "Shapes": "shapes",
    "Expand": "expand",
    "Story": "book-heart",
    "Merge": "merge",
    "Split": "split",
    "ChevronDown": "chevron-down",
    "Sparkles": "sparkles",
}

NUM = r"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?"


def tokens(d):
    """SVG yol verisini komut ve sayılara böler; yay bayrakları (0/1) bitişik yazılmış olabilir."""
    out = []
    i = 0
    cmd = None
    arg = 0
    while i < len(d):
        c = d[i]
        if c.isalpha():
            cmd = c
            arg = 0
            out.append(c)
            i += 1
            continue
        if c in " ,\t\r\n":
            i += 1
            continue
        if cmd in "Aa" and arg % 7 in (3, 4):
            out.append(c)  # bayrak: tek karakter
            i += 1
            arg += 1
            continue
        m = re.match(NUM, d[i:])
        if not m:
            raise ValueError(f"Yol çözülemedi: {d[i:i + 20]!r}")
        out.append(n(float(m.group(0))))
        i += m.end()
        arg += 1
    # Öğeler birleştirildiği için baştaki göreli "m" mutlak yapılır (SVG'de ilk "m" zaten mutlaktır);
    # ardından gelen örtük çiftler göreli çizgi olarak kalmalı.
    if out and out[0] == "m":
        out[0] = "M"
        if len(out) > 3 and not out[3].isalpha():
            out.insert(3, "l")
    return " ".join(out)


def f(v):
    return float(v)


def n(v):
    s = f"{v:.3f}".rstrip("0").rstrip(".")
    return "0" if s == "-0" else s


def element(el):
    tag = el.tag.split("}")[-1]
    a = el.attrib
    if tag == "path":
        return tokens(a["d"])
    if tag == "line":
        return f"M {a['x1']} {a['y1']} L {a['x2']} {a['y2']}"
    if tag in ("polyline", "polygon"):
        pts = re.findall(NUM, a["points"])
        pairs = [f"{pts[i]} {pts[i + 1]}" for i in range(0, len(pts), 2)]
        return "M " + " L ".join(pairs) + (" Z" if tag == "polygon" else "")
    if tag == "circle":
        cx, cy, r = f(a["cx"]), f(a["cy"]), f(a["r"])
        return f"M {n(cx - r)} {n(cy)} A {n(r)} {n(r)} 0 1 0 {n(cx + r)} {n(cy)} A {n(r)} {n(r)} 0 1 0 {n(cx - r)} {n(cy)} Z"
    if tag == "ellipse":
        cx, cy, rx, ry = f(a["cx"]), f(a["cy"]), f(a["rx"]), f(a["ry"])
        return f"M {n(cx - rx)} {n(cy)} A {n(rx)} {n(ry)} 0 1 0 {n(cx + rx)} {n(cy)} A {n(rx)} {n(ry)} 0 1 0 {n(cx - rx)} {n(cy)} Z"
    if tag == "rect":
        x, y, w, h = f(a.get("x", 0)), f(a.get("y", 0)), f(a["width"]), f(a["height"])
        r = f(a.get("rx", a.get("ry", 0)))
        if r <= 0:
            return f"M {n(x)} {n(y)} H {n(x + w)} V {n(y + h)} H {n(x)} Z"
        return (f"M {n(x + r)} {n(y)} H {n(x + w - r)} A {n(r)} {n(r)} 0 0 1 {n(x + w)} {n(y + r)} "
                f"V {n(y + h - r)} A {n(r)} {n(r)} 0 0 1 {n(x + w - r)} {n(y + h)} "
                f"H {n(x + r)} A {n(r)} {n(r)} 0 0 1 {n(x)} {n(y + h - r)} "
                f"V {n(y + r)} A {n(r)} {n(r)} 0 0 1 {n(x + r)} {n(y)} Z")
    raise ValueError("Desteklenmeyen öğe: " + tag)


def main(folder):
    print('<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"')
    print('                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">')
    print("  <!-- Lucide ikonları (ISC lisansı, https://lucide.dev) — tools/ikonlar/lucide_to_xaml.py ile üretilir; elle düzenlemeyin. -->")
    for key, name in ICONS.items():
        root = ET.parse(Path(folder) / f"{name}.svg").getroot()
        data = " ".join(element(el) for el in root)
        print(f'  <!-- {name} -->')
        print(f'  <Geometry x:Key="Icon.{key}">{data}</Geometry>')
    print("</ResourceDictionary>")


if __name__ == "__main__":
    main(sys.argv[1])
