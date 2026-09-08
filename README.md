# Geodashy — a medieval-fantasy rhythm platformer with a serious level editor

Geodashy is a Unity 6 (URP 2D) project. The player rides a mount from the left edge of a
level to the finish gate using a single button. Different mounts turn that one button
into different moves: a horse jumps, a dragon pitches up while held, a griffin flaps,
a war boar flips gravity, a wisp flies diagonally, a siege cart charges its jump, and a
shadow cat leaps between floor and ceiling.

This first milestone is the **in-game level editor** plus a playable runtime so every
level can be tested immediately.

## Opening it

1. Open the project in Unity 6000.4 or newer.
2. Menu **Geodashy ▸ Open Level Editor Scene** (or open `Assets/Geodashy/Scenes/LevelEditor.unity`).
3. Press Play. The whole interface is built from code at runtime, so there are no prefabs to wire up.
   If the scene file ever fails to import, **Geodashy ▸ Regenerate Level Editor Scene** rebuilds it.

Everything lives under `Assets/Geodashy`. The rest of the project is the stock URP 2D template.

## The editor

Three modes, switched with `1` / `2` / `3` or the tabs in the top bar:

| Mode | What the mouse does |
| --- | --- |
| **Build** | Click places the brush object at the snapped cursor. Drag to paint. Ctrl+click selects instead. |
| **Edit** | Click selects, drag moves. Shift+click adds. Dragging on empty space box-selects. |
| **Delete** | Click deletes. Drag sweeps. Optionally only the brush type. |

The docks around the viewport:

- **Palette (bottom, Build mode)**: ten categories, text search over names and tags, and the brush transform (rotate, flip, scale, swipe).
- **Tools (bottom, Edit mode)**: nudge with step size, rotate by 90/45/5°, rotate each object in place, flip, scale, copy/cut/paste/duplicate/delete, select all/none/invert/same type/by group, align left/right/top/bottom/centre, snap selection to grid.
- **Delete (bottom, Delete mode)**: sweep options, delete selection, delete all of one type, delete all triggers, clear level.
- **View (left)**: grid snap and size (¼ ½ 1 2), grid and camera-frame guides, BPM beat lines, editor layers with a show-all toggle, zoom, a scrub slider along the level, and the playtest marker.
- **Properties (right, appears with a selection)**: position, rotation, scale, flips, Z layer (B4…T4) and Z order, editor layer, base/detail colour channel, groups (chips, add, next free, select), flags, and every type-specific property (trigger parameters, portal exit offset, blade spin, text, start-position overrides).

Top bar: undo/redo, **Play** (from the start or the right-most Start Position object),
**Play from marker**, set marker, save, the file dialog, level settings and help.
Press `F1` in the editor for the full shortcut list.

Level settings cover the starting mount/speed/gravity/size, the three parallax background
layers (theme presets or per-layer overrides with individual parallax factors), the ground
theme, all colour channels, ceiling height for flying sections, finish padding and music.

## Objects

About 190 object types are defined in `ObjectCatalog.cs`, all fantasy themed:

- **Blocks** in fifteen material families (castle stone, dark keep, oak, mossy ruins, frost, sandstone, crystal, obsidian, gilded, lava rock, marble, swamp log, cloud…), each with half, 2×1, 1×2 and 2×2 variants, plus outline, invisible and ledge blocks.
- **Slopes** at 45° and 22°.
- **Hazards**: spikes (iron, wood, ice, crystal, bone), pike walls, lances, thorns, fire pits, poison bog, spinning blades, chain maces, lightning runes, cave spikes.
- **Runes** (orbs): wind, feather, fire, gravity, storm, void, dash, dash-gravity, shadow, trigger rune.
- **Pads**: spring/puff/great shrooms, gravity lily, shadow pad.
- **Portals**: one gate per mount, gravity, five speeds, size, mirror, twin (dual), waygate (teleport).
- **Loot**: gold coin, gem, dungeon key (with item id).
- **Decor**: banners, torches, chains, skulls, barrels, crates, trees, bushes, clouds, windows, arches, pillars, crystals, rune stones, statues, vines, mushrooms, flags, portcullis, facades, cobwebs.
- **Triggers**: move, rotate, colour, alpha, toggle, spawn, pulse, shake, follow, stop, camera zoom / offset / static, background switch, ground switch, hide/show player, touch, random, song, reverse, count. Triggers can be position-, touch- or spawn-triggered, and multi-trigger.
- **Special**: start position, finish gate/banner, text, waystone.

## Play mode

Playtests run the real game loop: kinematic GD-style physics at 120 Hz, hazards with a
forgiving inner hitbox, orbs that activate while the button is held, pads, portals, gravity
flips, mini mode, mirror mode, collectibles, triggers, camera zoom/offset/shake, parallax
and a ceiling for flying mounts. Starting from the marker pre-applies every portal to the
left of it so the mount and speed are correct. Esc pauses; the pause menu restarts or
returns to the editor with the camera exactly where it was.

Not simulated yet: dual (twin) riders. The portals exist so levels can be authored for it.

## Level format

Levels are plain JSON (`LevelData` in `LevelData.cs`), saved to
`<persistentDataPath>/geodashy/levels/<id>.json`. One unit is one block, positions are
object centres, the ground is at `y = 0`. Objects carry a type id, transform, Z layer and
order, editor layer, colour channels, groups and a flat list of string properties, so the
format is easy to diff and forward compatible. Export/Import via the clipboard is in the
file dialog. A bundled tutorial level is in `Resources/Levels/tutorial.json`. Autosave
writes every minute and is restored on the next launch.

## Adding real art

Everything currently drawn is a procedural placeholder so the editor is usable today.
To replace art, drop sprites into Resources using these names (Sprite import type, 64 px per block):

- Objects: `Assets/Geodashy/Resources/Sprites/<object id>.png` (e.g. `castle_stone.png`, `rune_wind.png`).
- Mounts: `Resources/Sprites/mount_<mount id>.png` (facing right, about 96 px).
- Background layers: `Resources/Backgrounds/<layer id>.png` (must tile horizontally; 16 px per block works well).
- Ground: `Resources/Backgrounds/ground_<ground id>.png` (tiles horizontally, top edge is the surface).
- Songs: `Resources/Songs/<song id>` audio clips referenced by the Song ID setting or Song trigger.

New object types are one line in `ObjectCatalog.cs`; new mounts go in `MountCatalog.cs`;
new background layers, themes and ground themes in `ThemeCatalog.cs`.

## Code map

```
Assets/Geodashy/Scripts
  Core/        level data, JSON, catalogs (objects, mounts, themes), maths & easing
  Rendering/   raster + bitmap font, placeholder art, sprite lookup, object view, parallax, ground
  LevelEditor/ the runtime editor (camera, grid, undo, clipboard, LevelEditor.cs) and its UI/
  Gameplay/    world, player + mounts, triggers, play camera, HUD, GameRunner
Assets/Geodashy/Editor   Unity Editor menu (open/regenerate scene, levels folder, catalog dump)
```
