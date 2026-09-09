"""Writes the three campaign quests into Assets/Geodashy/Resources/Levels as LyreFlyer level JSON."""
import json, os, uuid

OUT = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Geodashy', 'Resources', 'Levels')
TUTORIAL = json.load(open(os.path.join(OUT, 'tutorial.json')))
NS = uuid.UUID('6f1c2a7e-9b3d-4c1e-8a2f-1d3e5b7c9a11')

def col(hexstr, a=1.0):
    h = hexstr.lstrip('#')
    return {"r": int(h[0:2], 16) / 255, "g": int(h[2:4], 16) / 255, "b": int(h[4:6], 16) / 255, "a": a}

Z = {'block': -1, 'deco': 2, 'text': 3, 'trig': 4}

class Level:
    def __init__(self, lid, name, desc, order, mount, theme, ground, song, bpm, sky, groundcol, ceiling=10.0, tag='normal'):
        self.d = json.loads(json.dumps(TUTORIAL))
        d = self.d
        d.update(id=lid, name=name, description=desc, author='The Realm', campaignOrder=order,
                 createdUnix=1757900000, modifiedUnix=1757900000, editorCameraX=6.0, editorCameraY=4.0, editorZoom=1.0,
                 playtestX=-1.0, playtestY=-1.0)
        s = d['settings']
        s.update(startMount=mount, startSpeed=1, backgroundTheme=theme, groundTheme=ground, songId=song, songOffset=0.0,
                 bpm=float(bpm), ceilingHeight=ceiling, backgroundColor=col(sky), groundColor=col(groundcol), difficultyTag=tag)
        d['objects'] = []
        self.uid = 1

    def obj(self, t, x, y, rot=0.0, z=None, props=None, groups=None, sx=1.0, sy=1.0, flipx=False):
        if z is None:
            z = Z['deco'] if t in DECO else (Z['text'] if t == 'text' else (Z['trig'] if t.startswith('trig_') else (Z['block'] if t in BLOCKS else 0)))
        o = {"uid": self.uid, "type": t, "x": float(x), "y": float(y), "rotation": float(rot), "scaleX": float(sx), "scaleY": float(sy),
             "flipX": flipx, "flipY": False, "zLayer": z, "zOrder": 0, "editorLayer": 0, "baseColor": 0, "detailColor": 0,
             "groups": list(groups or []), "dontFade": False, "dontEnter": False, "noTouch": False, "highDetail": False,
             "props": [{"key": k, "value": str(v)} for k, v in (props or {}).items()]}
        self.uid += 1
        self.d['objects'].append(o)
        return o

    def text(self, s, x, y, size=1):
        self.obj('text', x, y, props={'text': s, 'size': size})

    def coins(self, pts):
        for x, y in pts: self.obj('gold_coin', x, y)

    def spikes(self, x0, n, y=0.5, rot=0.0):
        for i in range(n): self.obj(SPIKE, x0 + i, y, rot)

    def write(self, fname):
        self.d['nextUid'] = self.uid
        path = os.path.join(OUT, fname)
        json.dump(self.d, open(path, 'w'), indent=1)
        open(path + '.meta', 'w').write("fileFormatVersion: 2\nguid: %s\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid5(NS, 'levels/' + fname).hex)
        xs = [o['x'] for o in self.d['objects']]
        print(fname, len(self.d['objects']), 'objects, finish at', max(xs))

BLOCKS = set()
for fam in ['castle_stone', 'castle_dark', 'cobble', 'wood_plank', 'dark_wood', 'moss_stone', 'ice_block', 'sand_block', 'crystal_block', 'obsidian', 'gold_brick', 'lava_rock', 'marble', 'swamp_log', 'cloud_block']:
    for suf in ['', '_half', '_wide', '_tall', '_big']: BLOCKS.add(fam + suf)
BLOCKS |= {'platform_thin', 'platform_wood', 'outline_block', 'outline_block_wide', 'outline_block_big', 'invisible_block'}
DECO = {'banner_red', 'banner_blue', 'banner_green', 'torch', 'chain', 'skull', 'barrel', 'crate', 'tree_oak', 'tree_pine', 'tree_dead', 'bush', 'cloud_small', 'cloud_big',
        'window', 'window_stained', 'arch', 'pillar', 'crystal_cluster', 'rune_stone', 'statue_knight', 'vine', 'mushroom_red', 'mushroom_glow', 'stalactite', 'flag_pole', 'gate_deco', 'bricks_deco', 'planks_deco', 'cobweb',
        'glow_moss', 'moss_curtain', 'stalagmite_deco', 'stalagmite_basalt', 'tower_cap_slate', 'tent_green', 'tent_red', 'fence_wood', 'hay_round', 'wildflowers_red', 'wildflowers_yellow', 'fern', 'lichen',
        'relief_gargoyle_sandstone', 'brazier', 'lantern_hanging', 'column_doric_granite', 'arch_gothic_granite', 'ivy_hang_2', 'ivy_patch_2', 'boulder_mossy', 'log', 'tree_stump', 'training_dummy', 'archery_target',
        'signpost', 'campfire', 'crystal_lamp', 'flagstone', 'roof_thatch_gable', 'window_shutters_open', 'door_oak', 'chimney_stone', 'string_lights', 'weathervane', 'boulder_pile', 'rubble', 'beehive', 'scarecrow',
        'hedge_2', 'topiary_cone', 'birdbath', 'well', 'bell_bronze', 'tombstone_cross_mossy', 'lily_pads', 'reeds', 'briar', 'wisteria', 'moss_patch', 'flagstone_sandstone', 'stalagmite_deco'}
SPIKE = 'iron_spike'

# ============================================================ Dragon's Cavern
L = Level('dragons-cavern', "Dragon's Cavern", "Learn the dragon: hold to rise, release to dive. Thread the pillars and the gargoyle's breath.",
          2, 'horse', 'cavern', 'crystal', 'darkness_falls', 120, '3a2060', '7d55c8', ceiling=10.0, tag='normal')
L.text('DRAGON\'S CAVERN', 6, 5.5, 1.2)
L.obj('crystal_cluster', 3, 0.75); L.obj('glow_moss', 9, 0.5); L.obj('stalagmite_deco', 11.5, 1)
L.spikes(12, 2); L.coins([(13, 2.8)])
L.text('HOLD TO RISE · RELEASE TO DIVE', 22, 8, 0.8)
L.obj('portal_dragon', 19, 2)
# cavern corridor: pillars from the floor and stalactites from the roof
for i, x in enumerate([28, 36, 44, 52]):
    L.obj('castle_dark_tall', x, 1); L.obj(SPIKE, x, 2.5)
    L.obj('stalactite_hazard', x + 4, 9)
    L.obj('crystal_cluster', x + 1.5, 0.75); L.obj('moss_curtain', x + 4, 9, z=2) if i % 2 == 0 else L.obj('glow_moss', x + 4, 9.5, rot=180)
    L.coins([(x + 2, 5.5), (x + 4, 3.5), (x + 6, 5.5)])
L.obj('gem', 40, 8.6)
L.obj('checkpoint', 58, 0.75)
L.text('THE GARGOYLE BREATHES', 66, 9, 0.7)
# boss gate: the gargoyle relief on a pillar spits fire into the corridor on the beat (Volley trigger, group 5)
L.obj('castle_dark_big', 64, 1); L.obj('castle_dark_big', 64, 9); L.obj('relief_gargoyle_sandstone', 64, 6, z=2)
L.obj('brazier', 62.5, 2.75); L.obj('brazier', 65.5, 2.75)
for x, y in [(69, 3.5), (72, 6.5), (75, 3.5), (78, 6.5)]:
    L.obj('fire_pit', x, y, groups=[5])
L.obj('trig_volley', 60, 8, props={'target': 5, 'count': 8, 'interval': 1.0, 'visible': 0.45, 'delay': 0.2})
L.coins([(69, 6.5), (72, 3.5), (75, 6.5), (78, 3.5)])
# narrowing tunnel: big blocks squeeze the flight path to the middle band
for x in [84, 88, 92]:
    L.obj('castle_dark_big', x, 1); L.obj('castle_dark_big', x, 9)
    L.obj(SPIKE, x, 2.5); L.obj(SPIKE, x, 7.5, 180)
L.coins([(86, 5), (90, 5), (94, 5)])
L.obj('portal_horse', 99, 2)
L.obj('stalagmite_basalt', 96, 1, z=2); L.obj('crystal_lamp', 101, 0.75)
L.spikes(104, 2); L.coins([(105, 2.8)])
L.obj('crystal_cluster', 109, 0.75)
L.obj('finish_gate', 114, 2)
L.write('dragons_cavern.json')

# ============================================================ Griffin Cliffs
L = Level('griffin-cliffs', 'Griffin Cliffs', 'Learn the griffin: tap to flap. Ride the updrafts between the cloud towers and mind the blades.',
          3, 'horse', 'sky', 'cloud', 'serenity', 100, 'a8d0f8', 'c5cdea', ceiling=11.0, tag='normal')
L.text('GRIFFIN CLIFFS', 6, 5.5, 1.2)
L.obj('cloud_small', 4, 7); L.obj('cloud_big', 10, 9); L.obj('banner_blue', 8, 1)
L.spikes(11, 2); L.coins([(12, 2.8)])
L.text('TAP TO FLAP', 20, 8, 0.8)
L.obj('portal_griffin', 17, 2)
# cloud towers with blades between them; coins mark the flight line
towers = [(26, 3), (34, 5), (42, 2), (50, 6), (58, 4)]
for x, h in towers:
    for k in range(h): L.obj('cloud_block', x, 0.5 + k)
    L.obj(SPIKE, x, h + 0.5)
    L.obj('tower_cap_slate', x, h + 1.8, z=2) if h >= 4 else L.obj('banner_blue', x, h + 1.5, z=2)
    L.coins([(x + 4, h + 3.5), (x + 4, h + 2.2)])
L.obj('saw_blade', 30, 8); L.obj('saw_blade', 46, 9); L.obj('cloud_big', 38, 10, z=2)
L.obj('gem', 54, 10)
L.obj('checkpoint', 63, 0.75)
# hanging ledge run: fly under the ledges, over the blades
for x in [70, 78, 86]:
    L.obj('cloud_block_wide', x, 8.5); L.obj(SPIKE, x - 0.5, 7.5, 180); L.obj(SPIKE, x + 0.5, 7.5, 180)
    L.obj('saw_blade_big', x + 4, 2.5)
    L.coins([(x + 4, 5.5)])
L.obj('cloud_small', 74, 10.5, z=2); L.obj('cloud_small', 90, 10.5, z=2)
L.obj('portal_horse', 96, 2)
L.spikes(101, 3); L.obj('shroom_spring', 99.5, 0.5); L.coins([(102, 4)])
L.obj('cloud_big', 108, 8, z=2); L.obj('banner_blue', 110, 1)
L.obj('finish_gate', 114, 2)
L.write('griffin_cliffs.json')

# ============================================================ The Boar Hunt
L = Level('boar-hunt', 'The Boar Hunt', 'Learn the war boar: tap on any surface to flip gravity. Charge the forest, floor and ceiling alike.',
          4, 'horse', 'forest', 'grass', 'hillside_clash', 130, '6f9a7a', '5a3a1a', ceiling=8.0, tag='hard')
L.text('THE BOAR HUNT', 6, 5.5, 1.2)
L.obj('tree_oak', 3, 2); L.obj('bush', 8, 0.5); L.obj('wildflowers_yellow', 10, 0.5); L.obj('fence_wood', 12, 0.5)
L.spikes(13, 2); L.coins([(14, 2.8)])
L.text('TAP ON A SURFACE TO FLIP', 22, 6.5, 0.7)
L.obj('portal_boar', 18, 2)
# alternate floor and ceiling hazards: every set forces a flip
pattern = [(26, 'floor'), (32, 'ceil'), (38, 'floor'), (44, 'ceil'), (50, 'floor')]
for x, side in pattern:
    if side == 'floor':
        L.spikes(x, 2); L.obj('thorns', x + 3, 0.5); L.coins([(x + 1, 7.3), (x + 3, 7.3)])
        L.obj('tree_pine', x + 5, 2, z=2)
    else:
        L.spikes(x, 2, 7.5, 180); L.obj('thorns', x + 3, 7.5, 180); L.coins([(x + 1, 0.7), (x + 3, 0.7)])
        L.obj('hay_round', x + 5, 0.5, z=2)
L.obj('checkpoint', 56, 0.75)
L.obj('tent_green', 59, 0.75); L.obj('campfire', 61.5, 0.5); L.obj('training_dummy', 63, 1)
# mossy steps on both surfaces: run up the floor steps, flip, run along the ceiling steps
for i in range(3):
    L.obj('moss_stone', 66 + i * 2, 0.5 + i); L.obj(SPIKE, 67 + i * 2, 0.5)
    L.obj('moss_stone', 74 + i * 2, 7.5 - i); L.obj(SPIKE, 75 + i * 2, 7.5, 180)
L.coins([(72, 4), (80, 4)])
L.obj('gem', 71, 6.5)
L.obj('boulder_mossy', 83, 0.5, z=2); L.obj('fern', 85, 0.5)
L.spikes(86, 3); L.spikes(91, 3, 7.5, 180); L.coins([(87, 7.3), (92, 0.7)])
L.obj('portal_horse', 97, 2)
L.obj('scarecrow', 100, 1); L.spikes(102, 2); L.coins([(103, 2.8)]); L.obj('archery_target', 106, 0.75); L.obj('tree_oak', 109, 2)
L.obj('finish_gate', 114, 2)
L.write('boar_hunt.json')
