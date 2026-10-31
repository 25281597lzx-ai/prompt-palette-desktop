from PIL import Image, ImageDraw, ImageFilter
import math
import os

SIZE = 1024
MARGIN = 74
RADIUS = 208


def rounded_mask(box, radius, size=(SIZE, SIZE)):
    mask = Image.new("L", size, 0)
    d = ImageDraw.Draw(mask)
    x0, y0, x1, y1 = box
    d.rectangle((x0 + radius, y0, x1 - radius, y1), fill=255)
    d.rectangle((x0, y0 + radius, x1, y1 - radius), fill=255)
    d.ellipse((x0, y0, x0 + radius * 2, y0 + radius * 2), fill=255)
    d.ellipse((x1 - radius * 2, y0, x1, y0 + radius * 2), fill=255)
    d.ellipse((x0, y1 - radius * 2, x0 + radius * 2, y1), fill=255)
    d.ellipse((x1 - radius * 2, y1 - radius * 2, x1, y1), fill=255)
    return mask


img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

tile = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
pixels = tile.load()
span = float(SIZE - 2 * MARGIN)
for y in range(MARGIN, SIZE - MARGIN):
    t = (y - MARGIN) / span
    for x in range(MARGIN, SIZE - MARGIN):
        u = (x - MARGIN) / span
        k = min(1.0, max(0.0, u * 0.48 + t * 0.52))
        r = int(135 + (54 - 135) * k)
        g = int(106 + (204 - 106) * k)
        b = int(255 + (248 - 255) * k)
        pixels[x, y] = (r, g, b, 255)

img.paste(tile, (0, 0), rounded_mask((MARGIN, MARGIN, SIZE - MARGIN, SIZE - MARGIN), RADIUS))

# Subtle top highlight.
highlight = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
hd = ImageDraw.Draw(highlight)
hd.arc((MARGIN + 5, MARGIN + 5, SIZE - MARGIN - 5, SIZE - MARGIN - 5),
       start=196, end=320, fill=(255, 255, 255, 85), width=6)
highlight = highlight.filter(ImageFilter.GaussianBlur(3))
img.alpha_composite(highlight)

d = ImageDraw.Draw(img)


def star(cx, cy, outer, inner, points=4, rotation=-math.pi / 2):
    pts = []
    for i in range(points * 2):
        radius = outer if i % 2 == 0 else inner
        angle = rotation + math.pi * i / points
        pts.append((cx + math.cos(angle) * radius, cy + math.sin(angle) * radius))
    return pts


# Main sparkle.
d.polygon(star(512, 500, 238, 66), fill=(255, 255, 255, 255))
# Small satellite sparkle.
d.polygon(star(730, 300, 74, 21), fill=(255, 255, 255, 205))
# Little dot, gives the mark a "prompt cursor" feeling.
d.ellipse((690, 694, 738, 742), fill=(255, 255, 255, 210))

out_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
assets = os.path.join(out_dir, "assets")
os.makedirs(assets, exist_ok=True)
preview = img.resize((512, 512), Image.LANCZOS)
preview.save(os.path.join(assets, "icon-preview.png"))
img.save(os.path.join(assets, "app.ico"), format="ICO",
         sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("icon written")
