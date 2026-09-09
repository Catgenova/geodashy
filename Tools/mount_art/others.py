import math, sys
sys.path.insert(0, __file__.rsplit('/', 1)[0])
from render import *
from horse import TABARD, TABARD_D, TRIM, STEEL, STEEL_D, STEEL_L, PLUME, EYE, OUT, OUTLINE_W, TACK, TACK_L, HOOF

W = H = 144
OUT_DIR = SPRITES_DIR
SCRATCH = PREVIEW_DIR


def rider(c, sx, sy, ph=0.0, bob=0.0, lean=0.0, reins_to=None, small=False):
    """The knight, seated at (sx, sy). lean tilts the torso forward (deg)."""
    k = 0.8 if small else 1.0
    ry = sy + bob
    c.capsule((sx + 5 * k, ry + 2), (sx + 10 * k, ry - 11 * k), 6.5 * k, TABARD_D, OUT, 1.8)
    c.ellipse(sx + 11 * k, ry - 13 * k, 4 * k, 2.5 * k, fill=STEEL_D, outline=OUT, ow=1.4)
    torso = [rot(p, (sx, ry), -lean) for p in [(sx - 8 * k, ry + 2), (sx + 8 * k, ry + 2), (sx + 9 * k, ry + 21 * k), (sx - 7 * k, ry + 21 * k)]]
    c.poly(torso, fill=TABARD, outline=OUT, ow=OUTLINE_W)
    stripe = [rot(p, (sx, ry), -lean) for p in [(sx - 3 * k, ry + 6 * k), (sx + 3 * k, ry + 6 * k), (sx + 2 * k, ry + 19 * k), (sx - 2 * k, ry + 19 * k)]]
    c.poly(stripe, fill=TRIM)
    belt = [rot(p, (sx, ry), -lean) for p in [(sx - 8 * k, ry + 5 * k), (sx + 8 * k, ry + 5 * k)]]
    c.stroke(belt, 2, TRIM)
    arm0 = rot((sx + 6 * k, ry + 15 * k), (sx, ry), -lean); arm1 = rot((sx + 18 * k, ry + 13 * k), (sx, ry), -lean)
    c.capsule(arm0, arm1, 5.5 * k, TABARD, OUT, 1.8)
    c.ellipse(arm1[0] + 2, arm1[1], 3 * k, 3 * k, fill=STEEL, outline=OUT, ow=1.4)
    if reins_to is not None:
        c.stroke(bezier((arm1[0] + 2, arm1[1]), ((arm1[0] + reins_to[0]) / 2, arm1[1] + 6), reins_to, reins_to), 1.4, TACK)
    hx, hy = rot((sx, ry + 28 * k), (sx, ry), -lean)
    c.ellipse(hx, hy, 8 * k, 8.5 * k, fill=STEEL, outline=OUT, ow=OUTLINE_W)
    band = [rot(p, (hx, hy), -lean) for p in [(hx - 8 * k, hy - 1), (hx + 9 * k, hy - 1), (hx + 9 * k, hy - 4), (hx - 8 * k, hy - 4)]]
    c.poly(band, fill=STEEL_D, outline=OUT, ow=1.4)
    c.ellipse(hx - 3 * k, hy + 3 * k, 3 * k, 2 * k, fill=(STEEL_L[0], STEEL_L[1], STEEL_L[2], 0.7))
    plume = bezier((hx - 1, hy + 8 * k), (hx - 8, hy + 14 * k), (hx - 18, hy + 10 * k + 2 * math.sin(ph * 2)), (hx - 22, hy + 2))
    c.stroke(plume, 6 * k, OUT); c.stroke(plume, 4 * k, PLUME)


def two_leg(c, hip, a_upper, bend, near, fore, fill, dark, upper=18, lower=17, w1=8, w2=6, paw=None):
    k = 1.0 if near else 0.85
    f = fill if near else dark
    a1 = math.radians(a_upper); a2 = math.radians(a_upper + (bend if fore else -bend))
    knee = (hip[0] + upper * math.sin(a1), hip[1] - upper * math.cos(a1))
    foot = (knee[0] + lower * math.sin(a2), knee[1] - lower * math.cos(a2))
    c.capsule(hip, knee, w1 * k, f, OUT, OUTLINE_W); c.capsule(knee, foot, w2 * k, f, OUT, OUTLINE_W)
    c.ellipse(knee[0], knee[1], w1 * k * 0.55, w1 * k * 0.55, fill=f)
    if paw is not None:
        paw(c, foot, a2, k, f)
    return foot


def run_pose(ph, fore, up_amt=36, bend_amt=55):
    up = up_amt * math.sin(ph)
    bend = bend_amt * max(0.0, math.sin(ph + 0.4))
    return up, bend


# =============================================================== GRIFFIN
FEATHER = hexc("d8b060"); FEATHER_D = darken(FEATHER, 0.3); FEATHER_L = lighten(FEATHER, 0.25)
LION = hexc("b8863a"); LION_D = darken(LION, 0.3); BEAK = hexc("e8a020"); WHITE = hexc("f4efe4")


def griffin_frame(t):
    c = Canvas(W, H)
    ph = 2 * math.pi * t
    flap = math.sin(ph); bob = -5 * math.cos(ph)
    cx, cy = 62, 62 + bob

    # lion tail with a tuft, streaming behind
    wave = 6 * math.sin(ph - 1)
    tail = bezier((cx - 28, cy + 2), (cx - 48, cy - 6 + wave), (cx - 60, cy + 6 + wave), (cx - 62, cy + 20 + wave))
    c.stroke(tail, 8, OUT); c.stroke(tail, 5, LION)
    c.ellipse(tail[-1][0] - 1, tail[-1][1] + 3, 5, 6.5, fill=LION_D, outline=OUT, ow=1.5, rot=20)

    def wing(root, near):
        ang = -50 * flap + 6
        k = 1.0 if near else 0.9
        fill = FEATHER if near else FEATHER_D
        def P(dx, dy): return rot((root[0] + dx * k, root[1] + dy * k), root, ang)
        wrist = P(-16, 28)
        prim = [P(-60, 24), P(-52, 38), P(-40, 50), P(-24, 56)]
        outline = [root, wrist, prim[3], P(-32, 48), prim[2], P(-44, 36), prim[1], P(-54, 26), prim[0], P(-46, 10), P(-30, 2)]
        c.poly(outline, fill=fill, outline=OUT, ow=OUTLINE_W)
        c.shade([root, wrist, prim[1], prim[0], P(-46, 10), P(-30, 2)], (0, 0, 0, 0.12))
        for p in prim:
            c.stroke([wrist, p], 1.6, darken(fill, 0.35))
        c.capsule(root, wrist, 8 * k, lighten(fill, 0.15), OUT, 1.5)
        for i in range(4):  # covert feather rows
            q = P(-12 - i * 8, 16 + i * 4)
            c.ellipse(q[0], q[1], 4, 2.5, fill=(WHITE[0], WHITE[1], WHITE[2], 0.6), rot=ang - 20)

    def lion_paw(c, foot, a2, k, f):
        c.ellipse(foot[0], foot[1] - 1, 5 * k, 3.6 * k, fill=f, outline=OUT, ow=1.4, rot=math.degrees(a2))
        for j in range(3):
            x = foot[0] - 3 + j * 3
            c.poly([(x - 1, foot[1] - 2), (x + 1, foot[1] - 2), (x, foot[1] - 5.5)], fill=WHITE, outline=OUT, ow=0.9)

    def talon(c, foot, a2, k, f):
        c.ellipse(foot[0], foot[1], 4.6 * k, 3.4 * k, fill=BEAK, outline=OUT, ow=1.4, rot=math.degrees(a2))
        for j in range(3):
            x = foot[0] + 1 + j * 2.6
            c.poly([(x - 1.2, foot[1] - 0.5), (x + 1.2, foot[1] - 0.5), (x + 1.5, foot[1] - 5.5)], fill=WHITE, outline=OUT, ow=0.9)

    tuck = 5 * flap
    # far legs, tucked in flight
    two_leg(c, (cx - 18, cy - 6), -28 + tuck, 85, False, False, LION, LION_D, 15, 13, 9.5, 7, lion_paw)
    two_leg(c, (cx + 20, cy - 8), 18 - tuck, 72, False, True, FEATHER, FEATHER_D, 14, 12, 8.5, 6, talon)
    wing((cx + 4, cy + 14), False)

    # lion body + feathered chest
    c.ellipse(cx - 4, cy, 32, 16, fill=LION, outline=OUT, ow=OUTLINE_W, rot=4)
    c.ellipse(cx + 20, cy + 2, 17, 17, fill=FEATHER, outline=OUT, ow=OUTLINE_W)
    c.ellipse(cx + 20, cy + 2, 17, 17, fill=FEATHER)
    c.ellipse(cx - 4, cy, 32, 16, fill=LION, rot=4)
    c.ellipse(cx - 6, cy - 7, 24, 5, fill=(LION_D[0], LION_D[1], LION_D[2], 0.5), rot=4)
    c.ellipse(cx - 6, cy + 9, 22, 4, fill=(WHITE[0], WHITE[1], WHITE[2], 0.22), rot=4)
    for row in range(3):  # chest feather scallops
        for i in range(3):
            c.ellipse(cx + 10 + i * 6 + (row % 2) * 3, cy - 8 + row * 6, 3.4, 2.4, fill=(FEATHER_L[0], FEATHER_L[1], FEATHER_L[2], 0.75), outline=(0, 0, 0, 0.25), ow=0.8)

    # near legs (tucked under the chest and belly)
    two_leg(c, (cx - 12, cy - 4), -22 + tuck, 85, True, False, LION, LION_D, 15, 13, 9.5, 7, lion_paw)
    two_leg(c, (cx + 26, cy - 6), 22 - tuck, 72, True, True, FEATHER, FEATHER_D, 14, 12, 8.5, 6, talon)

    # feathered neck + eagle head
    nb = (cx + 28, cy + 8); nt = (cx + 44, cy + 28 - 2 * flap)
    d = (nt[0] - nb[0], nt[1] - nb[1]); L = math.hypot(*d); nx, ny = -d[1] / L, d[0] / L
    neck = [(nb[0] + nx * 11, nb[1] + ny * 11), (nb[0] - nx * 10, nb[1] - ny * 10), (nt[0] - nx * 7, nt[1] - ny * 7), (nt[0] + nx * 8, nt[1] + ny * 8)]
    c.poly(neck, fill=FEATHER, outline=OUT, ow=OUTLINE_W)
    c.stroke([(nb[0] - nx * 6, nb[1] - ny * 6), (nt[0] - nx * 5, nt[1] - ny * 5)], 3, (FEATHER_D[0], FEATHER_D[1], FEATHER_D[2], 0.5))
    hx, hy = nt[0] + 3, nt[1] + 4
    c.ellipse(hx, hy, 12, 10.5, fill=WHITE, outline=OUT, ow=OUTLINE_W)     # white head
    c.ellipse(hx - 4, hy - 2, 7, 6, fill=(FEATHER_L[0], FEATHER_L[1], FEATHER_L[2], 0.5))  # feathered nape
    beak = [(hx + 8, hy + 5), (hx + 20, hy + 3), (hx + 25, hy - 3), (hx + 19, hy - 7), (hx + 12, hy - 6), (hx + 9, hy - 3)]
    c.poly(beak, fill=BEAK, outline=OUT, ow=OUTLINE_W)  # hooked beak
    c.stroke([(hx + 10, hy - 1), (hx + 21, hy - 1)], 1.3, OUT)
    c.ellipse(hx + 13, hy + 1.5, 2.5, 1.4, fill=(1, 1, 1, 0.35))
    c.ellipse(hx + 3, hy + 3, 3.4, 3.2, fill=BEAK); c.ellipse(hx + 4, hy + 3, 1.8, 2, fill=EYE); c.ellipse(hx + 4.6, hy + 4, 0.7, 0.7, fill=(1, 1, 1, 0.9))
    c.stroke([(hx - 3, hy + 8), (hx + 8, hy + 6)], 2.4, OUT)  # fierce brow
    for i in range(2):  # ear tufts
        c.poly([(hx - 8 + i * 6, hy + 6), (hx - 3 + i * 6, hy + 8), (hx - 7 + i * 6, hy + 18 - i * 3)], fill=WHITE, outline=OUT, ow=1.3)

    # saddle pad, then the near wing and rider
    c.ellipse(cx - 8, cy + 12, 12, 4, fill=TABARD_D, outline=OUT, ow=1.4)
    wing((cx + 10, cy + 14), True)
    rider(c, cx - 8, cy + 12, ph, -2 * flap, lean=8, reins_to=(hx + 8, hy - 2))
    return c.down()


# =============================================================== WAR BOAR
HIDE = hexc("5a4032"); HIDE_D = darken(HIDE, 0.3); HIDE_L = lighten(HIDE, 0.18); TUSK = hexc("f0e6d0"); BRISTLE = hexc("2a1c14"); ARMOR = hexc("6a6e7a")


def boar_frame(t):
    c = Canvas(W, H)
    ph = 2 * math.pi * t
    bob = 3 * math.sin(ph * 2)
    cx, cy = 66, 40 + bob

    def hoof(c, foot, a2, k, f):
        c.ellipse(foot[0] + 1, foot[1] - 1.5, 4.5 * k, 3 * k, fill=HOOF, outline=OUT, ow=1.4, rot=math.degrees(a2))
    tail = bezier((cx - 34, cy + 8), (cx - 44, cy + 16), (cx - 40, cy + 22), (cx - 46, cy + 24))
    c.stroke(tail, 4, OUT); c.stroke(tail, 2.2, HIDE)
    u, b = run_pose(ph + math.pi + 0.5, False, 32, 50); two_leg(c, (cx - 24, cy - 6), u, b, False, False, HIDE, HIDE_D, 15, 14, 8, 6, hoof)
    u, b = run_pose(ph + 0.5, True, 36, 55); two_leg(c, (cx + 22, cy - 6), u, b, False, True, HIDE, HIDE_D, 15, 14, 8, 6, hoof)

    # bulky body with bristle ridge
    c.ellipse(cx, cy, 42, 22, fill=HIDE, outline=OUT, ow=OUTLINE_W, rot=4)
    c.ellipse(cx - 24, cy + 4, 20, 18, fill=HIDE, outline=OUT, ow=OUTLINE_W)
    c.ellipse(cx - 24, cy + 4, 20, 18, fill=HIDE); c.ellipse(cx, cy, 42, 22, fill=HIDE, rot=4)
    c.ellipse(cx - 2, cy - 12, 34, 7, fill=(HIDE_D[0], HIDE_D[1], HIDE_D[2], 0.55), rot=4)
    for i in range(9):  # bristles along the spine
        x = cx - 30 + i * 7
        y = cy + 20 - abs(i - 4) * 1.2
        c.poly([(x - 3, y - 2), (x + 2, y - 2), (x - 2 + 2 * math.sin(ph + i), y + 8)], fill=BRISTLE, outline=OUT, ow=1.2)
    # plate barding on the shoulder
    c.ellipse(cx + 18, cy + 4, 15, 14, fill=ARMOR, outline=OUT, ow=OUTLINE_W)
    c.ellipse(cx + 18, cy + 4, 15, 14, fill=ARMOR)
    for i in range(3):
        c.ellipse(cx + 12 + i * 6, cy + 10 - i * 2, 1.6, 1.6, fill=darken(ARMOR, 0.4))
    c.ellipse(cx + 14, cy + 9, 5, 3, fill=(1, 1, 1, 0.25))

    # head: low snout, tusks, small eye, ears
    hx, hy = cx + 40, cy + 2
    c.poly([(hx - 6, hy + 16), (hx + 18, hy + 8), (hx + 24, hy - 4), (hx + 10, hy - 12), (hx - 8, hy - 8)], fill=HIDE, outline=OUT, ow=OUTLINE_W)
    c.ellipse(hx + 22, hy - 3, 5, 4.5, fill=HIDE_L, outline=OUT, ow=1.6)   # snout disc
    c.ellipse(hx + 23, hy - 3, 1.2, 1.4, fill=OUT); c.ellipse(hx + 25.5, hy - 2, 1.2, 1.4, fill=OUT)
    c.poly([(hx + 12, hy - 8), (hx + 16, hy - 6), (hx + 8, hy - 20)], fill=TUSK, outline=OUT, ow=1.6)   # near tusk
    c.poly([(hx + 4, hy - 8), (hx + 8, hy - 7), (hx + 2, hy - 17)], fill=darken(TUSK, 0.2), outline=OUT, ow=1.4)
    c.ellipse(hx + 8, hy + 3, 2.6, 2.4, fill=WHITE); c.ellipse(hx + 8.6, hy + 3, 1.5, 1.6, fill=hexc("7a1a10"))
    c.poly([(hx - 4, hy + 12), (hx + 4, hy + 12), (hx - 2, hy + 24)], fill=HIDE, outline=OUT, ow=1.6)
    c.poly([(hx + 4, hy + 12), (hx + 10, hy + 11), (hx + 8, hy + 22)], fill=HIDE_D, outline=OUT, ow=1.6)

    c.ellipse(cx - 2, cy + 16, 14, 5, fill=TABARD_D, outline=OUT, ow=1.4)
    rider(c, cx - 4, cy + 15, ph, -2 * math.sin(ph * 2), lean=12, reins_to=(hx + 10, hy - 2))
    u, b = run_pose(ph + math.pi, False, 32, 50); two_leg(c, (cx - 18, cy - 4), u, b, True, False, HIDE, HIDE_D, 15, 14, 8, 6, hoof)
    u, b = run_pose(ph, True, 36, 55); two_leg(c, (cx + 26, cy - 4), u, b, True, True, HIDE, HIDE_D, 15, 14, 8, 6, hoof)
    return c.down()


# =============================================================== WISP
WISP = hexc("8ae0ff"); WISP_CORE = hexc("f0fcff"); SPIRIT = hexc("bff0ff")


def wisp_frame(t):
    c = Canvas(W, H)
    ph = 2 * math.pi * t
    cx, cy = 70, 58
    pulse = 1 + 0.08 * math.sin(ph * 2)
    # trailing wisps behind
    for i in range(4):
        a = ph + i * 1.6
        tail = bezier((cx - 14, cy + 6 * math.sin(a)), (cx - 34, cy + 14 * math.sin(a + 1)), (cx - 50, cy - 10 * math.sin(a)), (cx - 64 + 6 * math.sin(a * 2), cy + 8 * math.cos(a)))
        c.stroke(tail, 9 - i, (WISP[0], WISP[1], WISP[2], 0.28))
        c.stroke(tail, 4 - i * 0.6, (WISP_CORE[0], WISP_CORE[1], WISP_CORE[2], 0.45))
    c.glow(cx, cy, 44 * pulse, (WISP[0], WISP[1], WISP[2], 0.9))
    c.ellipse(cx, cy, 24 * pulse, 24 * pulse, fill=(WISP[0], WISP[1], WISP[2], 0.85))
    c.ellipse(cx, cy, 15 * pulse, 15 * pulse, fill=(WISP_CORE[0], WISP_CORE[1], WISP_CORE[2], 0.95))
    # orbiting motes
    for i in range(5):
        a = ph * 1.5 + i * 1.26
        r = 30 + 4 * math.sin(ph + i)
        c.ellipse(cx + r * math.cos(a), cy + r * 0.5 * math.sin(a), 2.5, 2.5, fill=(1, 1, 1, 0.9))
    # spirit knight: translucent, floating inside the light
    sx, sy = cx - 2, cy - 12 + 2 * math.sin(ph)
    ghost = (SPIRIT[0], SPIRIT[1], SPIRIT[2], 0.55)
    torso = [(sx - 7, sy + 2), (sx + 7, sy + 2), (sx + 8, sy + 20), (sx - 6, sy + 20)]
    c.poly(torso, fill=ghost, outline=(0.3, 0.6, 0.8, 0.7), ow=1.8)
    c.poly([(sx - 2, sy + 6), (sx + 2, sy + 6), (sx + 1, sy + 18), (sx - 1, sy + 18)], fill=(1, 1, 1, 0.6))
    c.ellipse(sx, sy + 27, 7.5, 8, fill=ghost, outline=(0.3, 0.6, 0.8, 0.7), ow=1.8)
    c.poly([(sx - 7, sy + 26), (sx + 8, sy + 26), (sx + 8, sy + 23), (sx - 7, sy + 23)], fill=(0.3, 0.5, 0.7, 0.6))
    plume = bezier((sx - 1, sy + 34), (sx - 8, sy + 40), (sx - 18, sy + 36), (sx - 22, sy + 28))
    c.stroke(plume, 4, (WISP_CORE[0], WISP_CORE[1], WISP_CORE[2], 0.7))
    c.capsule((sx + 5, sy + 13), (sx + 15, sy + 16), 4.5, ghost)
    return c.down()


# =============================================================== SIEGE CART
WOOD = hexc("8a5a2a"); WOOD_D = darken(WOOD, 0.3); IRON = hexc("4a4c56"); IRON_L = lighten(IRON, 0.25)


def cart_frame(t, air=0.0, stretch=0.0):
    c = Canvas(W, H)
    ph = 2 * math.pi * t
    bounce = 2 * math.sin(ph * 2) if air == 0 else 0
    base = 22 + bounce + 14 * air
    cx = 66
    tilt = 10 * stretch
    def P(x, y): return rot((cx + x, base + y), (cx, base), tilt)

    def wheel(x, y, r, spin):
        c.ellipse(*P(x, y), r, r, fill=IRON, outline=OUT, ow=OUTLINE_W)
        c.ellipse(*P(x, y), r - 4, r - 4, fill=WOOD)
        for i in range(6):
            a = spin + i * math.pi / 3
            p0 = P(x, y); p1 = P(x + (r - 4) * math.cos(a), y + (r - 4) * math.sin(a))
            c.stroke([p0, p1], 2.6, WOOD_D)
        c.ellipse(*P(x, y), 4, 4, fill=IRON_L, outline=OUT, ow=1.4)
    spin = -ph * 2
    wheel(-24, 14, 15, spin)   # far wheels drawn first (slightly darker via overlap)
    # chassis + armoured box with crenellated top
    body = [P(-44, 22), P(44, 22), P(48, 40), P(-48, 40)]
    c.poly(body, fill=WOOD, outline=OUT, ow=OUTLINE_W)
    for i in range(5):
        x = -40 + i * 20
        c.stroke([P(x, 22), P(x, 40)], 2, WOOD_D)
    c.stroke([P(-44, 31), P(44, 31)], 3, IRON)
    box = [P(-38, 40), P(38, 40), P(36, 64), P(-36, 64)]
    c.poly(box, fill=darken(WOOD, 0.1), outline=OUT, ow=OUTLINE_W)
    for i in range(4):  # crenellations
        x = -34 + i * 20
        c.poly([P(x, 64), P(x + 10, 64), P(x + 10, 72), P(x, 72)], fill=darken(WOOD, 0.1), outline=OUT, ow=1.6)
    c.poly([P(-6, 44), P(6, 44), P(6, 56), P(-6, 56)], fill=IRON, outline=OUT, ow=1.4)   # iron hatch
    c.poly([P(-36, 40), P(36, 40), P(36, 43), P(-36, 43)], fill=IRON)
    # ram head at the front
    c.stroke([P(30, 50), P(62, 50)], 8, OUT); c.stroke([P(30, 50), P(60, 50)], 5.5, WOOD)
    c.ellipse(*P(62, 50), 7, 7, fill=IRON, outline=OUT, ow=OUTLINE_W)
    c.poly([P(58, 46), P(66, 44), P(70, 50), P(66, 56), P(58, 54)], fill=IRON_L, outline=OUT, ow=1.6)
    # banner pole
    c.stroke([P(-30, 64), P(-30, 100)], 2.4, WOOD_D)
    flag = bezier(P(-30, 98), P(-18, 96 + 3 * math.sin(ph)), P(-10, 90 - 3 * math.sin(ph)), P(-2, 92))
    fl = flag + [P(-2, 82), P(-30, 84)]
    c.poly(fl, fill=TABARD, outline=OUT, ow=1.6)
    c.ellipse(*P(-16, 90), 3, 3, fill=TRIM)
    # rider standing in the box (torso visible above the wall)
    rider(c, cx + 6, base + 58, ph, bounce * 0.5, lean=0, small=False)
    wheel(24, 14, 15, spin)   # near wheels in front
    wheel(-24, 14, 15, spin + 0.5)
    # front near wheel overlaps the chassis edge; small mud guard
    return c.down()


def cart_jump_frames():
    spec = [(0.0, -0.6), (0.4, 0.8), (0.8, 0.4), (1.0, 0.0), (0.7, -0.4), (0.2, -0.7)]
    return [cart_frame(0.0, a, s) for a, s in spec]


# =============================================================== SHADOW CAT
FUR = hexc("2c2240"); FUR_D = darken(FUR, 0.3); FUR_L = hexc("6a3cff"); GLOW = hexc("b070ff")


def cat_frame(t, mode="run", air=0.0, stretch=0.0):
    c = Canvas(W, H)
    ph = 2 * math.pi * t
    bob = 4 * math.sin(ph * 2) if mode == "run" else 0
    base = 30 + bob + 16 * air
    cx, cy = 64, base + 6
    tilt = 14 * stretch

    def paw(c, foot, a2, k, f):
        c.ellipse(foot[0] + 1, foot[1] - 1, 4.5 * k, 3 * k, fill=f, outline=OUT, ow=1.4)
    # tail: long and expressive
    wag = 10 * math.sin(ph) if mode == "run" else 12 * air
    tail = bezier((cx - 30, cy + 4), (cx - 52, cy + 6 + wag), (cx - 60, cy + 30 + wag), (cx - 48, cy + 40 + wag * 0.5))
    c.stroke(tail, 9, OUT); c.stroke(tail, 6, FUR)
    c.stroke(tail[-4:], 4, FUR_L)
    if mode == "run":
        u, b = run_pose(ph + math.pi + 0.5, False, 40, 60); two_leg(c, (cx - 22, cy - 4), u, b, False, False, FUR, FUR_D, 16, 15, 7, 5.5, paw)
        u, b = run_pose(ph + 0.5, True, 40, 60); two_leg(c, (cx + 24, cy - 6), u, b, False, True, FUR, FUR_D, 16, 15, 7, 5.5, paw)
    else:
        two_leg(c, (cx - 22, cy - 4), -40 - 20 * stretch, 40 + 40 * air, False, False, FUR, FUR_D, 16, 15, 7, 5.5, paw)
        two_leg(c, (cx + 24, cy - 6), 35 + 20 * stretch, 70 * air + 10, False, True, FUR, FUR_D, 16, 15, 7, 5.5, paw)
    # sleek body
    body = ellipse_points(cx, cy, 40, 13, tilt)
    c.poly(body, fill=FUR, outline=OUT, ow=OUTLINE_W)
    c.ellipse(cx - 26, cy + 2, 16, 13, fill=FUR, outline=OUT, ow=OUTLINE_W, rot=tilt)
    c.ellipse(cx - 26, cy + 2, 16, 13, fill=FUR, rot=tilt); c.poly(body, fill=FUR)
    c.ellipse(cx - 2, cy + 6, 30, 4, fill=(FUR_L[0], FUR_L[1], FUR_L[2], 0.35), rot=tilt)
    # rune glow along the flank
    for i in range(4):
        x = cx - 20 + i * 12
        c.stroke([(x, cy - 4), (x + 4, cy + 6)], 1.8, (GLOW[0], GLOW[1], GLOW[2], 0.6 + 0.3 * math.sin(ph + i)))
    # neck + head
    nb = (cx + 32, cy + 6); nt = (cx + 48, cy + 22 + 3 * stretch)
    c.capsule(nb, nt, 16, FUR, OUT, OUTLINE_W)
    hx, hy = nt[0] + 4, nt[1] + 2
    c.ellipse(hx + 2, hy, 12, 10, fill=FUR, outline=OUT, ow=OUTLINE_W)
    c.ellipse(hx + 12, hy - 3, 6, 4.5, fill=FUR, outline=OUT, ow=OUTLINE_W)   # muzzle
    c.ellipse(hx + 2, hy, 12, 10, fill=FUR); c.ellipse(hx + 12, hy - 3, 6, 4.5, fill=FUR)
    c.ellipse(hx + 17, hy - 2, 1.6, 1.2, fill=GLOW)
    c.poly([(hx - 6, hy + 6), (hx - 1, hy + 8), (hx - 6, hy + 19)], fill=FUR, outline=OUT, ow=1.6)   # ears
    c.poly([(hx + 3, hy + 8), (hx + 8, hy + 6), (hx + 8, hy + 18)], fill=FUR, outline=OUT, ow=1.6)
    c.poly([(hx - 4, hy + 8), (hx - 2, hy + 9), (hx - 5, hy + 15)], fill=GLOW)
    c.ellipse(hx + 6, hy + 2, 3.4, 2.6, fill=GLOW); c.ellipse(hx + 6.6, hy + 2, 1.2, 2.4, fill=EYE)   # glowing eye
    c.glow(hx + 6, hy + 2, 8, (GLOW[0], GLOW[1], GLOW[2], 0.5), 4)
    for i in range(3):  # whiskers
        c.stroke([(hx + 12, hy - 4 - i), (hx + 24, hy - 8 - i * 3)], 0.9, (GLOW[0], GLOW[1], GLOW[2], 0.7))

    rider(c, cx - 4, cy + 10, ph, -2 * math.sin(ph * 2), lean=16 + 6 * stretch, reins_to=(hx + 8, hy - 2), small=True)
    if mode == "run":
        u, b = run_pose(ph + math.pi, False, 40, 60); two_leg(c, (cx - 18, cy - 2), u, b, True, False, FUR, FUR_D, 16, 15, 7, 5.5, paw)
        u, b = run_pose(ph, True, 40, 60); two_leg(c, (cx + 28, cy - 4), u, b, True, True, FUR, FUR_D, 16, 15, 7, 5.5, paw)
    else:
        two_leg(c, (cx - 18, cy - 2), -35 - 20 * stretch, 40 + 40 * air, True, False, FUR, FUR_D, 16, 15, 7, 5.5, paw)
        two_leg(c, (cx + 28, cy - 4), 30 + 20 * stretch, 70 * air + 10, True, True, FUR, FUR_D, 16, 15, 7, 5.5, paw)
    return c.down()


def cat_jump_frames():
    spec = [(0.0, -0.7), (0.4, 0.9), (0.8, 0.5), (1.0, 0.0), (0.7, -0.5), (0.2, -0.7)]
    return [cat_frame(0.0, "jump", a, s) for a, s in spec]


if __name__ == "__main__":
    which = sys.argv[1:] or ["griffin", "boar", "wisp", "cart", "shadowcat"]
    if "griffin" in which:
        f = [griffin_frame(i / 8) for i in range(8)]; write_png(f"{OUT_DIR}/mount_griffin_fly_8.png", f); preview_grid(f"{SCRATCH}/griffin.png", f, 2)
    if "boar" in which:
        f = [boar_frame(i / 8) for i in range(8)]; write_png(f"{OUT_DIR}/mount_boar_run_8.png", f); preview_grid(f"{SCRATCH}/boar.png", f, 2)
    if "wisp" in which:
        f = [wisp_frame(i / 8) for i in range(8)]; write_png(f"{OUT_DIR}/mount_wisp_fly_8.png", f); preview_grid(f"{SCRATCH}/wisp.png", f, 2)
    if "cart" in which:
        f = [cart_frame(i / 8) for i in range(8)]; write_png(f"{OUT_DIR}/mount_cart_run_8.png", f); preview_grid(f"{SCRATCH}/cart.png", f, 2)
        j = cart_jump_frames(); write_png(f"{OUT_DIR}/mount_cart_jump_6.png", j); preview_grid(f"{SCRATCH}/cart_jump.png", j, 2)
    if "shadowcat" in which:
        f = [cat_frame(i / 8) for i in range(8)]; write_png(f"{OUT_DIR}/mount_shadowcat_run_8.png", f); preview_grid(f"{SCRATCH}/cat.png", f, 2)
        j = cat_jump_frames(); write_png(f"{OUT_DIR}/mount_shadowcat_jump_6.png", j); preview_grid(f"{SCRATCH}/cat_jump.png", j, 2)
    print("done", which)
