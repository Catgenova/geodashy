"""The spectral rider ghost used for the death replay and the personal-best ghost.

A hooded, sheet-like spirit facing right: a rounded head with hollow eyes and a hem that waves
in three points. Eight frames bob up and down while the hem ripples and the glow
breathes. Output: Resources/Sprites/ghost_float_8.png (144 px frames, right-facing, same footprint
as the mounts so the runtime slicer treats it like one).

    python3 Tools/mount_art/ghost.py
"""
import math, sys
sys.path.insert(0, __file__.rsplit('/', 1)[0])
from render import *

W = H = 144

BODY = (0.86, 0.94, 1.0, 0.92)
BODY_D = (0.62, 0.74, 0.92, 0.92)
BODY_L = (0.97, 0.99, 1.0, 0.95)
OUT = (0.22, 0.30, 0.52, 0.9)
EYE = (0.10, 0.12, 0.28, 1.0)
EYE_GLOW = (0.55, 0.95, 1.0, 1.0)
GLOW = (0.55, 0.85, 1.0, 1.0)
OW = 2.4


def ghost_frame(t):
    """t in 0..1: one loop of bob, hem wave and breathing glow."""
    c = Canvas(W, H)
    ph = t * 2 * math.pi
    bob = 6 * math.sin(ph)
    cx, cy = 68, 84 + bob
    # halo
    c.glow(cx, cy - 6, 62, GLOW[:3] + (0.55 + 0.15 * math.sin(ph + 1.0),), steps=8)
    # body: rounded top, sides that taper a little, and a hem of three waving lobes
    top = cy + 40
    left, right = cx - 34, cx + 34
    hem = cy - 36
    pts = []
    # left side up, over the head, down the right side
    n = 20
    for i in range(n + 1):
        a = math.pi - i * math.pi / n     # pi .. 0
        pts.append((cx + 34 * math.cos(a), cy + 4 + 36 * math.sin(a)))
    # right side down to the hem
    pts.append((right + 2, hem + 8))
    # hem lobes right to left, each with its own wave phase
    lobes = 6
    for i in range(lobes + 1):
        u = i / lobes
        x = right + 2 - u * (right - left + 4)
        wave = math.sin(u * math.pi * 3 + ph * 1.6 + 0.6) * 6
        lobe = abs(math.sin(u * math.pi * 3))
        pts.append((x, hem - 4 - 9 * lobe + wave))
    pts.append((left - 2, hem + 8))
    c.poly(pts, fill=BODY, outline=OUT, ow=OW)
    # inner shading: darker along the left and the hem, a highlight over the head
    c.shade([(left + 4, hem + 6), (left + 18, hem + 2), (left + 22, cy + 30), (left + 4, cy + 20)], (BODY_D[0], BODY_D[1], BODY_D[2], 0.35))
    c.ellipse(cx - 8, cy + 26, 14, 8, fill=(BODY_L[0], BODY_L[1], BODY_L[2], 0.55), rot=-20)
    # hood fold across the brow
    c.stroke([(cx - 28, cy + 16), (cx - 6, cy + 24), (cx + 22, cy + 18)], 2.0, (BODY_D[0], BODY_D[1], BODY_D[2], 0.5))
    # face: two hollow eyes (the right one further forward) and a small open mouth
    blink = 1.0 if (t < 0.86 or t > 0.94) else 0.25
    for ex, ey, r in ((cx + 4, cy + 12, 6.5), (cx + 20, cy + 10, 6.0)):
        c.ellipse(ex, ey, r, r * blink, fill=EYE)
        c.ellipse(ex + 1.5, ey + 2 * blink, 2.2, 2.2 * blink, fill=(EYE_GLOW[0], EYE_GLOW[1], EYE_GLOW[2], 0.9))
    c.ellipse(cx + 14, cy - 4, 4.5, 3.5 + 1.5 * math.sin(ph * 2), fill=EYE)
    # trailing wisps behind the ghost, drifting with the loop
    for i in range(3):
        wx = cx - 46 - i * 9 - 4 * math.sin(ph + i)
        wy = cy - 20 + i * 6 + 5 * math.cos(ph * 1.3 + i * 1.7)
        c.ellipse(wx, wy, 5 - i, 3.5 - i * 0.7, fill=(BODY[0], BODY[1], BODY[2], 0.55 - i * 0.12))
    return c.down()


if __name__ == "__main__":
    frames = [ghost_frame(i / 8) for i in range(8)]
    write_png(f"{SPRITES_DIR}/ghost_float_{len(frames)}.png", frames)
    preview_grid(f"{PREVIEW_DIR}/ghost.png", frames, 2)
    print("ghost done")
