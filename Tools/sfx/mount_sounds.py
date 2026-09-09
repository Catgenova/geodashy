#!/usr/bin/env python3
"""Synthesises the per-mount landing and death sounds (land_<mount>.wav, death_<mount>.wav).

Each mount gets its own character: hooves for the horse, a heavy thud for the boar, a soft
whoosh for the wisp, a rattle for the cart, a wing beat for the griffin and the dragon, and a
quick padded landing for the shadowcat. Run from the repo root:

    python3 Tools/sfx/mount_sounds.py

Requires numpy and soundfile. Output goes to Assets/Geodashy/Resources/SFX.
"""
import os
import numpy as np
import soundfile as sf

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Geodashy', 'Resources', 'SFX')
rng = np.random.default_rng(7)


def env(n, attack, decay, curve=3.0):
    t = np.linspace(0, 1, n)
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    d = np.exp(-curve * np.clip((t - attack) / max(decay, 1e-4), 0, None))
    return a * d


def noise(n):
    return rng.standard_normal(n)


def lowpass(x, cutoff):
    # one-pole IIR, cheap and good enough for thuds
    rc = 1.0 / (2 * np.pi * cutoff)
    a = (1.0 / SR) / (rc + 1.0 / SR)
    y = np.zeros_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += a * (x[i] - acc)
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def tone(n, f0, f1=None, wave='sine'):
    t = np.arange(n) / SR
    f1 = f0 if f1 is None else f1
    freq = f0 + (f1 - f0) * (t / t[-1])
    phase = 2 * np.pi * np.cumsum(freq) / SR
    if wave == 'square':
        return np.sign(np.sin(phase))
    if wave == 'saw':
        return 2 * ((phase / (2 * np.pi)) % 1) - 1
    return np.sin(phase)


def sec(s):
    return int(SR * s)


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def delay(x, seconds):
    return np.concatenate([np.zeros(sec(seconds)), x])


def norm(x, peak=0.85):
    m = np.max(np.abs(x))
    return x / m * peak if m > 0 else x


def write(name, x):
    x = norm(x)
    x = np.concatenate([x, np.zeros(sec(0.02))])
    sf.write(os.path.join(OUT, name + '.wav'), x.astype(np.float32), SR, subtype='PCM_16')
    print('wrote', name, round(len(x) / SR, 2), 's')


def thud(n, f0=110, f1=45, cut=600, curve=6):
    return tone(n, f0, f1) * env(n, 0.003, 0.6, curve) + lowpass(noise(n), cut) * env(n, 0.002, 0.25, 8) * 0.6


# ---- landing sounds -----------------------------------------------------------
def land_horse():
    hoof = lambda: thud(sec(0.12), 160, 70, 900, 8)
    return mix(hoof(), delay(hoof() * 0.8, 0.07), delay(hoof() * 0.5, 0.11))


def land_boar():
    n = sec(0.32)
    return mix(thud(n, 90, 35, 400, 5) * 1.2, delay(lowpass(noise(sec(0.15)), 300) * env(sec(0.15), 0.01, 0.4, 6) * 0.5, 0.05))


def land_griffin():
    n = sec(0.3)
    flap = highpass(lowpass(noise(n), 1800), 250) * env(n, 0.04, 0.5, 5)
    return mix(flap, thud(sec(0.1), 140, 60, 700, 7) * 0.7)


def land_dragon():
    n = sec(0.42)
    boom = thud(n, 75, 30, 350, 4) * 1.3
    scrape = highpass(lowpass(noise(n), 2500), 600) * env(n, 0.05, 0.6, 6) * 0.35
    return mix(boom, scrape)


def land_wisp():
    n = sec(0.28)
    shimmer = tone(n, 880, 1320) * env(n, 0.01, 0.5, 5) * 0.5 + tone(n, 1320, 1760) * env(n, 0.02, 0.4, 6) * 0.3
    air = highpass(lowpass(noise(n), 4000), 1500) * env(n, 0.03, 0.5, 5) * 0.3
    return mix(shimmer, air)


def land_cart():
    n = sec(0.3)
    rattle = highpass(lowpass(noise(n), 3500), 900) * env(n, 0.005, 0.5, 7)
    clank = tone(sec(0.12), 1200, 900, 'square') * env(sec(0.12), 0.002, 0.3, 9) * 0.3
    return mix(thud(sec(0.15), 120, 55, 600, 7) * 0.8, rattle * 0.7, clank)


def land_shadowcat():
    n = sec(0.16)
    return mix(thud(n, 130, 65, 500, 9) * 0.7, highpass(lowpass(noise(n), 1500), 400) * env(n, 0.005, 0.3, 9) * 0.35)


# ---- death sounds ---------------------------------------------------------------
def death_horse():
    n = sec(0.55)
    whinny = tone(n, 900, 500, 'saw') * env(n, 0.02, 0.7, 4) * 0.5
    vib = np.sin(2 * np.pi * 9 * np.arange(n) / SR) * 0.15 + 1
    whinny = whinny * vib
    return mix(whinny, delay(thud(sec(0.3), 100, 40, 500, 5), 0.25))


def death_boar():
    n = sec(0.6)
    squeal = tone(n, 700, 300, 'square') * env(n, 0.01, 0.6, 4) * 0.35
    growl = lowpass(tone(n, 90, 50, 'saw'), 400) * env(n, 0.01, 0.9, 3) * 0.6
    return mix(squeal, growl, delay(thud(sec(0.3), 80, 30, 350, 5), 0.2))


def death_griffin():
    n = sec(0.6)
    screech = tone(n, 1500, 2200, 'saw') * env(n, 0.01, 0.5, 5) * 0.4
    feathers = highpass(lowpass(noise(n), 2500), 500) * env(n, 0.05, 0.9, 3) * 0.4
    return mix(screech, feathers)


def death_dragon():
    n = sec(0.8)
    roar = lowpass(tone(n, 110, 60, 'saw') + tone(n, 165, 90, 'saw') * 0.5, 900) * env(n, 0.03, 0.9, 3) * 0.7
    fire = lowpass(noise(n), 1800) * env(n, 0.1, 0.9, 3) * 0.35
    return mix(roar, fire)


def death_wisp():
    n = sec(0.7)
    fade = tone(n, 1760, 220) * env(n, 0.01, 1.0, 3) * 0.5
    sparkle = mix(*[delay(tone(sec(0.08), f, f * 0.7) * env(sec(0.08), 0.005, 0.5, 8) * 0.25, 0.05 * i) for i, f in enumerate([2200, 1900, 1600, 1300, 1000])])
    return mix(fade, sparkle)


def death_cart():
    n = sec(0.7)
    crash = highpass(lowpass(noise(n), 4000), 300) * env(n, 0.005, 0.9, 4)
    planks = mix(*[delay(tone(sec(0.1), f, f * 0.8, 'square') * env(sec(0.1), 0.002, 0.4, 9) * 0.25, 0.04 * i) for i, f in enumerate([800, 1300, 600, 1100])])
    return mix(thud(sec(0.3), 110, 40, 450, 5), crash * 0.7, planks)


def death_shadowcat():
    n = sec(0.5)
    yowl = tone(n, 1100, 650, 'saw') * env(n, 0.01, 0.6, 4) * 0.35
    vib = np.sin(2 * np.pi * 14 * np.arange(n) / SR) * 0.2 + 1
    smoke = highpass(lowpass(noise(n), 2000), 700) * env(n, 0.05, 0.9, 3) * 0.3
    return mix(yowl * vib, smoke)


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    for name, fn in [
        ('land_horse', land_horse), ('land_boar', land_boar), ('land_griffin', land_griffin), ('land_dragon', land_dragon),
        ('land_wisp', land_wisp), ('land_cart', land_cart), ('land_shadowcat', land_shadowcat),
        ('death_horse', death_horse), ('death_boar', death_boar), ('death_griffin', death_griffin), ('death_dragon', death_dragon),
        ('death_wisp', death_wisp), ('death_cart', death_cart), ('death_shadowcat', death_shadowcat),
    ]:
        write(name, fn())
