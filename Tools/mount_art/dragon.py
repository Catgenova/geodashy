import math, sys
sys.path.insert(0, __file__.rsplit('/', 1)[0])
from render import *
from horse import TABARD, TABARD_D, TRIM, STEEL, STEEL_D, STEEL_L, PLUME, EYE, OUT, OUTLINE_W

W = H = 144
SCALE = hexc("8a2a24"); SCALE_D = darken(SCALE, 0.3); SCALE_L = lighten(SCALE, 0.15)
BELLY = hexc("e0b06a"); WING = hexc("5a1a18"); WING_MEM = hexc("c8503a"); HORN = hexc("e8dcc0"); FIRE = hexc("ffb03a")


def dragon_frame(t):
    c = Canvas(W, H)
    ph = 2 * math.pi * t
    flap = math.sin(ph)                    # +1 wings up, -1 wings down
    bob = -4 * math.cos(ph)                # body rises as wings push down
    cx, cy = 66, 56 + bob

    # ---- tail (behind everything) --------------------------------------------
    wave = 6 * math.sin(ph - 1.2)
    tail = bezier((cx - 30, cy - 2), (cx - 52, cy - 8 + wave), (cx - 62, cy + 14 + wave), (cx - 56, cy + 30 + wave * 0.5))
    for i in range(len(tail) - 1):
        w = 13 * (1 - i / len(tail)) + 3
        c.stroke([tail[i], tail[i + 1]], w + 2 * OUTLINE_W, OUT)
    for i in range(len(tail) - 1):
        w = 13 * (1 - i / len(tail)) + 3
        c.stroke([tail[i], tail[i + 1]], w, SCALE)
    tip = tail[-1]
    c.poly([(tip[0] - 6, tip[1] - 4), (tip[0] + 6, tip[1] - 2), (tip[0] + 2, tip[1] + 10), (tip[0] - 8, tip[1] + 6)], fill=SCALE_D, outline=OUT, ow=1.8)  # spade
    # dorsal spines along the tail
    for i in range(2, len(tail) - 3, 3):
        p, q = tail[i], tail[i + 1]
        c.poly([(p[0] - 3, p[1] + 3), (p[0] + 3, p[1] + 3), (p[0] + 1, p[1] + 10)], fill=HORN, outline=OUT, ow=1.4)

    # ---- wings ----------------------------------------------------------------------
    def wing(root, near):
        """Bat wing: shoulder -> elbow, then three fingers fanning back; flap rotates the whole wing about the root."""
        ang = -55 * flap + 10            # flap +1 (wings up) -> rotated so the wing points up-back; -1 -> down-back
        k = 1.0 if near else 0.9
        fill = WING_MEM if near else darken(WING_MEM, 0.25)
        bone = WING if near else darken(WING, 0.2)
        def P(dx, dy): return rot((root[0] + dx * k, root[1] + dy * k), root, ang)
        elbow = P(-16, 24)
        f1, f2, f3 = P(-56, 30), P(-44, 46), P(-22, 54)
        trail = P(-40, 6)
        mem = [root, elbow, f3, P(-36, 42), f2, P(-52, 30), f1, trail]
        c.poly(mem, fill=fill, outline=OUT, ow=OUTLINE_W)
        c.shade([root, elbow, f2, f1, trail], (0, 0, 0, 0.14))
        c.capsule(root, elbow, 7 * k, bone, OUT, 1.5)
        for f in (f1, f2, f3):
            c.capsule(elbow, f, 4 * k, bone, OUT, 1.4)
            c.ellipse(f[0], f[1], 2.2, 2.2, fill=HORN, outline=OUT, ow=1.2)
        c.ellipse(elbow[0], elbow[1], 4, 4, fill=bone)
        c.poly([(elbow[0] - 2, elbow[1] + 2), (elbow[0] + 3, elbow[1] + 3), (elbow[0] + 1, elbow[1] + 9)], fill=HORN, outline=OUT, ow=1.2)  # thumb claw
    wing((cx + 8, cy + 12), False)

    # ---- far legs ----------------------------------------------------------------
    def leg(hip, a_upper, bend, near):
        k = 1 if near else 0.85
        fill = SCALE if near else SCALE_D
        a1 = math.radians(a_upper); a2 = math.radians(a_upper - bend)
        knee = (hip[0] + 15 * math.sin(a1), hip[1] - 15 * math.cos(a1))
        foot = (knee[0] + 14 * math.sin(a2), knee[1] - 14 * math.cos(a2))
        c.capsule(hip, knee, 9 * k, fill, OUT, OUTLINE_W); c.capsule(knee, foot, 6 * k, fill, OUT, OUTLINE_W)
        c.ellipse(knee[0], knee[1], 4.8 * k, 4.8 * k, fill=fill)
        for j in range(3):  # claws
            c.poly([(foot[0] - 3 + j * 3, foot[1] + 1), (foot[0] - 1 + j * 3, foot[1] + 1), (foot[0] - 2 + j * 3 + 2, foot[1] - 6)], fill=HORN, outline=OUT, ow=1.2)
    tuck = 25 + 10 * flap
    leg((cx - 14, cy - 6), -35 + tuck * 0.2, 60, False)
    leg((cx + 20, cy - 8), 15, 70, False)

    # ---- body ----------------------------------------------------------------------
    body = ellipse_points(cx, cy, 40, 17, 6)
    c.poly(body, fill=SCALE, outline=OUT, ow=OUTLINE_W)
    c.ellipse(cx + 2, cy - 7, 32, 9, fill=BELLY, rot=6)                     # belly plates
    for i in range(6):
        x = cx - 22 + i * 9
        c.stroke([(x, cy - 15), (x + 2, cy - 1)], 1.2, darken(BELLY, 0.35))
    c.ellipse(cx - 4, cy + 9, 30, 6, fill=(SCALE_L[0], SCALE_L[1], SCALE_L[2], 0.5), rot=6)   # back highlight
    # dorsal spines on the back
    for i in range(5):
        x = cx - 26 + i * 10
        y = cy + 15 + (i - 2) ** 2 * -0.6
        c.poly([(x - 3, y), (x + 3, y), (x + 1, y + 8)], fill=HORN, outline=OUT, ow=1.4)

    # ---- neck & head ---------------------------------------------------------------
    nb = (cx + 34, cy + 6); nt = (cx + 56, cy + 34 - 3 * flap)
    d = (nt[0] - nb[0], nt[1] - nb[1]); L = math.hypot(*d); nx, ny = -d[1] / L, d[0] / L
    neck = [(nb[0] + nx * 11, nb[1] + ny * 11), (nb[0] - nx * 10, nb[1] - ny * 10), (nt[0] - nx * 7, nt[1] - ny * 7), (nt[0] + nx * 8, nt[1] + ny * 8)]
    c.poly(neck, fill=SCALE, outline=OUT, ow=OUTLINE_W)
    c.shade([neck[1], neck[2], ((neck[2][0] + neck[3][0]) / 2, (neck[2][1] + neck[3][1]) / 2), ((neck[1][0] + neck[0][0]) / 2, (neck[1][1] + neck[0][1]) / 2)], (BELLY[0], BELLY[1], BELLY[2], 0.55))
    hx, hy = nt[0] + 4, nt[1] + 2
    c.ellipse(hx + 6, hy + 2, 14, 9, fill=SCALE, outline=OUT, ow=OUTLINE_W, rot=-12)        # skull
    c.poly([(hx + 8, hy - 2), (hx + 26, hy - 8), (hx + 24, hy - 12), (hx + 6, hy - 8)], fill=SCALE, outline=OUT, ow=OUTLINE_W)   # upper jaw
    c.poly([(hx + 6, hy - 8), (hx + 20, hy - 14), (hx + 18, hy - 16), (hx + 4, hy - 11)], fill=SCALE_D, outline=OUT, ow=OUTLINE_W)  # lower jaw
    c.ellipse(hx + 6, hy + 2, 14, 9, fill=SCALE, rot=-12)
    for j in range(3):  # teeth
        x = hx + 10 + j * 5
        c.poly([(x, hy - 9 - j), (x + 2.5, hy - 9 - j), (x + 1, hy - 13 - j)], fill=HORN)
    c.ellipse(hx + 25, hy - 9, 1.4, 1.2, fill=OUT)   # nostril
    # horns and brow
    c.poly([(hx - 2, hy + 6), (hx + 4, hy + 8), (hx - 10, hy + 20)], fill=HORN, outline=OUT, ow=1.6)
    c.poly([(hx + 4, hy + 8), (hx + 9, hy + 8), (hx + 2, hy + 18)], fill=darken(HORN, 0.15), outline=OUT, ow=1.6)
    c.ellipse(hx + 9, hy + 3, 3.2, 2.6, fill=FIRE); c.ellipse(hx + 10, hy + 3, 1.4, 2.2, fill=EYE)
    # small flame breath on the down-stroke
    if flap < -0.3:
        k = -flap
        fl = bezier((hx + 27, hy - 10), (hx + 34, hy - 6), (hx + 40, hy - 14), (hx + 34 + 8 * k, hy - 18))
        c.stroke(fl, 7 * k, (FIRE[0], FIRE[1], FIRE[2], 0.8)); c.stroke(fl, 3.5 * k, hexc("fff0a0"))

    # ---- near wing, then the rider sitting behind its root -------------------------------
    wing((cx + 14, cy + 12), True)
    sx, sy = cx - 8, cy + 15
    c.ellipse(sx, sy, 14, 4.5, fill=hexc("5a2a12"), outline=OUT, ow=1.6)
    ry = sy - 2 * flap
    c.capsule((sx + 5, ry + 2), (sx + 10, ry - 10), 6.5, TABARD_D, OUT, 1.8)
    torso = [(sx - 8, ry + 2), (sx + 8, ry + 2), (sx + 9, ry + 21), (sx - 7, ry + 21)]
    c.poly(torso, fill=TABARD, outline=OUT, ow=OUTLINE_W)
    c.poly([(sx - 3, ry + 6), (sx + 3, ry + 6), (sx + 2, ry + 19), (sx - 2, ry + 19)], fill=TRIM)
    c.stroke([(sx - 8, ry + 5), (sx + 8, ry + 5)], 2, TRIM)
    c.capsule((sx + 6, ry + 15), (sx + 18, ry + 14), 5.5, TABARD, OUT, 1.8)     # arm on the reins
    c.ellipse(sx + 20, ry + 13, 3, 3, fill=STEEL, outline=OUT, ow=1.4)
    c.stroke(bezier((sx + 20, ry + 13), (sx + 34, ry + 18), (hx - 6, hy + 4), (hx + 8, hy - 2)), 1.4, hexc("5a2a12"))
    hx2, hy2 = sx, ry + 28
    c.ellipse(hx2, hy2, 8, 8.5, fill=STEEL, outline=OUT, ow=OUTLINE_W)
    c.poly([(hx2 - 8, hy2 - 1), (hx2 + 9, hy2 - 1), (hx2 + 9, hy2 - 4), (hx2 - 8, hy2 - 4)], fill=STEEL_D, outline=OUT, ow=1.4)
    c.ellipse(hx2 - 3, hy2 + 3, 3, 2, fill=(STEEL_L[0], STEEL_L[1], STEEL_L[2], 0.7))
    plume = bezier((hx2 - 1, hy2 + 8), (hx2 - 8, hy2 + 14), (hx2 - 18, hy2 + 10), (hx2 - 22, hy2 + 2))
    c.stroke(plume, 6, OUT); c.stroke(plume, 4, PLUME)

    # ---- near legs (in front of the body) ---------------------------------------------
    leg((cx - 10, cy - 4), -25 + tuck * 0.2, 60, True)
    leg((cx + 24, cy - 6), 20, 70, True)
    return c.down()


if __name__ == "__main__":
    frames = [dragon_frame(i / 8) for i in range(8)]
    out_dir = SPRITES_DIR
    write_png(f"{out_dir}/mount_dragon_fly_8.png", frames)
    scratch = PREVIEW_DIR
    preview_grid(f"{scratch}/dragon.png", frames, 2)
    print("dragon done")
