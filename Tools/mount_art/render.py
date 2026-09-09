"""Tiny supersampled vector-ish rasterizer used to draw the mount sprite sheets.

Coordinates are in final pixels with y pointing UP (0 = bottom of the frame).
Everything is drawn at S x supersampling and box-filtered down for anti-aliasing.
"""
import math, zlib, struct
import numpy as np

S = 4  # supersample factor

import os
_HERE = os.path.dirname(os.path.abspath(__file__))
SPRITES_DIR = os.path.join(_HERE, "..", "..", "Assets", "Geodashy", "Resources", "Sprites")
PREVIEW_DIR = os.environ.get("MOUNT_ART_PREVIEW", os.path.join(_HERE, "previews"))
os.makedirs(PREVIEW_DIR, exist_ok=True)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.W, self.H = w * S, h * S
        self.rgb = np.zeros((self.H, self.W, 3), dtype=np.float64)
        self.a = np.zeros((self.H, self.W), dtype=np.float64)

    # ---- low level -----------------------------------------------------------
    def _blend(self, mask, color):
        """mask: HxW coverage 0..1 (raster space, row 0 = top). color: (r,g,b,a) floats 0..1."""
        r, g, b, a = color
        cov = mask * a
        if not np.any(cov):
            return
        inv = 1 - cov
        self.rgb[..., 0] = self.rgb[..., 0] * inv + r * cov
        self.rgb[..., 1] = self.rgb[..., 1] * inv + g * cov
        self.rgb[..., 2] = self.rgb[..., 2] * inv + b * cov
        self.a = self.a * inv + cov

    def _to_raster(self, pts):
        return [(x * S, self.H - y * S) for x, y in pts]

    def polygon_mask(self, pts):
        """Scanline even-odd fill of a polygon given in raster coords."""
        mask = np.zeros((self.H, self.W))
        n = len(pts)
        if n < 3:
            return mask
        ys = [p[1] for p in pts]
        y0, y1 = max(0, int(math.floor(min(ys)))), min(self.H - 1, int(math.ceil(max(ys))))
        for y in range(y0, y1 + 1):
            cy = y + 0.5
            xs = []
            for i in range(n):
                (xa, ya), (xb, yb) = pts[i], pts[(i + 1) % n]
                if (ya <= cy < yb) or (yb <= cy < ya):
                    xs.append(xa + (cy - ya) * (xb - xa) / (yb - ya))
            xs.sort()
            for i in range(0, len(xs) - 1, 2):
                xa, xb = xs[i], xs[i + 1]
                ia, ib = max(0, int(round(xa))), min(self.W, int(round(xb)))
                if ib > ia:
                    mask[y, ia:ib] = 1
        return mask

    def ellipse_mask(self, cx, cy, rx, ry, rot=0.0):
        cx, cy, rx, ry = cx * S, self.H - cy * S, rx * S, ry * S
        yy, xx = np.mgrid[0:self.H, 0:self.W]
        dx, dy = xx + 0.5 - cx, yy + 0.5 - cy
        if rot:
            c, s = math.cos(math.radians(-rot)), math.sin(math.radians(-rot))
            dx, dy = dx * c - dy * s, dx * s + dy * c
        return ((dx / max(rx, 0.01)) ** 2 + (dy / max(ry, 0.01)) ** 2 <= 1).astype(float)

    def polyline_mask(self, pts, width):
        """Thick polyline with round joins/caps."""
        rp = self._to_raster(pts)
        mask = np.zeros((self.H, self.W))
        r = width * S / 2
        yy, xx = np.mgrid[0:self.H, 0:self.W]
        for i in range(len(rp) - 1):
            (x0, y0), (x1, y1) = rp[i], rp[i + 1]
            dx, dy = x1 - x0, y1 - y0
            L2 = dx * dx + dy * dy
            # bounding box for speed
            bx0, bx1 = int(min(x0, x1) - r - 1), int(max(x0, x1) + r + 2)
            by0, by1 = int(min(y0, y1) - r - 1), int(max(y0, y1) + r + 2)
            bx0, by0 = max(bx0, 0), max(by0, 0)
            bx1, by1 = min(bx1, self.W), min(by1, self.H)
            if bx1 <= bx0 or by1 <= by0:
                continue
            px = xx[by0:by1, bx0:bx1] + 0.5 - x0
            py = yy[by0:by1, bx0:bx1] + 0.5 - y0
            t = np.clip((px * dx + py * dy) / L2, 0, 1) if L2 > 0 else 0
            ex, ey = px - t * dx, py - t * dy
            d2 = ex * ex + ey * ey
            mask[by0:by1, bx0:bx1] = np.maximum(mask[by0:by1, bx0:bx1], (d2 <= r * r).astype(float))
        return mask

    # ---- drawing ---------------------------------------------------------------
    def poly(self, pts, fill=None, outline=None, ow=2.0):
        rp = self._to_raster(pts)
        if fill is not None:
            self._blend(self.polygon_mask(rp), fill)
        if outline is not None:
            self._blend(self.polyline_mask(list(pts) + [pts[0]], ow), outline)

    def ellipse(self, cx, cy, rx, ry, fill=None, outline=None, ow=2.0, rot=0.0):
        if fill is not None:
            self._blend(self.ellipse_mask(cx, cy, rx, ry, rot), fill)
        if outline is not None:
            pts = ellipse_points(cx, cy, rx, ry, rot)
            self._blend(self.polyline_mask(pts + [pts[0]], ow), outline)

    def stroke(self, pts, width, color):
        self._blend(self.polyline_mask(pts, width), color)

    def capsule(self, a, b, width, fill, outline=None, ow=2.0):
        """A limb segment: thick line with an outline drawn first, then the fill on top."""
        if outline is not None:
            self._blend(self.polyline_mask([a, b], width + ow * 2), outline)
        self._blend(self.polyline_mask([a, b], width), fill)

    def shade(self, mask_pts, color):
        """Blend a translucent colour inside a polygon (for belly shadows / highlights)."""
        self._blend(self.polygon_mask(self._to_raster(mask_pts)), color)

    def glow(self, cx, cy, r, color, steps=6):
        for i in range(steps, 0, -1):
            k = i / steps
            c = (color[0], color[1], color[2], color[3] * (1 - k) ** 1.5 * 0.6)
            self._blend(self.ellipse_mask(cx, cy, r * k, r * k), c)

    # ---- output ------------------------------------------------------------------
    def down(self):
        rgb = self.rgb.reshape(self.h, S, self.w, S, 3).mean(axis=(1, 3))
        a = self.a.reshape(self.h, S, self.w, S).mean(axis=(1, 3))
        # premultiplied average, then un-premultiply
        prgb = (self.rgb * self.a[..., None]).reshape(self.h, S, self.w, S, 3).mean(axis=(1, 3))
        rgb = np.where(a[..., None] > 1e-6, prgb / np.maximum(a[..., None], 1e-6), 0)
        return np.clip(rgb, 0, 1), np.clip(a, 0, 1)


def ellipse_points(cx, cy, rx, ry, rot=0.0, n=48):
    c, s = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        x, y = rx * math.cos(t), ry * math.sin(t)
        pts.append((cx + x * c - y * s, cy + x * s + y * c))
    return pts


def bezier(p0, p1, p2, p3, n=16):
    out = []
    for i in range(n + 1):
        t = i / n
        u = 1 - t
        x = u ** 3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t ** 3 * p3[0]
        y = u ** 3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t ** 3 * p3[1]
        out.append((x, y))
    return out


def rot(p, origin, deg):
    c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    x, y = p[0] - origin[0], p[1] - origin[1]
    return (origin[0] + x * c - y * s, origin[1] + x * s + y * c)


def hexc(h, a=1.0):
    h = h.lstrip('#')
    return (int(h[0:2], 16) / 255, int(h[2:4], 16) / 255, int(h[4:6], 16) / 255, a)


def darken(c, k):
    return (c[0] * (1 - k), c[1] * (1 - k), c[2] * (1 - k), c[3])


def lighten(c, k):
    return (c[0] + (1 - c[0]) * k, c[1] + (1 - c[1]) * k, c[2] + (1 - c[2]) * k, c[3])


def write_png(path, frames):
    """frames: list of (rgb HxWx3, a HxW) floats. Writes a horizontal strip."""
    h = frames[0][0].shape[0]
    w = sum(f[0].shape[1] for f in frames)
    img = np.zeros((h, w, 4), dtype=np.uint8)
    x = 0
    for rgb, a in frames:
        fw = rgb.shape[1]
        img[:, x:x + fw, :3] = (rgb * 255 + 0.5).astype(np.uint8)
        img[:, x:x + fw, 3] = (a * 255 + 0.5).astype(np.uint8)
        x += fw
    raw = b"".join(b"\x00" + img[y].tobytes() for y in range(h))
    def chunk(t, d):
        return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d) & 0xffffffff)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    open(path, "wb").write(png)


def preview_grid(path, frames, scale=2, bg=(60, 50, 80)):
    """Debug view: frames side by side on a dark background, upscaled."""
    h = frames[0][0].shape[0]
    w = sum(f[0].shape[1] for f in frames)
    img = np.zeros((h, w, 3), dtype=np.float64)
    img[:] = np.array(bg) / 255
    x = 0
    for rgb, a in frames:
        fw = rgb.shape[1]
        img[:, x:x + fw] = img[:, x:x + fw] * (1 - a[..., None]) + rgb * a[..., None]
        x += fw
    img = np.repeat(np.repeat(img, scale, axis=0), scale, axis=1)
    frames2 = [(img, np.ones(img.shape[:2]))]
    write_png(path, frames2)
