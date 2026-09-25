"""Builds the runtime textures in src/SeedPlacement.App/Assets.

Run from the repository root:  python assets-src/generate_assets.py
Requires numpy, scipy and Pillow. Output is deterministic.
"""

from pathlib import Path

import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter

OUT = Path(__file__).resolve().parent.parent / "src" / "SeedPlacement.App" / "Assets"
MACOS = Path(__file__).resolve().parent.parent / "packaging" / "macos"
SS = 3  # supersampling factor


def smooth_noise(rng, shape, sigma, wrap=False):
    n = gaussian_filter(rng.standard_normal(shape), sigma, mode="wrap" if wrap else "reflect")
    return n / (np.abs(n).max() + 1e-9)


def save_rgba(path, rgb, alpha):
    img = np.dstack([np.clip(rgb, 0, 1), np.clip(alpha, 0, 1)])
    Image.fromarray((img * 255).round().astype(np.uint8), "RGBA").save(path, optimize=True)


def save_rgb(path, rgb):
    Image.fromarray((np.clip(rgb, 0, 1) * 255).round().astype(np.uint8), "RGB").save(path, optimize=True)


SEED_KINDS = {
    # aspect: length / width. point: taper at the (right, left) ends. relief: dome height.
    "cucumber": dict(aspect=2.45, point=((0.22, 0.34), (0.0, 0.0)), color=(0.90, 0.81, 0.62),
                     spread=(0.025, 0.03, 0.045), pattern="margin", relief=0.18, bend=0.12),
    "wheat": dict(aspect=2.15, point=((0.10, 0.18), (0.04, 0.10)), color=(0.84, 0.66, 0.42),
                  spread=(0.04, 0.035, 0.03), pattern="crease", relief=0.34, bend=0.08),
    "lettuce": dict(aspect=3.4, point=((0.38, 0.48), (0.30, 0.40)), color=(0.52, 0.46, 0.37),
                    spread=(0.04, 0.04, 0.035), pattern="ribs", relief=0.22, bend=0.16),
    "radish": dict(aspect=1.18, point=((0.0, 0.06), (0.0, 0.04)), color=(0.55, 0.32, 0.21),
                   spread=(0.05, 0.035, 0.025), pattern="mottle", relief=0.42, bend=0.05),
    "barnyard-grass": dict(aspect=1.75, point=((0.34, 0.44), (0.02, 0.08)), color=(0.66, 0.58, 0.38),
                           spread=(0.05, 0.05, 0.04), pattern="hull", relief=0.36, bend=0.05),
}
SEED_VARIANTS = 6
SEED_SPRITE = 240
SEED_FILL = 0.84  # seed length as a fraction of the sprite width; mirrored in SeedKinds.cs


def seed(kind, variant):
    """One seed seen from above on a square canvas, long axis horizontal."""
    spec = SEED_KINDS[kind]
    rng = np.random.default_rng(hash_seed(kind) + variant)
    size = SEED_SPRITE * SS
    y, x = np.mgrid[0:size, 0:size].astype(float)
    length = size * SEED_FILL * rng.uniform(0.95, 1.0)
    half_w = length / 2 / spec["aspect"] * rng.uniform(0.92, 1.08)
    u = (x - size / 2) / (length / 2)
    v = (y - size / 2) / half_w

    bend = rng.uniform(-1, 1) * spec["bend"]
    right = rng.uniform(*spec["point"][0])
    left = rng.uniform(*spec["point"][1])
    uc = np.clip(u, -1, 1)
    profile = np.sqrt(np.clip(1 - uc**2, 0, 1))
    profile *= (1 - right * np.clip(uc, 0, 1) ** 2) * (1 - left * np.clip(-uc, 0, 1) ** 2)
    profile = profile ** rng.uniform(0.85, 1.0)
    vc = v - bend * (uc**2 - 0.33)

    across = vc / np.maximum(profile, 1e-6)
    rel = np.abs(across)
    inside_px = (1 - rel) * profile * half_w
    alpha = np.clip(inside_px / SS + 0.5, 0, 1) * (np.abs(u) < 1)

    height_map = np.sqrt(np.clip(1 - np.clip(rel, 0, 1) ** 2, 0, 1)) * profile**0.5
    base = np.array(spec["color"]) + rng.uniform(-1, 1) * np.array(spec["spread"])
    color = np.broadcast_to(base, (size, size, 3)).copy()
    blotch = smooth_noise(rng, (size, size), 9 * SS) * 0.05
    grain = smooth_noise(rng, (size, size), 0.8 * SS) * 0.035
    pattern = spec["pattern"]

    if pattern == "margin":
        margin = np.clip((rel - 0.80) / 0.14, 0, 1) * (rel < 1)
        color = color * (1 - margin[..., None] * 0.10) + margin[..., None] * np.array([0.97, 0.93, 0.82]) * 0.10
        hilum = np.exp(-(((u - 0.93) / 0.05) ** 2 + (vc / 0.16) ** 2))
        color *= 1 - hilum[..., None] * np.array([0.25, 0.30, 0.40])
    elif pattern == "crease":
        crease = np.exp(-(across / 0.05) ** 2) * np.clip((0.80 - np.abs(u)) / 0.3, 0, 1)
        height_map = height_map * (1 - 0.10 * crease)
        color *= 1 - crease[..., None] * np.array([0.12, 0.14, 0.16])
        germ = np.exp(-(((u + 0.86) / 0.10) ** 2 + (across / 0.35) ** 2))
        color *= 1 - germ[..., None] * np.array([0.10, 0.16, 0.24])
        tip = np.clip((u - 0.82) / 0.18, 0, 1)
        color = color * (1 - tip[..., None] * 0.25) + tip[..., None] * np.array([0.93, 0.84, 0.66]) * 0.25
        blotch *= 1.4
    elif pattern == "ribs":
        ribs = 0.5 + 0.5 * np.cos(across * np.pi * 3.5)
        height_map = height_map * (1 - 0.12 * ribs)
        color *= (1 - 0.12 * ribs)[..., None]
    elif pattern == "mottle":
        blotch = smooth_noise(rng, (size, size), 4 * SS) * 0.16
    elif pattern == "hull":
        veins = 0.5 + 0.5 * np.cos(across * np.pi * 5.5)
        height_map = height_map * (1 - 0.06 * veins)
        color *= (1 - 0.10 * veins)[..., None]
        rim = np.clip((rel - 0.72) / 0.22, 0, 1) * (rel < 1)
        color *= 1 - rim[..., None] * np.array([0.22, 0.24, 0.26])
        blotch *= 1.6

    color = color * (1 + blotch + grain)[..., None]
    height_map = gaussian_filter(height_map, SS)
    gy, gx = np.gradient(height_map * half_w * spec["relief"])
    normal = np.dstack([-gx, -gy, np.ones_like(gx)])
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    light = np.array([-0.45, -0.55, 0.70])
    light /= np.linalg.norm(light)
    diffuse = np.clip((normal * light).sum(axis=2), 0, 1)
    half = light + np.array([0, 0, 1.0])
    half /= np.linalg.norm(half)
    spec_light = np.clip((normal * half).sum(axis=2), 0, 1) ** 28

    shade = 0.58 + 0.52 * diffuse
    rgb = color * shade[..., None] + spec_light[..., None] * 0.22
    img = Image.fromarray(
        (np.dstack([np.clip(rgb, 0, 1), alpha]) * 255).round().astype(np.uint8), "RGBA"
    ).resize((SEED_SPRITE, SEED_SPRITE), Image.LANCZOS)
    img.save(OUT / "Seeds" / f"{kind}-{variant}.png", optimize=True)


def hash_seed(text):
    return sum((i + 1) * ord(c) for i, c in enumerate(text)) * 1000


def paper(size=1024):
    """Filter paper disc texture: faint cloudy formation plus short fibres. Drawn once per dish, not tiled."""
    rng = np.random.default_rng(7)
    formation = smooth_noise(rng, (size, size), 28, wrap=True) * 0.030
    formation += smooth_noise(rng, (size, size), 7, wrap=True) * 0.018
    fibres = np.zeros((size, size))
    for _ in range(10000):
        x, y = rng.uniform(0, size, 2)
        angle = rng.uniform(0, np.pi)
        length = rng.uniform(6, 26)
        curl = rng.uniform(-0.05, 0.05)
        strength = rng.uniform(0.3, 1.0) * rng.choice([-1, 1], p=[0.35, 0.65])
        for t in np.linspace(0, length, int(length * 2)):
            a = angle + curl * t
            fibres[int(y + np.sin(a) * t) % size, int(x + np.cos(a) * t) % size] += strength
    fibres = gaussian_filter(fibres, 0.6, mode="wrap") * 0.030
    grain = gaussian_filter(rng.standard_normal((size, size)), 0.5, mode="wrap") * 0.012
    lum = 0.955 + formation + fibres + grain
    save_rgb(OUT / "paper.png", np.dstack([lum * 0.985, lum * 0.978, lum * 0.958]))


def bench(size=512):
    """Tileable bench-top grain as a neutral overlay: white where lighter, black where darker.

    The app lays it over the palette's bench colour, so one texture serves light and dark themes.
    """
    rng = np.random.default_rng(11)
    cloud = smooth_noise(rng, (size, size), 40, wrap=True) * 0.022
    cloud += smooth_noise(rng, (size, size), 10, wrap=True) * 0.014
    speck = (rng.random((size, size)) > 0.9965).astype(float) * rng.uniform(0.2, 0.6, (size, size))
    speck = gaussian_filter(speck, 0.55, mode="wrap") * 0.45
    grain = gaussian_filter(rng.standard_normal((size, size)), 0.7, mode="wrap") * 0.012
    signal = cloud + speck + grain
    rgb = np.broadcast_to((signal > 0)[..., None], (size, size, 3)).astype(float)
    save_rgba(OUT / "bench-grain.png", rgb, np.clip(np.abs(signal), 0, 1))


def icon(size=1024):
    """Dark rounded tile with a glass dish holding five seeds: the window icon, a 64 px PNG and the macOS .icns."""
    s = size * SS
    y, x = np.mgrid[0:s, 0:s].astype(float) + 0.5
    c = s / 2

    def rounded_rect(radius, inset):
        qx = np.maximum(np.abs(x - c) - (c - inset - radius), 0)
        qy = np.maximum(np.abs(y - c) - (c - inset - radius), 0)
        return np.hypot(qx, qy) - radius

    tile = np.clip(0.5 - rounded_rect(s * 0.2, s * 0.04), 0, 1)
    grad = 0.16 - 0.07 * (y / s)
    rgb = np.dstack([grad * 0.92, grad, grad * 1.04])
    alpha = tile.copy()

    d = np.hypot(x - c, y - c)
    R = s * 0.34
    shadow = np.clip(1 - np.hypot(x - c - s * 0.02, y - c - s * 0.035) / (R * 1.1), 0, 1) ** 1.5
    rgb *= (1 - 0.5 * shadow)[..., None]
    paper_r = R * 0.93
    paper_mask = np.clip(paper_r - d + 0.5, 0, 1)
    paper = 0.93 - 0.10 * np.clip((d / paper_r - 0.75) / 0.25, 0, 1)
    rgb = rgb * (1 - paper_mask[..., None]) + (paper * paper_mask)[..., None] * np.array([1, 0.995, 0.975])

    seed_img = Image.open(OUT / "Seeds" / "cucumber-0.png").convert("RGBA")
    canvas = Image.fromarray((np.dstack([np.clip(rgb, 0, 1), alpha]) * 255).round().astype(np.uint8), "RGBA")
    spots = [(-0.16, -0.14, 25), (0.15, -0.17, -40), (0.02, 0.02, 80), (-0.15, 0.16, -10), (0.17, 0.13, 50)]
    seed_len = R * 0.42
    for sx, sy, angle in spots:
        sprite = seed_img.resize((int(seed_len / SEED_FILL), int(seed_len / SEED_FILL)), Image.LANCZOS)
        sprite = sprite.rotate(angle, resample=Image.BICUBIC, expand=True)
        canvas.alpha_composite(sprite, (int(c + sx * s - sprite.width / 2), int(c + sy * s - sprite.height / 2)))

    arr = np.asarray(canvas).astype(float) / 255
    rgb, alpha = arr[..., :3], arr[..., 3]
    ring = np.clip(1 - np.abs(d - R * 0.965) / (R * 0.04), 0, 1)
    angle = np.arctan2(y - c, x - c)
    lit = 0.35 + 0.65 * np.clip(np.cos(angle + 2.3), 0, 1) ** 2
    rgb = rgb * (1 - ring[..., None] * 0.7) + (ring * lit)[..., None] * np.array([0.93, 0.96, 1.0]) * 0.7
    sprout = np.clip(1 - np.hypot(x - c - R * 0.72, y - c + R * 0.72) / (s * 0.07), 0, 1)
    sprout = np.clip(sprout * 6, 0, 1)
    rgb = rgb * (1 - sprout[..., None]) + sprout[..., None] * np.array([0.66, 0.86, 0.43])

    img = Image.fromarray((np.dstack([np.clip(rgb, 0, 1), alpha]) * 255).round().astype(np.uint8), "RGBA")
    img = img.resize((size, size), Image.LANCZOS)
    img.save(OUT / "icon.ico", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    img.resize((64, 64), Image.LANCZOS).save(OUT / "icon-64.png", optimize=True)
    MACOS.mkdir(parents=True, exist_ok=True)
    img.save(MACOS / "icon.icns")


if __name__ == "__main__":
    (OUT / "Seeds").mkdir(parents=True, exist_ok=True)
    for kind in SEED_KINDS:
        for i in range(SEED_VARIANTS):
            seed(kind, i)
    paper()
    bench()
    icon()
    print(f"Wrote textures to {OUT}")
