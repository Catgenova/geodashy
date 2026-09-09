# LyreFlyer — a medieval-fantasy rhythm platformer with a serious level editor

(Working title. The code, folders and save paths still use the internal name Geodashy.)

LyreFlyer is a Unity 6 (URP 2D) project. The player rides a mount from the left edge of a
level to the finish gate using a single button. Different mounts turn that one button
into different moves: a horse jumps, a dragon pitches up while held, a griffin flaps,
a war boar flips gravity, a wisp flies diagonally, a siege cart charges its jump, and a
shadow cat leaps between floor and ceiling.

This first milestone is the **in-game level editor** plus a playable runtime so every
level can be tested immediately.

## Opening it

1. Open the project in Unity 6000.4 or newer.
2. Menu **Geodashy ▸ Open Level Editor Scene** (or open `Assets/Geodashy/Scenes/LevelEditor.unity`).
3. Press Play. You land on the main menu: **Play** opens the level select (built-in and saved
   quests with your best progress, attempts and clears, plus Play / Practice / Edit), **Level
   Editor** opens the editor. Tick *Start In Editor* on the bootstrap object to skip the menu.
   The whole interface is built from code at runtime, so there are no prefabs to wire up.
   Set the Game view to **Free Aspect** (or maximise it) so the interface fits your panel; a fixed
   1920x1080 at 1x scale in a smaller panel gets cropped and resampled. The interface size can be
   changed in the View dock (persisted between sessions).
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
layers, the ground theme, all colour channels, ceiling height for flying sections, finish
padding and music. Each parallax layer can be edited individually: pick any built-in art for
it, and set its scroll speed, height offset, size, tint, visibility and mirroring. Theme
presets fill in sensible defaults for all three. Custom images are deliberately not supported.

Soundtracks: **Import song…** in Level Settings opens a built-in file browser for mp3, ogg
and wav files. The file is copied into the level's asset folder
(`<persistentDataPath>/geodashy/assets/<level id>/`) so the level stays self-contained, and
Preview plays it from the configured offset so you can line up the BPM guides. Save As copies
the assets along with the level; deleting a level deletes them.

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

Three difficulties: **Training** (place your own waystones, auto waystones, scrub between them),
**Checkpoints** (respawn at Waystone objects the author placed; clears are recorded separately) and
**Champion** (no checkpoints; the only mode that sets the personal best and death markers). Put a
Waystone from the Special category at every midpoint you want players to fall back to.

Not simulated yet: dual (twin) riders. The portals exist so levels can be authored for it.

## Level format

Levels are plain JSON (`LevelData` in `LevelData.cs`), saved to
`<persistentDataPath>/geodashy/levels/<id>.json`. One unit is one block, positions are
object centres, the ground is at `y = 0`. Objects carry a type id, transform, Z layer and
order, editor layer, colour channels, groups and a flat list of string properties, so the
format is easy to diff and forward compatible. Export/Import via the clipboard is in the
file dialog. A bundled tutorial level is in `Resources/Levels/tutorial.json`. Autosave
writes every minute and is restored on the next launch.

## Campaign maps and starter songs

Anything in `Assets/Geodashy/Resources/Levels/*.json` ships with the game as a ★ campaign map,
ordered by the level's *Campaign order* setting. To turn a map you built into a campaign map:

1. In the editor open **Files ▸ Export as campaign map**. Inside the Unity editor this writes
   the JSON straight into `Resources/Levels` (and copies an imported song into
   `Resources/Songs`, switching the map to reference it as a starter song). In a build it writes
   to `<persistentDataPath>/geodashy/campaign-exports/` for you to copy over.
2. Commit the JSON (and any song) to git. Levels are plain text and diff cleanly.

Starter songs live in `Assets/Geodashy/Resources/Songs/`. Commit mp3/ogg/wav files there and
they appear in the *Starter song* dropdown in Level Settings and can be named by Song triggers.
Songs a player imports from disk stay outside the project, so campaign maps must use starter songs.

## Sound effects

`Assets/Geodashy/Resources/SFX/` holds a synthesized starter pack of 27 effects: per-mount jumps,
landing, death, five rune types, two pads, five portal types, coin/gem/key, gate, waystone,
completion fanfare, horn and UI click. Replace any file with real audio of the same name. Music
and effect volumes are on the main menu under Options and persist.

## Editing aids and play feel

- **Context menu**: right-click an object (long-press on touch) for copy, duplicate, delete, properties,
  select same type and save as stamp.
- **Snapping guides** (View ▸ Snap to neighbours): while dragging, edges and centres snap to nearby
  objects and pink guide lines show what matched.
- **Stamps**: select objects and choose Save as stamp (Edit dock, context menu or ⋯ menu). They appear on
  the Stamps shelf of the palette and place like a brush; stamps live in `geodashy/stamps` next to the saves.
- **Song end flag** (View dock, also in Level Settings ▸ Music): a pink banner and line mark where the
  soundtrack runs out, computed from the song length minus the offset and walked through every speed
  portal; the timeline strip shows it too. "Place banner at song end" drops a Finish Banner there so a
  quest can be sized to its music.
- **Timeline strip** under the top bar: bar lines from the BPM, trigger, portal, waystone and start markers
  along the level, the camera window, and the level length. Click to jump; drag a trigger to retime it.
- **Groups** (View dock): hide or lock any group used in the level; locked groups ignore taps.
- **History**: every change is named; the History dialog jumps to any earlier or later state.
- **Object budget**: the status readout and the View dock turn amber past 1500 objects and red past 3000,
  with a per-shelf breakdown; in play only objects near the camera are rendered.
- **Share codes** (Files dialog): the whole level as pasteable text (`LYRE1:…`, gzip + base64); import one
  on another device. Songs are not carried, only the built-in song id.
- **Rating tags**: Settings ▸ Difficulty rating (Easy to Demon) plus an automatic length tag; the level
  select filters by rating and completion and sorts by name, difficulty, length or date.
- **Play feel**: per-mount trails (embers, feathers, dirt, dust, shadow wisps, hoof dust), a camera thump on
  hard landings, an edge vignette on the beat, a death replay ghost that loops your last two seconds at the
  crash site, a faint personal-best ghost on Champion runs, an "All loot gathered" banner (recorded in the
  stats and level select), a per-level death chart with the average survival and the most frequent killer,
  and vibration on phones (Options).
- **Android song picker**: Import song uses the system document picker on Android
  (`Assets/Plugins/Android/SongPickerFragment.java`), so files from Music or Downloads work under scoped storage.

## Round three

- **Campaign map**: the title's Campaign button shows the built-in quests as castles along a road; each
  unlocks when the one before is cleared and wears your seal once cleared on Champion.
- **Three new campaign quests**: Dragon's Cavern, Griffin Cliffs and the Boar Hunt, each teaching one mount.
- **Daily Quest**: a procedural Champion run seeded by the date (`DailyQuest.cs`), the same for everyone;
  its stats are kept per day.
- **Volley trigger**: boss fire. Shows a target group again and again on an interval; put spawn-triggered
  Move triggers in the group to fling projectiles.
- **Editor**: mixed-value property fields, Replace with brush / Replace all of type, the Path tool (click
  points, lay the brush at grid or beat spacing), a colour-channel strip with select and recolour, the
  last playtest path drawn over the level, and Check quest (lint) with click-to-jump findings.
- **Play**: a mount-switch flash and pop, banners, tents, lights and bells that react to the beat and to
  the rider passing, and an end-of-run report card with medals (under par, all loot, deathless), near
  misses and a run profile.
- **Controls**: any gamepad face button, trigger or shoulder jumps and Start pauses; the keyboard jump key
  is rebindable in Options.
- **Localisation**: `L10n.T("English text")` looks up `Resources/Strings/<code>.json`; English is the key
  and the fallback. `es.json` translates the menus as an example; Options cycles languages.
- **Accessibility**: colour-blind and high-contrast edge palettes, reduce flashing and shake, larger hitbox
  overlays, and a hold-to-jump assist.
- **Performance**: an fps / objects / drawn overlay (Options) and profiler markers (LyreFlyer.Player,
  Triggers, World, Effects).
- **Captures**: pause during a run for a stamped screenshot or a looping 5-second GIF of the last moments
  (clip recording is an Options toggle). Files go to Pictures/LyreFlyer on desktop and the app's captures
  folder on phones; both carry your crest and the quest name.

## Tests

`Assets/Geodashy/Tests` holds play-mode smoke tests (Window ▸ General ▸ Test Runner): every catalog entry
renders, ids are unique, share codes and stamps round-trip, and every built-in level runs a few seconds
under an auto-jump bot without logging an error. The Android workflow runs them before building the apk.
The code is split into three assemblies (`Geodashy`, `Geodashy.Editor`, `Geodashy.Tests`).

## Building the APK

The project is set up to ship as a sideloadable Android APK (landscape only, IL2CPP, ARM64,
package id `com.catgenova.lyreflyer`). Gameplay is one touch anywhere; the editor works with
one finger for place/select/drag and two fingers for pan and pinch zoom.

**Phone layout**: the interface switches to a landscape phone layout automatically on Android
(Options ▸ Switch layout forces it on or off anywhere, which is handy for previewing in the
editor). It runs at a larger interface scale with finger-sized buttons, applies the display's
safe area, and rearranges the editor: a compact top bar (mode tabs, undo/redo, play, a ⋯ menu
for save/files/settings/marker/help), View and Props as slide-in drawers instead of fixed docks,
a ▼ button that hides the bottom dock, a 150-unit dock with a category dropdown and a sideways
scrolling object strip in Build mode and two-row button groups in Edit and Delete mode, plus a
❚❚ pause button in play. Dialogs clamp to the screen and scroll.

**In Unity**: install the Android Build Support module (with OpenJDK, SDK and NDK) through Unity
Hub for 6000.4, open the project, then run **Geodashy > Build Android APK** and pick where to save.
The build method applies the Android settings itself, so nothing needs changing in Build Profiles.
Use **Build Android APK (Development)** for a build with the profiler and script debugging.

**From a terminal** (Unity on PATH, same modules installed):

```
Unity -batchmode -nographics -quit -projectPath . -buildTarget Android \
      -executeMethod Geodashy.EditorTools.BuildScript.BuildAndroid -logFile -
```

`BUILD_OUTPUT` overrides the apk path (default `Builds/Android/LyreFlyer.apk`), `BUILD_NUMBER`
sets the Android versionCode, `DEVELOPMENT_BUILD=1` makes a development build.

**GitHub Actions**: the `Android APK` workflow (`.github/workflows/android.yml`) builds the apk on a
manual run or on any `v*` tag and uploads it as the `LyreFlyer-apk` artifact. It needs three
repository secrets for Unity activation: `UNITY_LICENSE` (the contents of a `.ulf` personal
licence file, made by running `Unity -batchmode -createManualActivationFile` and activating the
`.alf` at license.unity3d.com/manual), plus `UNITY_EMAIL` and `UNITY_PASSWORD` for the account.
The first run is slow (it pulls the Android IL2CPP editor image and populates the Library
cache); later runs reuse the cache.

**Installing**: the apk is signed with the debug key, so enable "install unknown apps" on the
phone and open the file, or `adb install -r LyreFlyer.apk`. Saved levels live in the app's
private storage. Importing songs through the file browser needs a file the app can read; on
Android 11+ that means copying it into `Android/data/com.catgenova.lyreflyer/files/geodashy/`
first, because the sandboxed file browser cannot read the shared music folders.

## Adding real art

Everything currently drawn is a procedural placeholder so the editor is usable today.
To replace art, drop sprites into Resources using these names (Sprite import type, 64 px per block):

- Objects: `Assets/Geodashy/Resources/Sprites/<object id>.png` (e.g. `castle_stone.png`, `rune_wind.png`).
- Mounts: `Resources/Sprites/mount_<mount id>.png` (facing right, about 96 px) for a still, or animated
  strips `mount_<mount id>_run_<frames>.png` and `mount_<mount id>_jump_<frames>.png`, or a single
  `mount_<mount id>_fly_<frames>.png` loop for flying mounts (horizontal, equal-width frames, feet at the
  bottom edge, drawn facing RIGHT). Every mount ships with generated sheets of this shape: horse, boar,
  siege cart and shadow cat have run (8) + jump (6) strips; dragon, griffin and wisp have a flight loop (8).
  Frames are 144 px, drawn 1.3 blocks tall over a 1x1 hitbox, and the loader anchors the lowest opaque
  row on the hitbox bottom. Drop in a strip with the same name pattern (any frame count) to replace one.
  The shipped sheets are generated by `Tools/mount_art` (see below), so they can be re-tuned in code.
- Background layers: `Resources/Backgrounds/<layer id>.png` (must tile horizontally; 16 px per block works well).
- Ground: `Resources/Backgrounds/ground_<ground id>.png` (tiles horizontally, top edge is the surface).
- Songs: `Resources/Songs/<song id>` audio clips referenced by the Song ID setting or Song trigger.

**Castle decoration shelves.** Besides the original Decor shelf, the palette has Stonework, Ivy &
Plants, Roofs & Windows, Furnishings, Lights and Yard & Siege: around 340 no-collision pieces
generated in `ObjectCatalog.Decor.cs` from material × form tables (marble, slate, granite,
sandstone, limestone, basalt, cobble, mossy stone, plaster, brick, lead, iron, oak, copper,
terracotta, thatch…) and drawn procedurally by `DecorArt.cs`: wall facades in three sizes,
columns (plain, Doric, Ionic, Corinthian, twisted, broken, pilaster), arches, merlons and
parapets, carved reliefs, tombstones and monuments, ivy in three seasons and eleven shapes,
ferns, roses, wisteria, topiary and trees, roofs in six coverings, tower caps, windows (arrow
slits to rose windows), doors, thrones, tapestries, armoury pieces, braziers and lanterns,
fences, hay, wagons and siege engines. Every piece is searchable by name or tag; a real sprite
named `Resources/Sprites/<id>.png` still overrides any of them.

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

## Regenerating the mount art

`Tools/mount_art` holds a small Python renderer (numpy only) that draws every mount's animation
sheet procedurally: `render.py` is a supersampled rasterizer (polygons, ellipses, capsules,
bezier strokes, glows), `horse.py`, `dragon.py` and `others.py` hold the actual drawings with
jointed legs, flapping wings and the seated knight. Run

```
python3 Tools/mount_art/horse.py && python3 Tools/mount_art/dragon.py && python3 Tools/mount_art/others.py
```

to rewrite the strips in `Assets/Geodashy/Resources/Sprites` (the `.meta` files stay valid).
`others.py` also accepts a subset of `griffin boar wisp cart shadowcat`.
