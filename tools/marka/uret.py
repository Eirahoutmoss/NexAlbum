"""Marka varlıklarını üretir (Karar 17): uygulama simgesi (.ico) ve açılış ekranı (splash.png).

Gereksinim: Python 3, Pillow, Playwright (Chromium).
    pip install pillow playwright && python -m playwright install chromium
Çalıştırma (depo kökünden):
    python tools/marka/uret.py
Kaynaklar: docs/marka/nexalbum-logo.svg (>= 40 px), docs/marka/nexalbum-logo-kucuk.svg (<= 32 px), tools/marka/acilis.html.
"""

from pathlib import Path
from urllib.parse import quote

from PIL import Image
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "src" / "NexAlbum.UI" / "Assets"
LARGE = ROOT / "docs" / "marka" / "nexalbum-logo.svg"
SMALL = ROOT / "docs" / "marka" / "nexalbum-logo-kucuk.svg"
SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]


def render_svg(page, svg: Path, size: int) -> Image.Image:
    page.set_viewport_size({"width": size, "height": size})
    data = quote(svg.read_text(encoding="utf-8"))
    page.set_content(f"<html><body style='margin:0'><img src='data:image/svg+xml;utf8,{data}' width={size} height={size}></body></html>")
    page.wait_for_timeout(100)
    out = ASSETS / f"_tmp{size}.png"
    page.screenshot(path=str(out), omit_background=True)
    image = Image.open(out).convert("RGBA")
    image.load()
    out.unlink()
    return image


def main() -> None:
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page(device_scale_factor=1)
        icons = [render_svg(page, SMALL if s <= 32 else LARGE, s) for s in SIZES]
        icons[-1].save(ASSETS / "NexAlbum.ico", sizes=[(s, s) for s in SIZES], append_images=icons[:-1])
        icons[-1].save(ROOT / "docs" / "marka" / "nexalbum-logo-256.png")

        page.set_viewport_size({"width": 640, "height": 400})
        page.goto((ROOT / "tools" / "marka" / "acilis.html").as_uri())
        page.wait_for_timeout(500)
        page.screenshot(path=str(ASSETS / "splash.png"), omit_background=True, clip={"x": 0, "y": 0, "width": 640, "height": 400})
        browser.close()
    print("Üretildi:", ASSETS / "NexAlbum.ico", ASSETS / "splash.png")


if __name__ == "__main__":
    main()
