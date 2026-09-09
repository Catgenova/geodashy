import math, sys
sys.path.insert(0, __file__.rsplit('/', 1)[0])
from render import *

W = H = 144

# palette
COAT = hexc("9a5a2a"); COAT_D = darken(COAT, 0.28); COAT_L = lighten(COAT, 0.18)
MANE = hexc("2c1a10"); HOOF = hexc("2a1c14"); OUT = hexc("1e120c")
TACK = hexc("5a2a12"); TACK_L = hexc("c99b3a")
TABARD = hexc("2f8a3a"); TABARD_D = darken(TABARD, 0.3); TRIM = hexc("e0b64a")
STEEL = hexc("b8bcc8"); STEEL_D = darken(STEEL, 0.3); STEEL_L = lighten(STEEL, 0.4)
PLUME = hexc("b8302c"); SKIN = hexc("e8b892"); EYE = hexc("100808")
OUTLINE_W = 2.2


def leg(c, hip, a_upper, bend, near, fore, upper=20, lower=19):
    """Joint angles in degrees from straight down; positive a_upper swings forward.
    bend folds the lower segment: forward for a foreleg (knee), backward for a hind leg (hock)."""
    k = 1.0 if near else 0.85
    fill = COAT if near else COAT_D
    a1 = math.radians(a_upper)
    a2 = math.radians(a_upper + (bend if fore else -bend))
    knee = (hip[0] + upper * math.sin(a1), hip[1] - upper * math.cos(a1))
    hoof = (knee[0] + lower * math.sin(a2), knee[1] - lower * math.cos(a2))
    c.capsule(hip, knee, 8.5 * k, fill, OUT, OUTLINE_W)
    c.capsule(knee, hoof, 6 * k, fill, OUT, OUTLINE_W)
    c.ellipse(knee[0], knee[1], 4.6 * k, 4.6 * k, fill=fill)  # smooth the joint
    c.ellipse(hoof[0] + 1.2 * math.sin(a2), hoof[1] - 1.5, 5 * k, 3.4 * k, fill=HOOF, outline=OUT, ow=1.5, rot=math.degrees(a2))
    return hoof


def run_pose(ph, fore):
    """Gallop: upper swings, lower folds when the leg is off the ground."""
    up = 38 * math.sin(ph) if fore else 32 * math.sin(ph)
    lift = max(0.0, math.sin(ph + 0.4))
    bend = (55 if fore else 50) * lift
    return up, bend


def jump_pose(air, stretch, fore, near):
    """Explicit jump poses: crouch (stretch<0, air 0), launch (stretch>0), apex (air 1), landing."""
    if fore:
        if stretch > 0.3:      up, bend = 22 + 18 * air, 60 + 30 * air         # knees folded up on launch
        elif air > 0.6:        up, bend = 40, 95
        elif stretch < -0.3:   up, bend = 25, 20 * (1 - air)                 # landing / crouch: forward and planted
        else:                  up, bend = 35, 40
    else:
        if stretch > 0.3:      up, bend = -45 - 10 * air, 15                 # kicking back on launch
        elif air > 0.6:        up, bend = -10, 70                            # tucked at the apex
        elif stretch < -0.3:   up, bend = 5 * (1 - air), 35                  # crouched under the body
        else:                  up, bend = -20, 30
    return up + (0 if near else 6), bend


def horse_frame(t, mode="run", air=0.0, stretch=0.0):
    """t: 0..1 cycle phase. air: 0..1 how airborne (jump). stretch: -1 crouch .. 1 extended."""
    c = Canvas(W, H)
    bob = 3 * math.sin(2 * math.pi * t * 2) if mode == "run" else 0
    base = 34 + bob + 14 * air
    body_cx, body_cy = 60, base + 30
    tilt = 12 * stretch  # nose-up on launch, nose-down on landing
    ph = 2 * math.pi * t

    # ---- tail --------------------------------------------------------------
    wag = 8 * math.sin(ph + 1.0) if mode == "run" else 10 * (1 - air) - 6
    tail_pts = bezier((body_cx - 36, body_cy + 10), (body_cx - 52, body_cy + 14 + wag), (body_cx - 58, body_cy - 6 + wag), (body_cx - 50, body_cy - 26))
    c.stroke(tail_pts, 9, OUT); c.stroke(tail_pts, 6, MANE)
    tail2 = bezier((body_cx - 36, body_cy + 8), (body_cx - 48, body_cy + 4 + wag), (body_cx - 54, body_cy - 10), (body_cx - 44, body_cy - 22))
    c.stroke(tail2, 4, darken(MANE, -0.3))

    # ---- far legs -----------------------------------------------------------
    hip_far = (body_cx - 26, body_cy - 4)
    sh_far = (body_cx + 26, body_cy - 3)
    if mode == "run":
        u, bnd = run_pose(ph + math.pi + 0.5, False); leg(c, hip_far, u, bnd, False, False)
        u, bnd = run_pose(ph + 0.5, True); leg(c, sh_far, u, bnd, False, True)
    else:
        u, bnd = jump_pose(air, stretch, False, False); leg(c, hip_far, u, bnd, False, False)
        u, bnd = jump_pose(air, stretch, True, False); leg(c, sh_far, u, bnd, False, True)

    # ---- body ------------------------------------------------------------------
    body = ellipse_points(body_cx, body_cy, 40, 19, tilt)
    c.poly(body, fill=COAT, outline=OUT, ow=OUTLINE_W)
    c.ellipse(body_cx - 22, body_cy + 4, 20, 17, fill=COAT, outline=OUT, ow=OUTLINE_W, rot=tilt)   # rump
    c.ellipse(body_cx + 26, body_cy + 2, 17, 17, fill=COAT, outline=OUT, ow=OUTLINE_W, rot=tilt)   # chest
    # belly shadow + back highlight (drawn inside the silhouette, no outline)
    c.ellipse(body_cx - 2, body_cy - 9, 34, 9, fill=(COAT_D[0], COAT_D[1], COAT_D[2], 0.55), rot=tilt)
    c.ellipse(body_cx, body_cy + 11, 30, 5, fill=(COAT_L[0], COAT_L[1], COAT_L[2], 0.45), rot=tilt)
    c.ellipse(body_cx - 22, body_cy + 4, 20, 17, fill=COAT, rot=tilt)  # re-fill rump over the seam
    c.ellipse(body_cx + 26, body_cy + 2, 17, 17, fill=COAT, rot=tilt)

    # ---- neck & head --------------------------------------------------------------
    head_bob = 3 * math.sin(ph * 2 + 0.5) if mode == "run" else -5 * stretch
    nb = (body_cx + 30, body_cy + 10)                    # neck base on the withers
    nt = (body_cx + 54, body_cy + 38 + head_bob)         # throat/poll
    d = (nt[0] - nb[0], nt[1] - nb[1]); L = math.hypot(*d); nx, ny = -d[1] / L, d[0] / L  # normal
    neck = [(nb[0] + nx * 12, nb[1] + ny * 12), (nb[0] - nx * 10, nb[1] - ny * 10), (nt[0] - nx * 6, nt[1] - ny * 6), (nt[0] + nx * 7, nt[1] + ny * 7)]
    c.poly(neck, fill=COAT, outline=OUT, ow=OUTLINE_W)
    c.shade([neck[1], neck[2], ((neck[2][0] + neck[3][0]) / 2, (neck[2][1] + neck[3][1]) / 2), ((neck[1][0] + neck[0][0]) / 2, (neck[1][1] + neck[0][1]) / 2)], (COAT_D[0], COAT_D[1], COAT_D[2], 0.35))
    # head: long skull angled down-forward, jaw, muzzle
    hx, hy = nt[0] + 6, nt[1] + 2
    c.ellipse(hx + 8, hy - 4, 15, 8.5, fill=COAT, outline=OUT, ow=OUTLINE_W, rot=-32)
    c.ellipse(hx + 2, hy + 2, 9, 8, fill=COAT, outline=OUT, ow=OUTLINE_W)          # cheek
    c.ellipse(hx + 2, hy + 2, 9, 8, fill=COAT)
    c.ellipse(hx + 8, hy - 4, 15, 8.5, fill=COAT, rot=-32)
    c.ellipse(hx + 19, hy - 12, 7, 5.2, fill=COAT_L, outline=OUT, ow=OUTLINE_W, rot=-32)  # muzzle
    c.ellipse(hx + 19, hy - 12, 7, 5.2, fill=COAT_L, rot=-32)
    c.ellipse(hx + 23, hy - 13, 1.5, 1.1, fill=OUT)   # nostril
    c.stroke([(hx + 15, hy - 16), (hx + 21, hy - 17)], 1.4, OUT)  # mouth
    # ears
    c.poly([(hx - 2, hy + 7), (hx + 3, hy + 6), (hx + 1, hy + 17)], fill=COAT, outline=OUT, ow=1.6)
    c.poly([(hx + 4, hy + 7), (hx + 9, hy + 5), (hx + 8, hy + 16)], fill=COAT_D, outline=OUT, ow=1.6)
    # eye
    c.ellipse(hx + 9, hy + 1, 2.8, 2.4, fill=hexc("f4f0e8")); c.ellipse(hx + 9.7, hy + 0.8, 1.7, 1.7, fill=EYE)
    # mane: short tufts lying back along the crest of the neck
    for i in range(7):
        f = i / 6
        mx = neck[0][0] + (neck[3][0] - neck[0][0]) * f
        my = neck[0][1] + (neck[3][1] - neck[0][1]) * f
        wave = 2.5 * math.sin(ph * 2 + i * 0.9) if mode == "run" else 1.5 * (1 - air)
        tuft = bezier((mx + 1, my - 2), (mx - 3, my + 4 + wave), (mx - 8, my + 3 + wave), (mx - 9, my - 1 + wave * 0.5))
        c.stroke(tuft, 4.2, OUT); c.stroke(tuft, 2.4, lighten(MANE, 0.12))
    # forelock between the ears
    fl = bezier((hx + 3, hy + 8), (hx + 8, hy + 12), (hx + 14, hy + 8), (hx + 15, hy + 3))
    c.stroke(fl, 5, OUT); c.stroke(fl, 3, MANE)
    # bridle + reins
    c.stroke([(hx + 3, hy + 3), (hx + 16, hy - 12)], 1.8, TACK)
    c.stroke([(hx + 12, hy - 4), (hx + 19, hy - 3)], 1.6, TACK)
    c.stroke(bezier((hx + 18, hy - 10), (hx + 4, hy - 8), (body_cx + 34, body_cy + 20), (body_cx + 10, body_cy + 30)), 1.4, TACK)
    c.stroke([(body_cx + 12, body_cy + 18), (body_cx + 6, body_cy - 17)], 3, TACK)   # girth
    # ---- saddle & rider (sits above the back) --------------------------------------
    sx, sy = body_cx + 2, body_cy + 16
    c.ellipse(sx, sy, 15, 5, fill=TACK, outline=OUT, ow=1.6)
    c.ellipse(sx, sy + 1, 12, 3, fill=TACK_L)
    rider_bob = -2 * math.sin(ph * 2) if mode == "run" else -3 * air
    ry = sy + rider_bob
    # far leg (hangs on the far side)
    c.capsule((sx - 4, ry), (sx - 2, ry - 14), 6, TABARD_D, OUT, 1.8)
    # torso (tabard)
    torso = [(sx - 9, ry + 2), (sx + 8, ry + 2), (sx + 9, ry + 22), (sx - 8, ry + 22)]
    c.poly(torso, fill=TABARD, outline=OUT, ow=OUTLINE_W)
    c.poly([(sx - 3, ry + 6), (sx + 3, ry + 6), (sx + 2, ry + 20), (sx - 2, ry + 20)], fill=TRIM)
    c.stroke([(sx - 9, ry + 5), (sx + 9, ry + 5)], 2, TRIM)  # belt
    # near leg in stirrup
    c.capsule((sx + 4, ry + 2), (sx + 8, ry - 12), 6.5, TABARD_D, OUT, 1.8)
    c.ellipse(sx + 9, ry - 14, 4, 2.5, fill=STEEL_D, outline=OUT, ow=1.4)  # boot
    # arm holding the reins
    c.capsule((sx + 6, ry + 16), (sx + 16, ry + 12), 5.5, TABARD, OUT, 1.8)
    c.ellipse(sx + 18, ry + 11, 3, 3, fill=STEEL, outline=OUT, ow=1.4)
    # shield on the far arm
    shield = [(sx - 18, ry + 20), (sx - 6, ry + 20), (sx - 6, ry + 10), (sx - 12, ry + 3), (sx - 18, ry + 10)]
    c.poly(shield, fill=TRIM, outline=OUT, ow=OUTLINE_W)
    c.poly([(sx - 15, ry + 18), (sx - 9, ry + 18), (sx - 9, ry + 11), (sx - 12, ry + 6), (sx - 15, ry + 11)], fill=TABARD)
    c.stroke([(sx - 12, ry + 8), (sx - 12, ry + 17)], 1.6, TRIM); c.stroke([(sx - 15, ry + 12), (sx - 9, ry + 12)], 1.6, TRIM)
    # helmet with plume
    hx2, hy2 = sx, ry + 29
    c.ellipse(hx2, hy2, 8, 8.5, fill=STEEL, outline=OUT, ow=OUTLINE_W)
    c.poly([(hx2 - 8, hy2 - 1), (hx2 + 9, hy2 - 1), (hx2 + 9, hy2 - 4), (hx2 - 8, hy2 - 4)], fill=STEEL_D, outline=OUT, ow=1.4)  # visor slit band
    c.stroke([(hx2 + 2, hy2 - 2.5), (hx2 + 7, hy2 - 2.5)], 1.4, OUT)
    c.ellipse(hx2 - 3, hy2 + 3, 3, 2, fill=(STEEL_L[0], STEEL_L[1], STEEL_L[2], 0.7))
    plume = bezier((hx2 - 1, hy2 + 8), (hx2 - 6, hy2 + 16), (hx2 - 16, hy2 + 14), (hx2 - 20, hy2 + 6 + (2 * math.sin(ph * 2) if mode == "run" else 0)))
    c.stroke(plume, 6, OUT); c.stroke(plume, 4, PLUME)

    # ---- near legs (drawn last, in front of the body) --------------------------------
    hip_near = (body_cx - 20, body_cy - 2)
    sh_near = (body_cx + 30, body_cy - 1)
    if mode == "run":
        u, bnd = run_pose(ph + math.pi, False); leg(c, hip_near, u, bnd, True, False)
        u, bnd = run_pose(ph, True); leg(c, sh_near, u, bnd, True, True)
    else:
        u, bnd = jump_pose(air, stretch, False, True); leg(c, hip_near, u, bnd, True, False)
        u, bnd = jump_pose(air, stretch, True, True); leg(c, sh_near, u, bnd, True, True)
    return c.down()


def run_frames(n=8):
    return [horse_frame(i / n, "run") for i in range(n)]


def jump_frames():
    # crouch, launch, rise, apex, descend, land
    spec = [(0.0, -0.8), (0.35, 0.9), (0.75, 0.6), (1.0, 0.1), (0.7, -0.5), (0.2, -0.8)]
    return [horse_frame(0.0, "jump", air=a, stretch=s) for a, s in spec]


if __name__ == "__main__":
    run = run_frames(); jump = jump_frames()
    write_png(f"{OUT if isinstance(OUT, str) else ''}", []) if False else None
    out_dir = SPRITES_DIR
    write_png(f"{out_dir}/mount_horse_run_{len(run)}.png", run)
    write_png(f"{out_dir}/mount_horse_jump_{len(jump)}.png", jump)
    scratch = PREVIEW_DIR
    preview_grid(f"{scratch}/horse_run.png", run, 2)
    preview_grid(f"{scratch}/horse_jump.png", jump, 2)
    print("horse done")
