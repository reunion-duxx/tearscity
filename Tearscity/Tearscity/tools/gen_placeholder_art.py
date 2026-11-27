"""生成泪水之城（City of Tears）的占位美术资源。

后续替换为正式美术时，保持输出路径和尺寸不变即可。
运行：python tools/gen_placeholder_art.py
"""

from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]

# 阶段内容 ID：RitsuLib 会给 mod 模型加前缀，实际 Id.Entry = TEARSCITY_ACT_TEARSCITY
ACT_ID = "tearscity_act_tearscity"

MAP_SIZE = (2036, 1440)
BG_SIZE = (1920, 1080)

# 泪水之城配色
DEEP = (10, 20, 32)
MID = (24, 46, 66)
PALE = (108, 168, 196)
GLOW = (176, 226, 240)
WINDOW = (255, 214, 154)


def vertical_gradient(size, top, bottom):
    w, h = size
    img = Image.new("RGB", size, top)
    draw = ImageDraw.Draw(img)
    for y in range(h):
        t = y / max(1, h - 1)
        color = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        draw.line([(0, y), (w, y)], fill=color)
    return img


def add_spires(img, rng, count=14, base_y=None, color=(16, 32, 46), alpha=255):
    w, h = img.size
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    base_y = h if base_y is None else base_y
    for _ in range(count):
        cx = rng.randint(-100, w + 100)
        width = rng.randint(60, 220)
        height = rng.randint(int(h * 0.25), int(h * 0.75))
        top_y = base_y - height
        draw.rectangle(
            [cx - width // 2, top_y, cx + width // 2, base_y],
            fill=color + (alpha,),
        )
        # 尖顶
        draw.polygon(
            [
                (cx - width // 2, top_y),
                (cx, top_y - rng.randint(40, 160)),
                (cx + width // 2, top_y),
            ],
            fill=color + (alpha,),
        )
        # 窗户微光
        for _ in range(rng.randint(4, 18)):
            wx = rng.randint(cx - width // 2 + 6, cx + width // 2 - 6)
            wy = rng.randint(top_y + 12, base_y - 10)
            draw.rectangle([wx, wy, wx + 3, wy + 8], fill=WINDOW + (rng.randint(70, 170),))
    out = Image.alpha_composite(img.convert("RGBA"), layer)
    return out.convert("RGB")


def add_rain(img, rng, density=900):
    w, h = img.size
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for _ in range(density):
        x = rng.randint(-50, w + 50)
        y = rng.randint(0, h)
        length = rng.randint(10, 45)
        draw.line([(x, y), (x - 3, y + length)], fill=PALE + (rng.randint(18, 55),), width=1)
    out = Image.alpha_composite(img.convert("RGBA"), layer)
    return out.convert("RGB")


def soft_glow(img, center, radius, color, strength=90):
    w, h = img.size
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    steps = 24
    for i in range(steps):
        r = radius * (1 - i / steps)
        a = int(strength * (i / steps) ** 2)
        draw.ellipse([center[0] - r, center[1] - r, center[0] + r, center[1] + r], fill=color + (a,))
    layer = layer.filter(ImageFilter.GaussianBlur(radius // 6))
    out = Image.alpha_composite(img.convert("RGBA"), layer)
    return out.convert("RGB")


def make_map_piece(name, rng, top, bottom, spires, horizon_shift=0.0):
    img = vertical_gradient(MAP_SIZE, top, bottom)
    base_y = int(MAP_SIZE[1] * (0.95 + horizon_shift))
    img = add_spires(img, rng, count=spires, base_y=base_y, color=(12, 26, 40), alpha=230)
    img = add_spires(img, rng, count=max(3, spires // 3), base_y=base_y + 40, color=(8, 18, 30), alpha=255)
    img = add_rain(img, rng, density=400)
    img = soft_glow(img, (MAP_SIZE[0] // 2, int(MAP_SIZE[1] * 0.35)), 520, GLOW, 40)
    out = ROOT / "images" / "packed" / "map" / "map_bgs" / ACT_ID / name
    out.parent.mkdir(parents=True, exist_ok=True)
    img.save(out)
    return out


def make_background():
    rng = random.Random(7)
    img = vertical_gradient(BG_SIZE, (8, 16, 26), (30, 58, 80))
    # 远景穹顶与尖塔
    img = add_spires(img, rng, count=10, base_y=int(BG_SIZE[1] * 0.92), color=(14, 30, 46), alpha=200)
    img = add_spires(img, rng, count=6, base_y=int(BG_SIZE[1] * 1.02), color=(9, 20, 32), alpha=255)
    # 中央教堂式高塔
    layer = Image.new("RGBA", BG_SIZE, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    cx = BG_SIZE[0] // 2
    draw.rectangle([cx - 120, 240, cx + 120, BG_SIZE[1]], fill=(12, 26, 40, 255))
    draw.polygon([(cx - 120, 240), (cx, 90), (cx + 120, 240)], fill=(12, 26, 40, 255))
    draw.ellipse([cx - 46, 300, cx + 46, 392], fill=(150, 200, 220, 120))
    for y in range(420, BG_SIZE[1] - 60, 90):
        for x in range(cx - 90, cx + 100, 60):
            draw.rectangle([x, y, x + 14, y + 34], fill=WINDOW + (90,))
    img = Image.alpha_composite(img.convert("RGBA"), layer).convert("RGB")
    img = add_rain(img, rng, density=1600)
    img = soft_glow(img, (cx, 360), 620, GLOW, 70)
    img = soft_glow(img, (cx, BG_SIZE[1] - 60), 700, PALE, 45)
    out = ROOT / "Tearscity" / "images" / "tearscity_background.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    img.save(out)
    return out


def make_rest_site():
    rng = random.Random(11)
    img = make_rest_base()
    img = add_rain(img, rng, density=900)
    # 篝火暖光
    img = soft_glow(img, (BG_SIZE[0] // 2, int(BG_SIZE[1] * 0.72)), 420, (255, 176, 96), 120)
    out = ROOT / "Tearscity" / "images" / "tearscity_rest_site_bg.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    img.save(out)
    return out


def make_rest_base():
    img = vertical_gradient(BG_SIZE, (10, 20, 32), (26, 48, 68))
    rng = random.Random(3)
    img = add_spires(img, rng, count=8, base_y=int(BG_SIZE[1] * 0.9), color=(12, 26, 40), alpha=210)
    return img


def make_icon():
    img = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    draw.ellipse([8, 8, 120, 120], fill=(18, 38, 56, 255), outline=(108, 168, 196, 255), width=6)
    draw.polygon([(64, 24), (92, 96), (36, 96)], fill=(176, 226, 240, 255))
    draw.ellipse([52, 60, 76, 84], fill=(18, 38, 56, 255))
    out = ROOT / "icon.png"
    img.save(out)
    return out


def main():
    rng = random.Random(42)
    made = [
        make_map_piece(f"map_top_{ACT_ID}.png", rng, (6, 14, 24), (18, 38, 56), 12),
        make_map_piece(f"map_middle_{ACT_ID}.png", rng, (10, 22, 34), (26, 50, 70), 10),
        make_map_piece(f"map_bottom_{ACT_ID}.png", rng, (14, 30, 46), (34, 62, 84), 8, 0.03),
        make_background(),
        make_rest_site(),
        make_icon(),
    ]
    for p in made:
        print("wrote", p.relative_to(ROOT))


if __name__ == "__main__":
    main()
